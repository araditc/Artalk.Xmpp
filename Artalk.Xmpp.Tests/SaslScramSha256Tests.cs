using Artalk.Xmpp.Core.Sasl;
using Artalk.Xmpp.Core.Sasl.Mechanisms;
using System.Text;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class SaslScramSha256Tests {
	[TestMethod]
	public void Rfc7677ExchangeProducesExpectedMessages() {
		var mechanism = new SaslScramSha256("user", "pencil",
			"rOprNGfwEbeRWgbNEkqO");

		byte[] initial = mechanism.GetResponse(Array.Empty<byte>());
		Assert.AreEqual("n,,n=user,r=rOprNGfwEbeRWgbNEkqO",
			Encoding.UTF8.GetString(initial));

		byte[] final = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096"));
		Assert.AreEqual(
			"c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0," +
			"p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=",
			Encoding.UTF8.GetString(final));

		byte[] serverVerification = mechanism.GetResponse(Encoding.UTF8.GetBytes(
			"v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4="));
		Assert.IsEmpty(serverVerification);
		Assert.IsTrue(mechanism.IsCompleted);
	}

	[TestMethod]
	public void FactoryCanCreateScramSha256Mechanism() {
		SaslMechanism mechanism = SaslFactory.Create("SCRAM-SHA-256");

		Assert.AreEqual("SCRAM-SHA-256", mechanism.Name);
		Assert.IsTrue(mechanism.HasInitial);
	}

	[TestMethod]
	public void Sha224ProducesExpectedDigest() {
		byte[] digest = Sha224.HashData(Encoding.ASCII.GetBytes("abc"));

		Assert.AreEqual(
			"23097d223405d8228642a477bda255b32aadbce4bda0b3f7e36c9da7",
			Convert.ToHexString(digest).ToLowerInvariant());
	}

	[TestMethod]
	public void FactoryCanCreateAllScramMechanisms() {
		string[] names = {
			"SCRAM-SHA3-512",
			"SCRAM-SHA-512",
			"SCRAM-SHA-384",
			"SCRAM-SHA-256",
			"SCRAM-SHA-224",
			"SCRAM-SHA-1"
		};

		foreach (string name in names) {
			SaslMechanism mechanism = SaslFactory.Create(name);

			Assert.AreEqual(name, mechanism.Name);
			Assert.IsTrue(mechanism.HasInitial);
		}
	}
}
