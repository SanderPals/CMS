using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Site.Controllers
{
    public class FileController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<FileController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public FileController(SiteContext context,
            IStringLocalizer<FileController> localizer, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _contextAccessor = contextAccessor;
            _localizer = localizer;

            websiteId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteId").Value);
            websiteLanguageId = Convert.ToInt32(new User(_context, _userManager, _httpContextAccessor).GetClaimByType("WebsiteLanguageId").Value);
        }

        private string CheckAvailibiltyFile(string Location, string LocationNew, int Count)
        {
            if (System.IO.File.Exists(LocationNew))
            {
                string fDir = Path.GetDirectoryName(Location);
                string fName = Path.GetFileNameWithoutExtension(Location);
                string fExt = Path.GetExtension(Location);
                LocationNew = Path.Combine(fDir, string.Concat(fName, "_" + Count++, fExt));

                LocationNew = CheckAvailibiltyFile(Location, LocationNew, Count);
            }

            return LocationNew;
        }

        private string ValidateFileName(string FileName)
        {
            FileName = FileName.Trim(Path.GetInvalidFileNameChars());

            return FileName;
        }

        [Route("/spine-api/image/crop")]
        [HttpPost]
        public IActionResult CropImage()
        {
            try
            {
                Websites _website = new Website(_context).GetWebsiteById(websiteId);

                int id = int.Parse(Request.Form["id"]);
                string type = Request.Form["type"].ToString();
                string compressedPath;
                int width = 0;
                int height = 0;
                switch (type.ToLower())
                {
                    case "data":
                        compressedPath = _context.DataItemFiles.FirstOrDefault(DataItemFile => DataItemFile.Id == id).CompressedPath.Replace("~/", "");
                        var _d = _context.DataItemFiles.Join(_context.DataTemplateUploads, DataItemFile => DataItemFile.DataTemplateUploadId, DataTemplateUpload => DataTemplateUpload.Id, (DataItemFile, DataTemplateUpload) => new { DataItemFile, DataTemplateUpload }).FirstOrDefault(x => x.DataItemFile.Id == id);
                        width = _d.DataTemplateUpload.Width;
                        height = _d.DataTemplateUpload.Height;
                        compressedPath = _d.DataItemFile.CompressedPath.Replace("~/", "");
                        break;
                    case "page":
                        var _p = _context.PageFiles.Join(_context.PageTemplateUploads, PageFile => PageFile.PageTemplateUploadId, PageTemplateUpload => PageTemplateUpload.Id, (PageFile, PageTemplateUpload) => new { PageFile, PageTemplateUpload }).FirstOrDefault(x => x.PageFile.Id == id);
                        width = _p.PageTemplateUpload.Width;
                        height = _p.PageTemplateUpload.Height;
                        compressedPath = _p.PageFile.CompressedPath.Replace("~/", "");
                        break;
                    case "product":
                        var _pr = _context.ProductFiles.Join(_context.ProductUploads, ProductFile => ProductFile.ProductUploadId, ProductUpload => ProductUpload.Id, (ProductFile, ProductUpload) => new { ProductFile, ProductUpload }).FirstOrDefault(x => x.ProductFile.Id == id);
                        width = _pr.ProductUpload.Width;
                        height = _pr.ProductUpload.Height;
                        compressedPath = _pr.ProductFile.CompressedPath.Replace("~/", "");
                        break;
                    default: //website
                        var _w = _context.WebsiteFiles.Join(_context.WebsiteUploads, WebsiteFile => WebsiteFile.WebsiteUploadId, WebsiteUpload => WebsiteUpload.Id, (WebsiteFile, WebsiteUpload) => new { WebsiteFile, WebsiteUpload }).FirstOrDefault(x => x.WebsiteFile.Id == id);
                        width = _w.WebsiteUpload.Width;
                        height = _w.WebsiteUpload.Height;
                        compressedPath = _w.WebsiteFile.CompressedPath.Replace("~/", "");
                        break;
                }

                compressedPath = _env.WebRootPath + $@"\websites\{_website.Folder}\{compressedPath}";
                if (!System.IO.File.Exists(compressedPath))
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["FileNotExist"].Value
                    }));
                }


                IFormFile file =  Request.Form.Files["croppedImage"];
                Image image = Image.FromStream(file.OpenReadStream(), true, true);

                using (Bitmap bitmap = new Bitmap(image))
                {
                    var Extension = Path.GetExtension(compressedPath).ToLower();
                    if (Extension == ".png" || Extension == ".gif" || Extension == ".jpg" || Extension == ".jpeg")
                    {
                        var resized = new Bitmap(width, height);
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(bitmap, 0, 0, width, height);
                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, int.Parse(Request.Form["quality"].ToString()));
                            //encoderParameters.Param[1] = new EncoderParameter(Encoder.ColorDepth, 32);


                            Guid FileGuid;
                            switch (Extension)
                            {
                                case ".png":
                                    FileGuid = ImageFormat.Png.Guid;
                                    break;
                                case ".gif":
                                    FileGuid = ImageFormat.Gif.Guid;
                                    break;
                                case ".jpg":
                                    FileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                                default: // ".jpeg":
                                    FileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                            }


                            var codec = ImageCodecInfo.GetImageDecoders()
                                .FirstOrDefault(c => c.FormatID == FileGuid);

                            resized.Save(compressedPath, codec, encoderParameters);
                        }
                    }
                }

                return Ok(Json(new
                {
                    messageType = "success",
                    message = _localizer["FileSuccessCropped"].Value
                }));
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["SomethingWrong"].Value
                }));
            }
        }

        [Route("/api/image")]
        [HttpPost]
        public IActionResult PostProductFileApi()
        {
            try
            {
                IFormFileCollection files = Request.Form.Files;
                return Ok(new Site.Models.File(_context, _env)
                {
                    WebsiteId = websiteId,
                    WebsiteLanguageId = websiteLanguageId
                }.UploadImage(files));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["FileNotUploaded"].Value
                }));
            }
        }
    }
}