using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;

namespace QuantConnect
{
    public class StressSymbolsAlgorithm : QCAlgorithm
    {
        public IEnumerable<string> AllSymbols;

        /// <inheritdoc />
        public override void Initialize()
        {
            AllSymbols = new List<string>();

            //Backtest period:
            SetStartDate(2014, 01, 01);
            SetEndDate(2015, 01, 01);

            //Set cash to 250k for test algorithm
            SetCash(250000);

            foreach (var symbol in StockSymbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Second, true);
            }

            foreach (var symbol in ForexSymbols)
            {
                AddSecurity(SecurityType.Forex, symbol, Resolution.Second, true);
            }

            AllSymbols = StockSymbols.Concat(ForexSymbols);
        }

        /// <inheritdoc />
        public void OnData(TradeBars data)
        {
            Debug("REALTIME: " + DateTime.Now.ToString("o") + " DATATIME: " + data.Time.ToString("o") + " REALTIME DELTA: " + (DateTime.Now - data.Time).TotalSeconds.ToString("0.000") + "sec  COUNT: " + data.Count + " FILLFORWARD: " + data.Count(x => x.Value.IsFillForward));

            foreach (var symbol in AllSymbols)
            {
                if (!Portfolio.ContainsKey(symbol)) continue;

                if (!Portfolio[symbol].Invested)
                {
                    //Not invested, get invested:
                    Order(symbol, 10);
                }
                else
                {
                    if (Time.Second % 15 == 0)
                    {
                        var holdings = Portfolio[symbol].Quantity;
                        Order(symbol, holdings * -2);
                    }
                }
            }

            //Log timer:
            if (Time.Second % 15 == 0) Log("Time: " + Time.ToShortTimeString());
        }

        public List<string> StockSymbols = new List<string>
        {
            "ABT",
            "ABBV",
            "ACE",
            "ACN",
            "ACT",
            "ADBE",
            "ADT",
            "AES",
            "AET",
            "AFL",
            "AMG",
            "A",
            "GAS",
            "APD",
            "ARG",
            "AKAM",
            "AA",
            "ALXN",
            "ATI",
            "ALLE",
            "AGN",
            "ADS",
            "ALL",
            "ALTR",
            "MO",
            "AMZN",
            "AEE",
            "AEP",
            "AXP",
            "AIG",
            "AMT",
            "AMP",
            "ABC",
            "AME",
            "AMGN",
            "APH",
            "APC",
            "ADI",
            "AON",
            "APA",
            "AIV",
            "AAPL",
            "AMAT",
            "ADM",
            "AIZ",
            "T",
            "ADSK",
            "ADP",
            "AN",
            "AZO",
            "AVGO",
            "AVB",
            "AVY",
            "AVP",
            "BHI",
            "BLL",
            "BAC",
            "BK",
            "BCR",
            "BAX",
            "BBT",
            "BDX",
            "BBBY",
            "BMS",
            "BRK.B",
            "BBY",
            "BIIB",
            "BLK",
            "HRB",
            "BA",
            "BWA",
            "BXP",
            "BSX",
            "BMY",
            "BRCM",
            "BF.B",
            "CHRW",
            "CA",
            "CVC",
            "COG",
            "CAM",
            "CPB",
            "COF",
            "CAH",
            "CFN",
            "KMX",
            "CCL",
            "CAT",
            "CBG",
            "CBS",
            "CELG",
            "CNP",
            "CTL",
            "CERN",
            "CF",
            "SCHW"
        };

        public List<string> ForexSymbols = new List<string>
        {
            "EURUSD",
            "NZDUSD",
            "USDJPY",
            "USDCAD"
        };
    }
}