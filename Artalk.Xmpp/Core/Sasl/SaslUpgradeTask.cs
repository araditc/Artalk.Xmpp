using Artalk.Xmpp.Core.Sasl.Mechanisms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;

namespace Artalk.Xmpp.Core.Sasl {
	internal static class SaslUpgradeTask {
		public const string Namespace = "urn:xmpp:sasl:upgrade:0";
		public const string ScramNamespace = "urn:xmpp:scram-upgrade:0";

		static readonly TaskDefinition[] ScramTasks = {
			new TaskDefinition("UPGR-SCRAM-SHA3-512", 64, ComputeHmacSha3_512),
			new TaskDefinition("UPGR-SCRAM-SHA-512", 64, ComputeHmacSha512),
			new TaskDefinition("UPGR-SCRAM-SHA-384", 48, ComputeHmacSha384),
			new TaskDefinition("UPGR-SCRAM-SHA-256", 32, ComputeHmacSha256),
			new TaskDefinition("UPGR-SCRAM-SHA-224", 28, Sha224.ComputeHmac),
			new TaskDefinition("UPGR-SCRAM-SHA-1", 20, ComputeHmacSha1)
		};

		public static IReadOnlyList<string> SelectSupported(
			IEnumerable<string> advertisedTasks, string password) {
			if (password == null || advertisedTasks == null)
				return Array.Empty<string>();
			var advertised = new HashSet<string>(advertisedTasks,
				StringComparer.InvariantCultureIgnoreCase);
			return ScramTasks
				.Where(task => advertised.Contains(task.Name))
				.Select(task => task.Name)
				.ToArray();
		}

		public static bool Supports(string task) {
			return Find(task) != null;
		}

		public static XmlElement CreateUpgradeElement(string task) {
			task.ThrowIfNullOrEmpty("task");
			return Xml.Element("upgrade", Namespace).Text(task);
		}

		public static XmlElement CreateNextElement(string task) {
			task.ThrowIfNullOrEmpty("task");
			return Xml.Element("next", Sasl2Feature.Namespace).Attr("task", task);
		}

		public static XmlElement CreateHashTaskData(string task, string password,
			XmlElement taskData) {
			task.ThrowIfNullOrEmpty("task");
			password.ThrowIfNull("password");
			taskData.ThrowIfNull("taskData");
			TaskDefinition definition = Find(task);
			if (definition == null)
				throw new SaslException("Unsupported SASL upgrade task.");
			XmlElement saltElement = GetSaltElement(taskData);
			string iterationsValue = saltElement.GetAttribute("iterations");
			if (!Int32.TryParse(iterationsValue, out int iterations) ||
				iterations < 4096) {
				throw new SaslException("Invalid SCRAM upgrade iteration count.");
			}
			string salt = saltElement.InnerText.Trim();
			if (String.IsNullOrEmpty(salt))
				throw new SaslException("Invalid SCRAM upgrade salt.");
			byte[] saltedPassword = SaslScram.ComputeSaltedPassword(password, salt,
				iterations, definition.HashLength, definition.Hmac);
			return Xml.Element("task-data", Sasl2Feature.Namespace)
				.Child(Xml.Element("hash", ScramNamespace).Text(
					Convert.ToBase64String(saltedPassword)));
		}

		static XmlElement GetSaltElement(XmlElement taskData) {
			if (taskData.LocalName != "task-data" ||
				taskData.NamespaceURI != Sasl2Feature.Namespace) {
				throw new SaslException("Expected SASL2 task-data.");
			}
			foreach (XmlNode node in taskData.ChildNodes) {
				if (node is XmlElement element &&
					element.LocalName == "salt" &&
					element.NamespaceURI == ScramNamespace) {
					return element;
				}
			}
			throw new SaslException("SCRAM upgrade salt was not provided.");
		}

		static TaskDefinition Find(string task) {
			return ScramTasks.FirstOrDefault(definition =>
				String.Equals(definition.Name, task,
					StringComparison.InvariantCultureIgnoreCase));
		}

		static byte[] ComputeHmacSha1(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA1(key))
				return hmac.ComputeHash(data);
		}

		static byte[] ComputeHmacSha256(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA256(key))
				return hmac.ComputeHash(data);
		}

		static byte[] ComputeHmacSha384(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA384(key))
				return hmac.ComputeHash(data);
		}

		static byte[] ComputeHmacSha512(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA512(key))
				return hmac.ComputeHash(data);
		}

		static byte[] ComputeHmacSha3_512(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA3_512(key))
				return hmac.ComputeHash(data);
		}

		sealed class TaskDefinition {
			public string Name {
				get;
			}

			public int HashLength {
				get;
			}

			public Func<byte[], byte[], byte[]> Hmac {
				get;
			}

			public TaskDefinition(string name, int hashLength,
				Func<byte[], byte[], byte[]> hmac) {
				Name = name;
				HashLength = hashLength;
				Hmac = hmac;
			}
		}
	}
}
