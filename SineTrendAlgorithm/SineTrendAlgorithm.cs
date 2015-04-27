using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect;

namespace QuantConnect
{
    public class SineTrendAlgorithm : QCAlgorithm
    {
        string _symbol = "SPY";
        //string _customSymbol = "BTC";


        AlgoIndicators _indicators;

        public override void Initialize()
        {

            //Initialize
            SetStartDate(2013, 10, 07);
            SetEndDate(2014, 10, 11);
            SetCash(25000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            AddSecurity(SecurityType.Equity, _symbol, Resolution.Minute);

            _indicators = new AlgoIndicators
            {
                BB = BB(_symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily),
                RSI = RSI(_symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                ATR = ATR(_symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                EMA = EMA(_symbol, 14, Resolution.Daily),
                SMA = SMA(_symbol, 14, Resolution.Daily),
                MACD = MACD(_symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily),
                AROON = AROON(_symbol, 20, Resolution.Daily),
                MOM = MOM(_symbol, 20, Resolution.Daily),
                MOMP = MOMP(_symbol, 20, Resolution.Daily),
                STD = STD(_symbol, 20, Resolution.Daily),
                MIN = MIN(_symbol, 14, Resolution.Daily), // by default if the symbol is a tradebar type then it will be the min of the low property
                MAX = MAX(_symbol, 14, Resolution.Daily),  // by default if the symbol is a tradebar type then it will be the max of the high property
                WMA = WMA(_symbol, 4, Resolution.Minute)
            };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }

    }
}
