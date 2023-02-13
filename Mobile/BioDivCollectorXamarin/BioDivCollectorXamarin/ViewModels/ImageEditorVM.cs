using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using NativeMedia;
using Syncfusion.SfImageEditor.XForms;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.ViewModels
{
    public class ImageEditorViewModel:BaseViewModel
    {
        public SfImageEditor Editor { get; set; }
        public string Filename;
        public string RecordId { get; set; }
        public string BinaryDataId { get; set; }
        public int FormFieldId { get; set; }

        public ImageEditorViewModel()
        {
        }


        /// <summary>
        /// Create the image editor
        /// </summary>
        public SfImageEditor CreateEditor()
        {
            Editor = new SfImageEditor();
            Editor.ImageSaving += this.ImageEditor_ImageSaving;
            Editor.ImageLoaded += this.Editor_ImageLoaded;
            return Editor;
        }

        /// <summary>
        /// Save the file when the save button is pressed in the editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void ImageEditor_ImageSaving(object sender, ImageSavingEventArgs args)
        {
                args.Cancel = true;
                var stream = args.Stream;
                await SaveStreamToFile(BinaryDataId, stream);
        }

        /// <summary>
        /// Rotate Android images according to Exif
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Editor_ImageLoaded(object sender, ImageLoadedEventArgs args)
        {
            RotateImage();
        }


        /// <summary>
        /// Rotates the image according to its Exif data
        /// </summary>
        public void RotateImage()
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, BinaryDataId + ".jpg");
            var rotation = DependencyService.Get<CameraInterface>().GetImageRotation(filepath);
            if (rotation >= 90)
            {
                Editor.Rotate();
                if (rotation >= 180)
                {
                    Editor.Rotate();
                    if (rotation >= 270)
                    {
                        Editor.Rotate();
                    }
                }
            }
        }

        /// <summary>
        /// Add the camera and album buttons to the toolbar
        /// </summary>
        public void AddToolbarItems()
        {
            if (Editor.ToolbarSettings.ToolbarItems[Editor.ToolbarSettings.ToolbarItems.Count - 1].Name == "Album")
            {
                //Remove last two toolbar items
                Editor.ToolbarSettings.ToolbarItems.Remove(Editor.ToolbarSettings.ToolbarItems[Editor.ToolbarSettings.ToolbarItems.Count - 1]);
                Editor.ToolbarSettings.ToolbarItems.Remove(Editor.ToolbarSettings.ToolbarItems[Editor.ToolbarSettings.ToolbarItems.Count - 1]);
            }
            if (Editor.ToolbarSettings.ToolbarItems.Count == 9)
            {
                Editor.ToolbarSettings.ToolbarItems.Insert(0, new FooterToolbarItem()
                {
                    Icon = new FontImageSource
                    {
                        Glyph = "\ue412",
                        FontFamily = "Material",
                        Size = 44,
                        Color = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"]
                    },
                    Name = "Camera"
                });
                Editor.ToolbarSettings.ToolbarItems.Insert(1, new FooterToolbarItem()
                {
                    Icon = new FontImageSource
                    {
                        Glyph = "\ue413",
                        FontFamily = "Material",
                        Size = 44,
                        Color = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"]
                    },
                    Name = "Album"
                });
                Editor.ToolbarSettings.ToolbarItemSelected -= ToolbarSettings_ToolbarItemSelected;
                Editor.ToolbarSettings.ToolbarItemSelected += ToolbarSettings_ToolbarItemSelected;
            }

        }

        /// <summary>
        /// Determine what to do when the toolbar buttons are pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToolbarSettings_ToolbarItemSelected(object sender, ToolbarItemSelectedEventArgs e)
        {
            if (e.ToolbarItem.Name == "Camera")
            {
                await TakePhotoAsync();
            }
            else if (e.ToolbarItem.Name == "Album")
            {
                await PickPhotoAsync();
            }
        }

        /// <summary>
        /// Take a picture using the camera and save the image
        /// </summary>
        /// <returns></returns>
        public async Task TakePhotoAsync()
        {
            try
            {
                if (!MediaGallery.CheckCapturePhotoSupport())
                    return;

                var status = await Permissions.RequestAsync<Permissions.Camera>();
                var status2 = await Permissions.RequestAsync<SaveMediaPermission>();

                if (status != PermissionStatus.Granted || status2 != PermissionStatus.Granted)
                    return;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    using (var file = await MediaGallery.CapturePhotoAsync())
                    {
                        var stream = await file.OpenReadAsync();
                        var arr = stream.ToBytes();
                        SaveToAlbum(arr);
                        var success = await SaveToFile(arr);
                        if (success)
                        {
                            GetImage();
                        }
                    }
                });
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature is not supported on the device
            }
            catch (PermissionException pEx)
            {
                // Permissions not granted
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }


        public void GetData()
        {
            if (BinaryDataId == null)
            {
                GetSketch();
            }
            else
            {
                GetImage();
            }
        }

        /// <summary>
        /// Get the image relating to the binary Id
        /// </summary>
        private void GetImage()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var directory = DependencyService.Get<FileInterface>().GetImagePath();
                    string filepath = Path.Combine(directory, BinaryDataId + ".jpg");

                    App.CurrentRoute = $"//Records/Form/ImageEditor?formid={FormFieldId}&recid={RecordId}&binaryid={BinaryDataId}";
                    if (System.IO.File.Exists(filepath))
                    {
                        Editor.Source = ImageSource.FromFile(filepath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                AddToolbarItems();
            });
        }

        /// <summary>
        /// Create a blank sketch
        /// </summary>
        private void GetSketch()
        {
            Editor.Source = ImageSource.FromStream(() => new MemoryStream(new byte[1048576]));
        }

        /// <summary>
        /// Pick a photo from the album (iOS) or file (Android)
        /// </summary>
        /// <returns></returns>
        public async Task PickPhotoAsync()
        {
            try
            {
                var cts = new CancellationTokenSource();
                IMediaFile[] files = null;

                try
                {
                    var request = new MediaPickRequest(1, MediaFileType.Image, MediaFileType.Video)
                    {
                        PresentationSourceBounds = System.Drawing.Rectangle.Empty,
                        UseCreateChooser = true,
                        Title = "Select"
                    };

                    cts.CancelAfter(TimeSpan.FromMinutes(5));

                    var results = await MediaGallery.PickAsync(request, cts.Token);
                    files = results?.Files?.ToArray();
                }
                catch (OperationCanceledException)
                {
                    // handling a cancellation request
                }
                catch (Exception)
                {
                    // handling other exceptions
                }
                finally
                {
                    cts.Dispose();
                }


                if (files == null)
                    return;

                foreach (var file in files)
                {
                    var fileName = file.NameWithoutExtension; //Can return an null or empty value
                    var extension = file.Extension;
                    var contentType = file.ContentType;
                    using (var stream = await file.OpenReadAsync())
                    {
                        //var success = await SaveStreamToFile(BinaryDataId, stream);
                        var arr = stream.ToBytes();
                        var success = await SaveToFile(arr);
                        if (success)
                        {
                            GetImage();
                        }
                        file.Dispose();
                    }
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature is not supported on the device
            }
            catch (PermissionException pEx)
            {
                // Permissions not granted
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }

        /// <summary>
        /// Save the photo to the photo album (iOS - does nothing in Android)
        /// </summary>
        /// <param name="arr"></param>
        private async void SaveToAlbum(byte[] arr)
        {
            try
            {
                DependencyService.Get<CameraInterface>().SaveToAlbum(arr);
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht im Album gespeichert werden", String.Empty, "OK");
            }
        }


        /// <summary>
        /// Save the image array to a file in the directory system
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        private async Task<bool> SaveToFile(byte[] arr)
        {
            try
            {
                await WriteBinaryRecord();
                BinaryData.SaveData(arr, BinaryDataId);
                UpdateRoute();
                return true;
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Speichern fehlgeschlagen", "Das Foto/Die Notiz konnte nicht als Datei gespeichert werden", "OK");
                return false;
            }
        }

        /// <summary>
        /// Save the image stream to a file in the directory system
        /// </summary>
        /// <param name="binaryId"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task<bool> SaveStreamToFile(string binaryId, Stream stream)
        {
            try
            {
                await WriteBinaryRecord();
                BinaryData.SaveData(stream, BinaryDataId);
                UpdateRoute();
                await App.Current.MainPage.DisplayAlert("Foto/Notiz gespeichert", "Das Foto/Die Notiz wurde auf dem Gerät gespeichert", "OK");
                return true;
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Speichern fehlgeschlagen", "Das Foto/Die Notiz konnte nicht als Datei gespeichert werden", "OK");
                return false;
            }
        }

        /// <summary>
        /// Write the binary record to the database
        /// </summary>
        private async Task WriteBinaryRecord()
        {
            if (BinaryDataId == null)
            {
                BinaryData binDat = new BinaryData();
                binDat.record_fk = RecordId;
                binDat.formFieldId = FormFieldId;
                await binDat.SaveBinaryRecord();
                BinaryDataId = binDat.binaryId;
            }
        }

        private void UpdateRoute()
        {
            App.CurrentRoute = $"//Records/Form/ImageEditor?formid={FormFieldId}&recid={RecordId}&binaryid={BinaryDataId}";
        }
    }
}
