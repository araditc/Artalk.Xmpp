using Artalk.Xmpp.Core;
using Artalk.Xmpp.Core.Sasl;
using Artalk.Xmpp.Core.Sasl.Mechanisms;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class Sasl2FeatureTests {
	[TestMethod]
	public void ParsesSasl2AuthenticationFeature() {
		XmlElement features = Load(
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<authentication xmlns='urn:xmpp:sasl:2'>" +
			"<mechanism>SCRAM-SHA-256-PLUS</mechanism>" +
			"<mechanism>SCRAM-SHA-256</mechanism>" +
			"<inline>" +
			"<bind xmlns='urn:xmpp:bind:0'/>" +
			"<sm xmlns='urn:xmpp:sm:3'/>" +
			"</inline>" +
			"<upgrade xmlns='urn:xmpp:sasl:upgrade:0'>UPGR-SCRAM-SHA-256</upgrade>" +
			"<upgrade xmlns='urn:xmpp:sasl:upgrade:0'>UPGR-SCRAM-SHA-512</upgrade>" +
			"</authentication>" +
			"</stream:features>");

		Sasl2Feature feature = Sasl2Feature.Parse(features)!;

		CollectionAssert.AreEqual(new[] {
			"SCRAM-SHA-256-PLUS",
			"SCRAM-SHA-256"
		}, feature.Mechanisms.ToArray());
		Assert.AreEqual(2, feature.InlineFeatures.Count);
		Assert.AreEqual("bind", feature.InlineFeatures[0].LocalName);
		Assert.AreEqual("urn:xmpp:bind:0", feature.InlineFeatures[0].NamespaceURI);
		Assert.AreEqual("sm", feature.InlineFeatures[1].LocalName);
		Assert.AreEqual("urn:xmpp:sm:3", feature.InlineFeatures[1].NamespaceURI);
		CollectionAssert.AreEqual(new[] {
			"UPGR-SCRAM-SHA-256",
			"UPGR-SCRAM-SHA-512"
		}, feature.UpgradeTasks.ToArray());
	}

	[TestMethod]
	public void IgnoresLegacySaslFeature() {
		XmlElement features = Load(
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<mechanisms xmlns='urn:ietf:params:xml:ns:xmpp-sasl'>" +
			"<mechanism>PLAIN</mechanism>" +
			"</mechanisms>" +
			"</stream:features>");

		Assert.IsNull(Sasl2Feature.Parse(features));
	}

	[TestMethod]
	public void RequiresAtLeastOneMechanism() {
		XmlElement features = Load(
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<authentication xmlns='urn:xmpp:sasl:2'>" +
			"<inline><bind xmlns='urn:xmpp:bind:0'/></inline>" +
			"</authentication>" +
			"</stream:features>");

		Assert.IsNull(Sasl2Feature.Parse(features));
	}

	[TestMethod]
	public void CreatesAuthenticateWithInitialResponseAndUpgradeRequests() {
		var mechanism = new SaslPlain("user", "secret");

		XmlElement authenticate = XmppCore.CreateSasl2AuthenticateElement(
			mechanism, new[] { "UPGR-SCRAM-SHA-256" });

		Assert.AreEqual("authenticate", authenticate.LocalName);
		Assert.AreEqual(Sasl2Feature.Namespace, authenticate.NamespaceURI);
		Assert.AreEqual("PLAIN", authenticate.GetAttribute("mechanism"));
		XmlElement upgrade = authenticate["upgrade", SaslUpgradeTask.Namespace]!;
		Assert.IsNotNull(upgrade);
		Assert.AreEqual("UPGR-SCRAM-SHA-256", upgrade.InnerText);
		XmlElement initial = authenticate["initial-response", Sasl2Feature.Namespace]!;
		Assert.IsNotNull(initial);
		Assert.AreEqual("AHVzZXIAc2VjcmV0", initial.InnerText);
	}

	[TestMethod]
	public void SelectSupportedUpgradeTasksKeepsAdvertisedScramTasks() {
		IReadOnlyList<string> tasks = SaslUpgradeTask.SelectSupported(new[] {
			"UPGR-SCRAM-SHA-1",
			"UNSUPPORTED",
			"UPGR-SCRAM-SHA-512",
			"UPGR-SCRAM-SHA-256"
		}, "pencil");

		CollectionAssert.AreEqual(new[] {
			"UPGR-SCRAM-SHA-512",
			"UPGR-SCRAM-SHA-256",
			"UPGR-SCRAM-SHA-1"
		}, tasks.ToArray());
	}

	[TestMethod]
	public void SelectSupportedUpgradeTasksRequiresPassword() {
		IReadOnlyList<string> tasks = SaslUpgradeTask.SelectSupported(new[] {
			"UPGR-SCRAM-SHA-256"
		}, null!);

		Assert.AreEqual(0, tasks.Count);
	}

	[TestMethod]
	public void CreatesScramSha256UpgradeHashFromXep0480Salt() {
		XmlElement taskData = Load(
			"<task-data xmlns='urn:xmpp:sasl:2'>" +
			"<salt xmlns='urn:xmpp:scram-upgrade:0' iterations='4096'>" +
			"QV9TWENSWFE2c2VrOGJmX1o=" +
			"</salt>" +
			"</task-data>");

		XmlElement response = SaslUpgradeTask.CreateHashTaskData(
			"UPGR-SCRAM-SHA-256", "pencil", taskData);

		Assert.AreEqual("task-data", response.LocalName);
		Assert.AreEqual(Sasl2Feature.Namespace, response.NamespaceURI);
		XmlElement hash = response["hash", SaslUpgradeTask.ScramNamespace]!;
		Assert.IsNotNull(hash);
		Assert.AreEqual("Q8abK3WIX500A5++8zDamXbZWpoXgWMwdXKO9eFKk8w=",
			hash.InnerText);
	}

	[TestMethod]
	public void CreatesNextElementForUpgradeTask() {
		XmlElement next = SaslUpgradeTask.CreateNextElement("UPGR-SCRAM-SHA-256");

		Assert.AreEqual("next", next.LocalName);
		Assert.AreEqual(Sasl2Feature.Namespace, next.NamespaceURI);
		Assert.AreEqual("UPGR-SCRAM-SHA-256", next.GetAttribute("task"));
	}

	static XmlElement Load(string xml) {
		var document = new XmlDocument();
		document.LoadXml(xml);
		return document.DocumentElement!;
	}
}
