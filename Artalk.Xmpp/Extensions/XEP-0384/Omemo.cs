using Artalk.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Implements the PEP-based OMEMO device list and bundle surface from
	/// XEP-0384.
	/// </summary>
	internal class Omemo : XmppExtension {
		/// <summary>
		/// The XML namespace used by OMEMO PEP payloads.
		/// </summary>
		public const string Namespace = OmemoDeviceList.Namespace;

		/// <summary>
		/// The PEP node used to publish OMEMO device lists.
		/// </summary>
		public const string DeviceListNode = OmemoDeviceList.Node;

		/// <summary>
		/// The PEP node used to publish OMEMO bundles.
		/// </summary>
		public const string BundleNode = OmemoBundle.Node;

		/// <summary>
		/// The PEP item id used for OMEMO device lists and bundles.
		/// </summary>
		public const string CurrentItemId = "current";

		Pep pep;

		/// <summary>
		/// An enumerable collection of XMPP namespaces the extension implements.
		/// </summary>
		public override IEnumerable<string> Namespaces =>
			new[] {
				Namespace,
				DeviceListNode + "+notify"
			};

		/// <summary>
		/// The named constant of the Extension enumeration that corresponds to this
		/// extension.
		/// </summary>
		public override string Xep => Extension.Omemo;

		/// <summary>
		/// Determines whether the connected server supports PEP.
		/// </summary>
		public bool Supported => pep.Supported;

		/// <summary>
		/// The event that is raised when a contact publishes an OMEMO device list.
		/// </summary>
		public event EventHandler<OmemoDeviceListChangedEventArgs> DeviceListChanged;

		/// <summary>
		/// Invoked after all extensions have been loaded.
		/// </summary>
		public override void Initialize() {
			pep = im.GetExtension<Pep>();
			pep.Subscribe(DeviceListNode, OnDeviceListChanged);
		}

		/// <summary>
		/// Publishes the local OMEMO device list through PEP.
		/// </summary>
		public void PublishDeviceList(OmemoDeviceList deviceList) {
			deviceList.ThrowIfNull("deviceList");
			pep.Publish(DeviceListNode, CurrentItemId,
				new Dictionary<string, string> {
					{ "pubsub#access_model", "open" }
				},
				deviceList.ToXmlElement());
		}

		/// <summary>
		/// Retrieves an OMEMO device list for the specified XMPP entity.
		/// </summary>
		public OmemoDeviceList RetrieveDeviceList(Jid jid) {
			jid.ThrowIfNull("jid");
			XmlElement item = pep.RetrieveItem(jid, DeviceListNode, CurrentItemId);
			return OmemoDeviceList.Parse(RequiredPayload(item, "devices"));
		}

		/// <summary>
		/// Publishes the local OMEMO bundle for the specified device id.
		/// </summary>
		public void PublishBundle(uint deviceId, OmemoBundle bundle) {
			OmemoDevice.ValidateDeviceId(deviceId, "deviceId");
			bundle.ThrowIfNull("bundle");
			pep.Publish(BundleNode, deviceId.ToString(CultureInfo.InvariantCulture),
				new Dictionary<string, string> {
					{ "pubsub#max_items", "max" },
					{ "pubsub#access_model", "open" }
				},
				bundle.ToXmlElement());
		}

		/// <summary>
		/// Retrieves an OMEMO bundle for the specified XMPP entity and device id.
		/// </summary>
		public OmemoBundle RetrieveBundle(Jid jid, uint deviceId) {
			OmemoDevice.ValidateDeviceId(deviceId, "deviceId");
			jid.ThrowIfNull("jid");
			XmlElement item = pep.RetrieveItem(jid, BundleNode,
				deviceId.ToString(CultureInfo.InvariantCulture));
			return OmemoBundle.Parse(RequiredPayload(item, "bundle"));
		}

		/// <summary>
		/// Returns the OMEMO bundle PEP node for the specified device id.
		/// </summary>
		public static string GetBundleNode(uint deviceId) {
			OmemoDevice.ValidateDeviceId(deviceId, "deviceId");
			return BundleNode;
		}

		/// <summary>
		/// Initializes a new instance of the Omemo class.
		/// </summary>
		public Omemo(XmppIm im)
			: base(im) {
		}

		void OnDeviceListChanged(Jid jid, XmlElement item) {
			if (item == null)
				return;
			XmlElement list = Payload(item, "devices");
			if (list == null)
				return;
			DeviceListChanged.Raise(this,
				new OmemoDeviceListChangedEventArgs(jid, OmemoDeviceList.Parse(list)));
		}

		static XmlElement RequiredPayload(XmlElement item, string localName) {
			XmlElement payload = Payload(item, localName);
			if (payload == null)
				throw new XmppException("Expected OMEMO '" + localName + "' payload.");
			return payload;
		}

		static XmlElement Payload(XmlElement item, string localName) {
			foreach (XmlNode node in item.ChildNodes) {
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
