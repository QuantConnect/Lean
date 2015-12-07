using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class EmptyMinute400EquityAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2015, 09, 28);
            SetEndDate(2015, 11, 13);
            foreach (var symbol in Symbols.Equity.All.Take(400))
            {
                AddSecurity(SecurityType.Equity, symbol);
            }
        }

        public override void OnData(Slice slice)
        {
        }
    }
}