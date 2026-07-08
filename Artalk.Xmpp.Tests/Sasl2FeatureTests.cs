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
	public void CreatesAuthenticateWithInitialResponse() {
		var mechanism = new SaslPlain("user", "secret");

		XmlElement authenticate = XmppCore.CreateSasl2AuthenticateElement(mechanism);

		Assert.AreEqual("authenticate", authenticate.LocalName);
		Assert.AreEqual(Sasl2Feature.Namespace, authenticate.NamespaceURI);
		Assert.AreEqual("PLAIN", authenticate.GetAttribute("mechanism"));
		XmlElement initial = authenticate["initial-response", Sasl2Feature.Namespace]!;
		Assert.IsNotNull(initial);
		Assert.AreEqual("AHVzZXIAc2VjcmV0", initial.InnerText);
	}

	static XmlElement Load(string xml) {
		var document = new XmlDocument();
		document.LoadXml(xml);
		return document.DocumentElement!;
	}
}
