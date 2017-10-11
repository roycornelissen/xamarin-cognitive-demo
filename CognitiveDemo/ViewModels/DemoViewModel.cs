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

namespace CognitiveDemo
{
    public class DemoViewModel : BaseViewModel
    {
        EmotionServiceClient emotionClient;

        public ObservableCollection<Item> Items { get; set; }
        public Command CheckFaceApi { get; set; }
        public Command CheckEmotionApi { get; set; }

        MediaFile photo;

        string error;
        public string Error
        {
            get { return error; }
            set { SetProperty(ref error, value); }
        }

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


        public DemoViewModel()
        {
            Title = "Cognitive Demos";
            Items = new ObservableCollection<Item>();
            CheckFaceApi = new Command(async () => await ExecuteCheckFaceApiCommand());
            CheckEmotionApi = new Command(async () => await ExecuteCheckEmotionApiCommand());

            emotionClient = new EmotionServiceClient(ApiKeys.EmotionApiKey);

        }

        async Task ExecuteCheckFaceApiCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                
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
                    //photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                    //{
                    //    Name = "emotion.jpg",
                    //    PhotoSize = PhotoSize.Small
                    //});
                    photo = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                    {
                        PhotoSize = PhotoSize.Custom,
                        CustomPhotoSize = 40,
                        Directory = "Sample",
                        Name = "test.jpg"
                    });

                    if (photo != null)
                    {
                        EmotionPicture = ImageSource.FromStream(photo.GetStream);
                    }
                }
                else
                {
                    error= "Camera unavailable.";
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
    }
}
