using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Linq;

using Xamarin.Forms;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Microsoft.ProjectOxford.Text.Sentiment;
using Microsoft.ProjectOxford.Text.Language;
using Microsoft.ProjectOxford.Text.Core;

namespace CognitiveDemo
{
    public class DemoViewModel : BaseViewModel
    {
        EmotionServiceClient emotionClient;

        public ObservableCollection<Item> Items { get; set; }
        public Command CheckFaceApi { get; set; }
        public Command CheckEmotionApi { get; set; }
        public Command CheckTextAnalyticsApi { get; set; }

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
            CheckFaceApi = new Command(async () => await ExecuteCheckFaceApiCommand());
            CheckEmotionApi = new Command(async () => await ExecuteCheckEmotionApiCommand());
            CheckTextAnalyticsApi = new Command(async () => await ExecuteCheckTextAnalyticsApiCommand());

            emotionClient = new EmotionServiceClient(ApiKeys.EmotionApiKey);

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
                        PhotoSize = PhotoSize.Small
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
                        BinaryReader binaryReader = new BinaryReader(photoStream);
                        var imagesBytes = binaryReader.ReadBytes((int)photoStream.Length);

                        HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKeys.FaceApiKey);

                        string uri = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect";
                        // Request parameters. A third optional parameter is "details".
                        string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,hair,makeup,occlusion,accessories,blur,exposure,noise";


                        HttpResponseMessage response;

                        using (ByteArrayContent content = new ByteArrayContent(imagesBytes))
                        {
                            //sent byte array to cognitive services API
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            response = await client.PostAsync(uri + "?" + requestParameters, content);

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
                        PhotoSize = PhotoSize.Small
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
                        Emotion[] result = await emotionClient.RecognizeAsync(photoStream);
                        if (result.Any())
                        {
                            var emotion = result.FirstOrDefault();
                            var highestScore = emotion.Scores.ToRankedList().FirstOrDefault();
                            EmotionResult = $"Top Emotion: {highestScore.Key} {highestScore.Value.ToString()}";

                        }
                        photo.Dispose();
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

        #region ComputerVision
        #endregion

        #region customVision

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
                LanguageClient languageClient = new LanguageClient(ApiKeys.TextAnalyticsApiKey);
                languageClient.Url = "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0/languages";
                LanguageRequest lr = new LanguageRequest();
                lr.Documents.Add(new Document()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = TextAnalyticsText
                });
                var lrResult = await languageClient.GetLanguagesAsync(lr);
                var language = lrResult.Documents.First().DetectedLanguages.First().Name;
                var languageCode = lrResult.Documents.First().DetectedLanguages.First().Iso639Name;


                SentimentClient textClient = new SentimentClient(ApiKeys.TextAnalyticsApiKey);
                textClient.Url = "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

                SentimentRequest sr = new SentimentRequest();

                sr.Documents.Add(new SentimentDocument()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = TextAnalyticsText,
                    Language = languageCode
                });

                var result = await textClient.GetSentimentAsync(sr);
                TextAnalyticsResult = $"Language: {language}, Sentiment Score: {result.Documents.First().Score}"; 

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
