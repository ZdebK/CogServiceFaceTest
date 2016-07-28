using Microsoft.ProjectOxford.Face;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;
using System.Linq;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceApiApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly IFaceServiceClient _faceServiceClient = new FaceServiceClient("API_KEY_HERE");
        private StorageFile _imageFile;
        private SoftwareBitmap _bitmapSource;
        private const string _personGroupId = "family3";

        public MainPage()
        {
            this.InitializeComponent();
            IdentifyFace.IsEnabled = false;
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Thanks to Suresh-M at https://code.msdn.microsoft.com/File-Picker-in-Windows-10-846c2116
            // for filepicker code.
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;

            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".png");

            _imageFile = await openPicker.PickSingleFileAsync();

            if (_imageFile == null)
            {
                StatusField.Text = "Status: Image failed to load";
                return;
            }

            FaceCanvas.Children.Clear();

            var stream = await _imageFile.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            BitmapTransform transform = new BitmapTransform();
            const float sourceImageHeightLimit = 1280;

            if (decoder.PixelHeight > sourceImageHeightLimit)
            {
                float scalingFactor = (float)sourceImageHeightLimit / (float)decoder.PixelHeight;
                transform.ScaledWidth = (uint)Math.Floor(decoder.PixelWidth * scalingFactor);
                transform.ScaledHeight = (uint)Math.Floor(decoder.PixelHeight * scalingFactor);
            }

            _bitmapSource =
                await
                    decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, BitmapAlphaMode.Premultiplied, transform,
                        ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);

            ImageBrush brush = new ImageBrush();
            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(_bitmapSource);
            brush.ImageSource = bitmapSource;
            brush.Stretch = Stretch.Uniform;
            FaceCanvas.Background = brush;

            StatusField.Text = "Status: Image loaded";
        }

        private async void FindFaceButton_Click(object sender, RoutedEventArgs e)
        {
            FaceRectangle[] faceRects = await UploadAndDetectFaces();
            StatusField.Text = $"Detection Finished. {faceRects.Length} face(s) detected";

            if (faceRects.Length > 0)
            {
                MarkFaces(faceRects);
            }
        }

        private async Task<FaceRectangle[]> UploadAndDetectFaces()
        {
            try
            {
                StatusField.Text = "Status: Detecting...";
                using (Stream imageFileStream = await _imageFile.OpenStreamForReadAsync())
                {
                    var faces = await _faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);

                    return faceRects.ToArray();
                }
            }
            catch (Exception ex)
            {
                StatusField.Text = $"Status: {ex.Message}";
                return new FaceRectangle[0];
            }
        }
        
        private void MarkFaces(FaceRectangle[] faceRects)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(Colors.Green);
            SolidColorBrush fillBrush = new SolidColorBrush(Colors.Transparent);
            double lineThickness = 2.0;

            double dpi = _bitmapSource.DpiX;
            double resizeFactor = 96/dpi;

            if (faceRects != null)
            {
                double widthScale = _bitmapSource.PixelWidth / FaceCanvas.ActualWidth;
                double heightScale = _bitmapSource.PixelHeight / FaceCanvas.ActualHeight;

                foreach (var faceRectangle in faceRects)
                {
                    Rectangle box = new Rectangle
                    {
                        Width = (uint) (faceRectangle.Width / widthScale) - faceRectangle.Width,
                        Height = (uint) (faceRectangle.Height / heightScale),
                        Fill = fillBrush,
                        Stroke = lineBrush,
                        StrokeThickness = lineThickness,
                        Margin = new Thickness((uint)(faceRectangle.Left / widthScale) + faceRectangle.Width, (uint)(faceRectangle.Top / heightScale), 0, 0)
                };

                    FaceCanvas.Children.Add(box);
                }
            }
        }

        private async void IdentifyFace_Click(object sender, RoutedEventArgs e)
        {
            using (Stream s = await _imageFile.OpenStreamForReadAsync())
            {
                var faces = await _faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                StringBuilder resultText = new StringBuilder();

                var results = await _faceServiceClient.IdentifyAsync(_personGroupId, faceIds);

                if (results.Length > 0)
                    resultText.Append($"{results.Length} face(s) detected! \t");

                foreach(var identityResult in results)
                {
                    if (identityResult.Candidates.Length != 0)
                    {
                        var candidateId = identityResult.Candidates[0].PersonId;
                        var person = await _faceServiceClient.GetPersonAsync(_personGroupId, candidateId);
                        resultText.Append($"Detected: {person.Name}\t");
                    }
                }

                if (resultText.Length == 0)
                    resultText.Append("No persons identified");

                NameField.Text = resultText.ToString();
            }
        }

        private async void GeneratePersonGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var personGroup = await _faceServiceClient.GetPersonGroupAsync(_personGroupId);

            if(personGroup == null)
                await _faceServiceClient.CreatePersonGroupAsync(_personGroupId, "Family 3");
            
            var lady3 = await _faceServiceClient.CreatePersonAsync(_personGroupId, "Lily");
            var man3 = await _faceServiceClient.CreatePersonAsync(_personGroupId, "Marshall");

            string lady3ImageDir = @"./PersonGroup/Family3-Lady/";

            foreach(var path in Directory.GetFiles(lady3ImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(path))
                {
                    await _faceServiceClient.AddPersonFaceAsync(_personGroupId, lady3.PersonId, s);
                }
            }

            string man3ImageDir = @"./PersonGroup/Family3-Man/";

            foreach (var path in Directory.GetFiles(man3ImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(path))
                {
                    await _faceServiceClient.AddPersonFaceAsync(_personGroupId, man3.PersonId, s);
                }
            }

            NameField.Text = "Generated person group";
        }

        private async void TrainGroupButton_Click(object sender, RoutedEventArgs e)
        {
            await _faceServiceClient.TrainPersonGroupAsync(_personGroupId);

            TrainingStatus status = null;

            while(true)
            {
                status = await _faceServiceClient.GetPersonGroupTrainingStatusAsync(_personGroupId);
                
                if(status.Status.ToString() != "running")
                {
                    NameField.Text = "Person group training complete";
                    IdentifyFace.IsEnabled = true;
                    break;
                }

                await Task.Delay(1000);
            }
        }
    }
}
