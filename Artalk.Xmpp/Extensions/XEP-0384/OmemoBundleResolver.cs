namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Resolves the current OMEMO bundle for an XMPP account device.
	/// </summary>
	public delegate OmemoBundle OmemoBundleResolver(Jid jid, uint deviceId);
}
