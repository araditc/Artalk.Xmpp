using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Artalk.Xmpp.Core {
	internal static class ChannelBinding {
		public const string TlsServerEndPoint = "tls-server-end-point";

		public static byte[] CreateTlsServerEndPoint(X509Certificate certificate) {
			certificate.ThrowIfNull("certificate");
			var certificate2 = new X509Certificate2(certificate);
			byte[] rawData = certificate2.RawData;
			using (HashAlgorithm hash = CreateHash(certificate2.SignatureAlgorithm.Value)) {
				return hash.ComputeHash(rawData);
			}
		}

		static HashAlgorithm CreateHash(string signatureAlgorithmOid) {
			switch (signatureAlgorithmOid) {
				case "1.2.840.113549.1.1.4":
				case "1.2.840.113549.1.1.5":
				case "1.2.840.10040.4.3":
				case "1.3.14.3.2.29":
					return SHA256.Create();
				case "1.2.840.113549.1.1.11":
				case "1.2.840.10045.4.3.2":
				case "2.16.840.1.101.3.4.3.2":
					return SHA256.Create();
				case "1.2.840.113549.1.1.12":
				case "1.2.840.10045.4.3.3":
				case "2.16.840.1.101.3.4.3.3":
					return SHA384.Create();
				case "1.2.840.113549.1.1.13":
				case "1.2.840.10045.4.3.4":
				case "2.16.840.1.101.3.4.3.4":
					return SHA512.Create();
				default:
					return SHA256.Create();
			}
		}
	}
}
