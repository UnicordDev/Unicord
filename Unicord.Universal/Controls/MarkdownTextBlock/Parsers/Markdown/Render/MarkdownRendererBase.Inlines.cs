// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Unicord.Universal.Parsers.Markdown.Inlines;

namespace Unicord.Universal.Parsers.Markdown.Render
{
    /// <summary>
    /// Inline Rendering Methods.
    /// </summary>
    public partial class MarkdownRendererBase
    {
        /// <summary>
        /// Renders emoji element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderEmoji(EmojiInline element, IRenderContext context);

        /// <summary>
        /// Renders a Discord element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderDiscord(DiscordInline element, IRenderContext context);

        /// <summary>
        /// Renders a Spoiler element
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderSpoiler(SpoilerTextInline element, IRenderContext context);

        /// <summary>
        /// Renders a text run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderTextRun(TextRunInline element, IRenderContext context);

        /// <summary>
        /// Renders a bold run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderBoldRun(BoldTextInline element, IRenderContext context);

        /// <summary>
        /// Renders an underlined run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderUnderlineRun(UnderlineTextInline element, IRenderContext context);

        /// <summary>
        /// Renders a link element
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderMarkdownLink(MarkdownLinkInline element, IRenderContext context);

        /// <summary>
        /// Renders an image element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderImage(ImageInline element, IRenderContext context);

        /// <summary>
        /// Renders a raw link element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderHyperlink(HyperlinkInline element, IRenderContext context);

        /// <summary>
        /// Renders a text run element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderItalicRun(ItalicTextInline element, IRenderContext context);

        /// <summary>
        /// Renders a strikethrough element.
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderStrikethroughRun(StrikethroughTextInline element, IRenderContext context);

        /// <summary>
        /// Renders a code element
        /// </summary>
        /// <param name="element"> The parsed inline element to render. </param>
        /// <param name="context"> Persistent state. </param>
        protected abstract void RenderCodeRun(CodeInline element, IRenderContext context);
    }
}