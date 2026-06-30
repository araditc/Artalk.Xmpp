using Artalk.Xmpp;
using Artalk.Xmpp.Extensions;
using Artalk.Xmpp.Im;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class OmemoTests {
	[TestMethod]
	public void DeviceListSerializesDistinctSortedDeviceIds() {
		var deviceList = new OmemoDeviceList(new uint[] { 77, 42, 77 });

		XmlElement element = deviceList.ToXmlElement();

		Assert.AreEqual("devices", element.LocalName);
		Assert.AreEqual(OmemoDeviceList.Namespace, element.NamespaceURI);
		Assert.HasCount(2, element.GetElementsByTagName("device"));
		Assert.AreEqual("42", ((XmlElement) element.ChildNodes[0]!).GetAttribute("id"));
		Assert.AreEqual("77", ((XmlElement) element.ChildNodes[1]!).GetAttribute("id"));
	}

	[TestMethod]
	public void DeviceListParsesPayload() {
		XmlElement element = LoadElement(
			"<devices xmlns='urn:xmpp:omemo:2'>" +
			"<device id='10' label='Phone' labelsig='AQID'/>" +
			"<device id='20'/></devices>");

		OmemoDeviceList deviceList = OmemoDeviceList.Parse(element);

		CollectionAssert.AreEqual(new uint[] { 10, 20 }, deviceList.DeviceIds.ToArray());
		Assert.AreEqual("Phone", deviceList.Devices[0].Label);
		CollectionAssert.AreEqual(new byte[] { 1, 2, 3 },
			deviceList.Devices[0].LabelSignature);
	}

	[TestMethod]
	public void DeviceListRejectsInvalidDeviceId() {
		XmlElement element = LoadElement(
			"<devices xmlns='urn:xmpp:omemo:2'><device id='abc'/></devices>");

		Assert.ThrowsExactly<XmppException>(() => OmemoDeviceList.Parse(element));
	}

	[TestMethod]
	public void DeviceListParsesLegacyPayload() {
		XmlElement element = LoadElement(
			"<list xmlns='eu.siacs.conversations.axolotl'>" +
			"<device id='10'/><device id='20'/></list>");

		OmemoDeviceList deviceList = OmemoDeviceList.Parse(element);

		CollectionAssert.AreEqual(new uint[] { 10, 20 }, deviceList.DeviceIds.ToArray());
	}

	[TestMethod]
	public void BundleSerializesAndParsesPayload() {
		var bundle = new OmemoBundle(2,
			new byte[] { 1, 2, 3 },
			new byte[] { 4, 5, 6 },
			new byte[] { 7, 8, 9 },
			new Dictionary<uint, byte[]> {
				{ 9, new byte[] { 10, 11 } },
				{ 3, new byte[] { 12, 13 } }
			});

		OmemoBundle parsed = OmemoBundle.Parse(bundle.ToXmlElement());

		Assert.AreEqual(2u, parsed.SignedPreKeyId);
		CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, parsed.SignedPreKey);
		CollectionAssert.AreEqual(new byte[] { 4, 5, 6 },
			parsed.SignedPreKeySignature);
		CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, parsed.IdentityKey);
		CollectionAssert.AreEqual(new uint[] { 3, 9 }, parsed.PreKeys.Keys.ToArray());
		CollectionAssert.AreEqual(new byte[] { 12, 13 }, parsed.PreKeys[3]);
		CollectionAssert.AreEqual(new byte[] { 10, 11 }, parsed.PreKeys[9]);
	}

	[TestMethod]
	public void BundleClonesMutableInputAndOutput() {
		byte[] signedPreKey = { 1 };
		byte[] signature = { 2 };
		byte[] identity = { 3 };
		var preKeys = new Dictionary<uint, byte[]> {
			{ 4, new byte[] { 5 } }
		};
		var bundle = new OmemoBundle(9, signedPreKey, signature, identity, preKeys);

		signedPreKey[0] = 99;
		signature[0] = 99;
		identity[0] = 99;
		preKeys[4][0] = 99;
		byte[] output = bundle.SignedPreKey;
		output[0] = 88;
		byte[] preKeyOutput = bundle.PreKeys[4];
		preKeyOutput[0] = 88;

		CollectionAssert.AreEqual(new byte[] { 1 }, bundle.SignedPreKey);
		CollectionAssert.AreEqual(new byte[] { 2 }, bundle.SignedPreKeySignature);
		CollectionAssert.AreEqual(new byte[] { 3 }, bundle.IdentityKey);
		CollectionAssert.AreEqual(new byte[] { 5 }, bundle.PreKeys[4]);
	}

	[TestMethod]
	public void BundleRequiresAtLeastOnePreKey() {
		Assert.ThrowsExactly<ArgumentException>(() => new OmemoBundle(1,
			new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 },
			new Dictionary<uint, byte[]>()));
	}

	[TestMethod]
	public void BundleNodeUsesOmemoDeviceId() {
		Assert.AreEqual("urn:xmpp:omemo:2:bundles",
			Omemo.GetBundleNode(123));
	}

	[TestMethod]
	public void PayloadEncryptsAuthenticatesAndDecrypts() {
		byte[] key = Enumerable.Range(1, 32).Select(i => (byte) i).ToArray();
		byte[] plaintext = Encoding.UTF8.GetBytes("secret message");

		OmemoPayload payload = OmemoPayload.Encrypt(plaintext, key);
		byte[] decrypted = payload.Decrypt();

		CollectionAssert.AreEqual(plaintext, decrypted);
		Assert.HasCount(48, payload.KeyMaterial);
		CollectionAssert.AreEqual(key, payload.Key);
	}

	[TestMethod]
	public void PayloadRejectsTamperedCiphertext() {
		byte[] key = Enumerable.Range(1, 32).Select(i => (byte) i).ToArray();
		OmemoPayload payload = OmemoPayload.Encrypt(
			Encoding.UTF8.GetBytes("secret message"), key);
		byte[] ciphertext = payload.Ciphertext;
		ciphertext[0] ^= 0xff;
		var tampered = new OmemoPayload(payload.Key,
			payload.AuthenticationTag, ciphertext);

		Assert.ThrowsExactly<CryptographicException>(() => tampered.Decrypt());
	}

	[TestMethod]
	public void PayloadCanBeRecreatedFromKeyMaterial() {
		byte[] key = Enumerable.Range(1, 32).Select(i => (byte) i).ToArray();
		OmemoPayload payload = OmemoPayload.Encrypt(
			Encoding.UTF8.GetBytes("secret message"), key);

		OmemoPayload recreated = OmemoPayload.FromKeyMaterial(payload.KeyMaterial,
			payload.Ciphertext);

		CollectionAssert.AreEqual(payload.Key, recreated.Key);
		CollectionAssert.AreEqual(payload.AuthenticationTag,
			recreated.AuthenticationTag);
		CollectionAssert.AreEqual(payload.Decrypt(), recreated.Decrypt());
	}

	[TestMethod]
	public void EncryptedMessageSerializesAndParsesEnvelope() {
		var encryptedMessage = new OmemoEncryptedMessage(44,
			new[] {
				new OmemoRecipientKey("juliet@example.com/balcony", 10,
					new byte[] { 1, 2, 3 }, keyExchange: true),
				new OmemoRecipientKey("juliet@example.com", 11,
					new byte[] { 4, 5, 6 }),
				new OmemoRecipientKey("romeo@example.com", 12,
					new byte[] { 7, 8, 9 })
			},
			new byte[] { 10, 11, 12 });

		OmemoEncryptedMessage parsed =
			OmemoEncryptedMessage.Parse(encryptedMessage.ToXmlElement());

		Assert.AreEqual(44u, parsed.SenderDeviceId);
		Assert.HasCount(3, parsed.Keys);
		Assert.HasCount(2, parsed.GetKeysFor("juliet@example.com").ToList());
		Assert.IsTrue(parsed.GetKeysFor("juliet@example.com").First().KeyExchange);
		CollectionAssert.AreEqual(new byte[] { 10, 11, 12 }, parsed.Payload);
	}

	[TestMethod]
	public void EncryptedMessageTryParseReadsMessageStanza() {
		var message = new Message("juliet@example.com", type: MessageType.Chat);
		var encryptedMessage = new OmemoEncryptedMessage(44,
			new[] {
				new OmemoRecipientKey("juliet@example.com", 10,
					new byte[] { 1, 2, 3 })
			},
			new byte[] { 4, 5, 6 });
		message.Data.AppendChild(message.Data.OwnerDocument.ImportNode(
			encryptedMessage.ToXmlElement(), true));

		bool parsed = OmemoEncryptedMessage.TryParse(message, out var result);

		Assert.IsTrue(parsed);
		Assert.AreEqual(44u, result.SenderDeviceId);
		CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, result.Payload);
	}

	[TestMethod]
	public void MemoryTrustStoreTreatsChangedIdentityAsUntrusted() {
		var store = new MemoryOmemoTrustStore();
		var jid = new Jid("juliet@example.com");

		store.SetTrust(jid, 9, new byte[] { 1, 2, 3 }, OmemoTrustLevel.Trusted);

		Assert.AreEqual(OmemoTrustLevel.Trusted,
			store.GetTrust(jid, 9, new byte[] { 1, 2, 3 }));
		Assert.AreEqual(OmemoTrustLevel.Untrusted,
			store.GetTrust(jid, 9, new byte[] { 1, 2, 4 }));
	}

	[TestMethod]
	public void SessionManagerRequiresTrustedDevicesByDefault() {
		var manager = CreateSessionManager(new MemoryOmemoTrustStore());

		Assert.ThrowsExactly<InvalidOperationException>(() => manager.Encrypt(
			"romeo@example.com", 1, new[] { new Jid("juliet@example.com") },
			Encoding.UTF8.GetBytes("hello")));
	}

	[TestMethod]
	public void SessionManagerTrustOnFirstUseStoresIdentityAndEncrypts() {
		var trustStore = new MemoryOmemoTrustStore();
		var cipher = new FakeOmemoSessionCipher();
		var manager = CreateSessionManager(trustStore, cipher);
		manager.TrustPolicy = OmemoTrustPolicy.TrustOnFirstUse;

		OmemoEncryptedMessage encrypted = manager.Encrypt("romeo@example.com", 1,
			new[] { new Jid("juliet@example.com") }, Encoding.UTF8.GetBytes("hello"));

		Assert.HasCount(2, encrypted.Keys);
		Assert.AreEqual(OmemoTrustLevel.Trusted,
			trustStore.GetTrust("juliet@example.com", 7, Identity(7)));
		Assert.AreEqual(OmemoTrustLevel.Trusted,
			trustStore.GetTrust("romeo@example.com", 2, Identity(2)));
		CollectionAssert.AreEquivalent(new uint[] { 2, 7 },
			cipher.EncryptedDeviceIds.ToArray());
	}

	[TestMethod]
	public void SessionManagerSkipsLocalSendingDevice() {
		var trustStore = new MemoryOmemoTrustStore();
		trustStore.SetTrust("romeo@example.com", 2, Identity(2),
			OmemoTrustLevel.Trusted);
		trustStore.SetTrust("juliet@example.com", 7, Identity(7),
			OmemoTrustLevel.Trusted);
		var manager = CreateSessionManager(trustStore);

		OmemoEncryptedMessage encrypted = manager.Encrypt("romeo@example.com", 1,
			new[] { new Jid("juliet@example.com") }, Encoding.UTF8.GetBytes("hello"));

		CollectionAssert.AreEquivalent(new uint[] { 2, 7 },
			encrypted.Keys.Select(k => k.RecipientDeviceId).ToArray());
	}

	[TestMethod]
	public void SessionManagerDecryptsMatchingRecipientKey() {
		var cipher = new FakeOmemoSessionCipher();
		byte[] plaintext = Encoding.UTF8.GetBytes("hello secure world");
		OmemoPayload payload = OmemoPayload.Encrypt(plaintext,
			Enumerable.Range(1, 32).Select(i => (byte) i).ToArray());
		var encrypted = new OmemoEncryptedMessage(7,
			new[] {
				new OmemoRecipientKey("romeo@example.com", 2,
					cipher.Wrap(payload.KeyMaterial, 2))
			},
			payload.Ciphertext);
		var manager = CreateSessionManager(sessionCipher: cipher);

		byte[] decrypted = manager.Decrypt("juliet@example.com", "romeo@example.com",
			2, encrypted);

		CollectionAssert.AreEqual(plaintext, decrypted);
	}

	[TestMethod]
	public void DeviceListChangedEventArgsRequiresValues() {
		var jid = new Jid("juliet@example.com");
		var deviceList = new OmemoDeviceList(new uint[] { 1 });

		var args = new OmemoDeviceListChangedEventArgs(jid, deviceList);

		Assert.AreEqual(jid, args.Jid);
		Assert.AreEqual(deviceList, args.DeviceList);
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new OmemoDeviceListChangedEventArgs(null!, deviceList));
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new OmemoDeviceListChangedEventArgs(jid, null!));
	}

	static XmlElement LoadElement(string xml) {
		var document = new XmlDocument();
		document.LoadXml(xml);
		return document.DocumentElement!;
	}

	static OmemoSessionManager CreateSessionManager(IOmemoTrustStore? trustStore = null,
		FakeOmemoSessionCipher? sessionCipher = null) {
		sessionCipher ??= new FakeOmemoSessionCipher();
		return new OmemoSessionManager(GetDeviceList, GetBundle, sessionCipher,
			trustStore);
	}

	static OmemoDeviceList GetDeviceList(Jid jid) {
		return jid.GetBareJid().ToString() switch {
			"romeo@example.com" => new OmemoDeviceList(new uint[] { 1, 2 }),
			"juliet@example.com" => new OmemoDeviceList(new uint[] { 7 }),
			_ => new OmemoDeviceList(Array.Empty<uint>())
		};
	}

	static OmemoBundle GetBundle(Jid jid, uint deviceId) {
		return new OmemoBundle(1,
			new byte[] { 1, 2, 3 },
			new byte[] { 4, 5, 6 },
			Identity(deviceId),
			new Dictionary<uint, byte[]> {
				{ 1, new byte[] { 7, 8, 9 } }
			});
	}

	static byte[] Identity(uint deviceId) {
		return new[] { (byte) deviceId, (byte) (deviceId + 1) };
	}

	sealed class FakeOmemoSessionCipher : IOmemoSessionCipher {
		public List<uint> EncryptedDeviceIds {
			get;
		} = new List<uint>();

		public OmemoRecipientKey EncryptKeyMaterial(Jid recipientJid,
			uint recipientDeviceId, OmemoBundle recipientBundle, byte[] keyMaterial) {
			EncryptedDeviceIds.Add(recipientDeviceId);
			return new OmemoRecipientKey(recipientJid, recipientDeviceId,
				Wrap(keyMaterial, recipientDeviceId));
		}

		public byte[] DecryptKeyMaterial(Jid senderJid, uint senderDeviceId,
			OmemoRecipientKey recipientKey) {
			return recipientKey.EncryptedKeyMaterial.Skip(4).ToArray();
		}

		public byte[] Wrap(byte[] keyMaterial, uint deviceId) {
			byte[] output = new byte[keyMaterial.Length + 4];
			byte[] prefix = BitConverter.GetBytes(deviceId);
			Buffer.BlockCopy(prefix, 0, output, 0, prefix.Length);
			Buffer.BlockCopy(keyMaterial, 0, output, prefix.Length,
				keyMaterial.Length);
			return output;
		}
	}
}
