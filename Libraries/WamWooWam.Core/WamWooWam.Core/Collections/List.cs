using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WamWooWam.Core.Collections
{
    public static class ListExtensions
    {
        public static T RandomItem<T>(this IEnumerable<T> sourceList)
        {
            var random = new Random();
            return sourceList.ElementAt(random.Next(sourceList.Count()));
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> sourceList)
        {
            var random = new Random();
            var source = sourceList.ToList(); // Not the fastest but it'll work
            var initialCount = source.Count;

            for(var i = 0; i < initialCount; i++)
            {
                var result = source.ElementAt(random.Next(source.Count));
                source.Remove(result);
                yield return result;
            }
        }
    }
}
