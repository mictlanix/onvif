using System;
using System.Threading.Tasks;
using Mictlanix.DotNet.Onvif.Common;
using Mictlanix.DotNet.Onvif.Ptz;

namespace Mictlanix.DotNet.Onvif.Tests {
	class Program {
		static void Main (string[] args)
		{
			var host = args[0];
			var username = args[1];
			var password = args[2];

			MainAsync (host, username, password).Wait ();
		}

		static async Task MainAsync (string host, string username, string password)
		{
			var device = await OnvifClientFactory.CreateDeviceClientAsync (host, username, password);
			var media = await OnvifClientFactory.CreateMediaClientAsync (host, username, password);
			var ptz = await OnvifClientFactory.CreatePTZClientAsync (host, username, password);
			var imaging = await OnvifClientFactory.CreateImagingClientAsync (host, username, password);
			var caps = await device.GetCapabilitiesAsync (new CapabilityCategory[] { CapabilityCategory.All });
			bool absolute_move = false;
			bool relative_move = false;
			bool continuous_move = false;
			bool focus = false;

			Console.WriteLine ("Capabilities");

			Console.WriteLine ("\tDevice: " + caps.Capabilities.Device.XAddr);
			Console.WriteLine ("\tEvents: " + caps.Capabilities.Events.XAddr);
			Console.WriteLine ("\tImaging: " + caps.Capabilities.Imaging.XAddr);
			Console.WriteLine ("\tMedia: " + caps.Capabilities.Media.XAddr);
			Console.WriteLine ("\tPTZ: " + caps.Capabilities.PTZ.XAddr);
			
			var profiles = await media.GetProfilesAsync ();
			string profile_token = null;

			Console.WriteLine ("Profiles count :" + profiles.Profiles.Length);

			foreach (var profile in profiles.Profiles) {
				Console.WriteLine ($"Profile: {profile.token}");

				if (profile_token == null) {
					profile_token = profile.token;
					absolute_move = !string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultAbsolutePantTiltPositionSpace);
					relative_move = !string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultRelativePanTiltTranslationSpace);
					continuous_move = !string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultContinuousPanTiltVelocitySpace);
				}

				Console.WriteLine ($"\tTranslation Support");
				Console.WriteLine ($"\t\tAbsolute Translation: {!string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultAbsolutePantTiltPositionSpace)}");
				Console.WriteLine ($"\t\tRelative Translation: {!string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultRelativePanTiltTranslationSpace)}");
				Console.WriteLine ($"\t\tContinuous Translation: {!string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultContinuousPanTiltVelocitySpace)}");

