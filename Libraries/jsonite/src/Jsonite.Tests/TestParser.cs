// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Jsonite;
using NUnit.Framework;

namespace Textamina.Jsonite.Tests
{
    /// <summary>
    /// Tests for <see cref="Json.Deserialize"/>  and <see cref="Json.Serialize"/>.
    /// </summary>
    [TestFixture]
    public class TestParser
    {
        private const string RelativeBasePath = @"..\..\TestFiles";
        private const string InputFilePattern = "*.txt";
        private const string OutputEndFileExtension = ".out.txt";

        [Test]
        public void ParseBoolAndNull()
        {
            // See TestErrors files for other cases
            Assert.AreEqual(true, Json.Deserialize("true"));
            Assert.AreEqual(false, Json.Deserialize("false"));
            Assert.AreEqual(null, Json.Deserialize("null"));
            Assert.Catch<JsonException>(() => Json.Deserialize("invalid"));
        }

        [Test]
        public void ParseNumbers()
        {
            // See TestErrors files for other cases
            Assert.AreEqual(0, Json.Deserialize("0"));
            Assert.AreEqual(1, Json.Deserialize("1"));
            Assert.AreEqual(-1, Json.Deserialize("-1"));
            Assert.AreEqual(1.0, Json.Deserialize("1.0"));
            Assert.AreEqual(-1.0, Json.Deserialize("-1.0"));
            Assert.AreEqual(0.001, Json.Deserialize("0.001"));
            Assert.AreEqual(-0.001, Json.Deserialize("-0.001"));
            Assert.AreEqual(1e10, Json.Deserialize("1e10"));
            Assert.AreEqual(1e-10, Json.Deserialize("1e-10"));
            Assert.AreEqual(0.1e-10, Json.Deserialize("0.1e-10"));
            Assert.AreEqual(1.1e-10, Json.Deserialize("1.1e-10"));
            Assert.AreEqual(1.1e+10, Json.Deserialize("1.1e+10"));
            Assert.AreEqual(-1.0, Json.Deserialize("-1.0"));
            Assert.AreEqual(int.MaxValue, Json.Deserialize(int.MaxValue.ToString()));
            Assert.AreEqual(int.MinValue, Json.Deserialize(int.MinValue.ToString()));
            Assert.AreEqual(long.MaxValue, Json.Deserialize(long.MaxValue.ToString()));
            Assert.AreEqual(long.MinValue, Json.Deserialize(long.MinValue.ToString()));
            Assert.AreEqual(ulong.MaxValue, Json.Deserialize(ulong.MaxValue.ToString()));
            Assert.AreEqual(ulong.MinValue, Json.Deserialize(ulong.MinValue.ToString()));
            Assert.AreEqual(decimal.MaxValue, Json.Deserialize(decimal.MaxValue.ToString()));
            Assert.AreEqual(decimal.MinValue, Json.Deserialize(decimal.MinValue.ToString()));
            Assert.AreEqual(double.MaxValue, Json.Deserialize(double.MaxValue.ToString("R")));
            Assert.AreEqual(double.MinValue, Json.Deserialize(double.MinValue.ToString("R")));
        }

        [Test]
        public void ParseString()
        {
            // See TestErrors files for other cases
            Assert.AreEqual("test", Json.Deserialize(@"""test"""));
            TextAssert.AreEqual("\"\\\b\f\r\n\t ", Json.Deserialize(@"""\""\\\b\f\r\n\t\u0020""") as string);
        }

        [Test]
        public void TestObject()
        {
            // See TestErrors files for other cases
            var src = "{\"member\":15,\"member2\":null,\"toto\":[1,2,3,4]}";
            var obj = Json.Deserialize(src);
            Assert.NotNull(obj);
            Assert.AreEqual(typeof(JsonObject), obj.GetType());
            var output = Json.Serialize(obj);
            TextAssert.AreEqual(src, output);
        }

        [Test]
        public void TestArray()
        {
            // See TestErrors files for other cases
            var src = "[1,2,null,true,false,\"YES\"]";
            var obj = Json.Deserialize(src);
            Assert.NotNull(obj);
            Assert.AreEqual(typeof (JsonArray), obj.GetType());
            Assert.AreEqual(new JsonArray() { 1, 2, null, true, false, "YES"}, obj);
            var output = Json.Serialize(obj);
            TextAssert.AreEqual(src, output);
        }

        [Test]
        public void TestLarge()
        {
            var rootPath = Path.GetDirectoryName(typeof (TestParser).Assembly.Location);
            var src = File.ReadAllText(Path.Combine(rootPath, @"..\..\..\Textamina.Jsonite.Benchmarks\test.json"));
            var obj = Json.Deserialize(src);
            var output1 = Json.Serialize(obj);
            var obj1 = Json.Deserialize(output1);
            var output2 = Json.Serialize(obj1);
            TextAssert.AreEqual(output1, output2);
        }

        [TestCaseSource("TestFiles")]
        public void TestErrors(TestFilePath testFilePath)
        {
            var inputName = testFilePath.FilePath;
            var baseDir = Path.GetFullPath(Path.Combine(BaseDirectory, RelativeBasePath));

            var inputFile = Path.Combine(baseDir, inputName);
            var inputText = File.ReadAllText(inputFile);

            var expectedOutputFile = Path.ChangeExtension(inputFile, OutputEndFileExtension);
            Assert.True(File.Exists(expectedOutputFile), $"Expecting output result file [{expectedOutputFile}] for input file [{inputName}]");
            var expectedOutputText = File.ReadAllText(expectedOutputFile, Encoding.UTF8);

            var result = string.Empty;

            try
            {
                Json.Validate(inputText);
            }
            catch (JsonException exception)
            {
                result = exception.ToString();
            }

            Console.Write(result);

            TextAssert.AreEqual(expectedOutputText, result);
        }

        public static IEnumerable<object[]> TestFiles
        {
            get
            {
                var baseDir = Path.GetFullPath(Path.Combine(BaseDirectory, RelativeBasePath));
                return
                    Directory.EnumerateFiles(baseDir, InputFilePattern, SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(OutputEndFileExtension))
                        .Select(f => f.StartsWith(baseDir) ? f.Substring(baseDir.Length + 1) : f)
                        .OrderBy(f => f)
                        .Select(x => new object[]
                        {
                            new TestFilePath(x)
                        });
            }
        }

        /// <summary>
        /// Use an internal class to have a better display of the filename in Resharper Unit Tests runner.
        /// </summary>
        public struct TestFilePath
        {
            public TestFilePath(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }

            public override string ToString()
            {
                return FilePath;
            }
        }

        private static string BaseDirectory
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var codebase = new Uri(assembly.CodeBase);
                var path = codebase.LocalPath;
                return Path.GetDirectoryName(path);
            }
        }
    }
}