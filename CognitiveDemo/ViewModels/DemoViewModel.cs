using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.Linq;

using Xamarin.Forms;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using Microsoft.CognitiveServices.Speech;

namespace CognitiveDemo
{
    public class DemoViewModel : BaseViewModel
    {
		const string baseApiUri = "https://eastus.api.cognitive.microsoft.com";
		const string faceApiUri = baseApiUri + "/face/v1.0/detect";
		const string speechUri = baseApiUri + "/sts/v1.0/issuetoken";

		public ObservableCollection<Item> Items { get; set; }
        public Command CheckFaceApi { get; set; }
        public Command CheckEmotionApi { get; set; }
        public Command CheckTextAnalyticsApi { get; set; }
		public Command CheckSpeechApi { get; set; }

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
			CheckSpeechApi = new Command(async () => await ExecuteCheckSpeechAnalyticsApiCommand());
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

						// Request parameters. A third optional parameter is "details".
						string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,hair,makeup,occlusion,accessories,blur,exposure,noise";

						HttpResponseMessage response;

						using (ByteArrayContent content = new ByteArrayContent(imagesBytes))
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
						var client = new FaceClient(new ApiKeyServiceClientCredentials(ApiKeys.FaceApiKey),
													new DelegatingHandler[] { })
						{
							Endpoint = baseApiUri
						};

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

			try
			{
				var speechConfig = SpeechConfig.FromEndpoint(new Uri(speechUri), ApiKeys.SpeechApiKey);
				var client = new SpeechRecognizer(speechConfig);
				var result = await client.RecognizeOnceAsync();
				SpeechAnalyticsResult = $"{result.Text}";
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
					Endpoint = baseApiUri
				};

				var input = new BatchInput
				{
					Documents = new List<Input> { new Input { Id = "1", Text = TextAnalyticsText } }
				};
				var lrResult = await client.DetectLanguageAsync(input);
				var language = lrResult.Documents.First().DetectedLanguages.First().Name;
				var languageCode = lrResult.Documents.First().DetectedLanguages.First().Iso6391Name;

				var sentimentInput = new MultiLanguageBatchInput
				{
					Documents = new List<MultiLanguageInput>
					{
						new MultiLanguageInput
						{
							Language = languageCode,
							Text = TextAnalyticsText,
							Id = "1"
						}
					}
				};
				var result = await client.SentimentAsync(sentimentInput);
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
