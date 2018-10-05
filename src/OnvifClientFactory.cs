using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Mictlanix.DotNet.Onvif.Common;
using Mictlanix.DotNet.Onvif.Device;
using Mictlanix.DotNet.Onvif.Media;
using Mictlanix.DotNet.Onvif.Ptz;
using Mictlanix.DotNet.Onvif.Security;

namespace Mictlanix.DotNet.Onvif {
	public static class OnvifClientFactory {
		static Binding CreateBinding ()
		{
			var binding = new CustomBinding ();
			var textBindingElement = new TextMessageEncodingBindingElement {
				MessageVersion = MessageVersion.CreateVersion (EnvelopeVersion.Soap12, AddressingVersion.None)
			};
			var httpBindingElement = new HttpTransportBindingElement {
				AllowCookies = true,
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = int.MaxValue
			};

			binding.Elements.Add (textBindingElement);
			binding.Elements.Add (httpBindingElement);

			return binding;
		}

		public static async Task<DeviceClient> CreateDeviceClientAsync (string host, string username, string password)
		{
			return await CreateDeviceClientAsync (new Uri ($"http://{host}/onvif/device_service"), username, password);
		}

		public static async Task<DeviceClient> CreateDeviceClientAsync (Uri uri, string username, string password)
		{
			var binding = CreateBinding ();
			var endpoint = new EndpointAddress (uri);
			var device = new DeviceClient (binding, endpoint);

			device.ChannelFactory.Endpoint.EndpointBehaviors.Add (new SoapSecurityHeaderBehavior (username, password));

			// Connectivity Test
			await device.OpenAsync ();
			await device.GetSystemDateAndTimeAsync ();
			
			return device;
		}

		public static async Task<MediaClient> CreateMediaClientAsync (string host, string username, string password)
		{
			var device = await CreateDeviceClientAsync (host, username, password);
			var caps = await device.GetCapabilitiesAsync (new CapabilityCategory [] { CapabilityCategory.Media });

			return await CreateMediaClientAsync (new Uri (caps.Capabilities.Media.XAddr), username, password);
		}

		public static async Task<MediaClient> CreateMediaClientAsync (Uri uri, string username, string password)
		{
			var binding = CreateBinding ();
			var media = new MediaClient (binding, new EndpointAddress (uri));

			media.ChannelFactory.Endpoint.EndpointBehaviors.Add (new SoapSecurityHeaderBehavior (username, password));

			// Connectivity Test
			await media.OpenAsync ();

			return media;
		}

		public static async Task<PTZClient> CreatePTZClientAsync (string host, string username, string password)
		{
			var device = await CreateDeviceClientAsync (host, username, password);
			var caps = await device.GetCapabilitiesAsync (new CapabilityCategory [] { CapabilityCategory.PTZ });

			return await CreatePTZClientAsync (new Uri (caps.Capabilities.PTZ.XAddr), username, password);
		}

		public static async Task<PTZClient> CreatePTZClientAsync (Uri uri, string username, string password)
		{
			var binding = CreateBinding ();
			var ptz = new PTZClient (binding, new EndpointAddress (uri));

			ptz.ChannelFactory.Endpoint.EndpointBehaviors.Add (new SoapSecurityHeaderBehavior (username, password));

			// Connectivity Test
			await ptz.OpenAsync ();

			return ptz;
		}
	}
}
