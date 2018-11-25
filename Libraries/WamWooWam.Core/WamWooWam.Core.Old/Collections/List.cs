using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamWooWam.Core.Collections
{
    public static class ListExtensions
    {
        public static T RandomItem<T>(this IEnumerable<T> SourceList)
        {
            Random random = new Random();
            return SourceList.ElementAt(random.Next(SourceList.Count()));
        }
    }
}
