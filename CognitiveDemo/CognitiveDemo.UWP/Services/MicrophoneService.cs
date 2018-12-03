using CognitiveDemo.Services;
using CognitiveDemo.UWP.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(MicrophoneService))]

namespace CognitiveDemo.UWP.Services
{
    public class MicrophoneService : IMicrophoneService
    {
        public async Task EnableMicrophone()
        {
            bool isMicAvailable = true;
            try
            {
                var mediaCapture = new Windows.Media.Capture.MediaCapture();
                var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio
                };
                await mediaCapture.InitializeAsync(settings);
            }
            catch (Exception)
            {
                isMicAvailable = false;
            }
            if (!isMicAvailable)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-microphone"));
            }
            else
            {
                Console.WriteLine("Microphone was enabled");
            }
        }
    }
}
