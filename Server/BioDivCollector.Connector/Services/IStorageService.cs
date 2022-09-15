using Microsoft.AspNetCore.Http;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace BioDivCollector.Connector.Services
{
    public interface IStorageService
    {
        public string Root { get; }

        /// <summary>
        /// Save file to storage
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="objectstorageid"></param>
        /// <returns></returns>
        public Task<bool> UploadAsync(IFormFile formFile, Guid objectstorageid);

        public Task<Image> LoadImage(Guid id);

        /// <summary>
        /// Load file from storage
        /// </summary>
        /// <param name="objectstorageid"></param>
        /// <returns>Tuple with MemoryStream and original filename</returns>
        public Task<(MemoryStream, string)> LoadAsync(Guid objectstorageid);

        /// <summary>
        /// Delete file from storage
        /// </summary>
        /// <param name="objectstorageid"></param>
        /// <returns></returns>
        public Task<bool> DeleteAsync(Guid objectstorageid);
    }
}
