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

	[TestMethod]
	public void NormalizesLocalpartAndDomainpartForRfc7622() {
		var jid = new Jid("USER@XN--BCHER-KVA.Example./Resource");

		Assert.AreEqual("user", jid.Node);
		Assert.AreEqual("bücher.example", jid.Domain);
		Assert.AreEqual("Resource", jid.Resource);
		Assert.AreEqual("user@bücher.example/Resource", jid.ToString());
	}

	[TestMethod]
	public void SupportsDomainOnlyBareAndFullJids() {
		var bare = new Jid("conference.example.com");
		var full = new Jid("conference.example.com/room@host");

		Assert.IsTrue(bare.IsBareJid);
		Assert.IsFalse(bare.IsFullJid);
		Assert.IsFalse(full.IsBareJid);
		Assert.IsTrue(full.IsFullJid);
		Assert.IsNull(full.Node);
		Assert.AreEqual("room@host", full.Resource);
	}

	[TestMethod]
	public void KeepsSlashInsideResourcepart() {
		var jid = new Jid("room@conference.example.com/foo/bar");

		Assert.AreEqual("foo/bar", jid.Resource);
		Assert.AreEqual("room@conference.example.com/foo/bar", jid.ToString());
	}

	[TestMethod]
	public void MapsNonAsciiSpaceInResourcepart() {
		var jid = new Jid("user@example.com/resource\u1680name");

		Assert.AreEqual("resource name", jid.Resource);
	}

	[TestMethod]
	public void AllowsValidSupplementaryUnicodeInResourcepart() {
		var jid = new Jid("user@example.com/device-\U0001F600");

		Assert.AreEqual("device-\U0001F600", jid.Resource);
	}

	[TestMethod]
	[DataRow("bad:name@example.com")]
	[DataRow("bad@example.com/")]
	[DataRow("user@@example.com")]
	[DataRow("user@example.com\u0001/resource")]
	public void RejectsInvalidRfc7622Jids(string jid) {
		Assert.ThrowsExactly<ArgumentException>(() => new Jid(jid));
	}

	[TestMethod]
	public void RejectsPartLongerThan1023Utf8Octets() {
		string node = new string('a', 1024);

		Assert.ThrowsExactly<ArgumentException>(() => new Jid(node + "@example.com"));
	}
}
