// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Unicord.Universal.Parsers.Markdown
{
    /// <summary>
    /// Determines the type of Inline the Inline Element is.
    /// </summary>
    public enum MarkdownInlineType
    {
        /// <summary>
        /// A text run
        /// </summary>
        TextRun,

        /// <summary>
        /// A bold run
        /// </summary>
        Bold,

        /// <summary>
        /// An italic run
        /// </summary>
        Italic,

        Underline,

        /// <summary>
        /// A link in markdown syntax
        /// </summary>
        MarkdownLink,

        /// <summary>
        /// A raw hyper link
        /// </summary>
        RawHyperlink,

        /// <summary>
        /// A raw subreddit link
        /// </summary>
        RawSubreddit,

        /// <summary>
        /// A strike through run
        /// </summary>
        Spoiler,

        /// <summary>
        /// A superscript run
        /// </summary>
        Superscript,

        /// <summary>
        /// A code run
        /// </summary>
        Code,

        /// <summary>
        /// An image
        /// </summary>
        Image,

        /// <summary>
        /// Emoji
        /// </summary>
        Emoji,

        /// <summary>
        /// Link Reference
        /// </summary>
        LinkReference,
        Discord,
        Strikethrough
    }
}