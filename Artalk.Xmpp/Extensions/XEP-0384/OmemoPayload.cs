using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents an OMEMO encrypted payload and the payload key material used by
	/// the Double Ratchet layer.
	/// </summary>
	public class OmemoPayload {
		const int PayloadKeySize = 32;
		const int AuthenticationTagSize = 16;
		static readonly byte[] PayloadInfo =
			Encoding.ASCII.GetBytes("OMEMO Payload");

		/// <summary>
		/// The random 32 byte OMEMO payload key.
		/// </summary>
		public byte[] Key {
			get {
				return (byte[]) key.Clone();
			}
		}

		/// <summary>
		/// The truncated 16 byte HMAC-SHA-256 authentication tag.
		/// </summary>
		public byte[] AuthenticationTag {
			get {
				return (byte[]) authenticationTag.Clone();
			}
		}

		/// <summary>
		/// The AES-256-CBC encrypted payload.
		/// </summary>
		public byte[] Ciphertext {
			get {
				return (byte[]) ciphertext.Clone();
			}
		}

		/// <summary>
		/// The 48 byte key material that must be encrypted for each recipient device.
		/// </summary>
		public byte[] KeyMaterial {
			get {
				return key.Concat(authenticationTag).ToArray();
			}
		}

		readonly byte[] key;
		readonly byte[] authenticationTag;
		readonly byte[] ciphertext;

		/// <summary>
		/// Initializes a new instance of the OmemoPayload class.
		/// </summary>
		public OmemoPayload(byte[] key, byte[] authenticationTag, byte[] ciphertext) {
			ValidateKey(key, "key");
			authenticationTag.ThrowIfNull("authenticationTag");
			if (authenticationTag.Length != AuthenticationTagSize)
				throw new ArgumentException("OMEMO authentication tags must be 16 bytes.",
					"authenticationTag");
			ciphertext.ThrowIfNull("ciphertext");
			this.key = (byte[]) key.Clone();
			this.authenticationTag = (byte[]) authenticationTag.Clone();
			this.ciphertext = (byte[]) ciphertext.Clone();
		}

		/// <summary>
		/// Encrypts plaintext into an OMEMO payload.
		/// </summary>
		public static OmemoPayload Encrypt(byte[] plaintext, byte[] key = null) {
			plaintext.ThrowIfNull("plaintext");
			byte[] payloadKey = key == null ?
				RandomNumberGenerator.GetBytes(PayloadKeySize) : (byte[]) key.Clone();
			ValidateKey(payloadKey, "key");

			DerivePayloadKeys(payloadKey, out byte[] encryptionKey,
				out byte[] authenticationKey, out byte[] iv);
			byte[] ciphertext = EncryptAesCbc(plaintext, encryptionKey, iv);
			byte[] authenticationTag = Authenticate(authenticationKey, ciphertext);
			return new OmemoPayload(payloadKey, authenticationTag, ciphertext);
		}

		/// <summary>
		/// Creates an OMEMO payload from decrypted 48 byte key material.
		/// </summary>
		public static OmemoPayload FromKeyMaterial(byte[] keyMaterial,
			byte[] ciphertext) {
			keyMaterial.ThrowIfNull("keyMaterial");
			if (keyMaterial.Length != PayloadKeySize + AuthenticationTagSize) {
				throw new ArgumentException(
					"OMEMO payload key material must be 48 bytes.", "keyMaterial");
			}
			return new OmemoPayload(keyMaterial.Take(PayloadKeySize).ToArray(),
				keyMaterial.Skip(PayloadKeySize).Take(AuthenticationTagSize).ToArray(),
				ciphertext);
		}

		/// <summary>
		/// Decrypts and authenticates the OMEMO payload.
		/// </summary>
		public byte[] Decrypt() {
			DerivePayloadKeys(key, out byte[] encryptionKey,
				out byte[] authenticationKey, out byte[] iv);
			byte[] expectedTag = Authenticate(authenticationKey, ciphertext);
			if (!CryptographicOperations.FixedTimeEquals(expectedTag,
				authenticationTag)) {
				throw new CryptographicException(
					"The OMEMO payload authentication tag is invalid.");
			}
			return DecryptAesCbc(ciphertext, encryptionKey, iv);
		}

		static void DerivePayloadKeys(byte[] payloadKey, out byte[] encryptionKey,
			out byte[] authenticationKey, out byte[] iv) {
			byte[] material = HkdfSha256(payloadKey, new byte[32], PayloadInfo, 80);
			encryptionKey = material.Take(32).ToArray();
			authenticationKey = material.Skip(32).Take(32).ToArray();
			iv = material.Skip(64).Take(16).ToArray();
		}

		static byte[] Authenticate(byte[] authenticationKey, byte[] ciphertext) {
			using var hmac = new HMACSHA256(authenticationKey);
			return hmac.ComputeHash(ciphertext).Take(AuthenticationTagSize).ToArray();
		}

		static byte[] EncryptAesCbc(byte[] plaintext, byte[] encryptionKey,
			byte[] iv) {
			using Aes aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			aes.Key = encryptionKey;
			aes.IV = iv;
			using ICryptoTransform encryptor = aes.CreateEncryptor();
			return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
		}

		static byte[] DecryptAesCbc(byte[] ciphertext, byte[] encryptionKey,
			byte[] iv) {
			using Aes aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			aes.Key = encryptionKey;
			aes.IV = iv;
			using ICryptoTransform decryptor = aes.CreateDecryptor();
			return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
		}

		static byte[] HkdfSha256(byte[] inputKeyMaterial, byte[] salt,
			byte[] info, int length) {
			using var extract = new HMACSHA256(salt);
			byte[] pseudoRandomKey = extract.ComputeHash(inputKeyMaterial);
			var output = new byte[length];
			byte[] previous = Array.Empty<byte>();
			int offset = 0;
			byte counter = 1;

			while (offset < length) {
				using var expand = new HMACSHA256(pseudoRandomKey);
				byte[] input = previous.Concat(info).Concat(new[] { counter }).ToArray();
				previous = expand.ComputeHash(input);
				int count = Math.Min(previous.Length, length - offset);
				Buffer.BlockCopy(previous, 0, output, offset, count);
				offset += count;
				counter++;
			}
			return output;
		}

		static void ValidateKey(byte[] key, string paramName) {
			key.ThrowIfNull(paramName);
			if (key.Length != PayloadKeySize)
				throw new ArgumentException("OMEMO payload keys must be 32 bytes.",
					paramName);
		}
	}
}
