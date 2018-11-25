using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WamWooWam.Core
{
    public static class Files
    {
        static readonly string[] SizeSuffixes =
                 { " bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Recursively runs an action on all files in a directory.
        /// </summary>
        /// <param name="directoryPath">The directory path</param>
        /// <param name="action">The action to run on each file</param>
        public static void RunRecursive(string directoryPath, Action<string> action)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(directoryPath))
                {
                    try
                    {
                        action(file);
                    }
                    catch { continue; }
                }
                foreach (string subDir in Directory.EnumerateDirectories(directoryPath))
                {
                    RunRecursive(subDir, action);
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns a nicely formatted file size for a given number of bytes (1.10MB, 831 bytes, etc.)
        /// </summary>
        /// <param name="value">The size to suffix</param>
        /// <param name="decimalPlaces">The number of decimal places to round to, defaults to two.</param>
        /// <returns>A formatted file size for a given number of bytes (1.10MB, 831 bytes, etc.)</returns>
        public static string SizeSuffix(long value, int decimalPlaces = 2)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "}{1}", dValue, SizeSuffixes[i]);
        }

        /// <summary>
        /// Recurisvely deletes files in a directory
        /// </summary>
        /// <param name="dir">The directory to delete.</param>
        public static void DeleteDirectory(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            string[] dirs = Directory.GetDirectories(dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string subDir in dirs)
            {
                DeleteDirectory(subDir);
            }

            Directory.Delete(dir, true);
        }

        /// <summary>
        /// Returns the total size of a directory and all sub directories in bytes
        /// </summary>
        /// <param name="dir">The directory to size</param>
        /// <returns>The total size of a directory and all sub directories in bytes</returns>
        public static long SizeDirectory(string dir)
        {
            long dirSize = 0;

            RunRecursive(dir, p =>
            {
                FileInfo info = new FileInfo(p);
                dirSize += info.Length;
            });

            return dirSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        [Obsolete("This was a retarded idea and is entirely pointless. It has literally zero benifits. Use sync instead, if you're really fucked use Task.Factory.StartNew")]
        public static async Task<long> SizeDirectoryAsync(string dir)
        {
            Console.WriteLine($@"Getting size of directory ""{dir}""");
            long DirSize = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            FileInfo info = new FileInfo(file);
                            DirSize += info.Length;
                        }
                        catch { Console.WriteLine($@"Unable to access file ""{file}"""); }
                    });
                }
                foreach (string subDir in Directory.EnumerateDirectories(dir))
                {
                    DirSize += await SizeDirectoryAsync(subDir);
                }
            }
            catch { Console.WriteLine($@"Unable to enumerate files in ""{dir}"""); }
            return DirSize;
        }
    }
}
