using Artalk.Xmpp.Core;
using Artalk.Xmpp.Core.Sasl;
using Artalk.Xmpp.Core.Sasl.Mechanisms;
using System.Reflection;
using System.Text;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class SaslOAuthBearerTests {
	[TestMethod]
	public void InitialResponseContainsBearerTokenAndConnectionMetadata() {
		var mechanism = new SaslOAuthBearer("user@example.com", "token-value",
			"example.com", 5222);

		string response = Encoding.UTF8.GetString(
			mechanism.GetResponse(Array.Empty<byte>()));

		Assert.AreEqual("n,a=user@example.com," + "\x01" + "host=example.com" +
			"\x01" + "port=5222" + "\x01" + "auth=Bearer token-value" +
			"\x01" + "\x01", response);
		Assert.IsTrue(mechanism.IsCompleted);
	}

	[TestMethod]
	public void ErrorChallengeAcknowledgementReturnsSeparator() {
		var mechanism = new SaslOAuthBearer("user@example.com", "token-value",
			"example.com");
		mechanism.GetResponse(Array.Empty<byte>());

		byte[] response = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"{\"status\":\"invalid_token\"}"));

		CollectionAssert.AreEqual(new byte[] { 1 }, response);
	}

	[TestMethod]
	public void FactoryCanCreateOAuthBearerMechanism() {
		SaslMechanism mechanism = SaslFactory.Create("OAUTHBEARER");

		Assert.AreEqual("OAUTHBEARER", mechanism.Name);
		Assert.IsTrue(mechanism.HasInitial);
	}

	[TestMethod]
	public void SelectMechanismPrefersOAuthBearerWhenTokenIsSet() {
		var core = new XmppCore("example.com") {
			OAuthBearerToken = "token-value"
		};

		string mechanism = SelectMechanism(core, "PLAIN", "OAUTHBEARER");

		Assert.AreEqual("OAUTHBEARER", mechanism);
	}

	[TestMethod]
	public void SelectMechanismDoesNotUsePasswordMechanismsWithoutPassword() {
		var core = new XmppCore("example.com");

		Assert.ThrowsExactly<TargetInvocationException>(() =>
			SelectMechanism(core, "PLAIN"));
	}

	static string SelectMechanism(XmppCore core, params string[] mechanisms) {
		MethodInfo method = typeof(XmppCore).GetMethod("SelectMechanism",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (string) method.Invoke(core, new object[] { mechanisms })!;
	}
}
