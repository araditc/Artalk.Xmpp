using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// Coordinates OMEMO device discovery, trust enforcement, session encryption,
	/// and payload encryption/decryption.
	/// </summary>
	public class OmemoSessionManager {
		readonly OmemoDeviceListResolver deviceListResolver;
		readonly OmemoBundleResolver bundleResolver;
		readonly IOmemoSessionCipher sessionCipher;
		readonly IOmemoTrustStore trustStore;

		/// <summary>
		/// Defines how undecided identities are handled while encrypting.
		/// </summary>
		public OmemoTrustPolicy TrustPolicy {
			get;
			set;
		} = OmemoTrustPolicy.RequireTrusted;

		/// <summary>
		/// true to include the sender account's other devices while encrypting.
		/// </summary>
		public bool IncludeLocalDevices {
			get;
			set;
		} = true;

		/// <summary>
		/// Initializes a new instance of the OmemoSessionManager class.
		/// </summary>
		public OmemoSessionManager(OmemoDeviceListResolver deviceListResolver,
			OmemoBundleResolver bundleResolver, IOmemoSessionCipher sessionCipher,
			IOmemoTrustStore trustStore = null) {
			deviceListResolver.ThrowIfNull("deviceListResolver");
			bundleResolver.ThrowIfNull("bundleResolver");
			sessionCipher.ThrowIfNull("sessionCipher");
			this.deviceListResolver = deviceListResolver;
			this.bundleResolver = bundleResolver;
			this.sessionCipher = sessionCipher;
			this.trustStore = trustStore;
		}

		/// <summary>
		/// Encrypts plaintext for all recipient devices and returns an OMEMO envelope.
		/// </summary>
		public OmemoEncryptedMessage Encrypt(Jid localJid, uint localDeviceId,
			IEnumerable<Jid> recipients, byte[] plaintext) {
			localJid.ThrowIfNull("localJid");
			OmemoDevice.ValidateDeviceId(localDeviceId, "localDeviceId");
			recipients.ThrowIfNull("recipients");
			plaintext.ThrowIfNull("plaintext");

			OmemoPayload payload = OmemoPayload.Encrypt(plaintext);
			var recipientKeys = new List<OmemoRecipientKey>();
			string localBare = localJid.GetBareJid().ToString();
			foreach (Jid recipient in RecipientAccounts(localJid, recipients)) {
				string recipientBare = recipient.GetBareJid().ToString();
				OmemoDeviceList deviceList = deviceListResolver(recipient);
				if (deviceList == null)
					continue;
				foreach (uint deviceId in deviceList.DeviceIds) {
					if (recipientBare == localBare && deviceId == localDeviceId)
						continue;
					OmemoBundle bundle = bundleResolver(recipient, deviceId);
					if (bundle == null)
						continue;
					EnsureTrusted(recipient, deviceId, bundle.IdentityKey);
					OmemoRecipientKey key = sessionCipher.EncryptKeyMaterial(
						recipient, deviceId, bundle, payload.KeyMaterial);
					key.ThrowIfNull("key");
					recipientKeys.Add(key);
				}
			}
			if (recipientKeys.Count == 0) {
				throw new InvalidOperationException(
					"No trusted OMEMO recipient devices were available.");
			}
			return new OmemoEncryptedMessage(localDeviceId, recipientKeys,
				payload.Ciphertext);
		}

		/// <summary>
		/// Decrypts an OMEMO envelope for the local device.
		/// </summary>
		public byte[] Decrypt(Jid senderJid, Jid localJid, uint localDeviceId,
			OmemoEncryptedMessage encryptedMessage) {
			senderJid.ThrowIfNull("senderJid");
			localJid.ThrowIfNull("localJid");
			OmemoDevice.ValidateDeviceId(localDeviceId, "localDeviceId");
			encryptedMessage.ThrowIfNull("encryptedMessage");
			if (encryptedMessage.Payload == null)
				throw new InvalidOperationException("The OMEMO message has no payload.");

			var failures = new List<Exception>();
			foreach (OmemoRecipientKey key in encryptedMessage
				.GetKeysFor(localJid)
				.Where(k => k.RecipientDeviceId == localDeviceId)) {
				try {
					byte[] keyMaterial = sessionCipher.DecryptKeyMaterial(
						senderJid.GetBareJid(), encryptedMessage.SenderDeviceId, key);
					return OmemoPayload.FromKeyMaterial(keyMaterial,
						encryptedMessage.Payload).Decrypt();
				} catch (Exception ex) when (
					ex is ArgumentException ||
					ex is CryptographicException ||
					ex is XmppException ||
					ex is InvalidOperationException) {
					failures.Add(ex);
				}
			}
			throw new CryptographicException(
				"No OMEMO recipient key could be decrypted for the local device.",
				failures.FirstOrDefault());
		}

		IEnumerable<Jid> RecipientAccounts(Jid localJid, IEnumerable<Jid> recipients) {
			var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			if (IncludeLocalDevices)
				set.Add(localJid.GetBareJid().ToString());
			foreach (Jid recipient in recipients) {
				recipient.ThrowIfNull("recipient");
				set.Add(recipient.GetBareJid().ToString());
			}
			return set.Select(jid => new Jid(jid));
		}

		void EnsureTrusted(Jid jid, uint deviceId, byte[] identityKey) {
			if (trustStore == null)
				return;
			OmemoTrustLevel trustLevel =
				trustStore.GetTrust(jid, deviceId, identityKey);
			if (trustLevel == OmemoTrustLevel.Trusted)
				return;
			if (trustLevel == OmemoTrustLevel.Untrusted) {
				throw new InvalidOperationException(
					"OMEMO device is explicitly untrusted: " +
					jid.GetBareJid() + "/" + deviceId);
			}
			if (TrustPolicy == OmemoTrustPolicy.AllowUndecided)
				return;
			if (TrustPolicy == OmemoTrustPolicy.TrustOnFirstUse) {
				trustStore.SetTrust(jid, deviceId, identityKey,
					OmemoTrustLevel.Trusted);
				return;
			}
			throw new InvalidOperationException(
				"OMEMO device identity is undecided: " +
				jid.GetBareJid() + "/" + deviceId);
		}
	}
}
