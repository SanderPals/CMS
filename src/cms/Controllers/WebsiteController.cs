using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using Site.Data;
using static Site.Models.Website;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System;
using Site.Models;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Localization;

namespace Site.Controllers
{
    public class WebsiteController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private IStringLocalizer<WebsiteController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public WebsiteController(SiteContext context,
            IStringLocalizer<WebsiteController> localizer, UserManager<ApplicationUser> userManager, IHostingEnvironment env, IHttpContextAccessor contextAccessor, IHttpContextAccessor httpContextAccessor, SignInManager<ApplicationUser> signInManager = null)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _contextAccessor = contextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
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

        [Route("/spine-api/resources")]
        [HttpGet]
        public IActionResult GetList(int WebsiteLanguageId)
        {
            List<Dictionary<string, object>> WrParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> WrChildRow;

            try
            {
                IEnumerable<WebsiteResourcesBundle> WebsiteResourcesBundles = _context.WebsiteResources.Join(_context.WebsiteFields, WebsiteResources => WebsiteResources.WebsiteFieldId, WebsiteFields => WebsiteFields.Id, (WebsiteResources, WebsiteFields) => new { WebsiteResources, WebsiteFields })
                                                                                                       .Where(x => x.WebsiteResources.WebsiteLanguageId == WebsiteLanguageId)
                                                                                                       .Where(x => x.WebsiteFields.DeveloperOnly == false)
                                                                                                       .OrderBy(x => x.WebsiteFields.CustomOrder)
                                                                                                       .Select(x => new WebsiteResourcesBundle() {
                                                                                                          WebsiteField = x.WebsiteFields,
                                                                                                          WebsiteResource = x.WebsiteResources
                                                                                                       });

                foreach (var WebsiteResourcesBundle in WebsiteResourcesBundles)
                {
                    WrChildRow = new Dictionary<string, object>
                    {
                        {"Id", WebsiteResourcesBundle.WebsiteResource.Id},
                        {"WebsiteLanguageId", WebsiteResourcesBundle.WebsiteResource.WebsiteLanguageId},
                        {"WebsiteFieldId", WebsiteResourcesBundle.WebsiteResource.WebsiteFieldId},
                        {"Text", WebsiteResourcesBundle.WebsiteResource.Text},
                        {"Heading", WebsiteResourcesBundle.WebsiteField.Heading},
                        {"Type", WebsiteResourcesBundle.WebsiteField.Type}
                    };
                    WrParentRow.Add(WrChildRow);
                }

                IEnumerable<WebsiteUploads> _websiteUploads = _context.WebsiteUploads.Join(_context.WebsiteLanguages, WebsiteUploads => WebsiteUploads.WebsiteId, WebsiteLanguages => WebsiteLanguages.WebsiteId, (WebsiteUploads, WebsiteLanguages) => new { WebsiteUploads, WebsiteLanguages })
                                                                                     .Where(x => x.WebsiteLanguages.Id == WebsiteLanguageId)
                                                                                     .Where(x => x.WebsiteUploads.DeveloperOnly == false)
                                                                                     .Select(x => x.WebsiteUploads);
                bool WebsiteUploadsInDatabase = false;
                if (_websiteUploads.Count() > 0) { WebsiteUploadsInDatabase = true; };

                return Ok(Json(new
                {
                    data = WrParentRow,
                    files = WebsiteUploadsInDatabase
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowResources"].Value
                }));
            }
        }

