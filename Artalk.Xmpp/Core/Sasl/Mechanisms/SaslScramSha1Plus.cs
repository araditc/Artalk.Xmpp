using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-1-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha1Plus : SaslScram {
		private SaslScramSha1Plus()
			: base("SCRAM-SHA-1-PLUS", 20, ComputeHmac, SHA1.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha1Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA-1-PLUS", 20, ComputeHmac, SHA1.HashData, username,
				password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA1(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
