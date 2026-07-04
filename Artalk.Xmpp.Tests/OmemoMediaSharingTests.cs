using Artalk.Xmpp.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class OmemoMediaSharingTests {
	const string ExampleUrl =
		"https://download.montague.tld/4a771ac1-f0b2-4a4a-9700-f2a26fa2bb67/tr%C3%A8s%20cool.jpg";
	const string ExampleIv = "8c3d050e9386ec173861778f";
	const string ExampleKey =
		"68e9af38a97aaf82faa4063b4d0878a61261534410c8a84331eaac851759f587";
	const string ExampleAesGcmUrl =
		"aesgcm://download.montague.tld/4a771ac1-f0b2-4a4a-9700-f2a26fa2bb67/tr%C3%A8s%20cool.jpg#" +
		ExampleIv + ExampleKey;

	[TestMethod]
	public void MediaUriSerializesOfficialExampleShape() {
		var mediaUri = new OmemoMediaUri(new Uri(ExampleUrl),
			Convert.FromHexString(ExampleIv), Convert.FromHexString(ExampleKey));

		Assert.AreEqual(ExampleAesGcmUrl, mediaUri.ToString());
	}

	[TestMethod]
	public void MediaUriParsesOfficialExampleShape() {
		OmemoMediaUri mediaUri = OmemoMediaUri.Parse(ExampleAesGcmUrl);

		Assert.AreEqual(ExampleUrl, mediaUri.HttpsUrl.AbsoluteUri);
		CollectionAssert.AreEqual(Convert.FromHexString(ExampleIv), mediaUri.Iv);
		CollectionAssert.AreEqual(Convert.FromHexString(ExampleKey), mediaUri.Key);
	}

	[TestMethod]
	public void MediaUriRejectsNonHttpsUrlCreation() {
		Assert.ThrowsExactly<ArgumentException>(() =>
			OmemoMediaUri.Create(new Uri("http://download.example/file.bin")));
		Assert.ThrowsExactly<ArgumentException>(() =>
			OmemoMediaUri.Create(new Uri("https://user@download.example/file.bin")));
	}

	[TestMethod]
	[DataRow("https://download.example/file.bin")]
	[DataRow("aesgcm://download.example/file.bin")]
	[DataRow("aesgcm://download.example/file.bin#abc")]
	[DataRow("aesgcm://download.example/file.bin#zzzzzzzzzzzzzzzzzzzzzzzz68e9af38a97aaf82faa4063b4d0878a61261534410c8a84331eaac851759f587")]
	[DataRow("aesgcm://user@download.example/file.bin#8c3d050e9386ec173861778f68e9af38a97aaf82faa4063b4d0878a61261534410c8a84331eaac851759f587")]
	public void MediaUriRejectsInvalidAesGcmUris(string value) {
		Assert.IsFalse(OmemoMediaUri.TryParse(value, out _));
	}

	[TestMethod]
	public void MediaEncryptionRoundTripsAndAppendsTag() {
		var mediaUri = new OmemoMediaUri(new Uri(ExampleUrl),
			Convert.FromHexString(ExampleIv), Convert.FromHexString(ExampleKey));
		byte[] plaintext = Encoding.UTF8.GetBytes("media bytes");

		byte[] encrypted = mediaUri.Encrypt(plaintext);
		byte[] decrypted = mediaUri.Decrypt(encrypted);

		Assert.HasCount(plaintext.Length + OmemoMediaUri.AuthenticationTagSize,
			encrypted);
		CollectionAssert.AreEqual(plaintext, decrypted);
		Assert.IsFalse(plaintext.SequenceEqual(encrypted.Take(plaintext.Length)));
	}

	[TestMethod]
	public void MediaDecryptionRejectsTamperedTag() {
		var mediaUri = new OmemoMediaUri(new Uri(ExampleUrl),
			Convert.FromHexString(ExampleIv), Convert.FromHexString(ExampleKey));
		byte[] encrypted = mediaUri.Encrypt(Encoding.UTF8.GetBytes("media bytes"));
		encrypted[^1] ^= 0xff;

		try {
			mediaUri.Decrypt(encrypted);
			Assert.Fail("Expected a cryptographic exception for the tampered tag.");
		} catch (CryptographicException) {
		}
	}

	[TestMethod]
	public void MediaMessageSerializesAndParsesThumbnail() {
		var mediaUri = OmemoMediaUri.Parse(ExampleAesGcmUrl);
		string thumbnail = OmemoMediaMessage.CreateJpegThumbnailDataUri(
			new byte[] { 1, 2, 3 });
		var message = new OmemoMediaMessage(mediaUri, thumbnail);

		OmemoMediaMessage parsed = OmemoMediaMessage.Parse(message.ToString());

		Assert.AreEqual(ExampleAesGcmUrl, parsed.MediaUri.ToString());
		Assert.AreEqual(thumbnail, parsed.ThumbnailDataUri);
		Assert.AreEqual(ExampleAesGcmUrl + "\ndata:image/jpeg,AQID",
			message.ToString());
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("hello")]
	[DataRow(ExampleAesGcmUrl + "\r\ndata:image/jpeg,AQID")]
	[DataRow(ExampleAesGcmUrl + "\ndata:image/png,AQID")]
	[DataRow(ExampleAesGcmUrl + "\ndata:image/jpeg,not base64")]
	[DataRow(ExampleAesGcmUrl + "\ndata:image/jpeg,AQID\nextra")]
	public void MediaMessageRejectsNonStrictBodies(string body) {
		Assert.IsFalse(OmemoMediaMessage.TryParse(body, out _));
	}

	[TestMethod]
	public void EncryptedLengthAddsAuthenticationTag() {
		Assert.AreEqual(116L, OmemoMediaUri.GetEncryptedLength(100));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
			OmemoMediaUri.GetEncryptedLength(-1));
	}
}
