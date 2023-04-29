using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Site.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace Site.Models
{
    public class File
    {
        SiteContext _context;
        private readonly IHostingEnvironment _env;

        public File(SiteContext context = null, IHostingEnvironment env = null)
        {
            _context = context;
            _env = env;
        }

        public int WebsiteId = 0;
        public int WebsiteLanguageId = 0;

        public string RemoveInvalidCharacters(string fileName)
        {
            Regex illegalInFileName = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), RegexOptions.Compiled);

            return illegalInFileName.Replace(fileName, "");
        }

        private string RenameFileIfExists(string Location, string LocationNew, int Count)
        {
            if (System.IO.File.Exists(LocationNew))
            {
                string fDir = Path.GetDirectoryName(Location);
                string fName = Path.GetFileNameWithoutExtension(Location);
                string fExt = Path.GetExtension(Location);
                LocationNew = Path.Combine(fDir, string.Concat(fName, "_" + Count++, fExt));

                LocationNew = RenameFileIfExists(Location, LocationNew, Count);
            }

            return LocationNew;
        }

        private string ValidateFileName(string fileName)
        {
            return fileName.Trim(Path.GetInvalidFileNameChars());
        }

        public List<string> UploadImage(IFormFileCollection files)
        {
            Website website = new Website(_context);
            string websiteUrl = $@"{website.GetWebsiteUrl(WebsiteLanguageId)}/assets/uploads/files/";
            Websites _website = website.GetWebsiteById(WebsiteId);
            string path = _env.WebRootPath + $@"\websites\{_website.Folder}\assets\uploads\files";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> list = new List<string>();
            foreach (var file in files)
            {
                //File name validation
                string filename = ValidateFileName(file.FileName);

                //Check availibility of file, otherwise a number will be put on the end of the name
                path = $@"{path}\{filename}";
                path = RenameFileIfExists(path, path, 1);

                using (FileStream fs = System.IO.File.Create(path))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }

                list.Add(websiteUrl + Path.GetFileName(path));
            }

            return list;
        }

        public Dictionary<string, object> UploadFile(int id, int uploadId, string filename, string originalPath, string compressedPath, IFormFileCollection files)
        {
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }

            if (!Directory.Exists(compressedPath))
            {
                Directory.CreateDirectory(compressedPath);
            }

            //Variable for total size of all files
            //long TotalSize = 0;

            foreach (var file in files)
            {
                //File name validation
                filename = ValidateFileName(filename);

                //Check availibility of file, otherwise a number will be put on the end of the name
                originalPath = $@"{originalPath}\{filename}";
                originalPath = RenameFileIfExists(originalPath, originalPath, 1);

                //Counting total size of all files
                //TotalSize += file.Length;

                using (FileStream fs = System.IO.File.Create(originalPath))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }

                //const int size = 150;
                //const int quality = 75;

                compressedPath = $@"{compressedPath}\{filename}";
                compressedPath = RenameFileIfExists(compressedPath, compressedPath, 1);

                var extension = Path.GetExtension(originalPath).ToLower();
                if (extension == ".png" || extension == ".gif" || extension == ".jpg" || extension == ".jpeg")
                {
                    using (var image = new Bitmap(originalPath))
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

                            Guid fileGuid;
                            switch (extension)
                            {
                                case ".png":
                                    fileGuid = ImageFormat.Png.Guid;
                                    break;
                                case ".gif":
                                    fileGuid = ImageFormat.Gif.Guid;
                                    break;
                                case ".jpg":
                                    fileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                                default: // ".jpeg":
                                    fileGuid = ImageFormat.Jpeg.Guid;
                                    break;
                            }


                            var codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == fileGuid);
                            resized.Save(compressedPath, codec, encoderParameters);
                        }
                    }
                }
                else if (extension == ".pdf")
                {
                    PdfReader reader = new PdfReader(originalPath);
                    PdfStamper stamper = new PdfStamper(reader, new FileStream(compressedPath, FileMode.Create), PdfWriter.VERSION_1_5);

                    stamper.FormFlattening = true;
                    stamper.SetFullCompression();
                    stamper.Close();
                }
                else
                {
                    //Support for pdf etc is still under construction
                }
            }

            return new Dictionary<string, object>() {
                { "originalPath", originalPath },
                { "compressedPath", compressedPath }
            };
        }

        public void DeleteFiles(string[] files)
        {
            foreach(string file in files)
            {
                DeleteFile(file);
            }
        }

        public void DeleteFile(string file)
        {
            if (System.IO.File.Exists(file))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void DeleteDirectories(string[] directories)
        {
            foreach (string directory in directories)
            {
                DeleteDirectory(directory);
            }
        }

        public void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }

                Directory.Delete(directory);
            }
        }
    }
}
