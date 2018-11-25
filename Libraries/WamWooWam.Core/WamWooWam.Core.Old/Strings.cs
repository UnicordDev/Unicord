using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamWooWam.Core
{
    public static class Strings
    {
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;

            return commandLine.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;

                return !inQuotes && c == ' ';
            })
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }
    }

    public static class Text
    {
        public static string NaturalJoin(IEnumerable<string> strings, string separator = ",", string and = "&")
        {
            string result;
            int count = strings.Count();
            if (count <= 1)
                result = string.Join("", strings);
            else
            {
                result = string.Format("{0} {1} {2}"
                    , string.Join(separator + " ", strings.Take(count - 1))
                    , and
                    , strings.Last());
            }

            return result;
        }
    }

    public static class StringExtensions
    {
        public static string Replace(this string orig, string[] find, string[] replace)
        {
            for (int i = 0; i <= find.Length - 1; i++)
            {
                orig = orig.Replace(find[i], replace[i]);
            }
            return orig;
        }

        public static string Replace(this string orig, string[] find, string replace)
        {
            for (int i = 0; i <= find.Length - 1; i++)
            {
                orig = orig.Replace(find[i], replace);
            }
            return orig;
        }

        public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }
        
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
