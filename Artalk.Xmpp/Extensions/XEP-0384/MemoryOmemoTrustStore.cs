using System;
using System.Collections.Generic;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// An in-memory OMEMO trust store suitable for tests and short-lived clients.
	/// </summary>
	public class MemoryOmemoTrustStore : IOmemoTrustStore {
		readonly Dictionary<string, TrustEntry> entries =
			new Dictionary<string, TrustEntry>();

		/// <summary>
		/// Gets the trust level for the specified device identity.
		/// </summary>
		public OmemoTrustLevel GetTrust(Jid jid, uint deviceId, byte[] identityKey) {
			jid.ThrowIfNull("jid");
			OmemoDevice.ValidateDeviceId(deviceId, "deviceId");
			identityKey.ThrowIfNull("identityKey");
			string key = StoreKey(jid, deviceId);
			if (!entries.TryGetValue(key, out TrustEntry entry))
				return OmemoTrustLevel.Undecided;
			string fingerprint = GetFingerprint(identityKey);
			return String.Equals(entry.Fingerprint, fingerprint,
				StringComparison.OrdinalIgnoreCase) ?
				entry.TrustLevel : OmemoTrustLevel.Untrusted;
		}

		/// <summary>
		/// Stores the trust level for the specified device identity.
		/// </summary>
		public void SetTrust(Jid jid, uint deviceId, byte[] identityKey,
			OmemoTrustLevel trustLevel) {
			jid.ThrowIfNull("jid");
			OmemoDevice.ValidateDeviceId(deviceId, "deviceId");
			identityKey.ThrowIfNull("identityKey");
			entries[StoreKey(jid, deviceId)] = new TrustEntry(
				GetFingerprint(identityKey), trustLevel);
		}

		/// <summary>
		/// Returns the lowercase hexadecimal fingerprint for an identity key.
		/// </summary>
		public static string GetFingerprint(byte[] identityKey) {
			identityKey.ThrowIfNull("identityKey");
			return Convert.ToHexString(identityKey).ToLowerInvariant();
		}

		static string StoreKey(Jid jid, uint deviceId) {
			return jid.GetBareJid().ToString().ToLowerInvariant() + "/" + deviceId;
		}

		sealed class TrustEntry {
			public string Fingerprint {
				get;
			}

			public OmemoTrustLevel TrustLevel {
				get;
			}

			public TrustEntry(string fingerprint, OmemoTrustLevel trustLevel) {
				Fingerprint = fingerprint;
				TrustLevel = trustLevel;
			}
		}
	}
}
