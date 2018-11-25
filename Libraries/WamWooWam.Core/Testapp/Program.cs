using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using WamWooWam.Core.Collections;

namespace Testapp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Files.SizeDirectoryAsync(@"C:\", l =>
            {
                Console.WriteLine(l);
                return Task.CompletedTask;
            });

            Console.ReadKey();
        }
    }
}
