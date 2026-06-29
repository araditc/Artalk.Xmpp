using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Artalk.Xmpp.Extensions.Upnp {
	/// <summary>
	/// Provides a platform-neutral placeholder for UPnP port-forwarding.
	/// </summary>
	/// <remarks>
	/// The original implementation depended on the Windows UPnP COM component.
	/// The .NET 10 package keeps the public file-transfer setting but disables
	/// automatic UPnP discovery and port mapping in the cross-platform build.
	/// </remarks>
	internal static class UPnP {
		/// <summary>
		/// Retrieves an enumerable collection of external IP addresses from
		/// UPnP-enabled routers.
		/// </summary>
		/// <returns>An empty collection.</returns>
		public static IEnumerable<IPAddress> GetExternalAddresses() {
			return Array.Empty<IPAddress>();
		}

		/// <summary>
		/// Forwards the specified port, optionally for the specified protocol only.
		/// </summary>
		/// <param name="port">The port to forward.</param>
		/// <param name="protocol">If specified, must be either ProtocolType.Tcp or
		/// ProtocolType.Udp.</param>
		/// <param name="description">A description of the port-forwarding rule.</param>
		public static void ForwardPort(int port,
			ProtocolType? protocol = null, string description = null) {
			port.ThrowIfOutOfRange("port", 0, 65535);
			if (protocol.HasValue && protocol != ProtocolType.Tcp &&
				protocol != ProtocolType.Udp) {
				throw new ArgumentException("The protocol parameter must be either " +
					"ProtocolType.Tcp or ProtocolType.Udp.", "protocol");
			}
		}

		/// <summary>
		/// Removes existing port-mappings of the specified port.
		/// </summary>
		/// <param name="port">The port to unforward.</param>
		/// <param name="protocol">If specified, must be either ProtocolType.Tcp or
		/// ProtocolType.Udp.</param>
		public static void UnforwardPort(int port, ProtocolType? protocol = null) {
			port.ThrowIfOutOfRange("port", 0, 65535);
			if (protocol.HasValue && protocol != ProtocolType.Tcp &&
				protocol != ProtocolType.Udp) {
				throw new ArgumentException("The protocol parameter must be either " +
					"ProtocolType.Tcp or ProtocolType.Udp.", "protocol");
			}
		}
	}
}
