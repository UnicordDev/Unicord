using BenchmarkDotNet.Running;

namespace Jsonite.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchGenericDeserialize>();
        }
    }
}
