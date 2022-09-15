using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.Connector.Services
{
    public class LocalStorageService : IStorageService
    {

        private readonly IConfiguration _configuration;
        private readonly BioDivContext _context;

        /// <summary>
        /// e.g. C:\ProgramData\FeldAppStorage
        /// </summary>
        public string Root
        {
            get
            {
                var rootpath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);    //C:\ProgramData
                //string rootpath = AppDomain.CurrentDomain.BaseDirectory;
                string containerName = _configuration["Storage:ContainerName"];
                string rootContainerName = Path.Combine(rootpath, containerName);
                return rootContainerName;
            }
        }

        public LocalStorageService(IConfiguration configuration, BioDivContext context)
        {
            this._configuration = configuration;
            this._context = context;
        }

        public async Task<bool> UploadAsync(IFormFile file, Guid objectstorageid)
        {
            //if (!Directory.Exists(Root))
            //{
            //    Directory.CreateDirectory(Root);
            //}

            ObjectStorage objectStorage = await _context.ObjectStorage.SingleOrDefaultAsync(o => o.ObjectStorageId == objectstorageid);
            if (objectStorage == null || objectStorage.IsNotSet)
                return false;            

            using (var stream = file.OpenReadStream())
            {
                string filePath = Path.Combine(Root, objectStorage.SavedFilePath, objectStorage.SavedFileName);

                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }

                using (var fileStream = File.Create(filePath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
            }
            return true;
        }

        public async Task<bool> DeleteAsync(Guid objectstorageid)
        {           
            ObjectStorage objectStorage = await _context.ObjectStorage.SingleOrDefaultAsync(o => o.ObjectStorageId == objectstorageid);
            if (objectStorage == null)
                return false;
            else if (objectStorage.IsNotSet)
                return true;

            DirectoryInfo rootDir = new DirectoryInfo(this.Root);
            string fullfilepath = objectStorage.SavedFileName;
            if (objectStorage.SavedFilePath != null)
                fullfilepath = Path.Combine(objectStorage.SavedFilePath, fullfilepath);
            FileInfo[] filesInDir = rootDir.GetFiles(fullfilepath);
            foreach (FileInfo fileInfo in filesInDir)
            {
                fileInfo.Delete();
            }

            objectStorage.ResetObjectStorage();

            if (await _context.SaveChangesAsync() > 0)
                return true;
            else
                return false;
        }

        public async Task<Image> LoadImage(Guid id)
        {
            using (MemoryStream stream = (await LoadAsync(id)).Item1)
            {
                Image image = Image.FromStream(stream);
                return image;
            }
        }

        public async Task<(MemoryStream,string)> LoadAsync(Guid objectstorageid)
        {
            ObjectStorage objectStorage = await _context.ObjectStorage.AsNoTracking().SingleOrDefaultAsync(o => o.ObjectStorageId == objectstorageid);
            if (objectStorage == null || objectStorage.IsNotSet)
                return (null,null);

            DirectoryInfo rootDir = new DirectoryInfo(this.Root);
            string fullFilePath = objectStorage.SavedFileName;
            if (objectStorage.SavedFilePath != null)
                fullFilePath = Path.Combine(objectStorage.SavedFilePath, fullFilePath);

            FileInfo[] filesInDir = rootDir.GetFiles(fullFilePath);
            if (filesInDir.Length != 1)
            {
                return (null, null);
            }

            //load the file
            string rootFullFilePath = Path.Combine(this.Root, fullFilePath); 
            using (FileStream fs = File.OpenRead(rootFullFilePath))
            {
                MemoryStream ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                //ms.Position = 0;
                return (ms, objectStorage.OriginalFileName);
            }
        }

        /// <summary>
        /// replace invalid characters
        /// </summary>
        /// <param name="input"></param>
        /// <returns>valid filename, without extension</returns>
        public static string makeValidFileName(string input)
        {
            string output = input.Trim();

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                output = output.Replace(c, '_');
            }

            output = output.Replace(' ', '_');
            output = output.Replace('(', '_');
            output = output.Replace(')', '_');
            output = output.Replace('.', '_');
            output = output.Replace(':', '_');
            output = output.Replace(',', '_');
            output = output.Replace('/', '_');
            output = output.Replace('\\', '_');
            output = output.Replace('*', '_');
            output = output.Replace('"', '_');
            output = output.Replace('<', '_');
            output = output.Replace('>', '_');
            output = output.Replace('|', '_');

            return output;
        }


    }
}
