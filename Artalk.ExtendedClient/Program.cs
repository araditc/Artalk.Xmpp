using System;
using System.Xml;

namespace Artalk.ExtendedClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 3 || args[0] == "-h")
            {
                Console.WriteLine("Usage: ./Artalk.ExtendedClient.exe HOST LOGIN PASSWORD");
                return;
            }

            var host = args[0];
            var login = args[1];
            var password = args[2];
            var client = new ExtendedXmppClient(host, login, password);

            Console.Write("Message to save: ");
            var message = Console.ReadLine();
            var messageElement = new XmlDocument().CreateElement("message");
            messageElement.InnerText = message ?? string.Empty;

            client.SaveXml(messageElement);
            client.Close();

            Console.WriteLine("Data saved. Connection closed.");
        }
    }
}
