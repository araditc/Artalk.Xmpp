namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Describes the trust state of an OMEMO device identity.
	/// </summary>
	public enum OmemoTrustLevel {
		/// <summary>
		/// No explicit trust decision has been recorded.
		/// </summary>
		Undecided,
		/// <summary>
		/// The device identity is trusted for sending encrypted content.
		/// </summary>
		Trusted,
		/// <summary>
		/// The device identity must not receive encrypted content.
		/// </summary>
		Untrusted
	}
}
