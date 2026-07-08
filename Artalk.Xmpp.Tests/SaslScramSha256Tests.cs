using Artalk.Xmpp.Core.Sasl;
using Artalk.Xmpp.Core.Sasl.Mechanisms;
using Artalk.Xmpp.Core;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class SaslScramSha256Tests {
	[TestMethod]
	public void Rfc7677ExchangeProducesExpectedMessages() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");

		byte[] initial = mechanism.GetResponse(Array.Empty<byte>());
		Assert.AreEqual("n,,n=user,r=rOprNGfwEbeRWgbNEkqO",
			Encoding.UTF8.GetString(initial));

		byte[] final = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096"));
		Assert.AreEqual(
			"c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=",
			Encoding.UTF8.GetString(final));

		byte[] serverVerification = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4="));
		Assert.IsEmpty(serverVerification);
		Assert.IsTrue(mechanism.IsCompleted);
	}

	[TestMethod]
	public void DowngradeProtectionHashMatchesXep0474Example() {
		string hash = SaslScramDowngradeProtection.Compute("SCRAM-SHA-1-PLUS",
			new[] { "SCRAM-SHA-1", "SCRAM-SHA-1-PLUS" },
			new[] { "tls-server-end-point", "tls-exporter" });

		Assert.AreEqual("G6k/rBLDqgOhRRaCuuatSDFkJ08=", hash);
	}

	[TestMethod]
	public void ScramRejectsMismatchedDowngradeProtectionHash() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		mechanism.Properties[SaslScramDowngradeProtection.PropertyName] = "bad";
		mechanism.GetResponse(Array.Empty<byte>());

		Assert.ThrowsExactly<SaslException>(() => mechanism.GetResponse(
			Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096," +
			"h=" + SaslScramDowngradeProtection.Compute("SCRAM-SHA-256",
				new[] { "SCRAM-SHA-256" }, null))));
	}

	[TestMethod]
	public void ScramAcceptsMatchingDowngradeProtectionHash() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		string hash = SaslScramDowngradeProtection.Compute("SCRAM-SHA-256",
			new[] { "SCRAM-SHA-256" }, null);
		mechanism.Properties[SaslScramDowngradeProtection.PropertyName] = hash;
		mechanism.GetResponse(Array.Empty<byte>());

		byte[] final = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096,h=" + hash));

		StringAssert.Contains(Encoding.UTF8.GetString(final), ",p=");
	}

	[TestMethod]
	public void TlsDowngradeProtectionEncodesTlsVersions() {
		Assert.AreEqual("0303",
			SaslTlsDowngradeProtection.Encode(SslProtocols.Tls12));
		Assert.AreEqual("0304",
			SaslTlsDowngradeProtection.Encode(SslProtocols.Tls13));
	}

	[TestMethod]
	public void ScramRejectsMismatchedTlsDowngradeProtectionValue() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		mechanism.Properties[SaslTlsDowngradeProtection.PropertyName] = "0304";
		mechanism.GetResponse(Array.Empty<byte>());

		Assert.ThrowsExactly<SaslException>(() => mechanism.GetResponse(
			Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096,t=0303")));
	}

	[TestMethod]
	public void ScramAcceptsMatchingTlsDowngradeProtectionValue() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		mechanism.Properties[SaslTlsDowngradeProtection.PropertyName] = "0304";
		mechanism.GetResponse(Array.Empty<byte>());

		byte[] final = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096,t=0304"));

		StringAssert.Contains(Encoding.UTF8.GetString(final), ",p=");
	}

	[TestMethod]
	public void FactoryCanCreateScramSha256Mechanism() {
		SaslMechanism mechanism = SaslFactory.Create("SCRAM-SHA-256");

		Assert.AreEqual("SCRAM-SHA-256", mechanism.Name);
		Assert.IsTrue(mechanism.HasInitial);
	}

	[TestMethod]
	public void Sha224ProducesExpectedDigest() {
		byte[] digest = Sha224.HashData(Encoding.ASCII.GetBytes("abc"));

		Assert.AreEqual(
			"23097d223405d8228642a477bda255b32aadbce4bda0b3f7e36c9da7",
			Convert.ToHexString(digest).ToLowerInvariant());
	}

	[TestMethod]
	public void FactoryCanCreateAllScramMechanisms() {
		string[] names = {
			"SCRAM-SHA3-512-PLUS",
			"SCRAM-SHA3-512",
			"SCRAM-SHA-512-PLUS",
			"SCRAM-SHA-512",
			"SCRAM-SHA-384-PLUS",
			"SCRAM-SHA-384",
			"SCRAM-SHA-256-PLUS",
			"SCRAM-SHA-256",
			"SCRAM-SHA-224-PLUS",
			"SCRAM-SHA-224",
			"SCRAM-SHA-1-PLUS",
			"SCRAM-SHA-1"
		};

		foreach (string name in names) {
			SaslMechanism mechanism = SaslFactory.Create(name);

			Assert.AreEqual(name, mechanism.Name);
			Assert.IsTrue(mechanism.HasInitial);
		}
	}

	[TestMethod]
	public void ScramPlusInitialResponseUsesChannelBindingGs2Header() {
		var mechanism = new SaslScramSha256Plus("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");

		byte[] initial = mechanism.GetResponse(Array.Empty<byte>());

		Assert.AreEqual(
			"p=tls-server-end-point,,n=user,r=rOprNGfwEbeRWgbNEkqO",
			Encoding.UTF8.GetString(initial));
	}

	[TestMethod]
	public void ScramPlusFinalResponseIncludesChannelBindingData() {
		var mechanism = new SaslScramSha256Plus("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		byte[] channelBinding = { 1, 2, 3, 4 };
		mechanism.Properties["ChannelBindingData"] = channelBinding;
		mechanism.GetResponse(Array.Empty<byte>());

		byte[] final = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096"));

		string cbind = Convert.ToBase64String(Encoding.UTF8.GetBytes(
			"p=tls-server-end-point,,").Concat(channelBinding).ToArray());
		StringAssert.StartsWith(Encoding.UTF8.GetString(final),
			"c=" + cbind + ",r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,");
	}

	[TestMethod]
	public void ScramPlusFinalResponseRequiresChannelBindingData() {
		var mechanism = new SaslScramSha256Plus("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");
		mechanism.GetResponse(Array.Empty<byte>());

		Assert.ThrowsExactly<SaslException>(() => mechanism.GetResponse(
			Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096")));
	}

	[TestMethod]
	public void TlsServerEndPointUsesCertificateSignatureHash() {
		using RSA rsa = RSA.Create(2048);
		var request = new CertificateRequest("CN=example.com", rsa,
			HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
		using X509Certificate2 certificate = request.CreateSelfSigned(
			DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

		byte[] channelBinding =
			ChannelBinding.CreateTlsServerEndPoint(certificate);

		CollectionAssert.AreEqual(SHA384.HashData(certificate.RawData),
			channelBinding);
	}

	[TestMethod]
	public void SelectMechanismUsesScramPlusWhenXep0440AdvertisesTlsServerEndPoint() {
		var core = CreateCoreWithChannelBinding();

		string mechanism = SelectMechanism(core,
			new[] { "SCRAM-SHA-256", "SCRAM-SHA-256-PLUS" },
			new[] { ChannelBinding.TlsServerEndPoint });

		Assert.AreEqual("SCRAM-SHA-256-PLUS", mechanism);
	}

	[TestMethod]
	public void SelectMechanismFallsBackWhenXep0440AdvertisesUnsupportedBinding() {
		var core = CreateCoreWithChannelBinding();

		string mechanism = SelectMechanism(core,
			new[] { "SCRAM-SHA-256", "SCRAM-SHA-256-PLUS" },
			new[] { "tls-exporter" });

		Assert.AreEqual("SCRAM-SHA-256", mechanism);
	}

	[TestMethod]
	public void SelectMechanismRejectsChannelBindingTypesWithoutPlusMechanisms() {
		var core = CreateCoreWithChannelBinding();

		Assert.ThrowsExactly<TargetInvocationException>(() => SelectMechanism(core,
			new[] { "SCRAM-SHA-256" },
			new[] { ChannelBinding.TlsServerEndPoint }));
	}

	[TestMethod]
	public void ChannelBindingTypesParseFromStreamFeatures() {
		var document = new XmlDocument();
		document.LoadXml(
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<sasl-channel-binding xmlns='urn:xmpp:sasl-cb:0'>" +
			"<channel-binding type='tls-server-end-point'/>" +
			"<channel-binding type='tls-exporter'/>" +
			"</sasl-channel-binding>" +
			"</stream:features>");
		MethodInfo method = typeof(XmppCore).GetMethod("GetChannelBindingTypes",
			BindingFlags.Static | BindingFlags.NonPublic)!;

		var types = (HashSet<string>) method.Invoke(null,
			new object[] { document.DocumentElement! })!;

		CollectionAssert.AreEquivalent(new[] {
			ChannelBinding.TlsServerEndPoint,
			"tls-exporter"
		}, types.ToArray());
	}

	static XmppCore CreateCoreWithChannelBinding() {
		var core = new XmppCore("example.com", "user", "password");
		FieldInfo field = typeof(XmppCore).GetField("tlsServerEndPointChannelBinding",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		field.SetValue(core, new byte[] { 1, 2, 3, 4 });
		return core;
	}

	static string SelectMechanism(XmppCore core, string[] mechanisms,
		string[] channelBindingTypes) {
		MethodInfo method = typeof(XmppCore).GetMethod("SelectMechanism",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (string) method.Invoke(core,
			new object[] { mechanisms, channelBindingTypes })!;
	}
}
