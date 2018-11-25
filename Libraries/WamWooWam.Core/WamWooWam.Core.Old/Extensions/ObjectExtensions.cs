using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WamWooWam.Core.Extensions
{
    public static class ObjectExtensions

    {        /// <summary>
             /// Converts an object to it's JSON representation
             /// </summary>
             /// <param name="obj">The object to serialise</param>
             /// <returns>A JSON representation of the object</returns>
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Converts an object to it's JSON representation
        /// </summary>
        /// <param name="obj">The object to serialise</param>
        /// <param name="formatting">The format of the file.</param>
        /// <returns>A JSON representation of the object</returns>
        public static string ToJson(this object obj, Formatting formatting)
        {
            return JsonConvert.SerializeObject(obj, formatting);
        }

        /// <summary>
        /// Converts a file to it's XML representation
        /// </summary>
        /// <param name="obj">The object to serialise</param>
        /// <returns>An XML representation of the object.</returns>
        public static string ToXml(this object obj)
        {
            using (StringWriter stringwriter = new StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                serializer.Serialize(stringwriter, obj);
                return stringwriter.ToString();
            }
        }
    }
}
