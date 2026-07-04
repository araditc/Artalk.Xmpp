using System;
using System.Security.Cryptography;
using System.Text;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Represents the XEP-0454 aesgcm media URI that carries the encrypted media
	/// download URL and symmetric key material.
	/// </summary>
	public sealed class OmemoMediaUri {
		/// <summary>
		/// The aesgcm URI scheme used by XEP-0454.
		/// </summary>
		public const string Scheme = "aesgcm";

		/// <summary>
		/// The required AES-256-GCM key size.
		/// </summary>
		public const int KeySize = 32;

		/// <summary>
		/// The required AES-GCM IV size.
		/// </summary>
		public const int IvSize = 12;

		/// <summary>
		/// The AES-GCM authentication tag size appended to encrypted media.
		/// </summary>
		public const int AuthenticationTagSize = 16;

		const int FragmentHexLength = (IvSize + KeySize) * 2;

		/// <summary>
		/// The HTTPS download URL for the encrypted media.
		/// </summary>
		public Uri HttpsUrl {
			get;
		}

		/// <summary>
		/// The 12 byte AES-GCM IV.
		/// </summary>
		public byte[] Iv {
			get {
				return (byte[]) iv.Clone();
			}
		}

		/// <summary>
		/// The 32 byte AES-GCM key.
		/// </summary>
		public byte[] Key {
			get {
				return (byte[]) key.Clone();
			}
		}

		readonly byte[] iv;
		readonly byte[] key;

		/// <summary>
		/// Initializes a new instance of the OmemoMediaUri class.
		/// </summary>
		public OmemoMediaUri(Uri httpsUrl, byte[] iv, byte[] key) {
			ValidateHttpsUrl(httpsUrl);
			ValidateBytes(iv, IvSize, "iv");
			ValidateBytes(key, KeySize, "key");
			HttpsUrl = RemoveFragment(httpsUrl);
			this.iv = (byte[]) iv.Clone();
			this.key = (byte[]) key.Clone();
		}

		/// <summary>
		/// Creates an aesgcm media URI for an HTTPS download URL.
		/// </summary>
		public static OmemoMediaUri Create(Uri httpsUrl, byte[] iv = null,
			byte[] key = null) {
			return new OmemoMediaUri(httpsUrl,
				iv == null ? RandomNumberGenerator.GetBytes(IvSize) : iv,
				key == null ? RandomNumberGenerator.GetBytes(KeySize) : key);
		}

		/// <summary>
		/// Parses an aesgcm media URI.
		/// </summary>
		public static OmemoMediaUri Parse(string value) {
			if (!TryParse(value, out OmemoMediaUri uri))
				throw new ArgumentException("The value is not a valid XEP-0454 aesgcm URI.",
					"value");
			return uri;
		}

		/// <summary>
		/// Attempts to parse an aesgcm media URI.
		/// </summary>
		public static bool TryParse(string value, out OmemoMediaUri mediaUri) {
			mediaUri = null;
			if (String.IsNullOrWhiteSpace(value) ||
				value.IndexOfAny(new[] { '\r', '\n' }) >= 0 ||
				!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
				!String.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase) ||
				String.IsNullOrEmpty(uri.Host) ||
				!String.IsNullOrEmpty(uri.UserInfo)) {
				return false;
			}

			string fragment = uri.Fragment;
			if (fragment.StartsWith("#", StringComparison.Ordinal))
				fragment = fragment.Substring(1);
			if (fragment.Length != FragmentHexLength || !IsHex(fragment))
				return false;

			byte[] material;
			try {
				material = Convert.FromHexString(fragment);
			} catch (FormatException) {
				return false;
			}

			byte[] iv = new byte[IvSize];
			byte[] key = new byte[KeySize];
			Buffer.BlockCopy(material, 0, iv, 0, iv.Length);
			Buffer.BlockCopy(material, iv.Length, key, 0, key.Length);

			try {
				mediaUri = new OmemoMediaUri(ToHttpsUrl(uri), iv, key);
				return true;
			} catch (ArgumentException) {
				return false;
			}
		}

		/// <summary>
		/// Encrypts media bytes with AES-256-GCM and appends the authentication tag.
		/// </summary>
		public byte[] Encrypt(byte[] plaintext) {
			plaintext.ThrowIfNull("plaintext");
			byte[] ciphertext = new byte[plaintext.Length];
			byte[] tag = new byte[AuthenticationTagSize];
			using var aes = new AesGcm(key, AuthenticationTagSize);
			aes.Encrypt(iv, plaintext, ciphertext, tag);
			return AppendTag(ciphertext, tag);
		}

		/// <summary>
		/// Decrypts AES-256-GCM media bytes whose authentication tag is appended.
		/// </summary>
		public byte[] Decrypt(byte[] encryptedMedia) {
			encryptedMedia.ThrowIfNull("encryptedMedia");
			if (encryptedMedia.Length < AuthenticationTagSize) {
				throw new ArgumentException("Encrypted media must include an appended authentication tag.",
					"encryptedMedia");
			}
			int ciphertextLength = encryptedMedia.Length - AuthenticationTagSize;
			byte[] ciphertext = new byte[ciphertextLength];
			byte[] tag = new byte[AuthenticationTagSize];
			Buffer.BlockCopy(encryptedMedia, 0, ciphertext, 0, ciphertext.Length);
			Buffer.BlockCopy(encryptedMedia, ciphertext.Length, tag, 0, tag.Length);
			byte[] plaintext = new byte[ciphertext.Length];
			using var aes = new AesGcm(key, AuthenticationTagSize);
			aes.Decrypt(iv, ciphertext, tag, plaintext);
			return plaintext;
		}

		/// <summary>
		/// Returns the encrypted media length for a plaintext length.
		/// </summary>
		public static long GetEncryptedLength(long plaintextLength) {
			if (plaintextLength < 0)
				throw new ArgumentOutOfRangeException("plaintextLength");
			return plaintextLength + AuthenticationTagSize;
		}

		/// <summary>
		/// Serializes this instance to an aesgcm URI.
		/// </summary>
		public override string ToString() {
			var builder = new UriBuilder(HttpsUrl) {
				Scheme = Scheme,
				Fragment = ToHex(iv) + ToHex(key)
			};
			if (HttpsUrl.IsDefaultPort)
				builder.Port = -1;
			return builder.Uri.AbsoluteUri;
		}

		static Uri ToHttpsUrl(Uri aesgcmUri) {
			var builder = new UriBuilder(aesgcmUri) {
				Scheme = Uri.UriSchemeHttps,
				Fragment = null
			};
			if (aesgcmUri.IsDefaultPort)
				builder.Port = -1;
			return builder.Uri;
		}

		static Uri RemoveFragment(Uri httpsUrl) {
			var builder = new UriBuilder(httpsUrl) {
				Fragment = null
			};
			if (httpsUrl.IsDefaultPort)
				builder.Port = -1;
			return builder.Uri;
		}

		static byte[] AppendTag(byte[] ciphertext, byte[] tag) {
			byte[] output = new byte[ciphertext.Length + tag.Length];
			Buffer.BlockCopy(ciphertext, 0, output, 0, ciphertext.Length);
			Buffer.BlockCopy(tag, 0, output, ciphertext.Length, tag.Length);
			return output;
		}

		static string ToHex(byte[] bytes) {
			return Convert.ToHexString(bytes).ToLowerInvariant();
		}

		static bool IsHex(string value) {
			foreach (char c in value) {
				if (!((c >= '0' && c <= '9') ||
					(c >= 'a' && c <= 'f') ||
					(c >= 'A' && c <= 'F'))) {
					return false;
				}
			}
			return true;
		}

		static void ValidateHttpsUrl(Uri uri) {
			if (uri == null)
				throw new ArgumentNullException("uri");
			if (!uri.IsAbsoluteUri ||
				!String.Equals(uri.Scheme, Uri.UriSchemeHttps,
					StringComparison.OrdinalIgnoreCase) ||
				String.IsNullOrEmpty(uri.Host) ||
				!String.IsNullOrEmpty(uri.UserInfo) ||
				!String.IsNullOrEmpty(uri.Fragment)) {
				throw new ArgumentException(
					"OMEMO media sharing requires an absolute HTTPS URL without a fragment.",
					"uri");
			}
		}

		static void ValidateBytes(byte[] bytes, int length, string paramName) {
			bytes.ThrowIfNull(paramName);
			if (bytes.Length != length)
				throw new ArgumentException("The " + paramName + " value must be " +
					length + " bytes.", paramName);
		}
	}

	/// <summary>
	/// Represents the strict OMEMO media sharing message body from XEP-0454.
	/// </summary>
	public sealed class OmemoMediaMessage {
		const string JpegThumbnailPrefix = "data:image/jpeg,";
		const string JpegBase64ThumbnailPrefix = "data:image/jpeg;base64,";

		/// <summary>
		/// The aesgcm media URI.
		/// </summary>
		public OmemoMediaUri MediaUri {
			get;
		}

		/// <summary>
		/// The optional JPEG thumbnail data URI.
		/// </summary>
		public string ThumbnailDataUri {
			get;
		}

		/// <summary>
		/// Initializes a new instance of the OmemoMediaMessage class.
		/// </summary>
		public OmemoMediaMessage(OmemoMediaUri mediaUri,
			string thumbnailDataUri = null) {
			mediaUri.ThrowIfNull("mediaUri");
			if (thumbnailDataUri != null && !IsValidThumbnail(thumbnailDataUri))
				throw new ArgumentException("The thumbnail must be a JPEG data URI.",
					"thumbnailDataUri");
			MediaUri = mediaUri;
			ThumbnailDataUri = thumbnailDataUri;
		}

		/// <summary>
		/// Creates a JPEG thumbnail data URI from JPEG bytes.
		/// </summary>
		public static string CreateJpegThumbnailDataUri(byte[] jpegBytes) {
			jpegBytes.ThrowIfNull("jpegBytes");
			if (jpegBytes.Length == 0)
				throw new ArgumentException("The thumbnail must not be empty.",
					"jpegBytes");
			return JpegThumbnailPrefix + Convert.ToBase64String(jpegBytes);
		}

		/// <summary>
		/// Parses a strict OMEMO media sharing message body.
		/// </summary>
		public static OmemoMediaMessage Parse(string body) {
			if (!TryParse(body, out OmemoMediaMessage message))
				throw new ArgumentException("The body is not a valid XEP-0454 media sharing body.",
					"body");
			return message;
		}

		/// <summary>
		/// Attempts to parse a strict OMEMO media sharing message body.
		/// </summary>
		public static bool TryParse(string body, out OmemoMediaMessage message) {
			message = null;
			if (String.IsNullOrEmpty(body) || body.IndexOf('\r') >= 0)
				return false;
			string[] lines = body.Split('\n');
			if (lines.Length != 1 && lines.Length != 2)
				return false;
			if (!OmemoMediaUri.TryParse(lines[0], out OmemoMediaUri mediaUri))
				return false;
			if (lines.Length == 2 && !IsValidThumbnail(lines[1]))
				return false;
			message = new OmemoMediaMessage(mediaUri,
				lines.Length == 2 ? lines[1] : null);
			return true;
		}

		/// <summary>
		/// Serializes this message body.
		/// </summary>
		public override string ToString() {
			return ThumbnailDataUri == null ? MediaUri.ToString() :
				MediaUri + "\n" + ThumbnailDataUri;
		}

		static bool IsValidThumbnail(string value) {
			if (String.IsNullOrEmpty(value) ||
				value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
				return false;
			string payload = null;
			if (value.StartsWith(JpegThumbnailPrefix, StringComparison.Ordinal))
				payload = value.Substring(JpegThumbnailPrefix.Length);
			else if (value.StartsWith(JpegBase64ThumbnailPrefix,
				StringComparison.Ordinal)) {
				payload = value.Substring(JpegBase64ThumbnailPrefix.Length);
			}
			if (String.IsNullOrEmpty(payload))
				return false;
			try {
				Convert.FromBase64String(payload);
				return true;
			} catch (FormatException) {
				return false;
			}
		}
	}
}
