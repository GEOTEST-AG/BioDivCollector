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
            FormFieldId = formFieldId;
            BinaryDataId = binaryId;
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

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.Navigation.PopAsync(true);
                        /*if (Shell.Current.Navigation.NavigationStack[0].GetType() == typeof(CameraView))
                        {
                            await Shell.Current.Navigation.PopAsync(true);
                        }*/
                    });
                })
            });
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, true);

            Editor.ToolbarSettings.ToolbarItems.Insert(0, new FooterToolbarItem()
            {
                Icon = new FontImageSource {
                    Glyph = "\ue412",
                    FontFamily = "Material",
                    Size = 44
                }
            });
            Editor.ToolbarSettings.ToolbarItems.Insert(1, new FooterToolbarItem()
            {
                Icon = new FontImageSource
                {
                    Glyph = "\ue413",
                    FontFamily = "Material",
                    Size = 44
                }
            });
            /*Editor.ToolbarSettings.ToolbarItems.Move(3, 2);
        Editor.ToolbarSettings.ToolbarItems.Move(5, 3);
        Editor.ToolbarSettings.ToolbarItems.Move(5, 4);*/

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

