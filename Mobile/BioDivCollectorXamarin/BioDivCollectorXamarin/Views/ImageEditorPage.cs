using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using BioDivCollectorXamarin;
using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
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

        public SfImageEditorPage(int formFieldId, string binaryId)
        {
            Title = "Bildverarbeitung";
            //RecordId = recordId;
            FormFieldId = formFieldId;
            BinaryDataId = binaryId;
            //.Shell.SetPresentationMode(this, PresentationMode.ModalAnimated);
            //Shell.SetNavBarIsVisible(this, true);
            /*var button1 = new Xamarin.Forms.ToolbarItem();
            button1.Text = "Schliessen";
            button1.Command = new Command(() =>
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.Navigation.PopAsync();
                }

                )
            );
            Shell.Current.ToolbarItems.Add(button1);*/

            Editor = new SfImageEditor();
            Device.BeginInvokeOnMainThread((Action)(() =>
            {
                try
                {
                    var directory = DependencyService.Get<FileInterface>().GetImagePath();
                    string filepath = Path.Combine(directory, binaryId + ".jpg");
                    if (File.Exists(filepath))
                    {
                        Editor.Source = ImageSource.FromFile(filepath);
                        Content = Editor;
                    }
                }
                catch
                {

                }

                Content = Editor;
                Editor.ImageSaved += this.Editor_ImageSaved;
                Editor.ImageSaving += this.ImageEditor_ImageSaving;
            }));

            /*var rec = Record.FetchRecord(RecordId);
            ContentPage recpage = new RecordListPage(rec.geometry_fk);
            if (rec.geometry_fk == String.Empty || rec.geometry_fk == "0")
            {
                recpage = new UnlocalisedRecordListPage();
            }
            var formpage = new FlexFormPage(RecordId, rec.geometry_fk, rec.formId);

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (Editor.IsImageEdited)
                        {
                            var response = await App.Current.MainPage.DisplayAlert("Zurück", "Daten speichern?", "Speichern", "Abbrechen");
                            if (response == true)
                            {
                                Editor.Save();
                            }
                        }

                        await Shell.Current.Navigation.PopToRootAsync(false);

                        await Shell.Current.Navigation.PushAsync(recpage, false);

                        await Shell.Current.Navigation.PushAsync(formpage, false);
                    });
                })
            });*/
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, true);
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
                SaveStreamToFile(BinaryDataId, stream);
            });
        }

        public void SaveStreamToFile(string binaryId, Stream stream)
        {

            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, binaryId + ".jpg");

            if (stream.Length == 0) return;

            // Create a FileStream object to write a stream to a file
            using (FileStream fileStream = System.IO.File.Create(filepath, (int)stream.Length))
            {

                // Fill the bytes[] array with the stream data
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, (int)bytesInStream.Length);

                // Use FileStream object to write to the specified file
                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            }
        }
    }
}

