// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Unicord.Universal.Parsers.Markdown.Blocks;

namespace Unicord.Universal.Parsers.Markdown.Render
{
    /// <summary>
    /// Block Rendering Methods.
    /// </summary>
    public partial class MarkdownRendererBase
    {
        /// <summary>
        /// Renders a paragraph element.
        /// </summary>
        protected abstract void RenderParagraph(ParagraphBlock element, IRenderContext context);

        /// <summary>
        /// Renders a header element.
        /// </summary>
        protected abstract void RenderHeader(HeaderBlock element, IRenderContext context);

        /// <summary>
        /// Renders a list element.
        /// </summary>
        protected abstract void RenderListElement(ListBlock element, IRenderContext context);

        /// <summary>
        /// Renders a horizontal rule element.
        /// </summary>
        protected abstract void RenderHorizontalRule(IRenderContext context);

        /// <summary>
        /// Renders a quote element.
        /// </summary>
        protected abstract void RenderQuote(QuoteBlock element, IRenderContext context);

        /// <summary>
        /// Renders a code element.
        /// </summary>
        protected abstract void RenderCode(CodeBlock element, IRenderContext context);

        /// <summary>
        /// Renders a table element.
        /// </summary>
        protected abstract void RenderTable(TableBlock element, IRenderContext context);
    }
}