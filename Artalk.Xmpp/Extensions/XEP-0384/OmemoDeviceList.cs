using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents the XEP-0384 OMEMO device list published through PEP.
	/// </summary>
	public class OmemoDeviceList {
		/// <summary>
		/// The XML namespace used by OMEMO PEP payloads.
		/// </summary>
		public const string Namespace = "eu.siacs.conversations.axolotl";

		/// <summary>
		/// The OMEMO device ids advertised by an account.
		/// </summary>
		public IReadOnlyList<uint> DeviceIds {
			get;
		}

		/// <summary>
		/// Initializes a new instance of the OmemoDeviceList class.
		/// </summary>
		/// <param name="deviceIds">The OMEMO device ids advertised by an account.</param>
		public OmemoDeviceList(IEnumerable<uint> deviceIds) {
			deviceIds.ThrowIfNull("deviceIds");
			DeviceIds = new ReadOnlyCollection<uint>(
				deviceIds.Distinct().OrderBy(id => id).ToList());
		}

		/// <summary>
		/// Serializes the device list into the OMEMO PEP payload element.
		/// </summary>
		public XmlElement ToXmlElement() {
			var list = Xml.Element("list", Namespace);
			foreach (uint deviceId in DeviceIds) {
				list.Child(Xml.Element("device", Namespace).Attr("id",
					deviceId.ToString(CultureInfo.InvariantCulture)));
			}
			return list;
		}

		/// <summary>
		/// Parses an OMEMO device list payload.
		/// </summary>
		/// <param name="element">The list element to parse.</param>
		public static OmemoDeviceList Parse(XmlElement element) {
			element.ThrowIfNull("element");
			if (element.LocalName != "list" || element.NamespaceURI != Namespace)
				throw new XmppException("Expected OMEMO device list element.");

			var deviceIds = new List<uint>();
			foreach (XmlNode node in element.ChildNodes) {
				if (node is not XmlElement device ||
					device.LocalName != "device" ||
					device.NamespaceURI != Namespace) {
					continue;
				}
				string id = device.GetAttribute("id");
				if (!UInt32.TryParse(id, NumberStyles.None,
					CultureInfo.InvariantCulture, out uint deviceId)) {
					throw new XmppException("Invalid OMEMO device id: " + id);
				}
				deviceIds.Add(deviceId);
			}
			return new OmemoDeviceList(deviceIds);
		}
	}
}
