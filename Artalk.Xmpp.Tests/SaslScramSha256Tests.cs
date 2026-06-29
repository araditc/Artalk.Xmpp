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
}
