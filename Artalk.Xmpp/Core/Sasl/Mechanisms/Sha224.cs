using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	internal static class Sha224 {
		static readonly uint[] K = {
			0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
			0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
			0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
			0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
			0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
			0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
			0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
			0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
			0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
			0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
			0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
			0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
			0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
			0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
			0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
			0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
		};

		public static byte[] HashData(byte[] data) {
			data.ThrowIfNull("data");
			uint[] h = {
				0xc1059ed8, 0x367cd507, 0x3070dd17, 0xf70e5939,
				0xffc00b31, 0x68581511, 0x64f98fa7, 0xbefa4fa4
			};
			foreach (byte[] block in Pad(data))
				Compress(h, block);

			byte[] result = new byte[28];
			for (int i = 0; i < 7; i++)
				BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(i * 4), h[i]);
			return result;
		}

		public static byte[] ComputeHmac(byte[] key, byte[] data) {
			key.ThrowIfNull("key");
			data.ThrowIfNull("data");
			const int blockSize = 64;
			if (key.Length > blockSize)
				key = HashData(key);
			byte[] paddedKey = new byte[blockSize];
			Buffer.BlockCopy(key, 0, paddedKey, 0, key.Length);

			byte[] inner = new byte[blockSize + data.Length];
			byte[] outer = new byte[blockSize + 28];
			for (int i = 0; i < blockSize; i++) {
				inner[i] = (byte) (paddedKey[i] ^ 0x36);
				outer[i] = (byte) (paddedKey[i] ^ 0x5c);
			}
			Buffer.BlockCopy(data, 0, inner, blockSize, data.Length);
			byte[] innerHash = HashData(inner);
			Buffer.BlockCopy(innerHash, 0, outer, blockSize, innerHash.Length);
			return HashData(outer);
		}

		static IEnumerable<byte[]> Pad(byte[] data) {
			ulong bitLength = checked((ulong) data.Length * 8);
			int paddedLength = data.Length + 1 + 8;
			int remainder = paddedLength % 64;
			if (remainder != 0)
				paddedLength += 64 - remainder;
			byte[] padded = new byte[paddedLength];
			Buffer.BlockCopy(data, 0, padded, 0, data.Length);
			padded[data.Length] = 0x80;
			BinaryPrimitives.WriteUInt64BigEndian(
				padded.AsSpan(padded.Length - 8), bitLength);

			for (int offset = 0; offset < padded.Length; offset += 64) {
				byte[] block = new byte[64];
				Buffer.BlockCopy(padded, offset, block, 0, 64);
				yield return block;
			}
		}

		static void Compress(uint[] h, byte[] block) {
			uint[] w = new uint[64];
			for (int i = 0; i < 16; i++)
				w[i] = BinaryPrimitives.ReadUInt32BigEndian(
					block.AsSpan(i * 4, 4));
			for (int i = 16; i < 64; i++)
				w[i] = SmallSigma1(w[i - 2]) + w[i - 7] +
					SmallSigma0(w[i - 15]) + w[i - 16];

			uint a = h[0], b = h[1], c = h[2], d = h[3],
				e = h[4], f = h[5], g = h[6], hh = h[7];
			for (int i = 0; i < 64; i++) {
				uint t1 = hh + BigSigma1(e) + Ch(e, f, g) + K[i] + w[i];
				uint t2 = BigSigma0(a) + Maj(a, b, c);
				hh = g;
				g = f;
				f = e;
				e = d + t1;
				d = c;
				c = b;
				b = a;
				a = t1 + t2;
			}

			h[0] += a;
			h[1] += b;
			h[2] += c;
			h[3] += d;
			h[4] += e;
			h[5] += f;
			h[6] += g;
			h[7] += hh;
		}

		static uint RotateRight(uint value, int bits) {
			return (value >> bits) | (value << (32 - bits));
		}

		static uint Ch(uint x, uint y, uint z) {
			return (x & y) ^ (~x & z);
		}

		static uint Maj(uint x, uint y, uint z) {
			return (x & y) ^ (x & z) ^ (y & z);
		}

		static uint BigSigma0(uint x) {
			return RotateRight(x, 2) ^ RotateRight(x, 13) ^ RotateRight(x, 22);
		}

		static uint BigSigma1(uint x) {
			return RotateRight(x, 6) ^ RotateRight(x, 11) ^ RotateRight(x, 25);
		}

		static uint SmallSigma0(uint x) {
			return RotateRight(x, 7) ^ RotateRight(x, 18) ^ (x >> 3);
		}

		static uint SmallSigma1(uint x) {
			return RotateRight(x, 17) ^ RotateRight(x, 19) ^ (x >> 10);
		}
	}
}
