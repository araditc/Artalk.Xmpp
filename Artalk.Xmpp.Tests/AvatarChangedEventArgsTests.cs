using Artalk.Xmpp;
using Artalk.Xmpp.Extensions;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class AvatarChangedEventArgsTests {
	[TestMethod]
	public void ClearedIsTrueWhenAvatarDataIsMissing() {
		var args = new AvatarChangedEventArgs(new Jid("user@example.com"));

		Assert.IsTrue(args.Cleared);
		Assert.IsNull(args.AvatarData);
	}

	[TestMethod]
	public void CarriesAvatarBytesAndMimeType() {
		byte[] payload = [1, 2, 3];

		var args = new AvatarChangedEventArgs(new Jid("user@example.com"),
			"abc123", payload, "image/png");

		Assert.IsFalse(args.Cleared);
		CollectionAssert.AreEqual(payload, args.AvatarData);
		Assert.AreEqual("abc123", args.Hash);
		Assert.AreEqual("image/png", args.MimeType);
	}
}
