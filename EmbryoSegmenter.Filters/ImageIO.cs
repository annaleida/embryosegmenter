using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using itk;

namespace EmbryoSegmenter.Filters
{
    public static class ImageIO
    {
        public static itkImageBase ReadImage(string filename)
        {
            itkImageBase im = itkImage_UC2.New();
            im.Read(filename);
            return im;

        }

        /// <summary>
        /// Converts an itkImageBase to a System.Drawing.Bitmap.
        /// The itkImageBase must be a 2D image with scalar pixel type
        /// and UnsignedChar component type. A NotSupportedException
        /// is raised if the image does not meet these criteria.
        /// </summary>
        /// <param name="image">The image to convert</param>
        /// <returns></returns>
        public static Bitmap ConvertItkImageToBitmap(itkImageBase image)
        {
            // Check image is 2D
            if (image.Dimension != 2)
            {
                String message = String.Empty;
                message += "We can only display images with 2 dimensions.";
                message += "The given image has {0} dimensions!";
                throw new NotSupportedException(String.Format(message, image.Dimension));
            }

            // Check the pixel type is scalar
            if (!image.PixelType.IsScalar)
            {
                String message = "We can only display images with scalar pixels.";
                message += "The given image has {0} pixel type!";
                throw new NotSupportedException(String.Format(message, image.PixelType));
            }

            // Check the pixel type is unsigned char
            if (image.PixelType.TypeAsEnum != itkPixelTypeEnum.UnsignedChar)
            {
                String message = String.Empty;
                message += "We can only display images with UnsignedChar pixels.";
                message += "The given image has {0} pixel type!";
                throw new NotSupportedException(String.Format(message, image.PixelType));
            }

            // The result will be a System.Drawing.Bitmap
            Bitmap bitmap;

            // Set pixel format as 8-bit grayscale
            PixelFormat format = PixelFormat.Format8bppIndexed;

            // Check if the stride is the same as the width
            if (image.Size[0] % 4 == 0)
            {
                // Width = Stride: simply use the Bitmap constructor
                bitmap = new Bitmap(image.Size[0],  // Width
                                    image.Size[1],  // Height
                                    image.Size[0],  // Stride
                                    format,         // PixelFormat
                                    image.Buffer    // Buffer
                                    );
            }
            else
            {
                unsafe
                {
                    // Width != Stride: copy data from buffer to bitmap
                    int width = image.Size[0];
                    int height = image.Size[1];
                    byte* buffer = (byte*)image.Buffer.ToPointer();

                    // Compute the stride
                    int stride = width;
                    if (width % 4 != 0)
                        stride = ((width / 4) * 4 + 4);

                    bitmap = new Bitmap(width, height, format);
                    Rectangle rect = new Rectangle(0, 0, width, height);
                    BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, format);

                    for (int j = 0; j < height; j++)                          // Row
                    {
                        byte* row = (byte*)bitmapData.Scan0 + (j * stride);
                        for (int i = 0; i < width; i++)                       // Column
                            row[i] = buffer[(j * width) + i];
                    }
                    bitmap.UnlockBits(bitmapData);
                }// end unsafe
            }// end if (Width == Stride)

            // Set a color palette
            bitmap.Palette = CreateGrayscalePalette(format, 256);

            // Return the resulting bitmap
            return bitmap;

        }// end ConvertItkImageToBitmap()

        /// <summary>
        /// Create a 256 grayscale color palette.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="numberOfEntries">The number of entries in the palette.</param>
        /// <returns></returns>
        public static ColorPalette CreateGrayscalePalette(PixelFormat format, int numberOfEntries)
        {
            ColorPalette palette;    // The Palette we are stealing
            Bitmap bitmap;           // The source of the stolen palette

            // Make a new Bitmap object to steal its Palette
            bitmap = new Bitmap(1, 1, format);

            palette = bitmap.Palette;   // Grab the palette
            bitmap.Dispose();           // Cleanup the source Bitmap

            // Populate the palette
            for (int i = 0; i < numberOfEntries; i++)
                palette.Entries[i] = Color.FromArgb(i, i, i);

            // Return the palette
            return palette;
        }
    }
}
