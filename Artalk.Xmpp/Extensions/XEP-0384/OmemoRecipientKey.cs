using System;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents an encrypted OMEMO payload key for one recipient device.
	/// </summary>
	public class OmemoRecipientKey {
		/// <summary>
		/// The recipient bare JID.
		/// </summary>
		public Jid Jid {
			get;
		}

		/// <summary>
		/// The recipient OMEMO device id.
		/// </summary>
		public uint RecipientDeviceId {
			get;
		}

		/// <summary>
		/// true when the encrypted key material includes a key exchange.
		/// </summary>
		public bool KeyExchange {
			get;
		}

		/// <summary>
		/// The encrypted key material produced by the OMEMO session layer.
		/// </summary>
		public byte[] EncryptedKeyMaterial {
			get {
				return (byte[]) encryptedKeyMaterial.Clone();
			}
		}

		readonly byte[] encryptedKeyMaterial;

		/// <summary>
		/// Initializes a new instance of the OmemoRecipientKey class.
		/// </summary>
		public OmemoRecipientKey(Jid jid, uint recipientDeviceId,
			byte[] encryptedKeyMaterial, bool keyExchange = false) {
			jid.ThrowIfNull("jid");
			OmemoDevice.ValidateDeviceId(recipientDeviceId, "recipientDeviceId");
			encryptedKeyMaterial.ThrowIfNull("encryptedKeyMaterial");
			Jid = jid.GetBareJid();
			RecipientDeviceId = recipientDeviceId;
			this.encryptedKeyMaterial = (byte[]) encryptedKeyMaterial.Clone();
			KeyExchange = keyExchange;
		}
	}
}
