using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class BasicTemplateAlgorithm : QCAlgorithm
    {
        private SimpleMovingAverage _fast;
        private string _sym = QuantConnect.Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

        public override void Initialize()
        {
            SetStartDate(2017, 10, 1);  //Set Start Date
            SetEndDate(DateTime.Now);
            AddCrypto("BTCUSD", Resolution.Hour, Market.GDAX);
            SetCash(40000);
            _fast = SMA("BTCUSD", 5, Resolution.Hour);
        }

        public override void OnData(Slice data)
        {
            Debug("FASTSMA " + _fast);
        }
    }
}