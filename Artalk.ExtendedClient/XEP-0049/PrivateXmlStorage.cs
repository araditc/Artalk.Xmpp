using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Artalk.Xmpp.Core;
using Artalk.Xmpp.Extensions;
using Artalk.Xmpp.Im;

namespace Artalk.ExtendedClient
{
    public class PrivateXmlStorage : XmppExtension
    {
        private static string _namespace = "jabber:iq:private";

        public PrivateXmlStorage(XmppIm im)
            : base(im) {
        }

        public override IEnumerable<string> Namespaces { get; } = new[] { _namespace };

        public override string Xep { get; } = $"{nameof(PrivateXmlStorage)}";

        public void SaveXml(string name, string @namespace, XmlElement data) {
            var query = CreateXmlElement("query", _namespace);
            var child = CreateXmlElement(name, @namespace);
            child.AppendChild(data);
            query.AppendChild(child);
            var iq = new Iq(IqType.Set, GetNewId(), null, im.Jid, query);
            var result = IqRequest(iq);
            if (result.Type == IqType.Error)
                throw new Exception("The data could not be saved.");
        }

        public XmlElement LoadXml(string name, string @namespace) {
            var query = CreateXmlElement("query", _namespace);
            var child = CreateXmlElement(name, @namespace);
            query.AppendChild(child);
            var iq = new Iq(IqType.Get, GetNewId(), null, im.Jid, query);
            var result = IqRequest(iq);
            if (result.Type == IqType.Error)
                throw new Exception("The data could not be retrieved.");

            return result.Data["query"][name];
        }

        private XmlElement CreateXmlElement(string name, string @namespace) {
            return new XmlDocument().CreateElement(name, @namespace);
        }

        private string GetNewId() => Guid.NewGuid().ToString().ToLowerInvariant();
    }
}