/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Strategy example algorithm using CAPE - a bubble indicator dataset saved in dropbox. CAPE is based on a macroeconomic indicator(CAPE Ratio),
    /// we are looking for entry/exit points for momentum stocks CAPE data: January 1990 - December 2014
    /// Goals:
    /// Capitalize in overvalued markets by generating returns with momentum and selling before the crash
    /// Capitalize in undervalued markets by purchasing stocks at bottom of trough
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="custom data" />
    public class BubbleAlgorithm : QCAlgorithm
    {
        private decimal _currCape;
        private readonly decimal[] _c = new decimal[4];
        private readonly decimal[] _cCopy = new decimal[4];
        private bool _newLow;
        private int _counter;
        private int _counter2;
        private MovingAverageConvergenceDivergence _macd;
        private RelativeStrengthIndex _rsi = new RelativeStrengthIndex(14);
        private readonly ArrayList _symbols = new ArrayList();
        private readonly Dictionary<string, RelativeStrengthIndex> _rsiDic = new Dictionary<string, RelativeStrengthIndex>();
        private readonly Dictionary<string, MovingAverageConvergenceDivergence> _macdDic = new Dictionary<string, MovingAverageConvergenceDivergence>();

        /// <summary>
        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            SetCash(100000);
            _symbols.Add("SPY");
            SetStartDate(1998, 1, 1);
            SetEndDate(2014, 6, 1);

            //Present Social Media Stocks:
            // symbols.Add("FB");symbols.Add("LNKD");symbols.Add("GRPN");symbols.Add("TWTR");
            // SetStartDate(2011, 1, 1);
            // SetEndDate(2014, 12, 1);

            //2008 Financials:
            // symbols.Add("C");symbols.Add("AIG");symbols.Add("BAC");symbols.Add("HBOS");
            // SetStartDate(2003, 1, 1);
            // SetEndDate(2011, 1, 1);

            //2000 Dot.com:
            // symbols.Add("IPET");symbols.Add("WBVN");symbols.Add("GCTY");
            // SetStartDate(1998, 1, 1);
            // SetEndDate(2000, 1, 1);

            //CAPE data
            AddData<CAPE>("CAPE");

            foreach (string stock in _symbols)
            {
                AddSecurity(SecurityType.Equity, stock, Resolution.Minute);

                _macd = MACD(stock, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
                _macdDic.Add(stock, _macd);
                _rsi = RSI(stock, 14, MovingAverageType.Exponential, Resolution.Daily);
                _rsiDic.Add(stock, _rsi);

                Securities[stock].SetLeverage(10);
            }
        }

        /// <summary>
        /// Trying to find if current Cape is the lowest Cape in three months to indicate selling period
        /// </summary>
        public void OnData(CAPE data)
        {
            _newLow = false;
            //Adds first four Cape Ratios to array c
            _currCape = data.Cape;
            if (_counter < 4)
            {
                _c[_counter++] = _currCape;
            }
            //Replaces oldest Cape with current Cape
            //Checks to see if current Cape is lowest in the previous quarter
            //Indicating a sell off
            else
            {
                Array.Copy(_c, _cCopy, 4);
                Array.Sort(_cCopy);
                if (_cCopy[0] > _currCape) _newLow = true;
                _c[_counter2++] = _currCape;
                if (_counter2 == 4) _counter2 = 0;
            }

            Debug("Current Cape: " + _currCape + " on " + data.Time);
            if (_newLow) Debug("New Low has been hit on " + data.Time);
        }

        /// <summary>
        /// New TradeBar data for our assets.
        /// </summary>
        public void OnData(TradeBars data)
        {
            try
            {
                //Bubble territory
                if (_currCape > 20 && _newLow == false)
                {
                    foreach (string stock in _symbols)
                    {
                        //Order stock based on MACD
                        //During market hours, stock is trading, and sufficient cash
                        if (Securities[stock].Holdings.Quantity == 0 && _rsiDic[stock] < 70
                            && Securities[stock].Price != 0 && Portfolio.Cash > Securities[stock].Price * 100
                            && Time.Hour == 9 && Time.Minute == 31)
                        {
                            Buy(stock);
                        }
                        //Utilize RSI for overbought territories and liquidate that stock
                        if (_rsiDic[stock] > 70 && Securities[stock].Holdings.Quantity > 0
                                && Time.Hour == 9 && Time.Minute == 31)
                        {
                            Sell(stock);
                        }
                    }
                }

                // Undervalued territory
                else if (_newLow)
                {
                    foreach (string stock in _symbols)
                    {

                        //Sell stock based on MACD
                        if (Securities[stock].Holdings.Quantity > 0 && _rsiDic[stock] > 30
                            && Time.Hour == 9 && Time.Minute == 31)
                        {
                            Sell(stock);
                        }
                        //Utilize RSI and MACD to understand oversold territories
                        else if (Securities[stock].Holdings.Quantity == 0 && _rsiDic[stock] < 30
                            && Securities[stock].Price != 0 && Portfolio.Cash > Securities[stock].Price * 100
                            && Time.Hour == 9 && Time.Minute == 31)
                        {
                            Buy(stock);
                        }
                    }

                }
                // Cape Ratio is missing from orignial data
                // Most recent cape data is most likely to be missing
                else if (_currCape == 0)
                {
                    Debug("Exiting due to no CAPE!");
                    Quit("CAPE ratio not supplied in data, exiting.");
                }
            }
            catch (Exception err)
            {
                Error(err.Message);
            }
        }


        /// <summary>
        /// Buy this symbol
        /// </summary>
        public void Buy(string symbol)
        {
            var s = Securities[symbol].Holdings;
            if (_macdDic[symbol] > 0m)
            {
                SetHoldings(symbol, 1);

                Debug("Purchasing: " + symbol + "   MACD: " + _macdDic[symbol] + "   RSI: " + _rsiDic[symbol]
                    + "   Price: " + Math.Round(Securities[symbol].Price, 2) + "   Quantity: " + s.Quantity);
            }
        }

        /// <summary>
        /// Sell this symbol
        /// </summary>
        /// <param name="symbol"></param>
        public void Sell(string symbol)
        {
            var s = Securities[symbol].Holdings;
            if (s.Quantity > 0 && _macdDic[symbol] < 0m)
            {
                Liquidate(symbol);

                Debug("Selling: " + symbol + " at sell MACD: " + _macdDic[symbol] + "   RSI: " + _rsiDic[symbol]
                    + "   Price: " + Math.Round(Securities[symbol].Price, 2) + "   Profit from sale: " + s.LastTradeProfit);
            }
        }
    }

    /// <summary>
    /// CAPE Ratio for SP500 PE Ratio for avg inflation adjusted earnings for previous ten years
    /// Custom Data from DropBox
    /// Original Data from: http://www.econ.yale.edu/~shiller/data.htm
    /// </summary>
    public class CAPE : BaseData
    {
        public decimal Cape;
        private const string Format = "yyyy-MM";
        private readonly CultureInfo _provider = CultureInfo.InvariantCulture;

        /// <summary>
        /// Initializes a new instance of the <see cref="CAPE"/> indicator.
        /// </summary>
        public CAPE()
        {
            Symbol = "CAPE";
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // Remember to add the "?dl=1" for dropbox links
            return new SubscriptionDataSource("https://www.dropbox.com/s/ggt6blmib54q36e/CAPE.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }

        /// <summary>
        /// Reader Method :: using set of arguements we specify read out type. Enumerate
        /// until the end of the data stream or file. E.g. Read CSV file line by line and convert
        /// into data types.
        /// </summary>
        /// <returns>BaseData type set by Subscription Method.</returns>
        /// <param name="config">Config.</param>
        /// <param name="line">Line.</param>
        /// <param name="date">Date.</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var index = new CAPE();

            try
            {
                //Example File Format:
                //Date   |  Price |  Div  | Earning | CPI  | FractionalDate | Interest Rate | RealPrice | RealDiv | RealEarnings | CAPE
                //2014.06  1947.09  37.38   103.12   238.343    2014.37          2.6           1923.95     36.94        101.89     25.55
                var data = line.Split(',');
                //Dates must be in the format YYYY-MM-DD. If your data source does not have this format, you must use
                //DateTime.ParseExact() and explicit declare the format your data source has.
                var dateString = data[0];
                index.Time = DateTime.ParseExact(dateString, Format, _provider);
                index.Cape = Convert.ToDecimal(data[10], CultureInfo.InvariantCulture);
                index.Symbol = "CAPE";
                index.Value = index.Cape;
            }
            catch
            {

            }
            return index;
        }
    }
}