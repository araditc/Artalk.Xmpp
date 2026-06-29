using Artalk.Xmpp.Client;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class KeepAliveTests {
	[TestMethod]
	public void KeepAliveIntervalAllowsZeroToDisable() {
		using var client = new ArtalkXmppClient("example.com", "user", "password");

		client.KeepAliveInterval = TimeSpan.Zero;

		Assert.AreEqual(TimeSpan.Zero, client.KeepAliveInterval);
	}

	[TestMethod]
	public void KeepAliveIntervalRejectsNegativeValue() {
		using var client = new ArtalkXmppClient("example.com", "user", "password");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
			client.KeepAliveInterval = TimeSpan.FromSeconds(-1));
	}

	[TestMethod]
	public void KeepAliveTimeoutRejectsNonPositiveValue() {
		using var client = new ArtalkXmppClient("example.com", "user", "password");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
			client.KeepAliveTimeout = TimeSpan.Zero);
	}

	[TestMethod]
	public void KeepAliveFailedEventArgsRequiresException() {
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new KeepAliveFailedEventArgs(null!));
	}
}
