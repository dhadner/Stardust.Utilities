using BenchmarkDotNet.Running;
using Stardust.Utilities.Benchmarks;

namespace BenchmarkSuite1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(ShiftOperatorBenchmarks).Assembly).Run(args);
        }
    }
}
