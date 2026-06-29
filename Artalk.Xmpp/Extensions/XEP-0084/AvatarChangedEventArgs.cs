using System;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Provides data for the AvatarChanged event.
	/// </summary>
	[Serializable]
	public class AvatarChangedEventArgs : EventArgs {
		/// <summary>
		/// The JID of the XMPP entity that published the avatar information.
		/// </summary>
		public Jid Jid {
			get;
			private set;
		}

		/// <summary>
		/// True if the XMPP entity has cleared its avatar image.
		/// </summary>
		public bool Cleared {
			get {
				return AvatarData == null || AvatarData.Length == 0;
			}
		}

		/// <summary>
		/// The SHA-1 hash of the XMPP entity's avatar image.
		/// </summary>
		public string Hash {
			get;
			private set;
		}

		/// <summary>
		/// The MIME type of the avatar data.
		/// </summary>
		public string MimeType {
			get;
			private set;
		}

		/// <summary>
		/// The binary avatar payload.
		/// </summary>
		public byte[] AvatarData {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the AvatarChangedEventArgs class.
		/// </summary>
		/// <param name="jid">The JID of the XMPP entity that published the
		/// avatar information.</param>
		/// <param name="hash">The SHA-1 hash of the avatar image data.</param>
		/// <param name="avatarData">The avatar binary payload.</param>
		/// <param name="mimeType">The MIME type of the avatar payload.</param>
		/// <exception cref="ArgumentNullException">The jid parameter is
		/// null.</exception>
		public AvatarChangedEventArgs(Jid jid, string hash = null,
			byte[] avatarData = null, string mimeType = null) {
			jid.ThrowIfNull("jid");
			Jid = jid;
			Hash = hash;
			AvatarData = avatarData;
			MimeType = mimeType;
		}
	}
}
