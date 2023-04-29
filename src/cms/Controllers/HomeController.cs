using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers
{
    public class HomeController : Controller
    {
        //private object _environment;
        //private object _context;

        public IActionResult Index()
        {
            return View();
        }



        [Route("/changelog")]
        [HttpGet]
        public IActionResult Changelog()
        {
            return View();
        }

        //        Bitmap mg = new Bitmap(strUploadPath);
        //        Size newSize = new Size(Convert.ToInt32(DispMaxWidth), Convert.ToInt32(DispMaxHeight));
        //        Bitmap bp = ResizeImage(mg, newSize);
        //if (bp != null)
        //bp.Save(strUploadPath, System.Drawing.Imaging.ImageFormat.Jpeg);

        //        private Bitmap ResizeImage(Bitmap mg, Size newSize)
        //        {
        //            double ratio = 0d;
        //            double myThumbWidth = 0d;
        //            double myThumbHeight = 0d;
        //            int x = 0;
        //            int y = 0;

        //            Bitmap bp;

        //            if ((mg.Width / Convert.ToDouble(newSize.Width)) > (mg.Height /
        //            Convert.ToDouble(newSize.Height)))
        //                ratio = Convert.ToDouble(mg.Width) / Convert.ToDouble(newSize.Width);
        //            else
        //                ratio = Convert.ToDouble(mg.Height) / Convert.ToDouble(newSize.Height);
        //            myThumbHeight = Math.Ceiling(mg.Height / ratio);
        //            myThumbWidth = Math.Ceiling(mg.Width / ratio);

        //            //Size thumbSize = new Size((int)myThumbWidth, (int)myThumbHeight);
        //            Size thumbSize = new Size((int)newSize.Width, (int)newSize.Height);
        //            bp = new Bitmap(newSize.Width, newSize.Height);
        //            x = (newSize.Width - thumbSize.Width) / 2;
        //            y = (newSize.Height - thumbSize.Height);
        //            // Had to add System.Drawing class in front of Graphics ---
        //            System.Drawing.Graphics g = Graphics.FromImage(bp);
        //            g.SmoothingMode = SmoothingMode.HighQuality;
        //            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        //            Rectangle rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
        //            g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);

        //            return bp;

        //        }

        //private void VaryQualityLevel()
        //{
        //    // Get a bitmap. The using statement ensures objects  
        //    // are automatically disposed from memory after use.  
        //    using (Bitmap bmp1 = new Bitmap(@"C:\TestPhoto.jpg"))
        //    {
        //        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

        //        // Create an Encoder object based on the GUID  
        //        // for the Quality parameter category.  
        //        System.Drawing.Imaging.Encoder myEncoder =
        //            System.Drawing.Imaging.Encoder.Quality;

        //        // Create an EncoderParameters object.  
        //        // An EncoderParameters object has an array of EncoderParameter  
        //        // objects. In this case, there is only one  
        //        // EncoderParameter object in the array.  
        //        EncoderParameters myEncoderParameters = new EncoderParameters(1);

        //        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
        //        myEncoderParameters.Param[0] = myEncoderParameter;
        //        bmp1.Save(@"c:\TestPhotoQualityFifty.jpg", jpgEncoder, myEncoderParameters);

        //        myEncoderParameter = new EncoderParameter(myEncoder, 100L);
        //        myEncoderParameters.Param[0] = myEncoderParameter;
        //        bmp1.Save(@"C:\TestPhotoQualityHundred.jpg", jpgEncoder, myEncoderParameters);

        //        // Save the bitmap as a JPG file with zero quality level compression.  
        //        myEncoderParameter = new EncoderParameter(myEncoder, 0L);
        //        myEncoderParameters.Param[0] = myEncoderParameter;
        //        bmp1.Save(@"C:\TestPhotoQualityZero.jpg", jpgEncoder, myEncoderParameters);
        //    }
        //}

        //private ImageCodecInfo GetEncoder(ImageFormat format)
        //{
        //    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        //    foreach (ImageCodecInfo codec in codecs)
        //    {
        //        if (codec.FormatID == format.Guid)
        //        {
        //            return codec;
        //        }
        //    }
        //    return null;
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Index([Bind("ID,AccessRights,DOB,FirstName,LastName,NRIC,Nationality,StaffIdentity")]HomeViewModel vm)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var files = HttpContext.Request.Form.Files;
        //        foreach (var Image in files)
        //        {
        //            if (Image != null && Image.Length > 0)
        //            {

        //                var file = Image;
        //                var uploads = Path.Combine(_environment.WebRootPath, "images");

        //                if (file.Length > 0)
        //                {
        //                    var fileName = ContentDispositionHeaderValue.Parse
        //                        (file.ContentDisposition).FileName.Trim('"');

        //                    System.Console.WriteLine(fileName);
        //                    using (var fileStream = new FileStream(Path.Combine(uploads, file.FileName), FileMode.Create))
        //                    {
        //                        await file.CopyToAsync(fileStream);
        //                        vm.ImageName = file.FileName;
        //                    }


        //                }
        //            }
        //        }

        //        _context.Add(vm);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index");

        //    }
        //    else
        //    {
        //        var errors = ModelState.Values.SelectMany(v => v.Errors);
        //    }
        //    return View(vm);
        //}

        public IActionResult Error()
        {
            return View();
        }
    }
}
