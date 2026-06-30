namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Encrypts and decrypts OMEMO payload key material using a per-device session.
	/// </summary>
	/// <remarks>
	/// Implementations normally adapt an OMEMO-compatible X3DH and Double Ratchet
	/// engine. Artalk.Xmpp keeps this as an interface so applications can choose
	/// a vetted implementation and storage backend without changing XMPP wiring.
	/// </remarks>
	public interface IOmemoSessionCipher {
		/// <summary>
		/// Encrypts the 48 byte OMEMO payload key material for a recipient device.
		/// </summary>
		OmemoRecipientKey EncryptKeyMaterial(Jid recipientJid,
			uint recipientDeviceId, OmemoBundle recipientBundle, byte[] keyMaterial);

		/// <summary>
		/// Decrypts the 48 byte OMEMO payload key material for the local device.
		/// </summary>
		byte[] DecryptKeyMaterial(Jid senderJid, uint senderDeviceId,
			OmemoRecipientKey recipientKey);
	}
}
