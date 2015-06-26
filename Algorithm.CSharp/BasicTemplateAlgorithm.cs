using System;
using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;

namespace QuantConnect
{
    /*
    *   QuantConnect University: Helper Algorithm - Check there is always data:
    *
    */
    public class AlwaysDataAlgorithm : QCAlgorithm
    {
        //Data Required 
        List<string> _symbols = new List<string>() { "SPY", "AAPL", "IBM" };
        List<string> _forexSymbols = new List<string>() { "EURUSD" };
        TradeBars _bars = new TradeBars();

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {
            //Start and End Date range for the backtest:
            SetStartDate(2013, 1, 1);
            SetEndDate(DateTime.Now.Date.AddDays(-1));

            //Cash allocation
            SetCash(25000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Daily);
            }

            foreach (var symbol in _forexSymbols)
            {
                AddSecurity(SecurityType.Forex, symbol, Resolution.Daily);
            }
            _symbols.AddRange(_forexSymbols);
        }

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public void OnData(TradeBars data)
        {
            // UpdateBars(data);
            // if (_bars.Count != _symbols.Count) return;

            if (data.ContainsKey("AAPL"))
                Log("AAPL: " + data["AAPL"].Time); // + " EURUSD: " + data["EURUSD"].Time);

            if (!Portfolio.Invested) Buy("AAPL", 330);
        }

        //Update the global "_bars" object
        private void UpdateBars(TradeBars data)
        {
            foreach (var bar in data.Values)
            {
                if (!_bars.ContainsKey(bar.Symbol))
                {
                    _bars.Add(bar.Symbol, bar);
                }
                _bars[bar.Symbol] = bar;
            }
        }
    }
}