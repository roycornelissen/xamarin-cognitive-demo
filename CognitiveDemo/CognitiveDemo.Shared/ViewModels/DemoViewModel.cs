using AsyncAwaitBestPractices.MVVM;
using CognitiveDemo.Services;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CognitiveDemo
{
    public class DemoViewModel : BaseViewModel
    {
        const string baseApiUri = "https://eastus.api.cognitive.microsoft.com";
        const string faceApiUri = baseApiUri + "/face/v1.0/detect";
        public ObservableCollection<Item> Items { get; set; }
        public IAsyncCommand CheckFaceApi { get; set; }
        public IAsyncCommand CheckEmotionApi { get; set; }
        public IAsyncCommand CheckTextAnalyticsApi { get; set; }
        public IAsyncCommand CheckSpeechApi { get; set; }

        MediaFile photo;

        string error;
        public string Error
        {
            get { return error; }
            set { SetProperty(ref error, value); }
        }
        public DemoViewModel()
        {
            Title = "Cognitive Demos";
            Items = new ObservableCollection<Item>();
            CheckFaceApi = new AsyncCommand(ExecuteCheckFaceApiCommand);
            CheckEmotionApi = new AsyncCommand(ExecuteCheckEmotionApiCommand);
            CheckTextAnalyticsApi = new AsyncCommand(ExecuteCheckTextAnalyticsApiCommand);
            CheckSpeechApi = new AsyncCommand(ExecuteCheckSpeechAnalyticsApiCommand);
        }

        #region Face API

        ImageSource facePicture;
        public ImageSource FacePicture
        {
            get { return facePicture; }
            set { SetProperty(ref facePicture, value); }
        }

        string faceResult;
        public string FaceResult
        {
            get { return faceResult; }
            set { SetProperty(ref faceResult, value); }
        }

        async Task ExecuteCheckFaceApiCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await CrossMedia.Current.Initialize();

                // Take photo
                if (CrossMedia.Current.IsCameraAvailable || CrossMedia.Current.IsTakePhotoSupported)
                {
                    photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                    {
                        Name = "face.jpg",
                        DefaultCamera = CameraDevice.Front,
                        PhotoSize = PhotoSize.Small,
                        RotateImage = true,
                        AllowCropping = false
                    });

                    if (photo != null)
                    {
                        FacePicture = ImageSource.FromStream(photo.GetStream);
                    }
                }
                else
                {
                    error = "Camera unavailable.";
                }

                if (photo != null)
                {
                    using (var photoStream = photo.GetStream())
                    {
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKeys.FaceApiKey);

                        // Request parameters. A third optional parameter is "details".
                        string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,hair,makeup,occlusion,accessories,blur,exposure,noise";

                        HttpResponseMessage response;

                        using (var content = new StreamContent(photoStream))
                        {
                            //sent byte array to cognitive services API
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            response = await client.PostAsync(faceApiUri + "?" + requestParameters, content);

                            //read response as string and deserialize
                            string contentString = await response.Content.ReadAsStringAsync();
                            FaceResult = JsonPrettyPrint(contentString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Emotion API

        ImageSource emotionPicture;
        public ImageSource EmotionPicture
        {
            get { return emotionPicture; }
            set { SetProperty(ref emotionPicture, value); }
        }

        string emotionResult;
        public string EmotionResult
        {
            get { return emotionResult; }
            set { SetProperty(ref emotionResult, value); }
        }

        async Task ExecuteCheckEmotionApiCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await CrossMedia.Current.Initialize();

                // Take photo
                if (CrossMedia.Current.IsCameraAvailable || CrossMedia.Current.IsTakePhotoSupported)
                {
                    photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                    {
                        Name = "emotion.jpg",
                        DefaultCamera = CameraDevice.Front,
                        PhotoSize = PhotoSize.Small,
                        RotateImage = true,
                        AllowCropping = false
                    });

                    if (photo != null)
                    {
                        EmotionPicture = ImageSource.FromStream(photo.GetStream);
                    }
                }
                else
                {
                    error = "Camera unavailable.";
                }

                if (photo != null)
                {
                    using (var photoStream = photo.GetStream())
                    {
                        using (var client = new FaceClient(new ApiKeyServiceClientCredentials(ApiKeys.FaceApiKey),
                                                    new DelegatingHandler[] { })
                        {
                            Endpoint = baseApiUri
                        })
                        {

                            var faceAttributes = new FaceAttributeType[] { FaceAttributeType.Emotion };
                            var faceList = await client.Face.DetectWithStreamAsync(photoStream, true, false, faceAttributes);

                            if (faceList.Any())
                            {
                                var emotion = faceList.FirstOrDefault().FaceAttributes.Emotion;

                                var props = typeof(Emotion).GetProperties().Where(p => p.PropertyType == typeof(double));

                                var values = props.Select(p => $"{p.Name}: {p.GetValue(emotion)}");

                                EmotionResult = string.Join(Environment.NewLine, values);
                            }
                            photo.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Speech API

        string speechAnalyticsText;
        public string SpeechAnalyticsText
        {
            get { return speechAnalyticsText; }
            set { SetProperty(ref speechAnalyticsText, value); }
        }

        string speechAnalyticsResult;
        public string SpeechAnalyticsResult
        {
            get { return speechAnalyticsResult; }
            set { SetProperty(ref speechAnalyticsResult, value); }
        }

        async Task ExecuteCheckSpeechAnalyticsApiCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            var microphoneService = DependencyService.Get<IMicrophoneService>();
            if (microphoneService != null)
            {
                await microphoneService.EnableMicrophone();
            }

            try
            {
                var speechService = DependencyService.Get<ISpeechRecognitionService>();
                if (speechService != null)
                {
                    SpeechAnalyticsResult = "Say something...";

                    SpeechAnalyticsResult = await speechService.Recognize();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Text Analytics API

        string textAnalyticsText;
        public string TextAnalyticsText
        {
            get { return textAnalyticsText; }
            set { SetProperty(ref textAnalyticsText, value); }
        }

        string textAnalyticsResult;
        public string TextAnalyticsResult
        {
            get { return textAnalyticsResult; }
            set { SetProperty(ref textAnalyticsResult, value); }
        }

        async Task ExecuteCheckTextAnalyticsApiCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials(ApiKeys.TextAnalyticsApiKey),
                                                     new DelegatingHandler[] { })
                {
                    Endpoint = ApiKeys.TextAnalyticsEndpoint
                };

                var lrResult = await client.DetectLanguageAsync(TextAnalyticsText, countryHint: "US").ConfigureAwait(false);
                var language = lrResult.DetectedLanguages.First().Name;
                var languageCode = lrResult.DetectedLanguages.First().Iso6391Name;

                var result = await client.SentimentAsync(TextAnalyticsText, languageCode, showStats: true).ConfigureAwait(false);
                TextAnalyticsResult = $"Language: {language}, Sentiment Score: {result.Score}"; 
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region helpers

        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }

        #endregion

    }
}
