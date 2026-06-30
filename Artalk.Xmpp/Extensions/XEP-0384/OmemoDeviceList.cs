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
		public const string Namespace = "urn:xmpp:omemo:2";

		/// <summary>
		/// The legacy namespace used by older OMEMO implementations.
		/// </summary>
		public const string LegacyNamespace = "eu.siacs.conversations.axolotl";

		/// <summary>
		/// The PEP node used to publish XEP-0384 OMEMO device lists.
		/// </summary>
		public const string Node = Namespace + ":devices";

		/// <summary>
		/// The OMEMO device ids advertised by an account.
		/// </summary>
		public IReadOnlyList<uint> DeviceIds {
			get {
				return new ReadOnlyCollection<uint>(Devices.Select(d => d.Id).ToList());
			}
		}

		/// <summary>
		/// The OMEMO devices advertised by an account.
		/// </summary>
		public IReadOnlyList<OmemoDevice> Devices {
			get;
		}

		/// <summary>
		/// Initializes a new instance of the OmemoDeviceList class.
		/// </summary>
		/// <param name="deviceIds">The OMEMO device ids advertised by an account.</param>
		public OmemoDeviceList(IEnumerable<uint> deviceIds)
			: this(ToDevices(deviceIds)) {
		}

		/// <summary>
		/// Initializes a new instance of the OmemoDeviceList class.
		/// </summary>
		/// <param name="devices">The OMEMO devices advertised by an account.</param>
		public OmemoDeviceList(IEnumerable<OmemoDevice> devices) {
			devices.ThrowIfNull("devices");
			Devices = new ReadOnlyCollection<OmemoDevice>(
				devices.GroupBy(d => {
					d.ThrowIfNull("device");
					return d.Id;
				})
					.Select(g => g.First())
					.OrderBy(d => d.Id)
					.ToList());
		}

		/// <summary>
		/// Serializes the device list into the OMEMO PEP payload element.
		/// </summary>
		public XmlElement ToXmlElement() {
			var devices = Xml.Element("devices", Namespace);
			foreach (OmemoDevice device in Devices) {
				var deviceElement = Xml.Element("device", Namespace).Attr("id",
					device.Id.ToString(CultureInfo.InvariantCulture));
				if (!String.IsNullOrEmpty(device.Label))
					deviceElement.Attr("label", device.Label);
				if (device.LabelSignature != null) {
					deviceElement.Attr("labelsig",
						Convert.ToBase64String(device.LabelSignature));
				}
				devices.Child(deviceElement);
			}
			return devices;
		}

		/// <summary>
		/// Parses an OMEMO device list payload.
		/// </summary>
		/// <param name="element">The list element to parse.</param>
		public static OmemoDeviceList Parse(XmlElement element) {
			element.ThrowIfNull("element");
			bool legacy = element.LocalName == "list" &&
				element.NamespaceURI == LegacyNamespace;
			bool current = element.LocalName == "devices" &&
				element.NamespaceURI == Namespace;
			if (!current && !legacy)
				throw new XmppException("Expected OMEMO device list element.");

			var devices = new List<OmemoDevice>();
			foreach (XmlNode node in element.ChildNodes) {
				if (node is not XmlElement device ||
					device.LocalName != "device" ||
					(!current && device.NamespaceURI != LegacyNamespace) ||
					(current && device.NamespaceURI != Namespace)) {
					continue;
				}
				string id = device.GetAttribute("id");
				if (!UInt32.TryParse(id, NumberStyles.None,
					CultureInfo.InvariantCulture, out uint deviceId)) {
					throw new XmppException("Invalid OMEMO device id: " + id);
				}
				OmemoDevice.ValidateDeviceId(deviceId, "id");
				byte[] labelSignature = null;
				string signature = device.GetAttribute("labelsig");
				if (!String.IsNullOrEmpty(signature))
					labelSignature = Convert.FromBase64String(signature);
				devices.Add(new OmemoDevice(deviceId, device.GetAttribute("label"),
					labelSignature));
			}
			return new OmemoDeviceList(devices);
		}

		static IEnumerable<OmemoDevice> ToDevices(IEnumerable<uint> deviceIds) {
			deviceIds.ThrowIfNull("deviceIds");
			return deviceIds.Select(id => new OmemoDevice(id));
		}
	}
}
