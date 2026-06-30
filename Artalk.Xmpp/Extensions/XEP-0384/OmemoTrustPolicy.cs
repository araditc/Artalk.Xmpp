namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Defines how OMEMO encryption handles undecided device identities.
	/// </summary>
	public enum OmemoTrustPolicy {
		/// <summary>
		/// Send only to explicitly trusted identities.
		/// </summary>
		RequireTrusted,
		/// <summary>
		/// Send to trusted and undecided identities, but never to untrusted ones.
		/// </summary>
		AllowUndecided,
		/// <summary>
		/// Trust undecided identities on first use and send to them.
		/// </summary>
		TrustOnFirstUse
	}
}
