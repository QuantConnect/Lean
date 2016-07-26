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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect
{
    /// <summary>
    /// Based on a macroeconomic indicator(CAPE Ratio), we are looking for entry/exit points for momentum stocks
    /// CAPE data: January 1990 - December 2014. By Tim Co.
    /// Goals:    
    ///     Capitalize in overvalued markets by generating returns with momentum and selling before the crash
    ///     Capitalize in undervalued markets by purchasing stocks at bottom of trough
    /// </summary>
    public class BubbleAlgorithm : QCAlgorithm
    {
        decimal currCape;
        decimal[] c = new decimal[4];
        decimal[] cCopy = new decimal[4];
        bool newLow = false;
        int counter = 0;
        int counter2 = 0;
        MovingAverageConvergenceDivergence macd;
        RelativeStrengthIndex rsi = new RelativeStrengthIndex(14);
        ArrayList symbols = new ArrayList();

        Dictionary<string, RelativeStrengthIndex> rsiDic = new Dictionary<string, RelativeStrengthIndex>();
        Dictionary<string, MovingAverageConvergenceDivergence> macdDic = new Dictionary<string, MovingAverageConvergenceDivergence>();

        /// <summary>
        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            SetCash(100000);
            symbols.Add("SPY");
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

            foreach (string stock in symbols)
            {
                AddSecurity(SecurityType.Equity, stock, Resolution.Minute);

                macd = MACD(stock, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
                macdDic.Add(stock, macd);
                rsi = RSI(stock, 14, MovingAverageType.Exponential, Resolution.Daily);
                rsiDic.Add(stock, rsi);

                Securities[stock].SetLeverage(10);
            }
        }

        /// <summary>
        /// Trying to find if current Cape is the lowest Cape in three months to indicate selling period
        /// </summary>
        public void OnData(CAPE data)
        {
            newLow = false;
            //Adds first four Cape Ratios to array c
            currCape = data.Cape;
            if (counter < 4)
            {
                c[counter++] = currCape;
            }
            //Replaces oldest Cape with current Cape
            //Checks to see if current Cape is lowest in the previous quarter
            //Indicating a sell off 
            else
            {
                Array.Copy(c, cCopy, 4);
                Array.Sort(cCopy);
                if (cCopy[0] > currCape) newLow = true;
                c[counter2++] = currCape;
                if (counter2 == 4) counter2 = 0;
            }

            Debug("Current Cape: " + currCape + " on " + data.Time);
            if (newLow) Debug("New Low has been hit on " + data.Time);
        }

        /// <summary>
        /// New TradeBar data for our assets.
        /// </summary>
        public void OnData(TradeBars data)
        {
            try
            {
                //Bubble territory 
                if (currCape > 20 && newLow == false)
                {
                    foreach (string stock in symbols)
                    {
                        //Order stock based on MACD
                        //During market hours, stock is trading, and sufficient cash
                        if (Securities[stock].Holdings.Quantity == 0 && rsiDic[stock] < 70
                            && Securities[stock].Price != 0 && Portfolio.Cash > Securities[stock].Price * 100
                            && Time.Hour == 9 && Time.Minute == 30)
                        {
                            Buy(stock);
                        }
                        //Utilize RSI for overbought territories and liquidate that stock
                        if (rsiDic[stock] > 70 && Securities[stock].Holdings.Quantity > 0
                                && Time.Hour == 9 && Time.Minute == 30)
                        {
                            Sell(stock);
                        }
                    }
                }

                // Undervalued territory
                else if (newLow == true)
                {
                    foreach (string stock in symbols)
                    {

                        //Sell stock based on MACD 
                        if (Securities[stock].Holdings.Quantity > 0 && rsiDic[stock] > 30
                            && Time.Hour == 9 && Time.Minute == 30)
                        {
                            Sell(stock);
                        }
                        //Utilize RSI and MACD to understand oversold territories
                        else if (Securities[stock].Holdings.Quantity == 0 && rsiDic[stock] < 30
                            && Securities[stock].Price != 0 && Portfolio.Cash > Securities[stock].Price * 100
                            && Time.Hour == 9 && Time.Minute == 30)
                        {
                            Buy(stock);
                        }
                    }

                }
                // Cape Ratio is missing from orignial data
                // Most recent cape data is most likely to be missing
                else if (currCape == 0)
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
            SecurityHolding s = Securities[symbol].Holdings;
            if (macdDic[symbol] > 0m)
            {
                SetHoldings(symbol, 1);

                Debug("Purchasing: " + symbol + "   MACD: " + macdDic[symbol] + "   RSI: " + rsiDic[symbol]
                    + "   Price: " + Math.Round(Securities[symbol].Price, 2) + "   Quantity: " + s.Quantity);
            }
        }

        /// <summary>
        /// Sell this symbol
        /// </summary>
        /// <param name="symbol"></param>
        public void Sell(String symbol)
        {
            SecurityHolding s = Securities[symbol].Holdings;
            if (s.Quantity > 0 && macdDic[symbol] < 0m)
            {
                Liquidate(symbol);

                Debug("Selling: " + symbol + " at sell MACD: " + macdDic[symbol] + "   RSI: " + rsiDic[symbol]
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
        public decimal Cape = 0;
        string format = "yyyy-MM";
        CultureInfo provider = CultureInfo.InvariantCulture;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.CAPE"/> indicator.
        /// </summary>
        public CAPE()
        {
            this.Symbol = "CAPE";
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
                string[] data = line.Split(',');
                //Dates must be in the format YYYY-MM-DD. If your data source does not have this format, you must use
                //DateTime.ParseExact() and explicit declare the format your data source has.
                string dateString = data[0];
                index.Time = DateTime.ParseExact(dateString, format, provider);
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