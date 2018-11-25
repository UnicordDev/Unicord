using DSharpPlus;
using DSharpPlus.Entities;
using System;
#if WINDOWS_WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Unicord.Desktop.Converters;
#elif WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Unicord.Universal.Converters;
#endif

#if WINDOWS_WPF
namespace Unicord.Desktop.Controls
#elif WINDOWS_UWP
namespace Unicord.Universal.Controls
#endif
{
    public class ProfilePicture : Control
    {
        public DiscordUser User
        {
            get { return (DiscordUser)GetValue(UserProperty); }
            set { SetValue(UserProperty, value); }
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(DiscordUser), typeof(ProfilePicture), new PropertyMetadata(null, PropertyChanged));

        public DiscordChannel Channel
        {
            get { return (DiscordChannel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(ProfilePicture), new PropertyMetadata(null, PropertyChanged));

        public DiscordGuild Guild
        {
            get { return (DiscordGuild)GetValue(GuildProperty); }
            set { SetValue(GuildProperty, value); }
        }

        public static readonly DependencyProperty GuildProperty =
            DependencyProperty.Register("Guild", typeof(DiscordGuild), typeof(ProfilePicture), new PropertyMetadata(null, PropertyChanged));

        public double StatusSize
        {
            get { return (double)GetValue(StatusSizeProperty); }
            set { SetValue(StatusSizeProperty, value); }
        }

        public static readonly DependencyProperty StatusSizeProperty =
            DependencyProperty.Register("StatusSize", typeof(double), typeof(ProfilePicture), new PropertyMetadata(10d));

        public bool ShowStatus
        {
            get { return (bool)GetValue(ShowStatusProperty); }
            set { SetValue(ShowStatusProperty, value); }
        }

        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register("ShowStatus", typeof(bool), typeof(ProfilePicture), new PropertyMetadata(false));

        public bool AutoUpdate
        {
            get { return (bool)GetValue(AutoUpdateProperty); }
            set { SetValue(AutoUpdateProperty, value); }
        }

        public static readonly DependencyProperty AutoUpdateProperty =
            DependencyProperty.Register("AutoUpdate", typeof(bool), typeof(ProfilePicture), new PropertyMetadata(false));
#if WINDOWS_WPF
        private static GroupIconConverter _iconConverter = new GroupIconConverter();
#endif
        private static PresenceColourConverter _presenceConverter = new PresenceColourConverter();

        private bool _hasTemplate;

        static ProfilePicture()
        {
#if WINDOWS_WPF
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProfilePicture), new FrameworkPropertyMetadata(typeof(ProfilePicture)));
#endif
        }

#if WINDOWS_WPF
        public override void OnApplyTemplate()
#elif WINDOWS_UWP
        protected override void OnApplyTemplate()
#endif
        {
            _hasTemplate = true;
            Update(this);
        }

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picture = d as ProfilePicture;
            Update(picture);
        }

        private static void Update(ProfilePicture picture)
        {
            if (picture._hasTemplate && (picture.User != null || picture.Channel != null || picture.Guild != null))
            {
                var imageBrush = new ImageBrush();

                var image = picture.GetTemplateChild("PART_ProfilePicture") as FrameworkElement;
                var status = picture.GetTemplateChild("PART_Status") as FrameworkElement;
                if (picture.ShowStatus && picture.User != null)
                {
#if WINDOWS_WPF
                    image.OpacityMask = Application.Current.FindResource("UserMaskBrush") as Brush;
#endif
                    status.Visibility = Visibility.Visible;
                }
                else
                {
                    status.Visibility = Visibility.Collapsed;
#if WINDOWS_WPF
                    BindingOperations.ClearAllBindings(status);
#endif
                }

                var bitmapImage = new BitmapImage
                {
                    DecodePixelWidth = (int)picture.Width,
                    DecodePixelHeight = (int)picture.Height,
                };

#if WINDOWS_WPF
                bitmapImage.BeginInit();
#endif

                if (picture.User != null)
                {
                    bitmapImage.UriSource = new Uri(picture.User.NonAnimatedAvatarUrl);

                    if (picture.ShowStatus && status is Shape sh)
                    {
                        var bind = new Binding()
                        {
                            Path = new PropertyPath("Presence"),
                            Source = picture.User,
                            Converter = _presenceConverter
                        };

                        sh.SetBinding(Shape.FillProperty, bind);
                    }
                }
                else if (picture.Channel != null)
                {
                    if (picture.Channel.Type == ChannelType.Group)
                    {
                        // Currently unsupported in Universal.
#if WINDOWS_WPF
                        if (image is Shape sh)
                        {
                            sh.Fill = (Brush)_iconConverter.Convert(picture.Channel, null, null, null);
                        }

                        if (image is Control ct)
                        {
                            ct.Background = (Brush)_iconConverter.Convert(picture.Channel, null, null, null);
                        }
#endif
                        return;
                    }
                }
                else if (picture.Guild != null && picture.Guild.IconUrl != null)
                {
                    bitmapImage.UriSource = new Uri(picture.Guild.IconUrl);
                }
                else
                {
                    if (image is Shape sh)
                    {
                        sh.Fill = null;
                    }

                    if (image is Control ct)
                    {
                        ct.Background = null;
                    }

                    return;
                }

#if WINDOWS_WPF
                bitmapImage.EndInit();
#endif

                imageBrush.ImageSource = bitmapImage;

                if (image is Shape s)
                {
                    s.Fill = imageBrush;
                }

                if (image is Control c)
                {
                    c.Background = imageBrush;
                }
            }
        }

        static int ToNextNearest(int x)
        {
            if (x < 0) { return 0; }
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        static int ToNearest(int x)
        {
            int next = ToNextNearest(x);
            int prev = next >> 1;
            return next - x < x - prev ? next : prev;
        }
    }
}
