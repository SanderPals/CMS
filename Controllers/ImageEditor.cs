using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace Site.Controllers
{
    //class DemoThumb
    //{
    //    [STAThread]
    //    static void Main(string[] args)
    //    {
    //        ThumbMaker thumbMaker = new ThumbMaker(@"c:\Image.gif");
    //        thumbMaker.ResizeToPng(100, 0, @"c:\ScaledImage.png");
    //    }
    //}

    public class ImageEditor
    {
        double xFactor;
        double yFactor;
        IntPtr sourceScan0;
        int sourceStride;
        Bitmap scaledBitmap, originalBitmap;

        public ImageEditor(string fileName)
        {
            originalBitmap = new Bitmap(fileName);
        }

        public ImageEditor(Stream stream)
        {
            originalBitmap = new Bitmap(stream);
        }

        void AdjustSizes(ref int xSize, ref int ySize)
        {
            if (xSize != 0 && ySize == 0)
                ySize = Math.Abs(xSize * originalBitmap.Height / originalBitmap.Width);
            else if (xSize == 0 && ySize != 0)
                xSize = Math.Abs(ySize * originalBitmap.Width / originalBitmap.Height);
            else if (xSize == 0 && ySize == 0)
            {
                xSize = originalBitmap.Width;
                ySize = originalBitmap.Height;
            }
        }

        //Internal resize for indexed colored images
        void IndexedRezise(int xSize, int ySize)
        {
            BitmapData sourceData;
            BitmapData targetData;

            AdjustSizes(ref xSize, ref ySize);
            scaledBitmap = new Bitmap(xSize, ySize, originalBitmap.PixelFormat)
            {
                Palette = originalBitmap.Palette,
            };
            sourceData = originalBitmap.LockBits(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height),
                ImageLockMode.ReadOnly, originalBitmap.PixelFormat);
            try
            {
                targetData = scaledBitmap.LockBits(new Rectangle(0, 0, xSize, ySize),
                    ImageLockMode.WriteOnly, scaledBitmap.PixelFormat);
                try
                {
                    xFactor = originalBitmap.Width / scaledBitmap.Width;
                    yFactor = originalBitmap.Height / scaledBitmap.Height;
                    sourceStride = sourceData.Stride;
                    sourceScan0 = sourceData.Scan0;
                    int targetStride = targetData.Stride;
                    IntPtr targetScan0 = targetData.Scan0;
                    unsafe
                    {
                        byte* p = (byte*)(void*)targetScan0;
                        int nOffset = targetStride - scaledBitmap.Width;
                        int nWidth = scaledBitmap.Width;
                        for (int y = 0; y < scaledBitmap.Height; ++y)
                        {
                            for (int x = 0; x < nWidth; ++x)
                            {
                                p[0] = GetSourceByteAt(x, y);
                                ++p;
                            }
                            p += nOffset;
                        }
                    }
                }
                finally
                {
                    scaledBitmap.UnlockBits(targetData);
                }
            }
            finally
            {
                originalBitmap.UnlockBits(sourceData);
            }
        }

        //This gets the color index on the source image for coords x, y on the resized target image
        byte GetSourceByteAt(int x, int y)
        {
            unsafe
            {
                return ((byte*)((int)sourceScan0 + (int)(Math.Floor(y * yFactor) * sourceStride) +
                    (int)Math.Floor(x * xFactor)))[0];
            }
        }

        //Internal resize for RGB colored images
        void RGBRezise(int xSize, int ySize)
        {
            AdjustSizes(ref xSize, ref ySize);
            scaledBitmap = new Bitmap(xSize, ySize, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(scaledBitmap);
            Rectangle destRect = new Rectangle(0, 0, xSize, ySize);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawImage(originalBitmap, destRect);
        }

        void Save(string fileName, ImageFormat format)
        {
            scaledBitmap.Save(fileName, format);
        }

        void Save(string fileName, long jQuality, ImageFormat format)
        {
            ImageCodecInfo jpegCodecInfo = GetEncoderInfo("image/jpeg");
            Encoder qualityEncoder = Encoder.Quality;
            EncoderParameters encoderParams = new EncoderParameters(1);
            EncoderParameter qualityEncoderParam = new EncoderParameter(qualityEncoder, jQuality);
            encoderParams.Param[0] = qualityEncoderParam;
            scaledBitmap.Save(fileName, jpegCodecInfo, encoderParams);
        }

        void Save(Stream stream, ImageFormat format)
        {
            scaledBitmap.Save(stream, format);
        }

        void Save(Stream stream, long jQuality, ImageFormat format)
        {
            ImageCodecInfo jpegCodecInfo = GetEncoderInfo("image/jpeg");
            Encoder qualityEncoder = Encoder.Quality;
            EncoderParameters encoderParams = new EncoderParameters(1);
            EncoderParameter qualityEncoderParam = new EncoderParameter(qualityEncoder, jQuality);
            encoderParams.Param[0] = qualityEncoderParam;
            scaledBitmap.Save(stream, jpegCodecInfo, encoderParams);
        }

        ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType.ToUpper() == mimeType.ToUpper())
                    return encoders[j];
            }
            return null;
        }

        public void ResizeToJpeg(int xSize, int ySize, string fileName)
        {
            RGBRezise(xSize, ySize);
            Save(fileName, ImageFormat.Jpeg);
        }

        public void ResizeToJpeg(int xSize, int ySize, Stream stream)
        {
            RGBRezise(xSize, ySize);
            Save(stream, ImageFormat.Jpeg);
        }

        public void ResizeToJpeg(int xSize, int ySize, long jQuality, string fileName)
        {
            RGBRezise(xSize, ySize);
            Save(fileName, jQuality, ImageFormat.Jpeg);
        }

        public void ResizeToJpeg(int xSize, int ySize, long jQuality, Stream stream)
        {
            RGBRezise(xSize, ySize);
            Save(stream, jQuality, ImageFormat.Jpeg);
        }


        public void ResizeToGif(int xSize, int ySize, string fileName)
        {
            IndexedRezise(xSize, ySize);
            Save(fileName, ImageFormat.Gif);
        }

        public void ResizeToGif(int xSize, int ySize, Stream stream)
        {
            IndexedRezise(xSize, ySize);
            Save(stream, ImageFormat.Gif);
        }

        public void ResizeToPng(int xSize, int ySize, string fileName)
        {
            IndexedRezise(xSize, ySize);
            Save(fileName, ImageFormat.Png);
        }

        public void ResizeToPng(int xSize, int ySize, Stream stream)
        {
            IndexedRezise(xSize, ySize);
            Save(stream, ImageFormat.Png);
        }
    }
}