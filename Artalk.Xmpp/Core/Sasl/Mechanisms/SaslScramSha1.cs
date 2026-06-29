using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-1 authentication mechanism.
	/// </summary>
	internal class SaslScramSha1 : SaslScram {
		private SaslScramSha1()
			: base("SCRAM-SHA-1", HashAlgorithmName.SHA1, 20,
				key => new HMACSHA1(key)) {
		}

		internal SaslScramSha1(string username, string password, string cnonce)
			: base("SCRAM-SHA-1", HashAlgorithmName.SHA1, 20,
				key => new HMACSHA1(key), username, password, cnonce) {
		}

		public SaslScramSha1(string username, string password)
			: base("SCRAM-SHA-1", HashAlgorithmName.SHA1, 20,
				key => new HMACSHA1(key), username, password) {
		}
	}
}
