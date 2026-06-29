namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-224 authentication mechanism.
	/// </summary>
	internal class SaslScramSha224 : SaslScram {
		private SaslScramSha224()
			: base("SCRAM-SHA-224", 28, Sha224.ComputeHmac, Sha224.HashData) {
		}

		internal SaslScramSha224(string username, string password, string cnonce)
			: base("SCRAM-SHA-224", 28, Sha224.ComputeHmac, Sha224.HashData,
				username, password, cnonce) {
		}

		public SaslScramSha224(string username, string password)
			: base("SCRAM-SHA-224", 28, Sha224.ComputeHmac, Sha224.HashData,
				username, password) {
		}
	}
}
