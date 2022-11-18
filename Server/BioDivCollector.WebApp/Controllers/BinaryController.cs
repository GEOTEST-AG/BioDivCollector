using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BioDivCollector.WebApp.Controllers
{
    public class BinaryController : Controller
    {
        private BioDivContext _context = new BioDivContext();
        public IConfiguration Configuration { get; }

        public HttpContext context { get; set; }

        public BinaryController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<IRestResponse> getImageResponse(Guid binaryid, Boolean thumbnail = false)
        {
            try
            {
                if (HttpContext != null) context = HttpContext;

                var refreshToken = context.GetTokenAsync("refresh_token");
                // Get Access-Token
                var client = new RestClient(Configuration["JWT:Admin-Token-Url"]);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", Configuration["JWT:Client"]);
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("refresh_token", refreshToken.Result);
                request.AddParameter("client_secret", Configuration["JWT:Key"]);
                IRestResponse response = client.Execute(request);

                dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                string newAccessToken = json.access_token.Value;

                var imageDownload = new RestClient(String.Format("https://testconnector.biodivcollector.ch/api/Binary/{0}/{1}", binaryid, thumbnail));
                var imageRequest = new RestRequest(Method.GET);
                imageRequest.AddHeader("Authorization", "Bearer " + newAccessToken);
                return imageDownload.Execute(imageRequest);

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<IRestResponse> deleteImageResponse(Guid binaryid)
        {
            try
            {
                if (HttpContext != null) context = HttpContext;
                var refreshToken = context.GetTokenAsync("refresh_token");
                // Get Access-Token
                var client = new RestClient(Configuration["JWT:Admin-Token-Url"]);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", Configuration["JWT:Client"]);
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("refresh_token", refreshToken.Result);
                request.AddParameter("client_secret", Configuration["JWT:Key"]);
                IRestResponse response = client.Execute(request);
                dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                string newAccessToken = json.access_token.Value;
                var imageDownload = new RestClient(String.Format("https://testconnector.biodivcollector.ch/api/Binary/{0}", binaryid));
                var imageRequest = new RestRequest(Method.DELETE);
                imageRequest.AddHeader("Authorization", "Bearer " + newAccessToken);
                return imageDownload.Execute(imageRequest);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpGet("/Binary/{binaryid}/{thumbnail?}")]
        public async Task<ActionResult> GetBinaryAsync(Guid binaryid, Boolean thumbnail = false)
        {
            try
            {
                IRestResponse imageResponse = await getImageResponse(binaryid, thumbnail);

                return File(imageResponse.RawBytes, "image/jpg");

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> SaveBinaryAsync(Guid binaryid, string path, Boolean thumbnail = false)
        {
            try
            {
                IRestResponse imageResponse = await getImageResponse(binaryid, thumbnail);
                System.IO.File.WriteAllBytes(path, imageResponse.RawBytes);
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [HttpGet("/Binary/Delete/{binaryid}")]
        public async Task<ActionResult> DeleteBinaryAsync(Guid binaryid)
        {
            try
            {
                BinaryData bd = await _context.BinaryData.FindAsync(binaryid);

                IRestResponse imageResponse = await deleteImageResponse(binaryid);
                _context.BinaryData.Remove(bd);
                _context.Entry(bd).State = EntityState.Deleted;

                await _context.SaveChangesAsync();


                return Content("OK");

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<IActionResult> Upload(Guid id, int formFieldId, IFormFile file)
        {
            if (HttpContext != null) context = HttpContext;

            BinaryData bd = await _context.BinaryData.Where(m=>m.Id == id).FirstOrDefaultAsync();
            if (bd == null)
            {
                bd = new BinaryData();
                Record record = await _context.Records.Where(m=>m.RecordId == id).FirstOrDefaultAsync();
                FormField ff = await _context.FormFields.Where(m=>m.FormFieldId == formFieldId).FirstOrDefaultAsync();
                bd.Id = Guid.NewGuid();
                bd.Record = record;
                bd.FormField = ff;
                _context.BinaryData.Add(bd);
                record.BinaryData.Add(bd);
                await _context.SaveChangesAsync();

            }

            var fileName = file.FileName;

            var refreshToken = context.GetTokenAsync("refresh_token");
            // Get Access-Token
            var client = new RestClient(Configuration["JWT:Admin-Token-Url"]);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", Configuration["JWT:Client"]);
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken.Result);
            request.AddParameter("client_secret", Configuration["JWT:Key"]);
            IRestResponse response = client.Execute(request);

            dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

            string newAccessToken = json.access_token.Value;

           
            //await file.CopyToAsync(ms);

                client = new RestClient("https://testconnector.biodivcollector.ch/api/Binary/" + bd.Id);
                client.Timeout = -1;
                request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Bearer " + newAccessToken);
                request.Files.Add(new FileParameter
                {
                    Name = "file",
                    Writer = (s) => {
                        file.CopyTo(s);
                    },
                    FileName = fileName,
                    ContentLength = file.Length,
                    ContentType = file.ContentType
                });
                IRestResponse rep = client.Execute(request);

            

            

            return Json("OK");

        }
    }
}
