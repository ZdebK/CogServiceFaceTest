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

        public MainPage()
        {
            this.InitializeComponent();
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

            if (_imageFile != null)
            {
                var stream = await _imageFile.OpenAsync(FileAccessMode.Read);
                var image = new BitmapImage();
                image.SetSource(stream);
                FacePhoto.Source = image;

                StatusField.Text = "Status: Image loaded";
            }
            else
            {
                StatusField.Text = "Status: Image failed to load";
            }
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
            //DrawingVisual visual = new DrawingVisual();
            //DrawingContext drawingContext = visual.RenderOpen();
            //drawingContext.DrawImage(bitmapSource,
            //    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
            //double dpi = bitmapSource.DpiX;
            //double resizeFactor = 96 / dpi;

            //foreach (var faceRect in faceRects)
            //{
            //    drawingContext.DrawRectangle(
            //        Brushes.Transparent,
            //        new Pen(Brushes.Red, 2),
            //        new Rect(
            //            faceRect.Left * resizeFactor,
            //            faceRect.Top * resizeFactor,
            //            faceRect.Width * resizeFactor,
            //            faceRect.Height * resizeFactor
            //            )
            //    );
            //}

            //drawingContext.Close();
            //RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
            //    (int)(bitmapSource.PixelWidth * resizeFactor),
            //    (int)(bitmapSource.PixelHeight * resizeFactor),
            //    96,
            //    96,
            //    PixelFormats.Pbgra32);

            //faceWithRectBitmap.Render(visual);
            //FacePhoto.Source = faceWithRectBitmap;
        }
    }
}
