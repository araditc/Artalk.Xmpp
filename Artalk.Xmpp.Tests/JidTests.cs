using Artalk.Xmpp;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class JidTests {
	[TestMethod]
	public void ParsesFullJid() {
		var jid = new Jid("user@example.com/resource");

		Assert.AreEqual("user", jid.Node);
		Assert.AreEqual("example.com", jid.Domain);
		Assert.AreEqual("resource", jid.Resource);
		Assert.IsTrue(jid.IsFullJid);
		Assert.AreEqual("user@example.com/resource", jid.ToString());
	}

	[TestMethod]
	public void RejectsMalformedJidWithExtraAtSign() {
		Assert.ThrowsExactly<ArgumentException>(() => new Jid("user@@example.com"));
	}

	[TestMethod]
	public void BareJidDropsResource() {
		var jid = new Jid("user@example.com/resource");

		Assert.AreEqual("user@example.com", jid.GetBareJid().ToString());
	}
}
