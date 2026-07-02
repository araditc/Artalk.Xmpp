namespace Artalk.Xmpp.Core {
	/// <summary>
	/// Identifies URI-based XMPP transport bindings.
	/// </summary>
	public enum XmppTransportBinding {
		/// <summary>
		/// XMPP over BOSH as defined by XEP-0206.
		/// </summary>
		Bosh,
		/// <summary>
		/// XMPP over WebSocket as defined by RFC 7395.
		/// </summary>
		WebSocket
	}
}
