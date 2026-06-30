using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents the XEP-0384 OMEMO bundle published for a single device.
	/// </summary>
	public class OmemoBundle {
		/// <summary>
		/// The XML namespace used by OMEMO PEP payloads.
		/// </summary>
		public const string Namespace = OmemoDeviceList.Namespace;

		/// <summary>
		/// The id of the signed pre-key.
		/// </summary>
		public uint SignedPreKeyId {
			get;
		}

		/// <summary>
		/// The public signed pre-key bytes.
		/// </summary>
		public byte[] SignedPreKey {
			get {
				return (byte[]) signedPreKey.Clone();
			}
		}

		/// <summary>
		/// The signature for the signed pre-key.
		/// </summary>
		public byte[] SignedPreKeySignature {
			get {
				return (byte[]) signedPreKeySignature.Clone();
			}
		}

		/// <summary>
		/// The identity key bytes for the OMEMO device.
		/// </summary>
		public byte[] IdentityKey {
			get {
				return (byte[]) identityKey.Clone();
			}
		}

		/// <summary>
		/// The unsigned one-time pre-keys keyed by pre-key id.
		/// </summary>
		public IReadOnlyDictionary<uint, byte[]> PreKeys {
			get {
				return new ReadOnlyDictionary<uint, byte[]>(
					preKeys.ToDictionary(pair => pair.Key,
						pair => (byte[]) pair.Value.Clone()));
			}
		}

		readonly byte[] signedPreKey;
		readonly byte[] signedPreKeySignature;
		readonly byte[] identityKey;
		readonly IReadOnlyDictionary<uint, byte[]> preKeys;

		/// <summary>
		/// Initializes a new instance of the OmemoBundle class.
		/// </summary>
		public OmemoBundle(uint signedPreKeyId, byte[] signedPreKey,
			byte[] signedPreKeySignature, byte[] identityKey,
			IDictionary<uint, byte[]> preKeys) {
			signedPreKey.ThrowIfNull("signedPreKey");
			signedPreKeySignature.ThrowIfNull("signedPreKeySignature");
			identityKey.ThrowIfNull("identityKey");
			preKeys.ThrowIfNull("preKeys");
			if (preKeys.Count == 0)
				throw new ArgumentException("At least one OMEMO pre-key is required.",
					"preKeys");

			SignedPreKeyId = signedPreKeyId;
			this.signedPreKey = (byte[]) signedPreKey.Clone();
			this.signedPreKeySignature = (byte[]) signedPreKeySignature.Clone();
			this.identityKey = (byte[]) identityKey.Clone();
			this.preKeys = new ReadOnlyDictionary<uint, byte[]>(
				preKeys.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key,
					pair => ClonePreKey(pair.Value)));
		}

		/// <summary>
		/// Serializes the bundle into the OMEMO PEP payload element.
		/// </summary>
		public XmlElement ToXmlElement() {
			var bundle = Xml.Element("bundle", Namespace)
				.Child(Xml.Element("signedPreKeyPublic", Namespace)
					.Attr("signedPreKeyId",
						SignedPreKeyId.ToString(CultureInfo.InvariantCulture))
					.Text(Convert.ToBase64String(signedPreKey)))
				.Child(Xml.Element("signedPreKeySignature", Namespace)
					.Text(Convert.ToBase64String(signedPreKeySignature)))
				.Child(Xml.Element("identityKey", Namespace)
					.Text(Convert.ToBase64String(identityKey)));

			var prekeys = Xml.Element("prekeys", Namespace);
			foreach (var pair in preKeys) {
				prekeys.Child(Xml.Element("preKeyPublic", Namespace)
					.Attr("preKeyId", pair.Key.ToString(CultureInfo.InvariantCulture))
					.Text(Convert.ToBase64String(pair.Value)));
			}
			return bundle.Child(prekeys);
		}

		/// <summary>
		/// Parses an OMEMO bundle payload.
		/// </summary>
		/// <param name="element">The bundle element to parse.</param>
		public static OmemoBundle Parse(XmlElement element) {
			element.ThrowIfNull("element");
			if (element.LocalName != "bundle" || element.NamespaceURI != Namespace)
				throw new XmppException("Expected OMEMO bundle element.");

			XmlElement signedPreKeyElement = RequiredChild(element,
				"signedPreKeyPublic");
			string signedPreKeyIdValue =
				signedPreKeyElement.GetAttribute("signedPreKeyId");
			if (!UInt32.TryParse(signedPreKeyIdValue, NumberStyles.None,
				CultureInfo.InvariantCulture, out uint signedPreKeyId)) {
				throw new XmppException("Invalid OMEMO signed pre-key id: " +
					signedPreKeyIdValue);
			}

			var preKeys = new Dictionary<uint, byte[]>();
			XmlElement prekeysElement = RequiredChild(element, "prekeys");
			foreach (XmlNode node in prekeysElement.ChildNodes) {
				if (node is not XmlElement preKey ||
					preKey.LocalName != "preKeyPublic" ||
					preKey.NamespaceURI != Namespace) {
					continue;
				}
				string preKeyIdValue = preKey.GetAttribute("preKeyId");
				if (!UInt32.TryParse(preKeyIdValue, NumberStyles.None,
					CultureInfo.InvariantCulture, out uint preKeyId)) {
					throw new XmppException("Invalid OMEMO pre-key id: " +
						preKeyIdValue);
				}
				preKeys.Add(preKeyId, Convert.FromBase64String(preKey.InnerText));
			}

			return new OmemoBundle(signedPreKeyId,
				Convert.FromBase64String(signedPreKeyElement.InnerText),
				Convert.FromBase64String(RequiredChild(element,
					"signedPreKeySignature").InnerText),
				Convert.FromBase64String(RequiredChild(element,
					"identityKey").InnerText),
				preKeys);
		}

		static XmlElement RequiredChild(XmlElement parent, string localName) {
			foreach (XmlNode node in parent.ChildNodes) {
				if (node is XmlElement child &&
					child.LocalName == localName &&
					child.NamespaceURI == Namespace) {
					return child;
				}
			}
			throw new XmppException("Expected OMEMO '" + localName + "' element.");
		}

		static byte[] ClonePreKey(byte[] value) {
			value.ThrowIfNull("preKey");
			return (byte[]) value.Clone();
		}
	}
}
