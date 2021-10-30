// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Text;
using Unicord;
using Unicord.Universal;
using Unicord.Universal.Controls.Emoji;
using Unicord.Universal.Converters;
using Unicord.Universal.Parsers.Markdown;
using Unicord.Universal.Parsers.Markdown.Inlines;
using Unicord.Universal.Parsers.Markdown.Render;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls.Markdown.Render
{
    /// <summary>
    /// Inline UI Methods for UWP UI Creation.
    /// </summary>
    public partial class MarkdownRenderer
    {

        /// <summary>
        /// Renders emoji element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderEmoji(EmojiInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            var inlineCollection = localContext.InlineCollection;

            var emoji = new Run
            {
                FontFamily = EmojiFontFamily ?? DefaultEmojiFont,
                Text = element.Text
            };

            inlineCollection.Add(emoji);
        }

        /// <summary>
        /// Renders a text run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderTextRun(TextRunInline element, IRenderContext context)
        {
            InternalRenderTextRun(element, context);
        }

        private Run InternalRenderTextRun(TextRunInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            var inlineCollection = localContext.InlineCollection;

            // Create the text run
            var textRun = new Run
            {
                Text = CollapseWhitespace(context, element.Text)
            };

            // Add it
            inlineCollection.Add(textRun);
            return textRun;
        }

        /// <summary>
        /// Renders a bold run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderBoldRun(BoldTextInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            // Create the text run
            var boldSpan = new Span
            {
                FontWeight = FontWeights.Bold
            };

            var childContext = new InlineRenderContext(boldSpan.Inlines, context)
            {
                Parent = boldSpan,
                WithinBold = true
            };

            // Render the children into the bold inline.
            RenderInlineChildren(element.Inlines, childContext);

            // Add it to the current inlines
            localContext.InlineCollection.Add(boldSpan);
        }

        /// <summary>
        /// Renders an underlined run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderUnderlineRun(UnderlineTextInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            // Create the text run
            var boldSpan = new Span
            {
                TextDecorations = TextDecorations.Underline
            };

            var childContext = new InlineRenderContext(boldSpan.Inlines, context)
            {
                Parent = boldSpan,
                WithinUnderline = true
            };

            // Render the children into the bold inline.
            RenderInlineChildren(element.Inlines, childContext);

            // Add it to the current inlines
            localContext.InlineCollection.Add(boldSpan);
        }


        /// <summary>
        /// Renders a link element
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderMarkdownLink(MarkdownLinkInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            // Regular ol' hyperlink.
            var link = new Hyperlink();

            // Register the link
            LinkRegister.RegisterNewHyperLink(link, element.Url);

            // Render the children into the link inline.
            var childContext = new InlineRenderContext(link.Inlines, context)
            {
                Parent = link,
                WithinHyperlink = true
            };

            if (localContext.OverrideForeground)
            {
                link.Foreground = localContext.Foreground;
            }
            else if (LinkForeground != null)
            {
                link.Foreground = LinkForeground;
            }

            RenderInlineChildren(element.Inlines, childContext);
            context.TrimLeadingWhitespace = childContext.TrimLeadingWhitespace;

            ToolTipService.SetToolTip(link, element.Tooltip ?? element.Url);

            // Add it to the current inlines
            localContext.InlineCollection.Add(link);
        }

        /// <summary>
        /// Renders a raw link element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderHyperlink(HyperlinkInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            var link = new Hyperlink();

            // Register the link
            LinkRegister.RegisterNewHyperLink(link, element.Url);

            var brush = localContext.Foreground;
            if (LinkForeground != null && !localContext.OverrideForeground)
            {
                brush = LinkForeground;
            }

            // Make a text block for the link
            var linkText = new Run
            {
                Text = CollapseWhitespace(context, element.Text),
                Foreground = brush
            };

            link.Inlines.Add(linkText);

            // Add it to the current inlines
            localContext.InlineCollection.Add(link);
        }

        /// <summary>
        /// Renders an image element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override async void RenderImage(ImageInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            var inlineCollection = localContext.InlineCollection;

            var placeholder = InternalRenderTextRun(new TextRunInline { Text = element.Text, Type = MarkdownInlineType.TextRun }, context);
            var resolvedImage = await ImageResolver.ResolveImageAsync(element.RenderUrl, element.Tooltip);

            // if image can not be resolved we have to return
            if (resolvedImage == null)
            {
                return;
            }

            var image = new Image
            {
                Source = resolvedImage,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = ImageStretch
            };

            var hyperlinkButton = new HyperlinkButton()
            {
                Content = image
            };

            var viewbox = new Viewbox
            {
                Child = hyperlinkButton,
                StretchDirection = StretchDirection.DownOnly
            };

            viewbox.PointerWheelChanged += Preventative_PointerWheelChanged;

            var scrollViewer = new ScrollViewer
            {
                Content = viewbox,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var imageContainer = new InlineUIContainer() { Child = scrollViewer };

            var ishyperlink = false;
            if (element.RenderUrl != element.Url)
            {
                ishyperlink = true;
            }

            LinkRegister.RegisterNewHyperLink(image, element.Url, ishyperlink);

            if (ImageMaxHeight > 0)
            {
                viewbox.MaxHeight = ImageMaxHeight;
            }

            if (ImageMaxWidth > 0)
            {
                viewbox.MaxWidth = ImageMaxWidth;
            }

            if (element.ImageWidth > 0)
            {
                image.Width = element.ImageWidth;
                image.Stretch = Stretch.UniformToFill;
            }

            if (element.ImageHeight > 0)
            {
                if (element.ImageWidth == 0)
                {
                    image.Width = element.ImageHeight;
                }

                image.Height = element.ImageHeight;
                image.Stretch = Stretch.UniformToFill;
            }

            if (element.ImageHeight > 0 && element.ImageWidth > 0)
            {
                image.Stretch = Stretch.Fill;
            }

            // If image size is given then scroll to view overflown part
            if (element.ImageHeight > 0 || element.ImageWidth > 0)
            {
                scrollViewer.HorizontalScrollMode = ScrollMode.Auto;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }

            // Else resize the image
            else
            {
                scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            ToolTipService.SetToolTip(image, element.Tooltip);

            // Try to add it to the current inlines
            // Could fail because some containers like Hyperlink cannot have inlined images
            try
            {
                var placeholderIndex = inlineCollection.IndexOf(placeholder);
                inlineCollection.Remove(placeholder);
                inlineCollection.Insert(placeholderIndex, imageContainer);
            }
            catch
            {
                // Ignore error
            }
        }

        /// <summary>
        /// Renders a text run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderItalicRun(ItalicTextInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            // Create the text run
            var italicSpan = new Span
            {
                FontStyle = FontStyle.Italic
            };

            var childContext = new InlineRenderContext(italicSpan.Inlines, context)
            {
                Parent = italicSpan,
                WithinItalics = true
            };

            // Render the children into the italic inline.
            RenderInlineChildren(element.Inlines, childContext);

            // Add it to the current inlines
            localContext.InlineCollection.Add(italicSpan);
        }

        /// <summary>
        /// Renders a strikethrough element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderStrikethroughRun(StrikethroughTextInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            var span = new Span();

            if (TextDecorationsSupported)
            {
                span.TextDecorations = TextDecorations.Strikethrough;
            }
            else
            {
                span.FontFamily = new FontFamily("Consolas");
            }

            var childContext = new InlineRenderContext(span.Inlines, context)
            {
                Parent = span
            };

            // Render the children into the inline.
            RenderInlineChildren(element.Inlines, childContext);

            if (!TextDecorationsSupported)
            {
                AlterChildRuns(span, (parentSpan, run) =>
                {
                    var text = run.Text;
                    var builder = new StringBuilder(text.Length * 2);
                    foreach (var c in text)
                    {
                        builder.Append((char)0x0336);
                        builder.Append(c);
                    }
                    run.Text = builder.ToString();
                });
            }

            // Add it to the current inlines
            localContext.InlineCollection.Add(span);
        }

        /// <summary>
        /// Renders a code element
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected override void RenderCodeRun(CodeInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            if (localContext.Parent is Hyperlink)
            {
                // In case of Hyperlink, break glass (or add a run).

                var text = new Run { Text = CollapseWhitespace(context, element.Text) };

                if (localContext.WithinItalics)
                {
                    text.FontStyle = FontStyle.Italic;
                }

                if (localContext.WithinBold)
                {
                    text.FontWeight = FontWeights.Bold;
                }

                if (localContext.WithinUnderline)
                {
                    text.TextDecorations = TextDecorations.Underline;
                }

                localContext.InlineCollection.Add(text);
            }
            else
            {
                var text = CreateTextBlock(localContext);
                text.Text = CollapseWhitespace(context, element.Text);
                text.FontFamily = InlineCodeFontFamily ?? FontFamily;

                if (localContext.WithinItalics)
                {
                    text.FontStyle = FontStyle.Italic;
                }

                if (localContext.WithinBold)
                {
                    text.FontWeight = FontWeights.Bold;
                }

                if (localContext.WithinUnderline)
                {
                    text.TextDecorations = TextDecorations.Underline;
                }

                var borderthickness = InlineCodeBorderThickness;
                var padding = InlineCodePadding;
                var spacingoffset = -(borderthickness.Bottom + padding.Bottom);

                var margin = new Thickness(0, spacingoffset, 0, spacingoffset);

                var border = new Border
                {
                    BorderThickness = borderthickness,
                    BorderBrush = InlineCodeBorderBrush,
                    Background = InlineCodeBackground,
                    CornerRadius = InlineCodeCornerRadius,
                    Child = text,
                    Padding = padding,
                    Margin = margin
                };

                // Aligns content in InlineUI, see https://social.msdn.microsoft.com/Forums/silverlight/en-US/48b5e91e-efc5-4768-8eaf-f897849fcf0b/richtextbox-inlineuicontainer-vertical-alignment-issue?forum=silverlightarchieve
                border.RenderTransform = new TranslateTransform
                {
                    Y = 4
                };

                var inlineUIContainer = new InlineUIContainer
                {
                    Child = border,
                };

                RootElement.Margin = new Thickness(0, 0, 0, 4);
                // Add it to the current inlines
                localContext.InlineCollection.Add(inlineUIContainer);
            }
        }

        protected override void RenderSpoiler(SpoilerTextInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            // TODO (maybe): make this shit actually work?

            if (localContext.Parent is Hyperlink)
            {
                // In case of Hyperlink, break glass (or add a run).

                var span = new Span();
                var childContext = new InlineRenderContext(span.Inlines, context)
                {
                    Parent = span
                };

                // Render the children into the inline.
                RenderInlineChildren(element.Inlines, childContext);

                // Add it to the current inlines
                localContext.InlineCollection.Add(span);
            }
            else
            {
                var text = new RichTextBlock
                {
                    CharacterSpacing = CharacterSpacing,
                    FontFamily = FontFamily,
                    FontSize = FontSize,
                    FontStretch = FontStretch,
                    FontStyle = FontStyle,
                    FontWeight = FontWeight,
                    Foreground = localContext.Foreground,
                    IsTextSelectionEnabled = IsTextSelectionEnabled,
                    TextWrapping = TextWrapping
                };

                var paragraph = new Paragraph();
                var childContext = new InlineRenderContext(paragraph.Inlines, context)
                {
                    Parent = text
                };

                RenderInlineChildren(element.Inlines, childContext);

                if (localContext.WithinItalics)
                {
                    text.FontStyle = FontStyle.Italic;
                }

                if (localContext.WithinBold)
                {
                    text.FontWeight = FontWeights.Bold;
                }

                if (localContext.WithinUnderline)
                {
                    text.TextDecorations = TextDecorations.Underline;
                }

                text.Blocks.Add(paragraph);

                var borderthickness = InlineCodeBorderThickness;
                var padding = InlineCodePadding;
                var spacingoffset = -(borderthickness.Bottom + padding.Bottom);
                var margin = new Thickness(0, spacingoffset, 0, spacingoffset);

                var grid = new Grid
                {
                    BorderThickness = borderthickness,
                    BorderBrush = InlineCodeBorderBrush,
                    Background = InlineCodeBackground,
                    Padding = padding,
                    Margin = margin
                };

                // Aligns content in InlineUI, see https://social.msdn.microsoft.com/Forums/silverlight/en-US/48b5e91e-efc5-4768-8eaf-f897849fcf0b/richtextbox-inlineuicontainer-vertical-alignment-issue?forum=silverlightarchieve
                grid.RenderTransform = new TranslateTransform
                {
                    Y = 4
                };

                grid.Children.Add(text);

                if (App.RoamingSettings.Read(Constants.ENABLE_SPOILERS, true))
                {
                    var border = new Border()
                    {
                        Background = InlineCodeBackground,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    border.Tapped += (o, e) => { border.Opacity = 0; };

                    grid.Children.Add(border);
                }

                var inlineUIContainer = new InlineUIContainer
                {
                    Child = grid,
                };

                RootElement.Margin = new Thickness(0, 0, 0, 4);

                // Add it to the current inlines
                localContext.InlineCollection.Add(inlineUIContainer);
            }
        }

        private Brush GetDiscordBrush(DiscordColor color)
        {
            return color.Value == 0 ? Foreground : (Brush)ColourBrushConverter.Convert(color, typeof(Brush), null, "");
        }

        protected override void RenderDiscord(DiscordInline element, IRenderContext context)
        {
            if (!(context is InlineRenderContext localContext))
            {
                throw new RenderContextIncorrectException();
            }

            if (Channel != null)
            {
                var guild = Channel.Guild;
                var client = Channel.Discord as DiscordClient;

                if (element.DiscordType != DiscordInline.MentionType.Emote)
                {
                    var run = new Run() { FontWeight = FontWeights.Bold };

                    if (element.DiscordType == DiscordInline.MentionType.User)
                    {
                        var user = guild != null ? (guild.Members.TryGetValue(element.Id, out var memb) ? memb : null) : client.TryGetCachedUser(element.Id, out var u) ? u : null;
                        if (user != null)
                        {
                            run.Text = IsSystemMessage ? user.DisplayName : $"@{user.DisplayName}";
                            run.Foreground = GetDiscordBrush(user.Color);
                        }
                        else
                        {
                            run.Text = $"<@{element.Id}>";
                        }
                    }
                    else if (element.DiscordType == DiscordInline.MentionType.Role)
                    {
                        if (Channel.Guild != null && Channel.Guild.Roles.TryGetValue(element.Id, out var role))
                        {
                            run.Text = $"@{role.Name}";
                            run.Foreground = GetDiscordBrush(role.Color);
                        }
                        else
                        {
                            run.Text = $"<@&{element.Id}>";
                        }
                    }
                    else if (element.DiscordType == DiscordInline.MentionType.Channel)
                    {
                        if (client.TryGetCachedChannel(element.Id, out var channel) && !(channel is DiscordDmChannel))
                        {
                            run.Text = $"#{channel.Name}";
                        }
                        else
                        {
                            run.Text = $"#deleted-channel";
                        }
                    }


                    localContext.InlineCollection.Add(run);
                }
                else
                {
                    var border = RootElement.FindParent<Border>();
                    var uri = $"https://cdn.discordapp.com/emojis/{element.Id}?size=128";
                    var ui = new InlineUIContainer() { FontSize = IsHuge ? 42 : 24 };
                    var size = IsHuge ? 48 : 24;
                    var image = new EmojiControl()
                    {
                        Emoji = DiscordEmoji.FromGuildEmote(App.Discord, element.Id, element.Text),
                        Size = size,
                        MaxWidth = size * 3,
                        Margin = IsHuge ? default : new Thickness(0, 0, 0, -8)
                    };

                    ToolTipService.SetToolTip(image, element.Text);
                    ui.Child = image;

                    RootElement.Margin = new Thickness(0, 0, 0, 4);
                    localContext.InlineCollection.Add(ui);

                    //if (!IsHuge && RootElement.RenderTransform is not TranslateTransform tt)
                    //{
                    //    RootElement.Margin = new Thickness(0, -8, 0, 0);
                    //}
                }
            }
        }
    }
}