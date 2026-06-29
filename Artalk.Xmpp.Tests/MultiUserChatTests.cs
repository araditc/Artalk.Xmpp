using Artalk.Xmpp.Extensions;
using Artalk.Xmpp.Im;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class MultiUserChatTests {
	[TestMethod]
	public void GetRoomJidDropsOccupantNickname() {
		var occupantJid = new Jid("room@conference.example.com/juliet");

		var roomJid = MultiUserChat.GetRoomJid(occupantJid);

		Assert.AreEqual("room@conference.example.com", roomJid.ToString());
	}

	[TestMethod]
	public void ParseOccupantReadsMucUserItem() {
		var presence = ReadPresence(
			"<presence from='room@conference.example.com/juliet'>" +
			"<x xmlns='http://jabber.org/protocol/muc#user'>" +
			"<item affiliation='member' role='participant' jid='juliet@example.com/balcony'/>" +
			"<status code='110'/>" +
			"</x>" +
			"</presence>");

		MucOccupant occupant = MultiUserChat.ParseOccupant(presence);

		Assert.AreEqual("room@conference.example.com", occupant.RoomJid.ToString());
		Assert.AreEqual("room@conference.example.com/juliet", occupant.OccupantJid.ToString());
		Assert.AreEqual("juliet", occupant.Nickname);
		Assert.AreEqual("juliet@example.com/balcony", occupant.RealJid.ToString());
		Assert.AreEqual("member", occupant.Affiliation);
		Assert.AreEqual("participant", occupant.Role);
	}

	static Presence ReadPresence(string xml) {
		var document = new XmlDocument();
		document.LoadXml(xml);
		return new Presence(new Artalk.Xmpp.Core.Presence(document.DocumentElement));
	}
}
