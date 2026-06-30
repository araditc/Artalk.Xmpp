namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Stores trust decisions for OMEMO device identities.
	/// </summary>
	public interface IOmemoTrustStore {
		/// <summary>
		/// Gets the trust level for the specified device identity.
		/// </summary>
		OmemoTrustLevel GetTrust(Jid jid, uint deviceId, byte[] identityKey);

		/// <summary>
		/// Stores the trust level for the specified device identity.
		/// </summary>
		void SetTrust(Jid jid, uint deviceId, byte[] identityKey,
			OmemoTrustLevel trustLevel);
	}
}
