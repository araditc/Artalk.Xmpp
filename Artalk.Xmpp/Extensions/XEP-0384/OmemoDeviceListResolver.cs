namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Resolves the current OMEMO device list for an XMPP account.
	/// </summary>
	public delegate OmemoDeviceList OmemoDeviceListResolver(Jid jid);
}
