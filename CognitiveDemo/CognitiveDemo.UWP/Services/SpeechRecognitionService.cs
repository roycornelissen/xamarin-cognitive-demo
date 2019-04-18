using CognitiveDemo.Services;
using CognitiveDemo.UWP.Services;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpeechRecognitionService))]

namespace CognitiveDemo.UWP.Services
{
    public class SpeechRecognitionService : ISpeechRecognitionService
    {
        public async Task<string> Recognize()
        {
            var status = "";
            var speechConfig = SpeechConfig.FromSubscription(ApiKeys.SpeechApiKey, "eastus");

            using (var recognizer = new SpeechRecognizer(speechConfig))
            {
                var result = await recognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    status = $"You said: '{result.Text}'";
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    status = $"NOMATCH: Speech could not be recognized.";
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    status = $"CANCELED: Reason={cancellation.Reason}";

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }

            return status;
        }
    }
}
