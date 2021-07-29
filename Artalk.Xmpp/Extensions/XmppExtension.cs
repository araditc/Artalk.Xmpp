using System;
using Artalk.Xmpp.Im;
using System.Collections.Generic;
using System.Threading;
using Artalk.Xmpp.Core;

namespace Artalk.Xmpp.Extensions {
	/// <summary>
	/// The base class from which all implementations of XMPP extensions must
	/// derive.
	/// </summary>
	public abstract class XmppExtension {
		/// <summary>
		/// A reference to the instance of the XmppIm class.
		/// </summary>
		protected XmppIm im;

		/// <summary>
		/// An enumerable collection of XMPP namespaces the extension implements.
		/// </summary>
		/// <remarks>This is used for compiling the list of supported extensions
		/// advertised by the 'Service Discovery' extension.</remarks>
		public abstract IEnumerable<string> Namespaces {
			get;
		}

		/// <summary>
		/// The named constant of the Extension enumeration that corresponds to this
		/// extension.
		/// </summary>
		public abstract string Xep {
			get;
		}

		/// <summary>
		/// Initializes a new instance of the XmppExtension class.
		/// </summary>
		/// <param name="im">A reference to the XmppIm instance on whose behalf the
		/// extension is being created.</param>
		protected XmppExtension(XmppIm im) {
			im.ThrowIfNull("im");
			this.im = im;
		}

		/// <summary>
		/// Invoked after all extensions have been loaded.
		/// </summary>
		public virtual void Initialize() {
		}

		/// <summary>
		/// Performs an IQ set/get request and blocks until the response IQ comes in.
		/// </summary>
		protected Iq IqRequest(Iq iq, int millisecondsTimeout = Timeout.Infinite) {
			return im.IqRequest(iq, millisecondsTimeout);
		}

		/// <summary>
		/// Performs an IQ set/get request asynchronously and optionally invokes a
		/// callback method when the IQ response comes in.
		/// </summary>
		protected string IqRequestAsync(Iq iq, Action<string, Iq> callback = null) {
			return im.IqRequestAsync(iq, callback);
		}

		/// <summary>
		/// Sends an IQ response for the IQ request with the specified id.
		/// </summary>
		protected void IqResponse(Iq iq) {
			im.IqResponse(iq);
		}
	}
}
