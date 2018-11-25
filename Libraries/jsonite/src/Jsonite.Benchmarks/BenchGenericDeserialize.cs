using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace Jsonite.Benchmarks
{
    public class BenchGenericDeserialize
    {
        private readonly string testJson;

        public BenchGenericDeserialize()
        {
            testJson = File.ReadAllText("test.json");
        }

        [Benchmark(Description = "Textamina.Jsonite")]
        public void TestJsonite()
        {
            var result = Json.Deserialize(testJson);
        }

        [Benchmark(Description = "Newtonsoft.Json")]
        public void TestNewtonsoftJson()
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(testJson);
        }

        [Benchmark(Description = "System.Text.Json (FastJsonParser)")]
        public void TestSystemTextJson()
        {
            var parser = new System.Text.Json.JsonParser();
            var result = parser.Parse<Dictionary<string, object>>(testJson);
        }

        [Benchmark(Description = "ServiceStack.Text")]
        public void TestServiceStackText()
        {
            // Force ServiceStack.Text to deserialize completely the object (otherwise it is deserializing only the first object level, which is not what we want to test here)
            ServiceStack.Text.JsConfig.ConvertObjectTypesIntoStringDictionary = true;
            var result = (Dictionary<string, object>)ServiceStack.StringExtensions.FromJson<object>(testJson);
        }

        [Benchmark(Description = "fastJSON")]
        public void TestFastJson()
        {
            var result = fastJSON.JSON.Parse(testJson);
        }

        [Benchmark(Description = "Jil")]
        public void TestJil()
        {
            var result = Jil.JSON.Deserialize<Dictionary<string, object>>(testJson);
        }

        [Benchmark(Description = "JavaScriptSerializer")]
        public void TestJavaScriptSerializer()
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.DeserializeObject(testJson);
        }
    }
}