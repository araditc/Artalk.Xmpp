using System.Security.Authentication;

namespace Artalk.Xmpp.Core.Sasl {
	internal static class SaslTlsDowngradeProtection {
		public const string PropertyName = "TlsVersionDowngradeProtection";

		public static string Encode(SslProtocols protocol) {
			int value = (int) protocol;
			if (value == 192)
				return "0301";
			if (value == 768)
				return "0302";
			if (protocol == SslProtocols.Tls12)
				return "0303";
			if (protocol == SslProtocols.Tls13)
				return "0304";
			return null;
		}
	}
}
