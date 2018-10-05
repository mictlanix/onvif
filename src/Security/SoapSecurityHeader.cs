using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Mictlanix.DotNet.Onvif.Security {
	public class SoapSecurityHeader : MessageHeader {
		const string ns_wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
		readonly string username;
		readonly string password;

		public override string Name { get; } = "Security";
		public override string Namespace { get; } = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

		public SoapSecurityHeader (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		protected override void OnWriteHeaderContents (XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteStartElement ("UsernameToken");

			writer.WriteStartElement ("Username");
			writer.WriteValue (username);
			writer.WriteEndElement ();

			writer.WriteStartElement ("Password");
			writer.WriteAttributeString ("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText");
			writer.WriteValue (password);
			writer.WriteEndElement ();

			writer.WriteStartElement ("Nonce");
			writer.WriteAttributeString ("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
			writer.WriteValue ("");
			writer.WriteEndElement ();

			writer.WriteStartElement ("Created");
			writer.WriteXmlnsAttribute ("", ns_wsu);
			writer.WriteValue (DateTime.UtcNow.ToString ("yyyy'-'MM'-'ddTHH':'mm':'ss'Z'"));
			writer.WriteEndElement ();

			writer.WriteEndElement ();
		}

		protected override void OnWriteStartHeader (XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteStartElement ("", Name, Namespace);
			writer.WriteXmlnsAttribute ("", Namespace);
		}
	}
}