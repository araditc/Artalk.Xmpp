namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents an occupant in a multi-user chat room.
	/// </summary>
	public class MucOccupant {
		/// <summary>
		/// The bare JID of the room.
		/// </summary>
		public Jid RoomJid {
			get;
			private set;
		}

		/// <summary>
		/// The room occupant JID, including the occupant nickname as resource.
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
		/// The real JID of the occupant if the room exposes it.
		/// </summary>
		public Jid RealJid {
			get;
			private set;
		}

		/// <summary>
		/// The occupant affiliation, such as owner, admin, member, or none.
		/// </summary>
		public string Affiliation {
			get;
			private set;
		}

		/// <summary>
		/// The occupant role, such as moderator, participant, visitor, or none.
		/// </summary>
		public string Role {
			get;
			private set;
		}

		internal MucOccupant(Jid roomJid, Jid occupantJid, string nickname,
			Jid realJid = null, string affiliation = null, string role = null) {
			roomJid.ThrowIfNull("roomJid");
			occupantJid.ThrowIfNull("occupantJid");
			nickname.ThrowIfNullOrEmpty("nickname");
			RoomJid = roomJid;
			OccupantJid = occupantJid;
			Nickname = nickname;
			RealJid = realJid;
			Affiliation = affiliation;
			Role = role;
		}
	}
}
