using Artalk.Xmpp.Core.Sasl.Mechanisms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Artalk.Xmpp.Core.Sasl {
	internal static class SaslScramDowngradeProtection {
		public const string PropertyName = "DowngradeProtectionHash";

		public static string Compute(string mechanism,
			IEnumerable<string> advertisedMechanisms,
			IEnumerable<string> advertisedChannelBindingTypes) {
			mechanism.ThrowIfNullOrEmpty("mechanism");
			advertisedMechanisms.ThrowIfNull("advertisedMechanisms");
			Func<byte[], byte[]> hash = GetHash(mechanism);
			if (hash == null)
				return null;
			string value = String.Join(((char) 0x1e).ToString(),
				advertisedMechanisms
					.Where(item => !String.IsNullOrWhiteSpace(item))
					.OrderBy(item => item, StringComparer.Ordinal));
			if (advertisedChannelBindingTypes != null) {
				value += (char) 0x1f;
				value += String.Join(((char) 0x1e).ToString(),
					advertisedChannelBindingTypes
						.Where(item => !String.IsNullOrWhiteSpace(item))
						.OrderBy(item => item, StringComparer.Ordinal));
			}
			return Convert.ToBase64String(hash(Encoding.UTF8.GetBytes(value)));
		}

		static Func<byte[], byte[]> GetHash(string mechanism) {
			string name = mechanism.EndsWith("-PLUS",
				StringComparison.InvariantCultureIgnoreCase) ?
				mechanism.Substring(0, mechanism.Length - 5) : mechanism;
			switch (name.ToUpperInvariant()) {
				case "SCRAM-SHA-1":
					return SHA1.HashData;
				case "SCRAM-SHA-224":
					return Sha224.HashData;
				case "SCRAM-SHA-256":
					return SHA256.HashData;
				case "SCRAM-SHA-384":
					return SHA384.HashData;
				case "SCRAM-SHA-512":
					return SHA512.HashData;
				case "SCRAM-SHA3-512":
					return SHA3_512.HashData;
				default:
					return null;
			}
		}
	}
}
