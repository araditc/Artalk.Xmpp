using System;

namespace Artalk.Xmpp.Extensions.Socks5 {
	/// <summary>
	/// The exception that is thrown when a SOCKS5 error has been encountered.
	/// </summary>
	internal class Socks5Exception : Exception {
		/// <summary>
		/// Initializes a new instance of the Socks5Exception class.
		/// </summary>
		public Socks5Exception() : base() { }

		/// <summary>
		/// Initializes a new instance of the Socks5Exception class with its message
		/// string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		public Socks5Exception(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the Socks5Exception class with its message
		/// string set to <paramref name="message"/> and a reference to the inner exception.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public Socks5Exception(string message, Exception inner) : base(message, inner) { }
	}
}
