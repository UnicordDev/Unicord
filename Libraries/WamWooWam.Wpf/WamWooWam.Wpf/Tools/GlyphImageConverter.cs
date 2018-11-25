using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WamWooWam.Wpf.Tools
{
    public class GlyphImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            var fontFamily = parameter as FontFamily ?? Themes.CurrentConfiguration.FontFamily;
            var drawing = new DrawingGroup();
            var context = drawing.Append();

            var typeFace = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var size = (double)new FontSizeConverter().ConvertFrom("32px");
            var brush = Themes.CurrentConfiguration.GetColourMode() == ThemeColourMode.Dark ? Brushes.White : Brushes.Black;

            context.DrawText(new FormattedText(str, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, size, brush), new Point(0, 0));

            return new DrawingImage(drawing);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
