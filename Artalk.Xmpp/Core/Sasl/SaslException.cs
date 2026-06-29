using System;

namespace Artalk.Xmpp.Core.Sasl {
	/// <summary>
	/// The exception is thrown when a Sasl-related error or unexpected condition occurs.
	/// </summary>
	internal class SaslException : Exception {
		/// <summary>
		/// Initializes a new instance of the SaslException class.
		/// </summary>
		public SaslException() : base() { }

		/// <summary>
		/// Initializes a new instance of the SaslException class with its message
		/// string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		public SaslException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the SaslException class with its message
		/// string set to <paramref name="message"/> and a reference to the inner exception.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public SaslException(string message, Exception inner) : base(message, inner) { }
	}
}
