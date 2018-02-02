using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Engine
{
    public static class PerformanceBenchmarkAlgorithms
    {
        public static QCAlgorithm SingleSecurity_Second => new SingleSecurity_Second_BenchmarkTest();

        private class SingleSecurity_Second_BenchmarkTest : QCAlgorithm
        {
            public override void Initialize()
            {
                SetStartDate(2008, 01, 01);
                SetEndDate(2009, 01, 01);
                SetCash(100000);
                SetBenchmark(time => 0m);
                AddEquity("SPY", Resolution.Second, "usa", true);
            }
        }
    }
}