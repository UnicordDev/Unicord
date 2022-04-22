using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace System.Windows.Shell.Aurora
{
    /// <summary>
    /// Interaction logic for PreviewPaneAuroraControl.xaml
    /// </summary>
    public partial class PreviewPaneAuroraControl : UserControl
    {
        private record ColorSet(Color Aurora, Color Background);

        private ColorSet[] _colorSets = new[]
        {
            new ColorSet(Color.FromArgb(255, 0x85, 0x99, 0xB4), Color.FromArgb(255, 0x5A, 0x6B, 0x7D)), // Default
            new ColorSet(Color.FromArgb(255, 0x51, 0x90, 0xDA), Color.FromArgb(255, 0x24, 0x43, 0x8E)), // Documents
            new ColorSet(Color.FromArgb(255, 0xF2, 0xA4, 0x7B), Color.FromArgb(255, 0xD2, 0x64, 0x2A)), // Contacts
            new ColorSet(Color.FromArgb(255, 0xDA, 0x51, 0x51), Color.FromArgb(255, 0x74, 0x14, 0x14)), // Music
            new ColorSet(Color.FromArgb(255, 0x9E, 0xCA, 0x4E), Color.FromArgb(255, 0x6E, 0x97, 0x24)), // Games
            new ColorSet(Color.FromArgb(255, 0x6F, 0x49, 0x70), Color.FromArgb(255, 0x26, 0x08, 0x27)), // Photos
        };

        private int _i = 0;
        private Color _color = Color.FromArgb(255, 0x85, 0x99, 0xB4);
        private TimeSpan _duration = TimeSpan.FromSeconds(0.5);

        public PreviewPaneAuroraControl()
        {
            InitializeComponent();
            BackgroundLayer.Tapped += BackgroundLayer_Tapped;

            this._AnimateAurora(_colorSets[_i].Aurora, _colorSets[_i].Background);
            _i = (_i + 1) % _colorSets.Length;
        }

        private void BackgroundLayer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this._AnimateAurora(_colorSets[_i].Aurora, _colorSets[_i].Background);
            _i = (_i + 1) % _colorSets.Length;
        }

        private void _AnimateAurora(Color colorAurora, Color colorBackground)
        {
            var colorOld = _color;
            var storyboard = new Storyboard();
            if (BackgroundLayer.Background is not SolidColorBrush backgroundBrush)
                backgroundBrush = new SolidColorBrush();

            // RegisterName("BackgroundLayerBrush", backgroundBrush);

            var backgroundAnim = new ColorAnimation();
            backgroundAnim.From = backgroundBrush.Color;
            backgroundAnim.To = colorBackground;
            backgroundAnim.Duration = new Duration(_duration);

            Storyboard.SetTarget(backgroundAnim, backgroundBrush);
            Storyboard.SetTargetProperty(backgroundAnim, "Color");
            storyboard.Children.Add(backgroundAnim);

            this._AdjustAurora(storyboard, colorOld, colorAurora, BackgroundLayer);

            storyboard.Begin();

            _color = colorAurora;
        }

        private void _AdjustAurora(Storyboard sb, Color colorOld, Color colorNew, UIElement pe)
        {
            if (pe is Shape shape)
            {
                if (shape.Fill is GradientBrush fill)
                {
                    Color colorNew1 = colorNew;
                    Color colorOld1 = colorOld;
                    this._AdjustedLinearGradient(sb, fill, colorOld1, colorNew1);
                }
                if (shape.Stroke is GradientBrush stroke)
                {
                    Color colorNew2 = colorNew;
                    Color colorOld2 = colorOld;
                    this._AdjustedLinearGradient(sb, stroke, colorOld2, colorNew2);
                }
            }


            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(pe); i++)
            {
                var child = VisualTreeHelper.GetChild(pe, i);
                if (child is not UIElement element) continue;

                Color colorNew3 = colorNew;
                this._AdjustAurora(sb, colorOld, colorNew3, element);
            }
        }

        private void _AdjustedLinearGradient(
            Storyboard sb,
            GradientBrush pLinearGradient,
            Color colorOld,
            Color colorNew)
        {
            GradientStopCollection gradientStops = pLinearGradient.GradientStops;
            for (int i = 0; i < gradientStops.Count; i++)
            {
                GradientStop gradientStop = gradientStops[i];
                Color color1 = gradientStop.Color;
                Color color2 = gradientStop.Color;
                Color color1_1 = Color.FromArgb(255, gradientStop.Color.R, color2.G, color1.B);
                Color color2_1 = colorOld;
                Color color4 = !AreClose(color1_1, color2_1) ?
                    gradientStop.Color :
                    Color.FromArgb(gradientStop.Color.A, colorNew.R, colorNew.G, colorNew.B);


                var colorAnimation = new ColorAnimation();
                colorAnimation.From = color1;
                colorAnimation.To = color4;
                colorAnimation.Duration = new Duration(_duration);
                colorAnimation.EnableDependentAnimation = true;

                Storyboard.SetTarget(colorAnimation, gradientStop);
                Storyboard.SetTargetProperty(colorAnimation, "Color");

                sb.Children.Add(colorAnimation);
            }
        }

        private bool AreClose(Color color1, Color color2)
        {
            return AreClose(color1.R, color2.R) && AreClose(color1.G, color2.G) && AreClose(color1.B, color2.B) && AreClose(color1.A, color2.A);
        }

        internal static float FLT_EPSILON = 1.1920929E-07f;

        public static bool AreClose(float a, float b)
        {
            if (a == b)
            {
                return true;
            }

            float num = (Math.Abs(a) + Math.Abs(b) + 10f) * FLT_EPSILON;
            float num2 = a - b;
            if (0f - num < num2)
            {
                return num > num2;
            }

            return false;
        }

    }
}
