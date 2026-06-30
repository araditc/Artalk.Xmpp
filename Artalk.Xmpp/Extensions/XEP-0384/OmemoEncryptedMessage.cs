using Artalk.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents the XEP-0384 OMEMO encrypted message XML envelope.
	/// </summary>
	public class OmemoEncryptedMessage {
		/// <summary>
		/// The XML namespace used by OMEMO encrypted messages.
		/// </summary>
		public const string Namespace = OmemoDeviceList.Namespace;

		/// <summary>
		/// The sending OMEMO device id.
		/// </summary>
		public uint SenderDeviceId {
			get;
		}

		/// <summary>
		/// Encrypted key entries grouped by recipient bare JID on serialization.
		/// </summary>
		public IReadOnlyList<OmemoRecipientKey> Keys {
			get;
		}

		/// <summary>
		/// The encrypted payload bytes, or null for an empty OMEMO message.
		/// </summary>
		public byte[] Payload {
			get {
				return payload == null ? null : (byte[]) payload.Clone();
			}
		}

		readonly byte[] payload;

		/// <summary>
		/// Initializes a new instance of the OmemoEncryptedMessage class.
		/// </summary>
		public OmemoEncryptedMessage(uint senderDeviceId,
			IEnumerable<OmemoRecipientKey> keys, byte[] payload = null) {
			OmemoDevice.ValidateDeviceId(senderDeviceId, "senderDeviceId");
			keys.ThrowIfNull("keys");
			var keyList = keys.ToList();
			if (keyList.Count == 0)
				throw new ArgumentException("At least one OMEMO recipient key is required.",
					"keys");
			SenderDeviceId = senderDeviceId;
			Keys = new ReadOnlyCollection<OmemoRecipientKey>(keyList);
			this.payload = payload == null ? null : (byte[]) payload.Clone();
		}

		/// <summary>
		/// Serializes the encrypted message into an OMEMO encrypted element.
		/// </summary>
		public XmlElement ToXmlElement() {
			var encrypted = Xml.Element("encrypted", Namespace);
			var header = Xml.Element("header", Namespace).Attr("sid",
				SenderDeviceId.ToString(CultureInfo.InvariantCulture));
			foreach (var group in Keys.GroupBy(k => k.Jid.GetBareJid().ToString())) {
				var keysElement = Xml.Element("keys", Namespace).Attr("jid", group.Key);
				foreach (OmemoRecipientKey key in group) {
					var keyElement = Xml.Element("key", Namespace)
						.Attr("rid", key.RecipientDeviceId.ToString(
							CultureInfo.InvariantCulture))
						.Text(Convert.ToBase64String(key.EncryptedKeyMaterial));
					if (key.KeyExchange)
						keyElement.Attr("kex", "true");
					keysElement.Child(keyElement);
				}
				header.Child(keysElement);
			}
			encrypted.Child(header);
			if (payload != null) {
				encrypted.Child(Xml.Element("payload", Namespace)
					.Text(Convert.ToBase64String(payload)));
			}
			return encrypted;
		}

		/// <summary>
		/// Attempts to parse an OMEMO encrypted element.
		/// </summary>
		public static bool TryParse(XmlElement element,
			out OmemoEncryptedMessage encryptedMessage) {
			encryptedMessage = null;
			if (element == null ||
				element.LocalName != "encrypted" ||
				element.NamespaceURI != Namespace) {
				return false;
			}
			encryptedMessage = Parse(element);
			return true;
		}

		/// <summary>
		/// Attempts to parse an OMEMO encrypted element from a message stanza.
		/// </summary>
		public static bool TryParse(Message message,
			out OmemoEncryptedMessage encryptedMessage) {
			encryptedMessage = null;
			if (message == null)
				return false;
			foreach (XmlNode node in message.Data.ChildNodes) {
				if (node is XmlElement child && TryParse(child, out encryptedMessage))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Parses an OMEMO encrypted element.
		/// </summary>
		public static OmemoEncryptedMessage Parse(XmlElement element) {
			element.ThrowIfNull("element");
			if (element.LocalName != "encrypted" || element.NamespaceURI != Namespace)
				throw new XmppException("Expected OMEMO encrypted element.");
			XmlElement header = RequiredChild(element, "header");
			string sidValue = header.GetAttribute("sid");
			if (!UInt32.TryParse(sidValue, NumberStyles.None,
				CultureInfo.InvariantCulture, out uint senderDeviceId)) {
				throw new XmppException("Invalid OMEMO sender device id: " + sidValue);
			}
			OmemoDevice.ValidateDeviceId(senderDeviceId, "senderDeviceId");

			var keys = new List<OmemoRecipientKey>();
			foreach (XmlNode keysNode in header.ChildNodes) {
				if (keysNode is not XmlElement keysElement ||
					keysElement.LocalName != "keys" ||
					keysElement.NamespaceURI != Namespace) {
					continue;
				}
				var jid = new Jid(keysElement.GetAttribute("jid"));
				foreach (XmlNode keyNode in keysElement.ChildNodes) {
					if (keyNode is not XmlElement keyElement ||
						keyElement.LocalName != "key" ||
						keyElement.NamespaceURI != Namespace) {
						continue;
					}
					string ridValue = keyElement.GetAttribute("rid");
					if (!UInt32.TryParse(ridValue, NumberStyles.None,
						CultureInfo.InvariantCulture, out uint rid)) {
						throw new XmppException("Invalid OMEMO recipient device id: " +
							ridValue);
					}
					keys.Add(new OmemoRecipientKey(jid, rid,
						Convert.FromBase64String(keyElement.InnerText),
						keyElement.GetAttribute("kex") == "true"));
				}
			}

			XmlElement payload = Child(element, "payload");
			return new OmemoEncryptedMessage(senderDeviceId, keys,
				payload == null ? null : Convert.FromBase64String(payload.InnerText));
		}

		/// <summary>
		/// Returns encrypted key entries that target the specified bare JID.
		/// </summary>
		public IEnumerable<OmemoRecipientKey> GetKeysFor(Jid jid) {
			jid.ThrowIfNull("jid");
			string bare = jid.GetBareJid().ToString();
			return Keys.Where(k => k.Jid.ToString() == bare);
		}

		static XmlElement RequiredChild(XmlElement parent, string localName) {
			XmlElement child = Child(parent, localName);
			if (child == null)
				throw new XmppException("Expected OMEMO '" + localName + "' element.");
			return child;
		}

		static XmlElement Child(XmlElement parent, string localName) {
			foreach (XmlNode node in parent.ChildNodes) {
				if (node is XmlElement child &&
					child.LocalName == localName &&
					child.NamespaceURI == Namespace) {
					return child;
				}
			}
			return null;
		}
	}
}
