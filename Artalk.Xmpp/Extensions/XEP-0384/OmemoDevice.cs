using System;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents a device entry in an XEP-0384 OMEMO device list.
	/// </summary>
	public class OmemoDevice {
		/// <summary>
		/// The OMEMO device id.
		/// </summary>
		public uint Id {
			get;
		}

		/// <summary>
		/// An optional user-visible device label.
		/// </summary>
		public string Label {
			get;
		}

		/// <summary>
		/// An optional signature for the device label.
		/// </summary>
		public byte[] LabelSignature {
			get {
				return labelSignature == null ? null : (byte[]) labelSignature.Clone();
			}
		}

		readonly byte[] labelSignature;

		/// <summary>
		/// Initializes a new instance of the OmemoDevice class.
		/// </summary>
		public OmemoDevice(uint id, string label = null, byte[] labelSignature = null) {
			ValidateDeviceId(id, "id");
			Id = id;
			Label = label;
			this.labelSignature = labelSignature == null ?
				null : (byte[]) labelSignature.Clone();
		}

		internal static void ValidateDeviceId(uint id, string paramName) {
			if (id == 0 || id > Int32.MaxValue)
				throw new ArgumentOutOfRangeException(paramName,
					"OMEMO device ids must be between 1 and 2147483647.");
		}
	}
}