        [Route("/spine-api/website/resource/get")]
        [HttpGet]
        public IActionResult GetResource(int Id)
        {
            WebsiteResources _websiteResources = _context.WebsiteResources.Where(WebsiteResources => WebsiteResources.WebsiteLanguageId == websiteLanguageId).FirstOrDefault(WebsiteResources  => WebsiteResources.Id == Id);

            if (_websiteResources == null)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotFindResource"].Value
                }));
            }
            else
            {
                return Ok(Json(new
                {
                    websiteResourceId = _websiteResources.Id,
                    websiteLanguageId = _websiteResources.WebsiteLanguageId,
                    websiteFieldId = _websiteResources.WebsiteFieldId,
                    text = _websiteResources.Text
                }));
            }
        }

        [Route("/spine-api/website/resource/update")]
        [HttpPost]
        public IActionResult Post([FromBody]WebsiteResources WebsiteResource)
        {
            var _websiteResource = new WebsiteResources { Id = WebsiteResource.Id, Text = WebsiteResource.Text };
            _context.WebsiteResources.Attach(_websiteResource);
            _context.Entry(_websiteResource).Property(wr => wr.Text).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateResource"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["ResourceUpdated"].Value
            }));
        }

        [Route("/spine-api/website/uploads")]
        [HttpGet]
        public IActionResult Get(int WebsiteId)
        {
            List<Dictionary<string, object>> WuParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> WuChildRow;

            try
            {
                IEnumerable<WebsiteUploads> _websiteUploads = _context.WebsiteUploads.Where(WebsiteUploads => WebsiteUploads.WebsiteId == WebsiteId).Where(WebsiteUploads => WebsiteUploads.DeveloperOnly == false).OrderBy(WebsiteUploads => WebsiteUploads.CustomOrder);

                foreach (WebsiteUploads WebsiteUpload in _websiteUploads)
                {
                    WuChildRow = new Dictionary<string, object>
                    {
                        {"Id", WebsiteUpload.Id},
                        {"WebsiteId", WebsiteUpload.WebsiteId},
                        {"CallName", WebsiteUpload.CallName},
                        {"Heading", WebsiteUpload.Heading},
                        {"MimeTypes", WebsiteUpload.MimeTypes},
                        {"FileExtensions", WebsiteUpload.FileExtensions},
                        {"MinFiles", WebsiteUpload.MinFiles},
                        {"MaxFiles", WebsiteUpload.MaxFiles},
                        {"MaxSize", WebsiteUpload.MaxSize},
                        {"Width", WebsiteUpload.Width},
                        {"Height", WebsiteUpload.Height},
                        {"CustomOrder", WebsiteUpload.CustomOrder}
                    };
                    WuParentRow.Add(WuChildRow);
                }

                IEnumerable<WebsiteFields> _websiteFields = _context.WebsiteFields.Join(_context.WebsiteLanguages, WebsiteFields => WebsiteFields.WebsiteId, WebsiteLanguages => WebsiteLanguages.WebsiteId, (WebsiteFields, WebsiteLanguages) => new { WebsiteFields, WebsiteLanguages })
                                                                                  .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                                                                  .Where(x => x.WebsiteFields.DeveloperOnly == false)
                                                                                  .Select(x => x.WebsiteFields);
                bool WebsiteFieldsInDatabase = false;
                if (_websiteFields.Count() > 0) { WebsiteFieldsInDatabase = true; };

                return Ok(Json(new
                {
                    data = WuParentRow,
                    resources = WebsiteFieldsInDatabase
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowFiles"].Value
                }));
            }
        }

        [Route("/spine-api/website/files")]
        [HttpGet]
        public IActionResult GetImages(int WebsiteLanguageId, int WebsiteUploadId)
        {
            List<Dictionary<string, object>> WfParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> WfChildRow;

            try
            {
                var _website = _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites }).Where(x => x.WebsiteLanguages.Id == WebsiteLanguageId).Select(x => x.Websites).FirstOrDefault();
                var OriginalPath = $@"/websites/{_website.Folder}";
                var CompressedPath = $@"/websites/{_website.Folder}";

                var _websiteFiles = _context.WebsiteFiles.Where(x => x.WebsiteUploadId == WebsiteUploadId && x.WebsiteLanguageId == WebsiteLanguageId).OrderBy(x => x.CustomOrder).ToList();

                foreach (WebsiteFiles WebsiteFile in _websiteFiles)
                {
                    WfChildRow = new Dictionary<string, object>
                    {
                        {"Id", WebsiteFile.Id},
                        {"WebsiteUploadId", WebsiteFile.WebsiteUploadId},
                        {"OriginalPath", OriginalPath + WebsiteFile.OriginalPath.Replace('\\', '/').Replace("~/", "/")},
                        {"CompressedPath", CompressedPath + WebsiteFile.CompressedPath.Replace('\\', '/').Replace("~/", "/")},
                        {"Alt", WebsiteFile.Alt},
                        {"CustomOrder", WebsiteFile.CustomOrder},
                        {"Active", WebsiteFile.Active},
                        {"WebsiteLanguageId", WebsiteFile.WebsiteLanguageId}
                    };
                    WfParentRow.Add(WfChildRow);
                }
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotGetFiles"].Value
                }));
            }

            return Ok(Json(new
            {
                data = WfParentRow
            }));
        }

        [Route("/spine-api/website/files/update")]
        [HttpPost]
        public IActionResult UpdateOrder([FromBody] List<WebsiteFiles> WebsiteFiles)
        {
            var count = 0;
            foreach (var i in WebsiteFiles)
            {
                var _websiteFile = new WebsiteFiles { Id = i.Id, CustomOrder = ++count };
                _context.WebsiteFiles.Attach(_websiteFile);
                _context.Entry(_websiteFile).Property(x => x.CustomOrder).IsModified = true;
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateOrderFiles"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["OrderFilesUpdated"].Value
            }));
        }

        [Route("/spine-api/website/file/add")]
        [HttpPost]
        public IActionResult UploadFilesAjax()
        {
            var _website = _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites }).Where(x => x.WebsiteLanguages.Id == websiteLanguageId).Select(x => x.Websites).FirstOrDefault();

            //int FormWebsiteId = int.Parse(Request.Form["WebsiteId"]);

            string originalPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\files";
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }

            string compressedPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\files";
            if (!Directory.Exists(compressedPath))
            {
                Directory.CreateDirectory(compressedPath);
            }

            //Variable for total size of all files
            long TotalSize = 0;

            var files = Request.Form.Files;
            foreach (var file in files)
            {
                var filename = Request.Form["filename"].ToString().Trim('"');

                //File name validation
                filename = ValidateFileName(filename);

                //Check availibility of file, otherwise a number will be put on the end of the name
                string LocationOriginal = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\files\{filename}";
                LocationOriginal = CheckAvailibiltyFile(LocationOriginal, LocationOriginal, 1);

                //Counting total size of all files
                TotalSize += file.Length;

                using (FileStream fs = System.IO.File.Create(LocationOriginal))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }

                //const int size = 150;
                //const int quality = 75;

                var LocationCompressed = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\files\{filename}";
                LocationCompressed = CheckAvailibiltyFile(LocationCompressed, LocationCompressed, 1);

                var extension = Path.GetExtension(LocationOriginal).ToLower();
                if (extension == ".png" || extension == ".gif" || extension == ".jpg" || extension == ".jpeg")
                {
                    using (var image = new Bitmap(LocationOriginal))
                    {
                        var resized = new Bitmap(image.Width, image.Height);
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                            var qualityParamId = Encoder.Quality;
                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(qualityParamId, 75L);

                            Guid FileGuid;
                            switch (extension)
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


                            var codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == FileGuid);
                            resized.Save(LocationCompressed, codec, encoderParameters);
                        }
                    }
                }
                else if (extension == ".pdf")
                {
                    PdfReader reader = new PdfReader(LocationOriginal);
                    PdfStamper stamper = new PdfStamper(reader, new FileStream(LocationCompressed, FileMode.Create), PdfWriter.VERSION_1_5);

                    stamper.FormFlattening = true;
                    stamper.SetFullCompression();
                    stamper.Close();
                }
                else
                {
                    //Support for pdf etc is still under construction
                }

                int FormWebsiteUploadId = int.Parse(Request.Form["WebsiteUploadId"]);

                var _websiteFile = new WebsiteFiles { WebsiteLanguageId = websiteLanguageId, WebsiteUploadId = FormWebsiteUploadId, OriginalPath = $@"~/assets/uploads/original/files/{ Path.GetFileName(LocationOriginal) }", CompressedPath = $@"~/assets/uploads/compressed/files/{ Path.GetFileName(LocationCompressed) }", Alt = "", CustomOrder = 0, Active = true };
                _context.WebsiteFiles.Add(_websiteFile);
                _context.SaveChanges();
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileAdded"].Value
            }));
        }

        [Route("/spine-api/website/file/active")]
        [HttpPost]
        public IActionResult Post([FromBody]WebsiteFiles websiteFile)
        {
            var _websiteFile = new WebsiteFiles { Id = websiteFile.Id, Active = websiteFile.Active };
            _context.WebsiteFiles.Attach(_websiteFile);
            _context.Entry(_websiteFile).Property(WebsiteFiles => WebsiteFiles.Active).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileStatus"].Value
                }));
            }

            var Message = _localizer["FileDeactivated"].Value;
            if (websiteFile.Active)
            {
                Message = _localizer["FileActivated"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = Message
            }));
        }

        [Route("/spine-api/website/file/alt")]
        [HttpPost]
        public IActionResult WebsiteFileAltApi([FromBody]WebsiteFiles websiteFile)
        {
            var _websiteFile = new WebsiteFiles { Id = websiteFile.Id, Alt = websiteFile.Alt };
            _context.WebsiteFiles.Attach(_websiteFile);
            _context.Entry(_websiteFile).Property(WebsiteFiles => WebsiteFiles.Alt).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotUpdateFileDescription"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileDescriptionUpdated"].Value
            }));
        }

        [Route("/spine-api/website/file/delete")]
        [HttpPost]
        public IActionResult DeleteImage(int Id)
        {
            try
            {
                WebsiteFiles _websiteFile = _context.WebsiteFiles.FirstOrDefault(WebsiteFiles => WebsiteFiles.Id == Id);
                _context.WebsiteFiles.Remove(_websiteFile);
                _context.SaveChanges();

                Websites _website = _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites })
                                                             .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                                             .Select(x => x.Websites)
                                                             .FirstOrDefault();

                var path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _websiteFile.OriginalPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _websiteFile.CompressedPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotDeleteFile"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileDeleted"].Value
            }));
        }

        [Route("/website/resources")]
        public IActionResult Resources()
        {
            ViewBag.WebsiteId = websiteId;
            ViewBag.WebsiteLanguageId = websiteLanguageId;

            return View();
        }

        [Route("/website/files")]
        public IActionResult Files()
        {
            ViewBag.WebsiteId = websiteId;
            ViewBag.WebsiteLanguageId = websiteLanguageId;

            return View();
        }

        public async Task<IActionResult> UpdateWebsiteClaimAsync()
        {
            string websiteId = Request.Form["id"];
            await new Website(_context, _userManager, _httpContextAccessor, _signInManager).SetWebsiteClaimsByWebsiteIdAsync(websiteId, _userManager.GetUserId(User));

            return Redirect("~/dashboard");
        }

        public async Task<IActionResult> UpdateWebsiteLanguageIdClaimAsync()
        {
            string websiteId = Request.Form["id"];
            await new Website(_context, _userManager, _httpContextAccessor, _signInManager).SetWebsiteLanguageIdClaimAsync(websiteId, _userManager.GetUserId(User));

            return Redirect("~/dashboard");
        }
    }
}