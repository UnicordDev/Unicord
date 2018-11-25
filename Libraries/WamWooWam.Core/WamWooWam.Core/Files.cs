using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

#if !NET35
using System.Threading.Tasks;
#endif

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
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    try
                    {
                        action(file);
                    }
                    catch { continue; }
                }
                foreach (var subDir in Directory.GetDirectories(directoryPath))
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

            var i = 0;
            var dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1024)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + (i > 0 ? decimalPlaces : 0) + "}{1}", dValue, SizeSuffixes[i]);
        }

        /// <summary>
        /// Recurisvely deletes files in a directory
        /// </summary>
        /// <param name="dir">The directory to delete.</param>
        public static void DeleteDirectory(string dir)
        {
            var files = Directory.GetFiles(dir);
            var dirs = Directory.GetDirectories(dir);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var subDir in dirs)
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
                var info = new FileInfo(p);
                dirSize += info.Length;
            });

            return dirSize;
        }

#if !NET35 && !NET40

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        [Obsolete("This was a retarded idea and is entirely pointless. It has literally zero benifits. More than likely slower. Use sync instead, if you're really fucked use Task.Factory.StartNew")]
        public static async Task<long> SizeDirectoryAsync(string dir)
        {
            Console.WriteLine($@"Getting size of directory ""{dir}""");
            long DirSize = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            DirSize += info.Length;
                        }
                        catch { Console.WriteLine($@"Unable to access file ""{file}"""); }
                    });
                }
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    DirSize += await SizeDirectoryAsync(subDir);
                }
            }
            catch { Console.WriteLine($@"Unable to enumerate files in ""{dir}"""); }
            return DirSize;
        }

        /// <summary>
        /// Gets the size of a directory in bytes, with a periodic callback to a task.
        /// </summary>
        /// <param name="dir">The directory to size</param>
        /// <param name="callback">A callback function that will recieve the size as it's being calculated</param>
        /// <returns>The size in bytes of a directory.</returns>
        public static async Task<long> SizeDirectoryAsync(string dir, Func<long, Task> callback)
        {
            var info = new DirectoryInfo(dir);
            long dirSize = 0;

            try
            {
                var directories = info.GetDirectories();
                for (var i = 0; i < directories.Length; i++)
                {
                    var oldSize = dirSize;
                    dirSize = await InternalSizeDirectoryAsync(dirSize, directories[i], callback, true);
                    if ((dirSize - oldSize) > 100000)
                    {
                        await callback(dirSize);
                    }
                }
            }
            catch { }

            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in info.EnumerateFiles())
                    {
                        try
                        {
                            dirSize += file.Length;
                        }
                        catch { }
                    }
                }
                catch { }
            });

            await callback(dirSize);

            return dirSize;
        }

        internal static async Task<long> InternalSizeDirectoryAsync(long dirSize, DirectoryInfo dir, Func<long, Task> callback, bool root)
        {
            try
            {
                var directories = dir.GetDirectories();
                for (var i = 0; i < directories.Length; i++)
                {
                    var oldSize = dirSize;
                    dirSize = await InternalSizeDirectoryAsync(dirSize, directories[i], callback, false);
                    if ((dirSize - oldSize) > 100000)
                    {
                        await callback(dirSize);
                    }
                }
            }
            catch { }

            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in dir.EnumerateFiles())
                    {
                        try
                        {
                            dirSize += file.Length;
                        }
                        catch { }
                    }
                }
                catch { }
            });

            return dirSize;
        }
#endif
    }
}
