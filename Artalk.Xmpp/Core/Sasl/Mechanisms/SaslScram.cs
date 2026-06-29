using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the shared SCRAM authentication flow described in RFC 5802.
	/// </summary>
	internal abstract class SaslScram : SaslMechanism {
		readonly string mechanismName;
		readonly HashAlgorithmName hashAlgorithm;
		readonly int hashLength;
		readonly Func<byte[], HMAC> hmacFactory;

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

		protected SaslScram(string mechanismName, HashAlgorithmName hashAlgorithm,
			int hashLength, Func<byte[], HMAC> hmacFactory) {
			mechanismName.ThrowIfNullOrEmpty("mechanismName");
			hmacFactory.ThrowIfNull("hmacFactory");
			this.mechanismName = mechanismName;
			this.hashAlgorithm = hashAlgorithm;
			this.hashLength = hashLength;
			this.hmacFactory = hmacFactory;
		}

		protected SaslScram(string mechanismName, HashAlgorithmName hashAlgorithm,
			int hashLength, Func<byte[], HMAC> hmacFactory, string username,
			string password) : this(mechanismName, hashAlgorithm, hashLength,
			hmacFactory) {
			SetCredentials(username, password);
		}

		protected SaslScram(string mechanismName, HashAlgorithmName hashAlgorithm,
			int hashLength, Func<byte[], HMAC> hmacFactory, string username,
			string password, string cnonce) : this(mechanismName, hashAlgorithm,
			hashLength, hmacFactory, username, password) {
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
			return Encoding.UTF8.GetBytes("n,,n=" + SaslPrep(Username) + ",r=" +
				cnonce);
		}

		byte[] ComputeFinalResponse(byte[] challenge) {
			NameValueCollection nv = ParseServerFirstMessage(challenge);
			string salt = nv["s"], nonce = nv["r"];
			int iterationCount = Int32.Parse(nv["i"]);
			if (!VerifyServerNonce(nonce))
				throw new SaslException("Invalid server nonce: " + nonce);

			string clientFirstBare = "n=" + SaslPrep(Username) + ",r=" + cnonce,
				serverFirstMessage = Encoding.UTF8.GetString(challenge),
				withoutProof = "c=" +
				Convert.ToBase64String(Encoding.UTF8.GetBytes("n,,")) + ",r=" +
				nonce;
			authMessage = clientFirstBare + "," + serverFirstMessage + "," +
				withoutProof;
			saltedPassword = Hi(Password, salt, iterationCount);
			byte[] clientKey = ComputeHmac(saltedPassword, "Client Key"),
				storedKey = ComputeHash(clientKey),
				clientSignature = ComputeHmac(storedKey, authMessage),
				clientProof = Xor(clientKey, clientSignature);
			return Encoding.UTF8.GetBytes(withoutProof + ",p=" +
				Convert.ToBase64String(clientProof));
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

		byte[] Hi(string password, string salt, int count) {
			byte[] saltBytes = Convert.FromBase64String(salt);
			return Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, count,
				hashAlgorithm, hashLength);
		}

		byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = hmacFactory(key)) {
				return hmac.ComputeHash(data);
			}
		}

		byte[] ComputeHmac(byte[] key, string data) {
			return ComputeHmac(key, Encoding.UTF8.GetBytes(data));
		}

		byte[] ComputeHash(byte[] data) {
			if (hashAlgorithm == HashAlgorithmName.SHA1)
				return SHA1.HashData(data);
			if (hashAlgorithm == HashAlgorithmName.SHA256)
				return SHA256.HashData(data);
			throw new NotSupportedException("Unsupported SCRAM hash algorithm: " +
				hashAlgorithm.Name);
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
