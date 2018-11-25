using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WamWooWam.Wpf.Tools;

namespace WamWooWam.Wpf
{
    public static class Themes
    {
        public static ThemeConfiguration CurrentConfiguration { get; private set; }

        public static void SetTheme(ThemeConfiguration config)
        {
            LoadTheme(config, Application.Current.Resources);
        }

        public static void SetTheme(FrameworkElement element, ThemeConfiguration config)
        {
            LoadTheme(config, element.Resources);
        }

        /// <summary>
        /// Loads and applies my default theme.
        /// </summary>
        /// <param name="light">Chooses between light or dark themes, leave as <see langword="null"/> to use the current Windows setting (if available).</param>
        /// <param name="accentColour">The accent colour the app should use, leave as <see langword="null"/> to use the Windows accent color.</param>
        public static void SetTheme(bool? light = null, Color? accentColour = null)
        {
            LoadTheme(new ThemeConfiguration(light, accentColour), Application.Current.Resources);
        }

        /// <summary>
        /// Loads and applies my default theme to a specified element.
        /// </summary>
        /// <param name="element">The element to load to. Preferably a <see cref="Window"/> or a <see cref="Page"/></param>
        /// <param name="light">Chooses between light or dark themes, leave as <see langword="null"/> to use the current Windows setting (if available).</param>
        /// <param name="accentColour">The accent colour the app should use, leave as <see langword="null"/> to use the Windows accent color.</param>
        public static void SetTheme(FrameworkElement element, bool? light = null, Color? accentColour = null)
        {
            LoadTheme(new ThemeConfiguration(light, accentColour), element.Resources);
        }

        private static void LoadTheme(ThemeConfiguration config, ResourceDictionary current)
        {
            CurrentConfiguration = config;

            var asm = Assembly.GetExecutingAssembly().GetName();

            current["SystemFontFamily"] = config.FontFamily;
            current["SystemFontSize"] = config.FontSize;

            current["SystemMonospaceFontFamily"] = config.MonospaceFontFamily;
            current["SystemMonospaceFontSize"] = config.MonospaceFontSize;
            var colourResources = GetColourResources(config, asm);
            current.MergedDictionaries.Add(colourResources);

            SystemEvents.UserPreferenceChanged += (o, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    current.MergedDictionaries.Remove(colourResources);
                    colourResources = GetColourResources(config, asm);
                    current.MergedDictionaries.Add(colourResources);

                    foreach (var item in Application.Current.Windows.OfType<Window>())
                    {
                        item.InvalidateVisual();
                    }
                }
            };

            if (!config.NoLoad)
                current.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri($"pack://application:,,,/{asm.Name};component/Themes/All.xaml", UriKind.Absolute) });
        }

        private static ResourceDictionary GetColourResources(ThemeConfiguration config, AssemblyName asm)
        {
            var colourResources = new ResourceDictionary();
            if (config.GetColourMode() == ThemeColourMode.Light)
            {
                colourResources.MergedDictionaries.Add( new ResourceDictionary() { Source = new Uri($"pack://application:,,,/{asm.Name};component/Themes/LightColours.xaml", UriKind.Absolute) });
            }
            else
            {
                colourResources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri($"pack://application:,,,/{asm.Name};component/Themes/DarkColours.xaml", UriKind.Absolute) });
            }

            var accent = config.GetAccentColour();
            var accentLightness = 0.299 * ((double)accent.R / 255) + 0.587 * ((double)accent.G / 255) + 0.114 * ((double)accent.B / 255);

            colourResources["SystemAccentColor"] = accent;

            colourResources["SystemAccentLighten3Brush"] = new SolidColorBrush(accent.Lighten(0.6f));
            colourResources["SystemAccentLighten2Brush"] = new SolidColorBrush(accent.Lighten(0.4f));
            colourResources["SystemAccentLighten1Brush"] = new SolidColorBrush(accent.Lighten(0.2f));
            colourResources["SystemAccentBrush"] = new SolidColorBrush(accent);
            colourResources["SystemAccentDarken1Brush"] = new SolidColorBrush(accent.Darken(0.2f));
            colourResources["SystemAccentDarken2Brush"] = new SolidColorBrush(accent.Darken(0.4f));
            colourResources["SystemAccentDarken3Brush"] = new SolidColorBrush(accent.Darken(0.6f));

            colourResources["SystemAccentForegroundBrush"] = new SolidColorBrush(accentLightness > 0.5 ? Colors.Black : Colors.White);
            colourResources["SystemAccentBackgroundBrush"] = new SolidColorBrush(accent) { Opacity = 0.5 };
            return colourResources;
        }

        private static void SetColourResource(ResourceDictionary current, string colour, string trimmed)
        {
            var col = AccentColorSet.ActiveSet[colour];
            var brush = new SolidColorBrush(col);
            var colStr = $"System{trimmed}Color";
            var brushStr = $"System{trimmed}Brush";

            brush.Freeze();
            current[colStr] = col;
            current[brushStr] = brush;
        }
    }

    public class ThemeConfiguration
    {
        public ThemeConfiguration()
        {
            ColourMode = ThemeColourMode.Automatic;
            AccentColour = null;
            FontFamily = new FontFamily("Default");
            MonospaceFontFamily = new FontFamily("Fira Code, Consolas, Lucida Sans Typewriter, Courier New");
            FontSize = (double)(new FontSizeConverter().ConvertFromString("10pt"));
            MonospaceFontSize = (double)(new FontSizeConverter().ConvertFromString("9.5pt"));
        }

        public ThemeConfiguration(bool? light = null, Color? accentColour = null) : this()
        {
            if (light.HasValue)
            {
                ColourMode = light.Value ? ThemeColourMode.Light : ThemeColourMode.Dark;
            }
            else
            {
                ColourMode = ThemeColourMode.Automatic;
            }

            AccentColour = accentColour;
        }

        public ThemeColourMode ColourMode { get; set; }
        public Color? AccentColour { get; set; }
        public FontFamily FontFamily { get; set; }
        public FontFamily MonospaceFontFamily { get; set; }
        public double FontSize { get; set; }
        public double MonospaceFontSize { get; set; }

        public bool NoLoad { get; set; }

        internal Color GetAccentColour()
        {
            if (AccentColour == null)
            {
                if (OSVersion.IsWindows10)
                {
                    try
                    {
                        return AccentColorSet.ActiveSet["SystemAccent"];
                    }
                    catch
                    {
                        return Color.FromRgb(0x00, 0x78, 0xD7); // default blue
                    }
                }
                else if (OSVersion.IsWindows8 || OSVersion.IsWindows7)
                {
                    return SystemParameters.WindowGlassColor;
                }
                else
                {
                    return Color.FromRgb(0x00, 0x78, 0xD7); // default blue
                }
            }

            return AccentColour.Value;
        }

        internal ThemeColourMode GetColourMode()
        {
            if (ColourMode == ThemeColourMode.Automatic)
            {
                if (OSVersion.IsWindows10)
                {
                    try
                    {
                        var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                        var pkey = int.Parse(key.GetValue("AppsUseLightTheme", "1").ToString());
                        return pkey != 0 ? ThemeColourMode.Light : ThemeColourMode.Dark;
                    }
                    catch
                    {
                        return ThemeColourMode.Light;
                    }
                }
                else
                {
                    return ThemeColourMode.Light;
                }
            }

            return ColourMode;
        }
    }

    public enum ThemeColourMode
    {
        Automatic, Light, Dark
    }
}