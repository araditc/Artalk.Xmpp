
namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// An enumeration of supported XMPP extensions.
	/// </summary>
	public static class Extension {
		/// <summary>
		/// An extension for discovering information about other XMPP entities.
		/// </summary>
		/// <remarks>Defined in XEP-0030.</remarks>
		public static string ServiceDiscovery = "ServiceDiscovery";
		/// <summary>
		/// An extension for retrieving information about the software application
		/// associated with an XMPP entity. 
		/// </summary>
		public static string SoftwareVersion = "SoftwareVersion";
		/// <summary>
		/// An extension for broadcasting and dynamically discovering client,
		/// device, or generic entity capabilities.
		/// </summary>
		public static string EntityCapabilities = "EntityCapabilities";
		/// <summary>
		/// An extension for sending application-level pings over XML streams.
		/// </summary>
		public static string Ping = "Ping";
		/// <summary>
		/// An extension for getting the attention of another user.
		/// </summary>
		public static string Attention = "Attention";
		/// <summary>
		/// An extension for communicating the local time of an entity.
		/// </summary>
		public static string EntityTime = "EntityTime";
		/// <summary>
		/// An extension for communications blocking that is intended to be
		/// simpler than privacy lists.
		/// </summary>
		public static string BlockingCommand = "BlockingCommand";
		/// <summary>
		/// An extension for publishing and subscribing to broadcast state change
		/// events associated with an instant messaging and presence account.
		/// </summary>
		public static string PersonalEventingProcotol = "PersonalEventingProcotol";
		/// <summary>
		/// An extension for communicating information about music to which a user
		/// is listening, including the title, track number, length and others.
		/// </summary>
		public static string UserTune = "UserTune";
		/// <summary>
		/// An extension for exchanging user avatars, which are small images or
		/// icons associated with human users. 
		/// </summary>
		public static string UserAvatar = "UserAvatar";
		/// <summary>
		/// An extension for for communicating information about user moods, such
		/// as whether a person is currently happy, sad, angy, or annoyed.
		/// </summary>
		public static string UserMood = "UserMood";
		/// <summary>
		/// An extension extension for data forms that can be used in workflows
		/// such as service configuration as well as for application-specific
		/// data description and reporting.
		/// </summary>
		public static string DataForms = "DataForms";
		/// <summary>
		/// An extension that enables two entities to mutually negotiate feature
		/// options, such as parameters related to a file transfer or a
		/// communications session.
		/// </summary>
		public static string FeatureNegotiation = "FeatureNegotiation";
		/// <summary>
		/// An extension for initiating a data stream between any two XMPP
		/// entities.
		/// </summary>
		public static string StreamInitiation = "StreamInitiation";
		/// <summary>
		/// An extension for transferring files between two entities.
		/// </summary>
		public static string SIFileTransfer = "SIFileTransfer";
		/// <summary>
		/// An extension that enables any two entities to establish a one-to-one
		/// bytestream between themselves, where the data is broken down into
		/// smaller chunks and transported in-band over XMPP.
		/// </summary>
		public static string InBandBytestreams = "InBandBytestreams";
		/// <summary>
		/// An extension for communicating information about user activities,
		/// such as whether a person is currently working, travelling, or
		/// relaxing.
		/// </summary>
		public static string UserActivity = "UserActivity";
		/// <summary>
		/// An extension for establishing an out-of-band bytestream between any
		/// two XMPP users, mainly for the purpose of file transfer.
		/// </summary>
		public static string Socks5Bytestreams = "Socks5Bytestreams";
		/// <summary>
		/// An extension that enables a client to discover its external IP
		/// address.
		/// </summary>
		public static string ServerIpCheck = "ServerIpCheck";
		/// <summary>
		/// An extension for in-band registration with XMPP-based instant messaging
		/// servers and other services hosted on an XMPP network.
		/// </summary>
		public static string InBandRegistration = "InBandRegistration";
		/// <summary>
		/// An extension for including or referring to small bits of binary data in
		/// an XML stanza.
		/// </summary>
		public static string BitsOfBinary = "BitsOfBinary";
		/// <summary>
		/// An extension for communicating the status of a user in a chat session,
		/// thus indicating whether a chat partner is actively engaged in the chat,
		/// composing a message, temporarily paused, inactive, or gone.
		/// </summary>
		public static string ChatStateNotifications = "ChatStateNotifications";
	}
}
