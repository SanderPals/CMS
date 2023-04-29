using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Site.Data;
using Site.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Localization;
using static Site.Models.Data;

namespace Site.Controllers
{
    public class DataController : Controller
    {
        SiteContext _context;
        UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<DataController> _localizer;

        private int websiteId;
        private int websiteLanguageId;

        public DataController(SiteContext context,
            IStringLocalizer<DataController> localizer, UserManager<ApplicationUser> userManager = null, IHostingEnvironment env = null, IHttpContextAccessor httpContextAccessor = null, IHttpContextAccessor contextAccessor = null)
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

        private string ValidateFileName(string fileName)
        {
            return fileName.Trim(Path.GetInvalidFileNameChars());
        }

        [Route("/spine-api/data/item/delete")]
        [HttpPost]
        public IActionResult DeleteDataItem(int id)
        {
            try
            {
                Models.Data data = new Models.Data(_context);
                string alternateGuid = data.GetDataItemAlternateGuidById(id);

                //Check if the page is in a navigation
                IEnumerable<Navigations> _navigations = new Navigation(_context).GetNavigationsByLinkedToAlternateGuidAndWebsiteIdOrByFilterAlternateGuidAndWebsiteId(alternateGuid, alternateGuid, "dataitem", websiteId);
                if (_navigations.Count() != 0)
                {
                    string navigationsString = string.Join("<br />- ", _navigations.GroupBy(Navigations => Navigations.Id).Select(x => x.FirstOrDefault().Name));
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["ThisPageInFollowingNavs", navigationsString].Value
                    }));
                }

                data.DeleteDataItem(id);

                Websites _website = new Website(_context).GetWebsiteById(websiteId);

