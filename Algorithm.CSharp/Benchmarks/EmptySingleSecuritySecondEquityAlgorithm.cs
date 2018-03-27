using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class EmptySingleSecuritySecondEquityAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2008, 01, 01);
            SetEndDate(2009, 01, 01);
            SetBenchmark(dt => 1m);
            AddEquity("SPY", Resolution.Second);
        }

        public override void OnData(Slice data)
        {
        }
    }
}