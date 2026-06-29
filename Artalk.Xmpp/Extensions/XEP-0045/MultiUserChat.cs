using Artalk.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Implements basic Multi-User Chat support as defined in XEP-0045.
	/// </summary>
	internal class MultiUserChat : XmppExtension, IInputFilter<Message>,
		IInputFilter<Presence> {
		const string MucNamespace = "http://jabber.org/protocol/muc";
		const string MucUserNamespace = "http://jabber.org/protocol/muc#user";

		readonly Dictionary<string, string> joinedRooms =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		/// <inheritdoc/>
		public override IEnumerable<string> Namespaces => new[] {
			MucNamespace,
			MucUserNamespace
		};

		/// <inheritdoc/>
		public override string Xep => Extension.MultiUserChat;

		/// <summary>
		/// Raised when a groupchat message is received from a joined room.
		/// </summary>
		public event EventHandler<RoomMessageEventArgs> RoomMessage;

		/// <summary>
		/// Raised when occupant presence is received from a joined room.
		/// </summary>
		public event EventHandler<RoomPresenceEventArgs> RoomPresence;

		public MultiUserChat(XmppIm im)
			: base(im) {
		}

		public void JoinRoom(Jid roomJid, string nickname, string password = null,
			int? maxHistoryStanzas = null) {
			roomJid.ThrowIfNull("roomJid");
			nickname.ThrowIfNullOrEmpty("nickname");
			if (maxHistoryStanzas.HasValue && maxHistoryStanzas.Value < 0)
				throw new ArgumentOutOfRangeException("maxHistoryStanzas");

			Jid bareRoom = EnsureBareRoom(roomJid);
			Jid occupantJid = CreateOccupantJid(bareRoom, nickname);
			XmlElement x = Xml.Element("x", MucNamespace);
			if (password != null)
				x.Child(Xml.Element("password").Text(password));
			if (maxHistoryStanzas.HasValue)
				x.Child(Xml.Element("history")
					.Attr("maxstanzas", maxHistoryStanzas.Value.ToString(
						CultureInfo.InvariantCulture)));

			joinedRooms[RoomKey(bareRoom)] = nickname;
			im.SendPresence(new Presence(occupantJid, im.Jid,
				PresenceType.Available, null, null, x));
		}

		public void LeaveRoom(Jid roomJid, string nickname, string status = null) {
			roomJid.ThrowIfNull("roomJid");
			nickname.ThrowIfNullOrEmpty("nickname");

			Jid bareRoom = EnsureBareRoom(roomJid);
			Jid occupantJid = CreateOccupantJid(bareRoom, nickname);
			XmlElement statusElement = status == null ? null :
				Xml.Element("status").Text(status);
			im.SendPresence(new Presence(occupantJid, im.Jid,
				PresenceType.Unavailable, null, null, statusElement));
			joinedRooms.Remove(RoomKey(bareRoom));
		}

		public void SendRoomMessage(Jid roomJid, string body,
			string subject = null) {
			roomJid.ThrowIfNull("roomJid");
			body.ThrowIfNullOrEmpty("body");

			Jid bareRoom = EnsureBareRoom(roomJid);
			im.SendMessage(new Message(bareRoom, body, subject, null,
				MessageType.Groupchat));
		}

		public bool Input(Message stanza) {
			if (stanza.Type != MessageType.Groupchat || stanza.From == null)
				return false;

			Jid roomJid = GetRoomJid(stanza.From);
			if (!IsJoinedRoom(roomJid))
				return false;

			RoomMessage.Raise(this, new RoomMessageEventArgs(roomJid,
				stanza.From, stanza.From.Resource, stanza));
			return false;
		}

		public bool Input(Presence stanza) {
			if (stanza.From == null)
				return false;

			Jid roomJid = GetRoomJid(stanza.From);
			if (!IsJoinedRoom(roomJid))
				return false;

			MucOccupant occupant = ParseOccupant(stanza);
			RoomPresence.Raise(this, new RoomPresenceEventArgs(roomJid, occupant,
				stanza.Type == PresenceType.Available, ParseStatusCodes(stanza)));
			return true;
		}

		internal static MucOccupant ParseOccupant(Presence presence) {
			presence.ThrowIfNull("presence");
			if (presence.From == null || String.IsNullOrEmpty(presence.From.Resource))
				throw new ArgumentException("The presence stanza must be from a room occupant.");

			XmlElement item = GetMucUserItem(presence);
			Jid realJid = null;
			string affiliation = null, role = null;
			if (item != null) {
				string jid = item.GetAttribute("jid");
				if (!String.IsNullOrEmpty(jid))
					realJid = new Jid(jid);
				affiliation = EmptyToNull(item.GetAttribute("affiliation"));
				role = EmptyToNull(item.GetAttribute("role"));
			}

			return new MucOccupant(GetRoomJid(presence.From), presence.From,
				presence.From.Resource, realJid, affiliation, role);
		}

		internal static Jid GetRoomJid(Jid occupantOrRoomJid) {
			occupantOrRoomJid.ThrowIfNull("occupantOrRoomJid");
			return new Jid(occupantOrRoomJid.Domain, occupantOrRoomJid.Node);
		}

		static IEnumerable<string> ParseStatusCodes(Presence presence) {
			XmlElement x = GetMucUserElement(presence);
			if (x == null)
				return Array.Empty<string>();

			return x.ChildNodes
				.OfType<XmlElement>()
				.Where(e => e.Name == "status")
				.Select(e => e.GetAttribute("code"))
				.Where(code => !String.IsNullOrEmpty(code))
				.ToArray();
		}

		static XmlElement GetMucUserItem(Presence presence) {
			XmlElement x = GetMucUserElement(presence);
			if (x == null)
				return null;
			return x.ChildNodes
				.OfType<XmlElement>()
				.FirstOrDefault(e => e.Name == "item");
		}

		static XmlElement GetMucUserElement(Presence presence) {
			return presence.Data.ChildNodes
				.OfType<XmlElement>()
				.FirstOrDefault(e => e.Name == "x" &&
					e.NamespaceURI == MucUserNamespace);
		}

		static Jid EnsureBareRoom(Jid roomJid) {
			if (!String.IsNullOrEmpty(roomJid.Resource))
				return GetRoomJid(roomJid);
			return roomJid;
		}

		static Jid CreateOccupantJid(Jid roomJid, string nickname) {
			return new Jid(roomJid.Domain, roomJid.Node, nickname);
		}

		static string RoomKey(Jid roomJid) {
			return EnsureBareRoom(roomJid).ToString();
		}

		bool IsJoinedRoom(Jid roomJid) {
			return joinedRooms.ContainsKey(RoomKey(roomJid));
		}

		static string EmptyToNull(string value) {
			return String.IsNullOrEmpty(value) ? null : value;
		}
	}
}
