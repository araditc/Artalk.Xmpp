using Artalk.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Implements the 'User Avatar' extension as defined in XEP-0084.
	/// </summary>
	/// <remarks>This currently does not account for avatar images hosted on
	/// HTTP servers.</remarks>
	internal class UserAvatar : XmppExtension {
		/// <summary>
		/// A reference to the 'Personal Eventing Protocol' extension instance.
		/// </summary>
		Pep pep;
		/// <summary>
		/// A cache of avatar payloads indexed by their respective SHA-1 hashes.
		/// </summary>
		IDictionary<string, byte[]> cachedAvatars = new Dictionary<string, byte[]>();

		/// <summary>
		/// An enumerable collection of XMPP namespaces the extension implements.
		/// </summary>
		/// <remarks>This is used for compiling the list of supported extensions
		/// advertised by the 'Service Discovery' extension.</remarks>
		public override IEnumerable<string> Namespaces =>
			new[] {
				"urn:xmpp:avatar:data",
				"urn:xmpp:avatar:metadata",
				"urn:xmpp:avatar:metadata+notify"
			};

		/// <summary>
		/// The named constant of the Extension enumeration that corresponds to this
		/// extension.
		/// </summary>
		public override string Xep => Extension.UserAvatar;

		/// <summary>
		/// Determines whether our server supports personal eventing and thusly
		/// the user avatar extension.
		/// </summary>
		public bool Supported => pep.Supported;

		/// <summary>
		/// The event that is raised when another XMPP entity has changed its
		/// avatar image.
		/// </summary>
		public event EventHandler<AvatarChangedEventArgs> AvatarChanged;

		/// <summary>
		/// Invoked after all extensions have been loaded.
		/// </summary>
		public override void Initialize() {
			pep = im.GetExtension<Pep>();
			pep.Subscribe("urn:xmpp:avatar:metadata", OnMetadata);
		}

		/// <summary>
		/// Publishes the image data provided by the specified stream as the user's
		/// avatar.
		/// </summary>
		/// <param name="stream">A stream containing the image data to publish as
		/// the user's avatar.</param>
		/// <exception cref="ArgumentNullException">The stream parameter is
		/// null.</exception>
		/// <exception cref="ArgumentException">The stream does not have a recognized
		/// image format.</exception>
		public void Publish(Stream stream) {
			stream.ThrowIfNull("stream");
			using (var ms = new MemoryStream()) {
				stream.CopyTo(ms);
				Publish(ms.ToArray());
			}
		}

		/// <summary>
		/// Publishes the specified image data as the user's avatar.
		/// </summary>
		/// <param name="data">The image data to publish.</param>
		/// <param name="mimeType">The MIME type of the image data. If null, the
		/// type is inferred from common image signatures.</param>
		/// <param name="width">The width of the image, if known.</param>
		/// <param name="height">The height of the image, if known.</param>
		public void Publish(byte[] data, string mimeType = null, int? width = null,
			int? height = null) {
			data.ThrowIfNull("data");
			if (data.Length == 0)
				throw new ArgumentException("Avatar data must not be empty.", "data");
			mimeType = mimeType ?? DetectMimeType(data);
			if (mimeType == null)
				throw new ArgumentException("The avatar MIME type could not be determined.",
					"data");
			TryReadDimensions(data, ref width, ref height);
			string hash = Hash(data);
			string base64Data = Convert.ToBase64String(data);
			pep.Publish("urn:xmpp:avatar:data", hash,
				Xml.Element("data", "urn:xmpp:avatar:data").Text(base64Data));
			XmlElement info = Xml.Element("info")
				.Attr("bytes", data.Length.ToString())
				.Attr("id", hash)
				.Attr("type", mimeType);
			if (height.HasValue)
				info.Attr("height", height.Value.ToString());
			if (width.HasValue)
				info.Attr("width", width.Value.ToString());
			pep.Publish("urn:xmpp:avatar:metadata", hash,
				Xml.Element("metadata", "urn:xmpp:avatar:metadata").Child(info));
		}

		/// <summary>
		/// Publishes the image located at the specified path as the user's avatar.
		/// </summary>
		/// <param name="filePath">The path to the image to publish as the user's
		/// avatar.</param>
		public void Publish(string filePath) {
			using (Stream s = File.OpenRead(filePath)) {
				Publish(s);
			}
		}

		/// <summary>
		/// Initializes a new instance of the UserAvatar class.
		/// </summary>
		/// <param name="im">A reference to the XmppIm instance on whose behalf this
		/// instance is created.</param>
		public UserAvatar(XmppIm im)
			: base(im) {
		}

		/// <summary>
		/// Invoked when a contact has published avatar metadata information.
		/// </summary>
		/// <param name="jid">The JID of the XMPP entity that published the tune
		/// information.</param>
		/// <param name="item">The 'item' Xml element of the pubsub publish
		/// event.</param>
		void OnMetadata(Jid jid, XmlElement item) {
			if (item == null || item["metadata"] == null)
				return;
			// An empty 'metadata' element means the XMPP entity wishes to temporarily
			// disable avatar publishing.
			if (item["metadata"].IsEmpty) {
				AvatarChanged.Raise(this, new AvatarChangedEventArgs(jid));
				return;
			}
			var info = item["metadata"]["info"];
			if (info == null)
				return;
			string hash = info.GetAttribute("id");
			if (String.IsNullOrEmpty(hash))
				return;
			if (!cachedAvatars.ContainsKey(hash))
				cachedAvatars.Add(hash, RequestImage(jid, hash));
			byte[] avatar = cachedAvatars[hash];
			AvatarChanged.Raise(this, new AvatarChangedEventArgs(jid, hash, avatar,
				info.GetAttribute("type")));
		}

		/// <summary>
		/// Generates a hex-encoded SHA-1 hash from the specified input data.
		/// </summary>
		/// <param name="data">The input data to hash.</param>
		/// <returns>The hashed input data as a lowercase hex string.</returns>
		/// <exception cref="ArgumentNullException">The data parameter is
		/// null.</exception>
		string Hash(byte[] data) {
			data.ThrowIfNull("data");
			return Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant();
		}

		/// <summary>
		/// Requests the avatar image with the specified hash from the node service
		/// running at the specified JID.
		/// </summary>
		/// <param name="jid">The JID of the node service to request the avatar
		/// image from.</param>
		/// <param name="hash">The hash of the avatar image to retrieve.</param>
		/// <returns>The retrieved avatar image data.</returns>
		byte[] RequestImage(Jid jid, string hash) {
			jid.ThrowIfNull("jid");
			hash.ThrowIfNull("hash");
			XmlElement item = pep.RetrieveItem(jid, "urn:xmpp:avatar:data", hash);
			if (item["data"] == null || item["data"].NamespaceURI != "urn:xmpp:avatar:data")
				throw new XmppException("Erroneous avatar data: " + item);
			try {
				return Convert.FromBase64String(item["data"].InnerText);
			} catch (FormatException e) {
				throw new XmppException("Invalid image data.", e);
			}
		}

		static string DetectMimeType(byte[] data) {
			if (data.Length >= 8 &&
				data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E &&
				data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A &&
				data[6] == 0x1A && data[7] == 0x0A)
				return "image/png";
			if (data.Length >= 3 &&
				data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
				return "image/jpeg";
			if (data.Length >= 6 &&
				data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 &&
				data[3] == 0x38 && (data[4] == 0x37 || data[4] == 0x39) &&
				data[5] == 0x61)
				return "image/gif";
			if (data.Length >= 12 &&
				data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 &&
				data[3] == 0x46 && data[8] == 0x57 && data[9] == 0x45 &&
				data[10] == 0x42 && data[11] == 0x50)
				return "image/webp";
			return null;
		}

		static void TryReadDimensions(byte[] data, ref int? width, ref int? height) {
			if (width.HasValue && height.HasValue)
				return;
			if (data.Length >= 24 && DetectMimeType(data) == "image/png") {
				width = width ?? ReadBigEndianInt32(data, 16);
				height = height ?? ReadBigEndianInt32(data, 20);
			} else if (data.Length >= 10 && DetectMimeType(data) == "image/gif") {
				width = width ?? ReadLittleEndianUInt16(data, 6);
				height = height ?? ReadLittleEndianUInt16(data, 8);
			}
		}

		static int ReadBigEndianInt32(byte[] data, int offset) {
			return (data[offset] << 24) | (data[offset + 1] << 16) |
				(data[offset + 2] << 8) | data[offset + 3];
		}

		static int ReadLittleEndianUInt16(byte[] data, int offset) {
			return data[offset] | (data[offset + 1] << 8);
		}
	}
}
