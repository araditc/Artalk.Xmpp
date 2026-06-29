using Artalk.Xmpp.Im;
using System;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Provides data for a multi-user chat room message.
	/// </summary>
	public class RoomMessageEventArgs : EventArgs {
		/// <summary>
		/// The bare JID of the room.
		/// </summary>
		public Jid RoomJid {
			get;
			private set;
		}

		/// <summary>
		/// The room occupant JID that sent the message.
		/// </summary>
		public Jid OccupantJid {
			get;
			private set;
		}

		/// <summary>
		/// The occupant nickname.
		/// </summary>
		public string Nickname {
			get;
			private set;
		}

		/// <summary>
		/// The received groupchat message.
		/// </summary>
		public Message Message {
			get;
			private set;
		}

		internal RoomMessageEventArgs(Jid roomJid, Jid occupantJid,
			string nickname, Message message) {
			roomJid.ThrowIfNull("roomJid");
			occupantJid.ThrowIfNull("occupantJid");
			message.ThrowIfNull("message");
			RoomJid = roomJid;
			OccupantJid = occupantJid;
			Nickname = nickname;
			Message = message;
		}
	}
}
