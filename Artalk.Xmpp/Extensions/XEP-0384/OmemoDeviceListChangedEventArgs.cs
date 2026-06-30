using System;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Provides data for an OMEMO device list update event.
	/// </summary>
	public class OmemoDeviceListChangedEventArgs : EventArgs {
		/// <summary>
		/// The JID of the XMPP entity that published the device list.
		/// </summary>
		public Jid Jid {
			get;
		}

		/// <summary>
		/// The published OMEMO device list.
		/// </summary>
		public OmemoDeviceList DeviceList {
			get;
		}

		/// <summary>
		/// Initializes a new instance of the OmemoDeviceListChangedEventArgs class.
		/// </summary>
		public OmemoDeviceListChangedEventArgs(Jid jid, OmemoDeviceList deviceList) {
			jid.ThrowIfNull("jid");
			deviceList.ThrowIfNull("deviceList");
			Jid = jid;
			DeviceList = deviceList;
		}
	}
}
