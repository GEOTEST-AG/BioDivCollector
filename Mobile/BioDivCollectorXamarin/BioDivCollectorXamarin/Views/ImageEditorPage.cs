using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using NativeMedia;
using SQLite;
using Syncfusion.SfImageEditor.XForms;
using Xamarin.Auth.OAuth2;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using static System.Net.WebRequestMethods;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.Views
{
    [QueryProperty(nameof(RecIdString), "recid")]
    [QueryProperty(nameof(BinaryIdString), "binaryid")]
    [QueryProperty(nameof(FormIdString), "formid")]
    public class SfImageEditorPage : ContentPage
    {
        public string RecIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                RecordId = parsedValue;
            }
        }
        
        public string FormIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                FormFieldId = parsedValue;
            }
        }

        public string BinaryIdString
        {
            set
            {
                BinaryDataId = value;
            }
        }

        private string Filename;
        private int RecordId { get; set; }
        private string BinaryDataId { get; set; }
        private int FormFieldId { get; set; }
        private SfImageEditor Editor { get; set; }

        public SfImageEditorPage()
        {
            CreateEditor();
        }

        public SfImageEditorPage(int formFieldId, string binaryId, int recordId)
        {
            CreateEditor();
            FormFieldId = formFieldId;
            BinaryDataId = binaryId;
            RecordId = recordId;
        }

        /// <summary>
        /// Create the image editor
        /// </summary>
        private void CreateEditor()
        {
            Title = "Bildverarbeitung";
            Editor = new SfImageEditor();
            Editor.ImageSaving += this.ImageEditor_ImageSaving;
            Editor.ImageLoaded += this.Editor_ImageLoaded;
        }

        /// <summary>
        /// Set up the view
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Xamarin.Forms.NavigationPage.SetHasNavigationBar(this, true);
            var safeInsets = On<iOS>().SafeAreaInsets();
            Padding = safeInsets;
            AddToolbarItems();
            Shell.SetNavBarIsVisible(this, true);

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        var rec = Record.FetchRecord(RecordId);
                        await Shell.Current.GoToAsync($"..?formid={rec.formId}&recid={RecordId}&geomid={rec.geometry_fk}");
                    });
                })
            });
        }

        /// <summary>
        /// When the window is opened, either create a blank sheet, or load the image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (BinaryDataId == null)
            {
                Editor.Source = ImageSource.FromStream(() => new MemoryStream(new byte[1048576]));
                Content = Editor;
            }
            else
            {
                if (width > 0 && height > 0)
                {
                    GetImage();
                }
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
                    if (System.IO.File.Exists(filepath) && Editor.Width > 0 && Editor.Height > 0)
                    {
                        Editor.Source = ImageSource.FromFile(filepath);
                        Content = Editor;
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
        /// Rotate Android images according to Exif
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Editor_ImageLoaded(object sender, ImageLoadedEventArgs args)
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, BinaryDataId + ".jpg");
            var rotate = true;
            if (rotate)
            {
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
        }

        /// <summary>
        /// Add the camera and album buttons to the toolbar
        /// </summary>
        private void AddToolbarItems()
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
        /// Save the file when the save button is pressed in the editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ImageEditor_ImageSaving(object sender, ImageSavingEventArgs args)
        {
            Task.Run(async () =>
            {
                args.Cancel = true;
                var stream = args.Stream;
                await SaveStreamToFile(BinaryDataId, stream);
            });
        }

        /// <summary>
        /// Take a picture using the camera and save the image
        /// </summary>
        /// <returns></returns>
        private async Task TakePhotoAsync()
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

        /// <summary>
        /// Pick a photo from the album (iOS) or file (Android)
        /// </summary>
        /// <returns></returns>
        private async Task PickPhotoAsync()
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
                        if (success) {
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
                WriteBinaryRecord();
                BinaryData.SaveData(arr, BinaryDataId);
                UpdateRoute();
                return true;
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
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
                WriteBinaryRecord();
                BinaryData.SaveData(stream, BinaryDataId);
                UpdateRoute();
                return true;
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
                return false;
            }
        }

        /// <summary>
        /// Write the binary record to the database
        /// </summary>
        private void WriteBinaryRecord()
        {
            if (BinaryDataId == null)
            {
                BinaryData binDat = new BinaryData();
                binDat.record_fk = RecordId;
                binDat.formFieldId = FormFieldId;
                binDat.SaveBinaryRecord();
                BinaryDataId = binDat.binaryId;
            }
        }

        private void UpdateRoute()
        {
            App.CurrentRoute = $"//Records/Form/ImageEditor?formid={FormFieldId}&recid={RecordId}&binaryid={BinaryDataId}";
        }

    }
}

