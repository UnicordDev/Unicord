using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using Windows.UI.Xaml;

namespace Unicord.Universal.Utilities
{
    // TODO: Componentise rest of ThemeManager.cs
    internal class ThemePostProcessor
    {
        private Type[] _processedTargetTypes = new[] {
            typeof(Controls.Messages.MessageControl),
            typeof(Controls.Messages.SystemMessageControl)
        };

        public void PostProcessDictionary(Theme theme, ResourceDictionary resourceDictionary)
        {
            //foreach (var dict in resourceDictionary.MergedDictionaries)
            //{
            //    PostProcessDictionary(theme, dict);
            //}

            //foreach (var resource in resourceDictionary)
            //{
            //    if (resource.Value is Style style && _processedTargetTypes.Any(t => t.IsAssignableFrom(style.TargetType)))
            //    {
            //        var newKey = Strings.Normalise($"{theme.Name} {style.TargetType.FullName.Replace('.', ' ')} {resource.Key}", '_' , false);
            //        resourceDictionary.Remove(resource.Key);
            //        resourceDictionary.Add(newKey, resource.Value);
            //    }
            //}
        }
    }
}
