using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Views;
using NativeMedia;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.ViewModels
{
    public class CameraViewModel
    {
        public Record Rec;
        public int FormFieldId;
        public string BinaryDataId;
        private SfImageEditorPage ImageEditor { get; set; }

        public CameraViewModel(Record rec, int formFieldId, string binaryDataId)
        {
            Rec = rec;
            FormFieldId = formFieldId;
            BinaryDataId = binaryDataId;
        }

        public void SwitchView()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var ie = new SfImageEditorPage(FormFieldId, BinaryDataId, Rec.recordId);
                Shell.Current.Navigation.PushAsync(ie);
            });
        }

        public async void TakePhoto_Clicked(object sender, EventArgs e)
        {
            await TakePhotoAsync();
        }

        public async void BrowsePhoto_Clicked(object sender, EventArgs e)
        {
            await PickPhotoAsync();
        }

        public void MakeSketch_Clicked(object sender, EventArgs e)
        {
            SwitchView();
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
                    if (success) { SwitchView(); }
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
                        await SaveToFile(arr);
                        SwitchView();
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
                    //Rec.binaries = conn.Table<BinaryData>().Where(x => x.record_fk == Rec.Id).ToList();
                    Rec.binaries = await BinaryData.FetchBinaryDataByRecordId(Rec.recordId);
                    //Rec.texts = conn.Table<TextData>().Where(x => x.record_fk == Rec.Id).ToList();
                    Rec.texts = await TextData.FetchTextDataByRecordId(Rec.recordId);
                    //Rec.booleans = conn.Table<BooleanData>().Where(x => x.record_fk == Rec.Id).ToList();
                    Rec.booleans = await BooleanData.FetchBooleanDataByRecordId(Rec.recordId);
                    //Rec.numerics = conn.Table<NumericData>().Where(x => x.record_fk == Rec.Id).ToList();
                    Rec.numerics = await NumericData.FetchNumericDataByRecordId(Rec.recordId);
                    var conn = App.ActiveDatabaseConnection;
                    if (BinaryDataId == null)
                    {
                        BinaryData binDat = new BinaryData();
                        binDat.record_fk = Rec.recordId;
                        binDat.formFieldId = FormFieldId;
                        await conn.InsertOrReplaceAsync(binDat);
                        BinaryDataId = binDat.binaryId;
                        Rec.binaries.Add(binDat);
                    }
                    BinaryData.SaveData(arr, BinaryDataId);
                    Rec.timestamp = DateTime.Now;
                    Rec.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
                    Rec.userName = App.CurrentUser.userId;
                    if (Rec.status != -1)
                    {
                        Rec.status = 2;
                    }
                    await conn.InsertOrReplaceAsync(Rec);
                return true;
            }
            catch (Exception e)
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
                return false;
            }
        }
    }
}