                string path = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\data\{id}";
                if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    Directory.Delete(path);
                }

                path = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\data\{id}";
                if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    Directory.Delete(path);
                }
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotDeleteItem"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["ItemDeleted"].Value
            }));
        }

        [Route("/spine-api/data/item/active")]
        [HttpPost]
        public IActionResult UpdateActive([FromBody]DataItems dataItem)
        {
            try
            {
                Models.Data data = new Models.Data(_context);

                data.UpdateDataItemActive(dataItem.Id, dataItem.Active);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotUpdateStatusItem"].Value
                }));
            }

            string message = _localizer["ItemTurnedOff"].Value;
            if (dataItem.Active)
            {
                message = _localizer["ItemPublished"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message
            }));
        }

        [Route("/spine-api/data/items")]
        [HttpGet]
        public IActionResult GetList(int id)
        {
            List<Dictionary<string, object>> dParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> dChildRow;

            try
            {
                Models.Data data = new Models.Data(_context);
                IEnumerable<DataBundle> _dataBundles = data.GetDataBundlesByDataTemplateIdAndWebsiteLanguageId(id, websiteLanguageId);
                foreach (DataBundle dataBundle in _dataBundles)
                {
                    dChildRow = new Dictionary<string, object>
                    {
                        {"Id", dataBundle.DataItem.Id},
                        {"Title", dataBundle.DataItem.Title}
                    };

                    dParentRow.Add(dChildRow);
                }

                string name = "";
                if (_dataBundles.Count() == 0)
                {
                    DataTemplates _dataTemplate = data.GetDataTemplateById(id);

                    //Does the review template exists?
                    if (_dataTemplate != null)
                    {
                        name = _dataTemplate.Name;
                    }
                    else
                    {
                        return StatusCode(400, Json(new
                        {
                            redirect = "/dashboard"
                        }));
                    }
                }
                else
                {
                    name = _dataBundles.FirstOrDefault().DataTemplate.Name;
                }

                return Ok(Json(new
                {
                    name,
                    data = dParentRow,
                    maxDepth = 1
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListItems"].Value
                }));
            }
        }

        [Route("/spine-api/data/items/update")]
        [HttpPost]
        public IActionResult UpdateList([FromBody] List<DataItems> dataItems)
        {
            foreach (var i in dataItems)
            {
                var _dataItem = new DataItems { Id = i.Id, CustomOrder = i.CustomOrder };
                _context.DataItems.Attach(_dataItem);
                _context.Entry(_dataItem).Property(p => p.CustomOrder).IsModified = true;
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
                    message = _localizer["CouldNotSaveOrder"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["OrderSaved"].Value
            }));
        }

        [Route("/spine-api/data/item/get")]
        [HttpPost]
        public IActionResult Get(int id, int dataTemplateId)
        {
            Models.Data data = new Models.Data(_context);
            DataBundle _dataBundle = new DataBundle();
            if (id != 0)
            {
                _dataBundle = data.GetDataItemAndDataTemplate(id);
            }
            else
            {
                _dataBundle.DataTemplate = data.GetDataTemplateById(dataTemplateId);
            }

            if (_dataBundle == null)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotFindItem"].Value
                }));
            }


            IEnumerable<DataBundle> _dataBundles = data.GetDataTemplateFieldAndDataItemResources(id, dataTemplateId);

            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;
            foreach (DataBundle dataBundle in _dataBundles)
            {
                if (dataBundle.DataTemplateField.Type.ToLower() == "selectlinkedto")
                {
                    IEnumerable<DataItems> _dataItems = data.GetDataItemsByDataTemplateId(dataBundle.DataTemplateField.LinkedToDataTemplateId ?? 0);

                    List<Dictionary<string, object>> diParentRow = new List<Dictionary<string, object>>();
                    Dictionary<string, object> diChildRow;
                    foreach (DataItems dataItem in _dataItems)
                    {
                        diChildRow = new Dictionary<string, object>
                        {
                            { "id", dataItem.Id },
                            { "text", dataItem.Title }
                        };
                        diParentRow.Add(diChildRow);
                    }

                    List<string> dirParent = new List<string>();
                    foreach (DataItemResources dataItemResource in dataBundle.DataItemResources)
                    {
                        dirParent.Add(dataItemResource.Text);
                    }

                    childRow = new Dictionary<string, object>
                    {
                        { "Id", Guid.NewGuid().ToString() },
                        { "Data", diParentRow },
                        { "DataItemId", id },
                        { "DataTemplateFieldId", dataBundle.DataTemplateField.Id },
                        { "Text", dirParent },
                        { "Heading", dataBundle.DataTemplateField.Heading },
                        { "Type", dataBundle.DataTemplateField.Type }
                    };
                }
                else
                {
                    childRow = new Dictionary<string, object>
                    {
                        { "Id", (dataBundle.DataItemResources.FirstOrDefault() != null) ? dataBundle.DataItemResources.FirstOrDefault().Id : 0 },
                        { "DataItemId", id },
                        { "DataTemplateFieldId", dataBundle.DataTemplateField.Id },
                        { "Text", (dataBundle.DataItemResources.FirstOrDefault() != null) ? dataBundle.DataItemResources.FirstOrDefault().Text : dataBundle.DataTemplateField.DefaultValue },
                        { "Heading", dataBundle.DataTemplateField.Heading },
                        { "Type", dataBundle.DataTemplateField.Type }
                    };
                }
                parentRow.Add(childRow);
            }

            IQueryable<DataTemplateUploads> _dataTemplateUploads = data.GetDataTemplateUploads(dataTemplateId);

            return Ok(Json(new
            {
                item = _dataBundle.DataItem,
                template = _dataBundle.DataTemplate,
                uploads = _dataTemplateUploads,
                resources = parentRow,
                url = new Website(_context).GetWebsiteUrl(websiteLanguageId)
            }));
        }

        [Route("/spine-api/data/item/update")]
        [HttpPost]
        public IActionResult Post([FromBody] DataUpdate dataUpdate)
        {
            try
            {
                return new Models.Data(_context).UpdateOrInsert(dataUpdate, websiteLanguageId);
            }
            catch (Exception e)
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = dataUpdate.Item.Id == 0 ? _localizer["CouldNotAdd"].Value : _localizer["CouldNotUpdateResource"].Value
                }));
            }
        }

        [Route("/spine-api/data/item/files/update/order")]
        [HttpPost]
        public IActionResult UpdateOrder([FromBody] List<DataItemFiles> dataItemFiles)
        {
            try
            {
                var count = 0;
                foreach (DataItemFiles dataItemFile in dataItemFiles)
                {
                    var _dataItemFile = new DataItemFiles { Id = dataItemFile.Id, CustomOrder = ++count };
                    _context.DataItemFiles.Attach(_dataItemFile);
                    _context.Entry(_dataItemFile).Property(DataItemFiles => DataItemFiles.CustomOrder).IsModified = true;
                }

                _context.SaveChanges();
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CouldNotUpdateOrderFiles"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["OrderFilesSaved"].Value
            });
        }

        [Route("/spine-api/data/item/file/delete")]
        [HttpPost]
        public IActionResult DeleteFile(int Id)
        {
            try
            {
                DataItemFiles _dataItemFile = _context.DataItemFiles.FirstOrDefault(DataItemFiles => DataItemFiles.Id == Id);
                _context.DataItemFiles.Remove(_dataItemFile);
                _context.SaveChanges();

                Websites _website = _context.WebsiteLanguages.Join(_context.Websites, WebsiteLanguages => WebsiteLanguages.WebsiteId, Websites => Websites.Id, (WebsiteLanguages, Websites) => new { WebsiteLanguages, Websites })
                                                             .Where(x => x.WebsiteLanguages.Id == websiteLanguageId)
                                                             .Select(x => x.Websites)
                                                             .FirstOrDefault();

                var path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _dataItemFile.OriginalPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                path = _env.WebRootPath + $@"\websites\{_website.Folder}{ _dataItemFile.CompressedPath.Replace("~/", "/") }";
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["FileCouldNotDeleted"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                messageType = "success",
                message = _localizer["FileDeleted"].Value
            });
        }

        [Route("/spine-api/data/item/file")]
        [HttpPost]
        public IActionResult UploadFiles()
        {
            Websites _website = new Website(_context).GetWebsiteById(websiteId);

            int FormDataItemId = int.Parse(Request.Form["dataItemId"]);
            string originalPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\data\{FormDataItemId}";
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }

            string compressedPath = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\data\{FormDataItemId}";
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
                string LocationOriginal = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\original\data\{FormDataItemId}\{filename}";
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

                var LocationCompressed = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\compressed\data\{FormDataItemId}\{filename}";
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

                            Guid FileGuid = ImageFormat.Jpeg.Guid;
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
                                default: //".jpeg":
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

                int FormDataTemplateUploadId = int.Parse(Request.Form["dataTemplateUploadId"]);

                DataItemFiles _dataItemFile = new DataItemFiles { DataItemId = FormDataItemId, DataTemplateUploadId = FormDataTemplateUploadId, OriginalPath = $@"~/assets/uploads/original/data/{FormDataItemId}/{ Path.GetFileName(LocationOriginal) }", CompressedPath = $@"~/assets/uploads/compressed/data/{FormDataItemId}/{ Path.GetFileName(LocationCompressed) }", Alt = "", CustomOrder = 0, Active = true };
                _context.DataItemFiles.Add(_dataItemFile);
                _context.SaveChanges();
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileAdded"].Value
            }));
        }

        [Route("/spine-api/data/item/file/active")]
        [HttpPost]
        public IActionResult Post([FromBody]DataItemFiles dataItemFile)
        {
            var _dataItemFile = new DataItemFiles { Id = dataItemFile.Id, Active = dataItemFile.Active };
            _context.DataItemFiles.Attach(_dataItemFile);
            _context.Entry(_dataItemFile).Property(DataItemFiles => DataItemFiles.Active).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotUpdateFileStatus"].Value
                }));
            }

            var Message = _localizer["FileDeactivated"].Value;
            if (_dataItemFile.Active ?? true)
            {
                Message = _localizer["FileActivated"].Value;
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = Message
            }));
        }

        [Route("/spine-api/data/item/file/alt")]
        [HttpPost]
        public IActionResult DataItemFileAltApi([FromBody]DataItemFiles dataItemFile)
        {
            var _dataItemFile = new DataItemFiles { Id = dataItemFile.Id, Alt = dataItemFile.Alt };
            _context.DataItemFiles.Attach(_dataItemFile);
            _context.Entry(_dataItemFile).Property(DataItemFiles => DataItemFiles.Alt).IsModified = true;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotUpdateFileDescription"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["FileDescriptionUpdated"].Value
            }));
        }

        [Route("/spine-api/data/item/files")]
        [HttpGet]
        public IActionResult GetFiles(int dataItemId, int dataTemplateUploadId)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;

            try
            {
                var _website = _context.WebsiteLanguages.Join(_context.Websites, wl => wl.WebsiteId, w => w.Id, (wl, w) => new { wl, w }).FirstOrDefault(x => x.wl.Id == websiteLanguageId);
                var path = $@"/websites/{_website.w.Folder}";

                IQueryable<DataItemFiles> _dataItemFiles = _context.DataItemFiles.Where(DataItemFiles => DataItemFiles.DataTemplateUploadId == dataTemplateUploadId && DataItemFiles.DataItemId == dataItemId)
                                                                                 .OrderBy(DataItemFiles => DataItemFiles.CustomOrder);

                foreach (DataItemFiles dataItemFile in _dataItemFiles)
                {
                    childRow = new Dictionary<string, object>
                    {
                        { "Id", dataItemFile.Id},
                        { "DataItemId", dataItemFile.DataItemId},
                        { "DataTemplateUploadId", dataItemFile.DataTemplateUploadId},
                        { "OriginalPath", path + dataItemFile.OriginalPath.Replace('\\', '/').Replace("~/", "/")},
                        { "CompressedPath", path + dataItemFile.CompressedPath.Replace('\\', '/').Replace("~/", "/")},
                        { "Alt", dataItemFile.Alt},
                        { "CustomOrder", dataItemFile.CustomOrder},
                        { "Active", dataItemFile.Active}
                    };
                    parentRow.Add(childRow);
                }
            }
            catch
            {
                return Json(new
                {
                    success = "False",
                    messageType = "warning",
                    message = _localizer["CouldNotGetFiles"].Value
                });
            }

            return Json(new
            {
                success = "Valid",
                data = parentRow
            });
        }

        [Route("/spine-api/data/get/options")]
        [HttpGet]
        public IActionResult GetOptions()
        {
            try
            {
                return Ok(Json(new
                {
                    data = new Models.Data(_context).GetDataOptionsArrayList(websiteId, websiteLanguageId)
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListItems"].Value
                }));
            }
        }

        [Route("/spine-api/data/template/sections/get/options")]
        [HttpGet]
        public IActionResult GetSectionOptions(string alternateGuid)
        {
            try
            {
                return Ok(Json(new
                {
                    data = new Models.Data(_context).GetSectionOptionsArrayList(alternateGuid, websiteLanguageId)
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowListSections"].Value
                }));
            }
        }

        [Route("/data/{id}")]
        public IActionResult Index(int id)
        {
            DataTemplates _dataTemplate = new Models.Data(_context).GetDataTemplateById(id);
            if (_dataTemplate != null)
            {
                ViewData["Title"] = _dataTemplate.Name;
            }
            else
            {
                return RedirectToRoute("Dashboard");
            }

            return View();
        }

        [Route("/data/add/{dataTemplateId}")]
        [Route("/data/{dataTemplateId}/{id}")]
        public IActionResult DataAdd()
        {
            return View();
        }
    }
}