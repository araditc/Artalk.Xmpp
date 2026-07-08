using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the shared SCRAM authentication flow described in RFC 5802.
	/// </summary>
	internal abstract class SaslScram : SaslMechanism {
		readonly string mechanismName;
		readonly int hashLength;
		readonly Func<byte[], byte[], byte[]> hmac;
		readonly Func<byte[], byte[]> hash;
		readonly string channelBindingName;

		bool completed;
		string cnonce = GenerateCnonce();
		int step;
		byte[] saltedPassword;
		string authMessage;

		/// <inheritdoc/>
		public override bool IsCompleted {
			get {
				return completed;
			}
		}

		/// <inheritdoc/>
		public override bool HasInitial {
			get {
				return true;
			}
		}

		/// <inheritdoc/>
		public override string Name {
			get {
				return mechanismName;
			}
		}

		string Username {
			get {
				return Properties.ContainsKey("Username") ?
					Properties["Username"] as string : null;
			}
			set {
				Properties["Username"] = value;
			}
		}

		string Password {
			get {
				return Properties.ContainsKey("Password") ?
					Properties["Password"] as string : null;
			}
			set {
				Properties["Password"] = value;
			}
		}

		protected SaslScram(string mechanismName, int hashLength,
			Func<byte[], byte[], byte[]> hmac, Func<byte[], byte[]> hash,
			string channelBindingName = null) {
			mechanismName.ThrowIfNullOrEmpty("mechanismName");
			hmac.ThrowIfNull("hmac");
			hash.ThrowIfNull("hash");
			this.mechanismName = mechanismName;
			this.hashLength = hashLength;
			this.hmac = hmac;
			this.hash = hash;
			this.channelBindingName = channelBindingName;
		}

		protected SaslScram(string mechanismName, int hashLength,
			Func<byte[], byte[], byte[]> hmac, Func<byte[], byte[]> hash,
			string username, string password) : this(mechanismName, hashLength,
			hmac, hash) {
			SetCredentials(username, password);
		}

		protected SaslScram(string mechanismName, int hashLength,
			Func<byte[], byte[], byte[]> hmac, Func<byte[], byte[]> hash,
			string username, string password, string cnonce) : this(mechanismName,
			hashLength, hmac, hash, username, password) {
			cnonce.ThrowIfNullOrEmpty("cnonce");
			this.cnonce = cnonce;
		}

		protected SaslScram(string mechanismName, int hashLength,
			Func<byte[], byte[], byte[]> hmac, Func<byte[], byte[]> hash,
			string username, string password, string cnonce,
			string channelBindingName) : this(mechanismName, hashLength, hmac,
			hash, channelBindingName) {
			SetCredentials(username, password);
			cnonce.ThrowIfNullOrEmpty("cnonce");
			this.cnonce = cnonce;
		}

		protected void SetCredentials(string username, string password) {
			username.ThrowIfNull("username");
			if (username == String.Empty)
				throw new ArgumentException("The username must not be empty.");
			password.ThrowIfNull("password");

			Username = username;
			Password = password;
		}

		/// <inheritdoc/>
		protected override byte[] ComputeResponse(byte[] challenge) {
			if (String.IsNullOrEmpty(Username) || Password == null) {
				throw new SaslException("The username must not be null or empty and " +
					"the password must not be null.");
			}
			if (step == 2)
				completed = true;
			byte[] ret = step == 0 ? ComputeInitialResponse() :
				(step == 1 ? ComputeFinalResponse(challenge) :
				VerifyServerSignature(challenge));
			step = step + 1;
			return ret;
		}

		byte[] ComputeInitialResponse() {
			return Encoding.UTF8.GetBytes(GetGs2Header() + "n=" + SaslPrep(Username) + ",r=" +
				cnonce);
		}

		byte[] ComputeFinalResponse(byte[] challenge) {
			NameValueCollection nv = ParseServerFirstMessage(challenge);
			string salt = nv["s"], nonce = nv["r"];
			int iterationCount = Int32.Parse(nv["i"]);
			if (!VerifyServerNonce(nonce))
				throw new SaslException("Invalid server nonce: " + nonce);
			VerifyDowngradeProtectionHash(nv["h"]);
			VerifyTlsDowngradeProtection(nv["t"]);

			string clientFirstBare = "n=" + SaslPrep(Username) + ",r=" + cnonce,
				serverFirstMessage = Encoding.UTF8.GetString(challenge),
				withoutProof = "c=" + Convert.ToBase64String(GetCbindInput()) + ",r=" +
				nonce;
			authMessage = clientFirstBare + "," + serverFirstMessage + "," +
				withoutProof;
			saltedPassword = ComputeSaltedPassword(Password, salt, iterationCount,
				hashLength, ComputeHmac);
			byte[] clientKey = ComputeHmac(saltedPassword, "Client Key"),
				storedKey = ComputeHash(clientKey),
				clientSignature = ComputeHmac(storedKey, authMessage),
				clientProof = Xor(clientKey, clientSignature);
			return Encoding.UTF8.GetBytes(withoutProof + ",p=" +
				Convert.ToBase64String(clientProof));
		}

		void VerifyDowngradeProtectionHash(string serverHash) {
			if (String.IsNullOrEmpty(serverHash))
				return;
			if (!Properties.TryGetValue(SaslScramDowngradeProtection.PropertyName,
				out object expectedValue) || expectedValue is not string expectedHash ||
				String.IsNullOrEmpty(expectedHash)) {
				throw new SaslException("SCRAM downgrade-protection hash was not " +
					"available.");
			}
			if (!String.Equals(serverHash, expectedHash, StringComparison.Ordinal))
				throw new SaslException("SCRAM downgrade-protection hash mismatch.");
		}

		void VerifyTlsDowngradeProtection(string serverTlsVersion) {
			if (String.IsNullOrEmpty(serverTlsVersion))
				return;
			if (!Properties.TryGetValue(SaslTlsDowngradeProtection.PropertyName,
				out object expectedValue) || expectedValue is not string expectedVersion ||
				String.IsNullOrEmpty(expectedVersion)) {
				throw new SaslException("SCRAM TLS downgrade-protection value was " +
					"not available.");
			}
			if (!String.Equals(serverTlsVersion, expectedVersion,
				StringComparison.Ordinal)) {
				throw new SaslException("SCRAM TLS downgrade-protection mismatch.");
			}
		}

		bool VerifyServerNonce(string nonce) {
			return nonce.StartsWith(cnonce);
		}

		byte[] VerifyServerSignature(byte[] challenge) {
			string s = Encoding.UTF8.GetString(challenge);
			if (!s.StartsWith("v="))
				return Encoding.UTF8.GetBytes("*");

			byte[] serverSignature = Convert.FromBase64String(s.Substring(2));
			byte[] serverKey = ComputeHmac(saltedPassword, "Server Key"),
				calculatedSignature = ComputeHmac(serverKey, authMessage);
			return serverSignature.SequenceEqual(calculatedSignature) ?
				new byte[0] : Encoding.UTF8.GetBytes("*");
		}

		NameValueCollection ParseServerFirstMessage(byte[] challenge) {
			challenge.ThrowIfNull("challenge");
			string message = Encoding.UTF8.GetString(challenge);
			NameValueCollection coll = new NameValueCollection();
			foreach (string s in message.Split(',')) {
				int delimiter = s.IndexOf('=');
				if (delimiter < 0)
					continue;
				string name = s.Substring(0, delimiter), value =
					s.Substring(delimiter + 1);
				coll.Add(name, value);
			}
			return coll;
		}

		internal static byte[] ComputeSaltedPassword(string password, string salt,
			int count, int hashLength, Func<byte[], byte[], byte[]> hmac) {
			password.ThrowIfNull("password");
			salt.ThrowIfNull("salt");
			hmac.ThrowIfNull("hmac");
			byte[] saltBytes = Convert.FromBase64String(salt);
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
			byte[] block = new byte[saltBytes.Length + 4];
			Buffer.BlockCopy(saltBytes, 0, block, 0, saltBytes.Length);
			block[block.Length - 1] = 1;

			byte[] u = hmac(passwordBytes, block);
			byte[] output = (byte[]) u.Clone();
			for (int i = 1; i < count; i++) {
				u = hmac(passwordBytes, u);
				for (int j = 0; j < output.Length; j++)
					output[j] = (byte) (output[j] ^ u[j]);
			}
			if (output.Length == hashLength)
				return output;
			return output.Take(hashLength).ToArray();
		}

		byte[] ComputeHmac(byte[] key, byte[] data) {
			return hmac(key, data);
		}

		byte[] ComputeHmac(byte[] key, string data) {
			return ComputeHmac(key, Encoding.UTF8.GetBytes(data));
		}

		byte[] ComputeHash(byte[] data) {
			return hash(data);
		}

		string GetGs2Header() {
			return channelBindingName == null ? "n,," : "p=" + channelBindingName + ",,";
		}

		byte[] GetCbindInput() {
			byte[] gs2Header = Encoding.UTF8.GetBytes(GetGs2Header());
			if (channelBindingName == null)
				return gs2Header;
			byte[] channelBindingData = GetChannelBindingData();
			byte[] cbind = new byte[gs2Header.Length + channelBindingData.Length];
			Buffer.BlockCopy(gs2Header, 0, cbind, 0, gs2Header.Length);
			Buffer.BlockCopy(channelBindingData, 0, cbind, gs2Header.Length,
				channelBindingData.Length);
			return cbind;
		}

		byte[] GetChannelBindingData() {
			object data;
			if (!Properties.TryGetValue("ChannelBindingData", out data))
				throw new SaslException("SCRAM-PLUS requires channel binding data.");
			byte[] bytes = data as byte[];
			if (bytes == null || bytes.Length == 0)
				throw new SaslException("SCRAM-PLUS requires channel binding data.");
			return bytes;
		}

		byte[] Xor(byte[] a, byte[] b) {
			a.ThrowIfNull("a");
			b.ThrowIfNull("b");
			if (a.Length != b.Length)
				throw new ArgumentException();
			byte[] ret = new byte[a.Length];
			for (int i = 0; i < a.Length; i++)
				ret[i] = (byte)(a[i] ^ b[i]);
			return ret;
		}

		static string GenerateCnonce() {
			return Guid.NewGuid().ToString("N").Substring(0, 16);
		}

		static string SaslPrep(string s) {
			return s
				.Replace("=", "=3D")
				.Replace(",", "=2C");
		}
	}
}
