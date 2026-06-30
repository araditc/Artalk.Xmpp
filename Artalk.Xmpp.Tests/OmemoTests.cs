using Artalk.Xmpp;
using Artalk.Xmpp.Extensions;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class OmemoTests {
	[TestMethod]
	public void DeviceListSerializesDistinctSortedDeviceIds() {
		var deviceList = new OmemoDeviceList(new uint[] { 77, 42, 77 });

		XmlElement element = deviceList.ToXmlElement();

		Assert.AreEqual("list", element.LocalName);
		Assert.AreEqual(OmemoDeviceList.Namespace, element.NamespaceURI);
		Assert.HasCount(2, element.GetElementsByTagName("device"));
		Assert.AreEqual("42", ((XmlElement) element.ChildNodes[0]!).GetAttribute("id"));
		Assert.AreEqual("77", ((XmlElement) element.ChildNodes[1]!).GetAttribute("id"));
	}

	[TestMethod]
	public void DeviceListParsesPayload() {
		XmlElement element = LoadElement(
			"<list xmlns='eu.siacs.conversations.axolotl'>" +
			"<device id='10'/><device id='20'/></list>");

		OmemoDeviceList deviceList = OmemoDeviceList.Parse(element);

		CollectionAssert.AreEqual(new uint[] { 10, 20 }, deviceList.DeviceIds.ToArray());
	}

	[TestMethod]
	public void DeviceListRejectsInvalidDeviceId() {
		XmlElement element = LoadElement(
			"<list xmlns='eu.siacs.conversations.axolotl'><device id='abc'/></list>");

		Assert.ThrowsExactly<XmppException>(() => OmemoDeviceList.Parse(element));
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
		Assert.AreEqual("eu.siacs.conversations.axolotl.bundles:123",
			Omemo.GetBundleNode(123));
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
}
