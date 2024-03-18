using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using BioDivCollector.Connector.Services;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;

namespace BioDivCollector.Connector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[AllowAnonymous]
    public class BinaryController : ControllerBase
    {
        private readonly BioDivContext _context;
        private readonly ILogger _logger;
        private readonly IStorageService _storage;

        public BinaryController(BioDivContext context, ILogger<ProjectController> logger, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _storage = storageService;
        }

        /// <summary>
        /// Get binary data from object storage
        /// </summary>
        /// <param name="containerId">binary or company Id</param>
        /// <param name="thumbnail">true: resize to 400px; false: original image (default)</param>
        /// <param name="type">binarydata (default) = 0, plan = 1, or company = 9</param>
        /// <returns></returns>
        [HttpGet("{containerId}/{thumbnail?}")]
        public async Task<ActionResult> GetBinaryAsync(Guid containerId, Boolean thumbnail = false, BinaryType type = BinaryType.binarydata)
        {
            try
            {
                string userName = ((ClaimsIdentity)User.Identity).Name;
                _logger.LogInformation("BINARY GET:\tbinaryid = '{id}', thumb = '{thumbnail}', user = '{userName}' start...", containerId, thumbnail, userName);

                FileStreamResult result;
                Guid objectId = Guid.NewGuid();

                if (type == BinaryType.binarydata)
                {
                    BinaryData binary = await _context.BinaryData
                                        .AsNoTracking()
                                        .SingleOrDefaultAsync(b => b.Id == containerId);
                    if (binary != null && binary.ValueId != null)
                    {
                        objectId = (Guid)binary.ValueId;
                    }
                    else
                    {
                        _logger.LogError("BINARY GET:\tbinaryid = '{id}', thumb = '{thumbnail}' not found", containerId, thumbnail);
                        return NotFound();
                    }
                }
                else
                {
                    throw new NotImplementedException(type.ToString() + " not implemented");
                }

                (MemoryStream memoryStream, string originalFilename) = await _storage.LoadAsync(objectId);
                if (memoryStream != null)
                {
                    // get file content type
                    string contentType;
                    new FileExtensionContentTypeProvider().TryGetContentType(originalFilename, out contentType);

                    if (thumbnail && contentType.Contains("image/"))// == "image/jpeg")
                    {
                        MemoryStream memoryStreamThumb = makeJPEGThumbnail(memoryStream);
                        {
                            result = new FileStreamResult(memoryStreamThumb, contentType);
                        }
                    }
                    else
                    {
                        result = new FileStreamResult(memoryStream, contentType);
                    }

                    // filename
                    string imageFileName = LocalStorageService.makeValidFileName(Path.GetFileNameWithoutExtension(originalFilename));
                    if (thumbnail)
                        result.FileDownloadName = $"{imageFileName}_small{Path.GetExtension(originalFilename)}";
                    else
                        result.FileDownloadName = $"{imageFileName}{Path.GetExtension(originalFilename)}";

                    _logger.LogInformation("BINARY GET:\tbinaryid = '{id}', thumb = '{thumbnail}' finished", containerId, thumbnail);
                    return result;
                }
                else
                {
                    _logger.LogError("BINARY GET:\tbinaryid = '{id}', thumb = '{thumbnail}' is empty", containerId, thumbnail);
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("BINARY GET:\tbinaryid = '{id}', thumb = '{thumbnail}'\n{ex}", containerId, thumbnail, ex.ToString());
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Upload binary data to object storage
        /// </summary>
        /// <param name="file"></param>
        /// <param name="containerId">binary or company Id, must already be in the db</param>
        /// <param name="type">binarydata (default) = 0, plan = 1, or company = 9</param>
        /// <returns></returns>
        [HttpPost("{containerId}")]
        public async Task<ActionResult> UploadAsync(IFormFile file, Guid containerId, BinaryType type = BinaryType.binarydata)
        {
            try
            {
                string userName = ((ClaimsIdentity)User.Identity).Name;
                _logger.LogInformation("BINARY POST:\tbinaryid = '{id}', type = {type}, user = '{userName}' start...", containerId, type, userName);

                if (file == null)
                {
                    _logger.LogError("BINARY POST:\tbinaryid = '{id}', no file provided", containerId);
                    return BadRequest("no file provided");
                }

                ObjectStorage newObjectStorage = new ObjectStorage();

                if (type == BinaryType.binarydata)
                {
                    var binary = await _context.BinaryData
                        .Include(b => b.Value)
                        .Include(b => b.Record)
                            .ThenInclude(r => r.Geometry)
                        .SingleOrDefaultAsync(b => b.Id == containerId);

                    if (binary != null)
                    {
                        if (binary.Value != null)   //if there is an already set object storage 
                        {
                            await this.DeleteContentAsync((Guid)binary.Id); //delete object storage
                        }

                        string savedFilePath = binary.Record.RecordId.ToString();

                        newObjectStorage = new ObjectStorage()
                        {
                            ObjectStorageId = Guid.NewGuid(),
                            OriginalFileName = file.FileName,
                            SavedFileName = binary.Id + Path.GetExtension(file.FileName),
                            SavedFilePath = savedFilePath
                        };
                        binary.Value = newObjectStorage;
                        _context.ObjectStorage.Add(newObjectStorage);
                    }
                    else
                    {
                        _logger.LogError("BINARY POST:\tbinaryid = '{id}' not found", containerId);
                        return BadRequest($"binaryid {containerId} not found");
                    }
                }
                else
                {
                    throw new NotImplementedException(type.ToString() + " not implemented");
                }

                
                await _context.SaveChangesAsync();
                await _storage.UploadAsync(file, newObjectStorage.ObjectStorageId);     //save the file

                double fileSizeMB = Math.Round((file.Length / 1024f) / 1024f, 3);
                _logger.LogInformation("BINARY POST:\tbinaryid = '{id}', type = {type}, size = '{fileSizeMB} MB' saved", containerId, type, fileSizeMB);
                return Ok($"saved {fileSizeMB:F3} MB, id {containerId}");

            }
            catch (Exception ex)
            {
                _logger.LogError("BINARY POST:\tbinaryid = '{id}'\n{ex}", containerId, ex.ToString());
                return StatusCode(500);
            }

        }

        /// <summary>
        /// delete object storage of binary data. Binary data remains.
        /// </summary>
        /// <param name="binaryid"></param>
        /// <returns></returns>
        [HttpDelete("{binaryid}")]
        public async Task<ActionResult> DeleteContentAsync(Guid binaryid, string userName = null)
        {
            try
            {
                if (userName == null)
                {
                    userName = ((ClaimsIdentity)User.Identity).FindFirst("preferred_username").Value;
                }
                _logger.LogInformation("BINARY DELETE:\tbinaryid = '{id}', user = '{userName}' start...", binaryid, userName);

                var binary = await _context.BinaryData
                    .Include(b => b.Value)
                    .Where(b => b.Id == binaryid)
                    .SingleOrDefaultAsync();

                var objectStorage = binary.Value;

                if (await _storage.DeleteAsync(objectStorage.ObjectStorageId))
                {
                    binary.Value = null;                            //remove link from binary data to object storage
                    _context.ObjectStorage.Remove(objectStorage);   //delete table entry
                    _context.Entry(binary).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("BINARY DELETE:\tbinaryid = '{id}' finished", binaryid);
                    return Ok();
                }
                else
                {
                    _logger.LogError("BINARY DELETE:\tbinaryid = '{id}'\n, file deletion not successfull", binaryid);
                    return StatusCode(500);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError("BINARY DELETE:\tbinaryid = '{id}'\n{ex}", binaryid, ex.ToString());
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get SHA256 checksum for binary object
        /// </summary>
        /// <param name="binaryid">binary data id</param>
        /// <returns>SHA256 string</returns>
        [HttpGet("checksum/{binaryid}")]
        public async Task<IActionResult> SHA256CheckSumAsync(Guid binaryid)
        {
            try
            {
                BinaryData binary = await _context.BinaryData
                    .AsNoTracking()
                    .SingleOrDefaultAsync(b => b.Id == binaryid);

                if (binary != null && binary.ValueId != null)
                {
                    string userName = ((ClaimsIdentity)User.Identity).Name;
                    _logger.LogInformation("BINARY SHA256 GET:\tobjectyid = '{id}', user = '{userName}' start...", binaryid, userName);

                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    {
                        using (var stream = (await _storage.LoadAsync((Guid)binary.ValueId)).Item1)
                        {
                            stream.Seek(0, SeekOrigin.Begin);

                            var hash = sha.ComputeHash(stream);
                            _logger.LogInformation("BINARY SHA256 GET:\tbinaryid = '{id}' finished", binaryid);

                            string value = BitConverter.ToString(hash).Replace("-", "");
                            return Ok(value);
                        }
                    }
                }
                throw new Exception("object storage not found or empty");
            }
            catch (Exception ex)
            {
                _logger.LogError("BINARY SHA256 GET:\tbinaryid = '{id}'\n{ex}", binaryid, ex.ToString());
                return StatusCode(500);
            }
        }


        /// <summary>
        /// JPEG resize
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="maxSize">set maximal width or height (pixel), default: 400</param>
        /// <returns></returns>
        private MemoryStream makeJPEGThumbnail(MemoryStream stream, int maxSize = 400)
        {
            MemoryStream memoryStream = new MemoryStream();

            Image img = Image.FromStream(stream);
            stream.Dispose();

            int imgHeight = maxSize;
            int imgWidth = maxSize;
            if (img.Width < img.Height)
            {
                //portrait image  
                imgHeight = maxSize;
                var imgRatio = (float)imgHeight / (float)img.Height;
                imgWidth = Convert.ToInt32(img.Height * imgRatio);
            }
            else if (img.Height < img.Width)
            {
                //landscape image  
                imgWidth = maxSize;
                var imgRatio = (float)imgWidth / (float)img.Width;
                imgHeight = Convert.ToInt32(img.Height * imgRatio);
            }
            Image thumb = img.GetThumbnailImage(imgWidth, imgHeight, () => false, IntPtr.Zero);
            //img.Dispose();

            thumb.Save(memoryStream, ImageFormat.Jpeg);
            memoryStream.Position = 0;
            return memoryStream;
        }

    }

    public enum BinaryType
    {
        binarydata = 0,
        plan = 1,
        company = 9
    }
}


