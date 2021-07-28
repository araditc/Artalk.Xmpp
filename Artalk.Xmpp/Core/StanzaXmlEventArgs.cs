using System;

namespace Artalk.Xmpp.Core
{
    /// <summary>
    /// Provides data for the SendXml and ReceiveXml event.
    /// </summary>
    public class StanzaXmlEventArgs : EventArgs {
        /// <summary>
        /// The stanza xml.
        /// </summary>
        public string StanzaXml {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the StanzaXmlEventArgs class.
        /// </summary>
        /// <param name="xml">The stanza on whose behalf the event is raised.</param>
        /// <exception cref="ArgumentNullException">The xml parameter is null.</exception>
        /// <exception cref="ArgumentException">The xml parameter is empty.</exception>
        public StanzaXmlEventArgs(string xml) {
            xml.ThrowIfNullOrEmpty(nameof(xml));
            StanzaXml = xml;
        }
    }
}