using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Mictlanix.DotNet.Onvif.Security {
	public class SoapSecurityHeaderInspector : IClientMessageInspector {
		readonly string username;
		readonly string password;

		public SoapSecurityHeaderInspector (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		public void AfterReceiveReply (ref Message reply, object correlationState)
		{

		}

		public object BeforeSendRequest (ref Message request, IClientChannel channel)
		{
			request.Headers.Add (new SoapSecurityHeader (username, password));

			return null;
		}
	}
}
