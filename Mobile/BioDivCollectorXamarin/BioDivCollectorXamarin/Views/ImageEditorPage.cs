using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BioDivCollectorXamarin;
using BioDivCollectorXamarin.Models.DatabaseModel;
using NativeMedia;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Syncfusion.SfImageEditor.XForms;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.Views
{
    public class SfImageEditorPage : ContentPage
    {
        private string Filename;
        private int RecordId { get; set; }
        private string BinaryDataId { get; set; }
        private int FormFieldId { get; set; }
        private SfImageEditor Editor { get; set; }

        public SfImageEditorPage(int formFieldId, string binaryId, int recordId)
        {
            Title = "Bildverarbeitung";
            Editor = new SfImageEditor();
            FormFieldId = formFieldId;
            BinaryDataId = binaryId;
            RecordId = recordId;
            if (binaryId == null)
            {
                Editor.Source = ImageSource.FromStream(() => new MemoryStream(new byte[1048576]));
                Content = Editor;
                Editor.ImageSaved += this.Editor_ImageSaved;
                Editor.ImageSaving += this.ImageEditor_ImageSaving;
                Editor.ToolbarSettings.ToolbarItemSelected += ToolbarSettings_ToolbarItemSelected;
                AddToolbarItems();
            }
            else
            {
                GetImage();
            }
      
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.Navigation.PopAsync(true);
                    });
                })
            });
        }

        private void CreateSketchIfNull()
        {
            Device.BeginInvokeOnMainThread((Action)(() =>
            {
                Editor.Source = ImageSource.FromStream(() => new MemoryStream(new byte[1048576]));
                Content = Editor;
                Editor.ImageSaved += this.Editor_ImageSaved;
                Editor.ImageSaving += this.ImageEditor_ImageSaving;
                Editor.ToolbarSettings.ToolbarItemSelected += ToolbarSettings_ToolbarItemSelected;
                AddToolbarItems();
            }));
        }

        private void GetImage()
        {
            Device.BeginInvokeOnMainThread((Action)(() =>
            {
                try
                {
                    //Editor = new SfImageEditor();
                    var directory = DependencyService.Get<FileInterface>().GetImagePath();
                    string filepath = Path.Combine(directory, BinaryDataId + ".jpg");
                    if (File.Exists(filepath))
                    {
                        Editor.Source = ImageSource.FromFile(filepath);
                        Content = Editor;
                    }
                }
                catch
                {

                }
                Editor.ImageSaved += this.Editor_ImageSaved;
                Editor.ImageSaving += this.ImageEditor_ImageSaving;
                Editor.ToolbarSettings.ToolbarItemSelected += ToolbarSettings_ToolbarItemSelected;
                AddToolbarItems();
            }));
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, true);
        }

        private void AddToolbarItems()
        {
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
            }
        }

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

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }

        void Editor_ImageSaved(object sender, ImageSavedEventArgs args)
        {
        }

        private void ImageEditor_ImageSaving(object sender, ImageSavingEventArgs args)
        {
            Task.Run(async () =>
            {
                args.Cancel = true;
                var stream = args.Stream;
                await SaveStreamToFile(BinaryDataId, stream);
            });
        }

        public async Task<bool> SaveStreamToFile(string binaryId, Stream stream)
        {
            try
            {
                if (BinaryDataId == null)
                {
                    BinaryData binDat = new BinaryData();
                    binDat.record_fk = RecordId;
                    binDat.formFieldId = FormFieldId;
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        conn.Insert(binDat);
                    }
                    BinaryDataId = binDat.binaryId;
                }
                var directory = DependencyService.Get<FileInterface>().GetImagePath();
                string filepath = Path.Combine(directory, BinaryDataId + ".jpg");

                if (stream.Length == 0) { return true; }

                // Create a FileStream object to write a stream to a file
                using (FileStream fileStream = System.IO.File.Create(filepath, (int)stream.Length))
                {

                    // Fill the bytes[] array with the stream data
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, (int)bytesInStream.Length);

                    // Use FileStream object to write to the specified file
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                }
                Record.UpdateRecord(RecordId);

            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
            }
            return true;
        }

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

                using (var file = await MediaGallery.CapturePhotoAsync())
                {
                    var stream = await file.OpenReadAsync();
                    var arr = stream.ToBytes();
                    SaveToAlbum(arr);
                    var success = await SaveToFile(arr);
                    if (success) {
                        GetImage();
                    }
                    else { }
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

        private async Task<bool> SaveToFile(byte[] arr)
        {
            try
            {

                if (BinaryDataId == null)
                {
                    BinaryData binDat = new BinaryData();
                    binDat.record_fk = RecordId;
                    binDat.formFieldId = FormFieldId;
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        conn.Insert(binDat);
                    }
                    BinaryDataId = binDat.binaryId;
                }
                BinaryData.SaveData(arr, BinaryDataId);
                Record.UpdateRecord(RecordId);
                return true;
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
                return false;
            }
        }
    }
}