				if(!string.IsNullOrWhiteSpace (profile.PTZConfiguration.DefaultRelativePanTiltTranslationSpace)) {
					var pan = profile.PTZConfiguration.PanTiltLimits.Range.XRange;
					var tilt = profile.PTZConfiguration.PanTiltLimits.Range.YRange;
					var zoom = profile.PTZConfiguration.ZoomLimits.Range.XRange;

					Console.WriteLine ($"\tPan Limits: [{pan.Min}, {pan.Max}] Tilt Limits: [{tilt.Min}, {tilt.Max}] Tilt Limits: [{zoom.Min}, {zoom.Max}]");
				}
			}
			
			var configs  = await ptz.GetConfigurationsAsync ();

			foreach (var config in configs.PTZConfiguration) {
				Console.WriteLine ($"PTZ Configuration: {config.token}");
			}
			
			var video_sources = await media.GetVideoSourcesAsync ();
			string vsource_token = null;

			foreach (var source in video_sources.VideoSources) {
				Console.WriteLine ($"Video Source: {source.token}");

				if (vsource_token == null) {
					vsource_token = source.token;
					focus = source.Imaging.Focus != null;
				}
				
				Console.WriteLine ($"\tFramerate: {source.Framerate}");
				Console.WriteLine ($"\tResolution: {source.Resolution.Width}x{source.Resolution.Height}");

				Console.WriteLine ($"\tFocus Settings");

				if (source.Imaging.Focus == null) {
					Console.WriteLine ($"\t\tNone");
				} else {
					Console.WriteLine ($"\t\tMode: {source.Imaging.Focus.AutoFocusMode}");
					Console.WriteLine ($"\t\tNear Limit: {source.Imaging.Focus.NearLimit}");
					Console.WriteLine ($"\t\tFar Limit: {source.Imaging.Focus.FarLimit}");
					Console.WriteLine ($"\t\tDefault Speed: {source.Imaging.Focus.DefaultSpeed}");
				}

				Console.WriteLine ($"\tExposure Settings");

				if (source.Imaging.Exposure == null) {
					Console.WriteLine ($"\t\tNone");
				} else {
					Console.WriteLine ($"\t\tMode: {source.Imaging.Exposure.Mode}");
					Console.WriteLine ($"\t\tMin Iris: {source.Imaging.Exposure.MinIris}");
					Console.WriteLine ($"\t\tMax Iris: {source.Imaging.Exposure.MaxIris}");
				}

				Console.WriteLine ($"\tImage Settings");

				var imaging_opts = await imaging.GetOptionsAsync (source.token);

				Console.WriteLine ($"\t\tBrightness: {source.Imaging.Brightness} [{imaging_opts.Brightness.Min}, {imaging_opts.Brightness.Max}]");
				Console.WriteLine ($"\t\tColor Saturation: {source.Imaging.ColorSaturation} [{imaging_opts.ColorSaturation.Min}, {imaging_opts.ColorSaturation.Max}]");
				Console.WriteLine ($"\t\tContrast: {source.Imaging.Contrast} [{imaging_opts.Contrast.Min}, {imaging_opts.Contrast.Max}]");
				Console.WriteLine ($"\t\tSharpness: {source.Imaging.Sharpness} [{imaging_opts.Sharpness.Min}, {imaging_opts.Sharpness.Max}]");
			}

			if (focus) {
				Console.WriteLine ($"Focus");

				var image_status = await imaging.GetStatusAsync (vsource_token);

				Console.WriteLine ($"\tStatus");

				Console.WriteLine ($"\t\tPosition: {image_status.FocusStatus20.Position}");
				Console.WriteLine ($"\t\tStatus: {image_status.FocusStatus20.MoveStatus}");
				Console.WriteLine ($"\t\tError: {image_status.FocusStatus20.Error}");

				Console.WriteLine ($"\tSetting Focus Mode: Manual");

				await imaging.SetImagingSettingsAsync (vsource_token, new ImagingSettings20 {
					Focus = new FocusConfiguration20 {
						AutoFocusMode = AutoFocusMode.MANUAL
					}
				}, true);
			}

			var focus_opts = await imaging.GetMoveOptionsAsync (vsource_token);

			if (focus_opts.Absolute != null) {
				Console.WriteLine ($"\tMoving Focus Absolute");

				await imaging.MoveAsync (vsource_token, new FocusMove {
					Absolute = new AbsoluteFocus {
						Position = 0f,
						//Speed = 1f,
						//SpeedSpecified = true
					}
				});

				await Task.Delay (500);
			}

			if (focus_opts.Relative != null) {
				Console.WriteLine ($"\tMoving Focus Relative");

				await imaging.MoveAsync (vsource_token, new FocusMove {
					Relative = new RelativeFocus {
						Distance = 0.2f,
						//Speed = 1f,
						//SpeedSpecified = true
					}
				});

				await Task.Delay (500);
			}

			if (focus_opts.Continuous != null) {
				Console.WriteLine ($"\tMoving Focus Continuous...");

				await imaging.MoveAsync (vsource_token, new FocusMove {
					Continuous = new ContinuousFocus {
						Speed = 0.1f
					}
				});

				await Task.Delay (500);

				await imaging.StopAsync (vsource_token);
			}

			var ptz_status = await ptz.GetStatusAsync (profile_token);

			Console.WriteLine ($"Position: [{ptz_status.Position.PanTilt.x}, {ptz_status.Position.PanTilt.y}, {ptz_status.Position.Zoom.x}]");
			Console.WriteLine ($"Pan/Tilt Status: {ptz_status.MoveStatus.PanTilt} Zoom Status: {ptz_status.MoveStatus.Zoom}");

			if (absolute_move) {
				Console.WriteLine ($"Absolute Move...");

				await ptz.AbsoluteMoveAsync (profile_token, new PTZVector {
					PanTilt = new Vector2D {
						x = 0.5f,
						y = 0
					},
					Zoom = new Vector1D {
						x = 0f
					}
				}, new PTZSpeed {
					PanTilt = new Vector2D {
						x = 1f,
						y = 1f
					},
					Zoom = new Vector1D {
						x = 0f
					}
				});

				await Task.Delay (3000);

				ptz_status = await ptz.GetStatusAsync (profile_token);

				Console.WriteLine ($"Position: [{ptz_status.Position.PanTilt.x}, {ptz_status.Position.PanTilt.y}, {ptz_status.Position.Zoom.x}]");
				Console.WriteLine ($"Pan/Tilt Status: {ptz_status.MoveStatus.PanTilt} Zoom Status: {ptz_status.MoveStatus.Zoom}");
			}

			if (relative_move) {
				Console.WriteLine ($"Relative Move...");

				await ptz.RelativeMoveAsync (profile_token, new PTZVector {
					PanTilt = new Vector2D {
						x = 0.1f,
						y = 0.1f
					},
					Zoom = new Vector1D {
						x = 0.1f
					}
				}, new PTZSpeed {
					PanTilt = new Vector2D {
						x = 0.1f,
						y = 0.1f
					},
					Zoom = new Vector1D {
						x = 0.1f
					}
				});

				await Task.Delay (3000);

				ptz_status = await ptz.GetStatusAsync (profile_token);

				Console.WriteLine ($"Position: [{ptz_status.Position.PanTilt.x}, {ptz_status.Position.PanTilt.y}, {ptz_status.Position.Zoom.x}]");
				Console.WriteLine ($"Pan/Tilt Status: {ptz_status.MoveStatus.PanTilt} Zoom Status: {ptz_status.MoveStatus.Zoom}");
			}

			if (continuous_move) {
				Console.WriteLine ($"Continuous Move...");

				await ptz.ContinuousMoveAsync (profile_token, new PTZSpeed {
					PanTilt = new Vector2D {
						x = 0,
						y = -1
					},
					Zoom = new Vector1D {
						x = 0
					}
				}, null);

				await Task.Delay (1500);
				await ptz.StopAsync (profile_token, true, true);

				ptz_status = await ptz.GetStatusAsync (profile_token);

				Console.WriteLine ($"Position: [{ptz_status.Position.PanTilt.x}, {ptz_status.Position.PanTilt.y}, {ptz_status.Position.Zoom.x}]");
				Console.WriteLine ($"Pan/Tilt Status: {ptz_status.MoveStatus.PanTilt} Zoom Status: {ptz_status.MoveStatus.Zoom}");
			}

			var presets = await ptz.GetPresetsAsync (profile_token);

			Console.WriteLine ("Presets count: " + presets.Preset.Length);

			foreach (var preset in presets.Preset) {
				var pan = preset.PTZPosition.PanTilt.x;
				var tilt = preset.PTZPosition.PanTilt.y;
				var zoom = preset.PTZPosition.Zoom.x;

				Console.WriteLine ($"Preset: {preset.token} Name: {preset.Name} Pan: {pan} Tilt: {tilt} Zoom: {zoom}");

				await ptz.GotoPresetAsync (profile_token, preset.token, null);
				await Task.Delay (1500);
				//await ptz.RemovePresetAsync (profile_token, preset.token);
			}

			if (presets.Preset.Length == 0) {
				var new_preset = await ptz.SetPresetAsync (new SetPresetRequest {
					ProfileToken = profile_token,
					PresetName = "P1"
				});

				Console.WriteLine ($"New Preset: {new_preset.PresetToken}");
			}
		}
	}
}
