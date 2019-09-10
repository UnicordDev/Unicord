using System.Collections.Generic;
using System.Text;
using WamWooWam.Parsers.Markdown.Helpers;

namespace WamWooWam.Parsers.Markdown.Inlines
{
    public class DiscordInline : MarkdownInline
    {
        public MentionType DiscordType { get; set; }
        public string Text { get; set; }
        public ulong Id { get; set; }

        public DiscordInline() : base(MarkdownInlineType.Discord)
        {
        }

        /// <summary>
        /// Returns the chars that if found means we might have a match.
        /// </summary>
        internal static void AddTripChars(List<InlineTripCharHelper> tripCharHelpers)
        {
            tripCharHelpers.Add(new InlineTripCharHelper() { FirstChar = '<', Method = InlineParseMethod.Discord });
        }

        internal static InlineParseResult Parse(string markdown, int start, int maxEnd)
        {
            if (start >= maxEnd - 1)
            {
                return null;
            }

            // Check the start sequence.
            var startSequence = markdown[start];
            if (startSequence != '<')
            {
                return null;
            }

            // Find the end of the span.
            var innerStart = start + 1;
            var innerEnd = Common.IndexOf(markdown, '>', innerStart, maxEnd);
            if (innerEnd == -1)
            {
                return null;
            }

            // The span must contain at least one character.
            if (innerStart == innerEnd || (innerEnd - innerStart) < 4)
            {
                return null;
            }

            var text = markdown.Substring(innerStart, innerEnd - innerStart);
            var builder = new StringBuilder();
            var index = 0;
            var type = MentionType.User;
            char c;

            switch (text[index])
            {
                case '@':
                    // user or role mention

                    index++;
                    c = text[index];
                    type = MentionType.User;
                    HandleUser(text, builder, ref index, ref type, c);
                    break;
                case '#':
                    // channel mention

                    index++;
                    c = text[index];
                    type = MentionType.Channel;

                    if (char.IsDigit(c))
                    {
                        ReadDigits(text, builder, ref index);
                    }
                    break;
                case 'a':
                    // animated emote
                    index += 2;
                    type = MentionType.Emote;
                    return HandleEmote(text, builder, index, innerStart, innerEnd);
                case ':':
                    // normal emote
                    index++;
                    type = MentionType.Emote;
                    return HandleEmote(text, builder, index, innerStart, innerEnd);
                default:
                    return null;
            }

            if (ulong.TryParse(builder.ToString(), out var id))
            {
                return new InlineParseResult(new DiscordInline() { Id = id, DiscordType = type }, innerStart - 1, innerEnd + 1);
            }

            return null;
        }

        private static void HandleUser(string text, StringBuilder builder, ref int index, ref MentionType type, char c)
        {
            if (c == '!')
            {
                index++;
                ReadDigits(text, builder, ref index);
            }
            else if (char.IsDigit(c))
            {
                ReadDigits(text, builder, ref index);
            }
            else if (c == '&')
            {
                index++;
                ReadDigits(text, builder, ref index);
                type = MentionType.Role;
            }
        }

        private static InlineParseResult HandleEmote(string text, StringBuilder builder, int index, int innerStart, int innerEnd)
        {
            var nextIndex = text.IndexOf(':', index);
            if (nextIndex == -1)
            {
                return null;
            }

            var emoteName = text.Substring(index, nextIndex - index);
            nextIndex++;

            ReadDigits(text, builder, ref nextIndex);
            if (ulong.TryParse(builder.ToString(), out var id))
            {
                return new InlineParseResult(new DiscordInline() { Id = id, DiscordType = MentionType.Emote, Text = emoteName }, innerStart - 1, innerEnd + 1);
            }

            return null;
        }

        private static void ReadDigits(string text, StringBuilder builder, ref int index)
        {
            char c;
            while (index < text.Length && char.IsDigit(c = text[index]))
            {
                builder.Append(c);
                index++;
            }
        }

        public enum MentionType
        {
            User, Channel, Role, Emote
        }
    }
}
