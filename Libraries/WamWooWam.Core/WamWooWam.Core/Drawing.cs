#if NET35 || NET40 || NET45 || NET461 || NETSTANDARD2_0

using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

#endif
using System;

namespace WamWooWam.Core
{
    public static class Drawing
    {

#if NET35 || NET40 || NET45 || NET461 || NETSTANDARD2_0
        public static Image ResizeImage(Image image, Size size, bool preserveAspectRatio = true)
        {
            int newWidth;
            int newHeight;

            if (preserveAspectRatio)
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;
                float percentWidth = (float)size.Width / (float)originalWidth;
                float percentHeight = (float)size.Height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = size.Width;
                newHeight = size.Height;
            }

            Image newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }
#endif

        public static void ScaleProportions(ref int currentWidth, ref int currentHeight, int maxWidth, int maxHeight)
        {
            if (currentWidth <= maxWidth && currentHeight <= maxHeight)
            {
                return ;
            }
            else
            {
                var ratioX = (double)maxWidth / currentWidth;
                var ratioY = (double)maxHeight / currentHeight;
                var ratio = Math.Min(ratioX, ratioY);

                currentWidth = (int)(currentWidth * ratio);
                currentHeight  = (int)(currentHeight * ratio);
            }
        }
    }
}
