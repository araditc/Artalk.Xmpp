using System;
using System.Collections.Generic;
using System.Linq;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Provides data for a multi-user chat room occupant presence update.
	/// </summary>
	public class RoomPresenceEventArgs : EventArgs {
		/// <summary>
		/// The bare JID of the room.
		/// </summary>
		public Jid RoomJid {
			get;
			private set;
		}

		/// <summary>
		/// The occupant described by the presence update.
		/// </summary>
		public MucOccupant Occupant {
			get;
			private set;
		}

		/// <summary>
		/// True when the occupant is available in the room.
		/// </summary>
		public bool IsAvailable {
			get;
			private set;
		}

		/// <summary>
		/// MUC status codes included in the presence stanza.
		/// </summary>
		public IEnumerable<string> StatusCodes {
			get;
			private set;
		}

		internal RoomPresenceEventArgs(Jid roomJid, MucOccupant occupant,
			bool isAvailable, IEnumerable<string> statusCodes) {
			roomJid.ThrowIfNull("roomJid");
			occupant.ThrowIfNull("occupant");
			RoomJid = roomJid;
			Occupant = occupant;
			IsAvailable = isAvailable;
			StatusCodes = statusCodes == null ? Array.Empty<string>() :
				statusCodes.ToArray();
		}
	}
}
