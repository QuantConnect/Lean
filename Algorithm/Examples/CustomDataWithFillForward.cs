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
using System.Collections.Generic;
using QuantConnect.Data.Test;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm.Examples
{
    public class CustomDataWithFillForward : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2014, 05, 01);
            SetEndDate(2014, 05, 30);

            // create 'custom' data that just looks in the normal place for data

            AddData<FakeForexTradeBarCustom>("EURUSD", Resolution.Minute, true);
            Securities["EURUSD"].Exchange = new ForexExchange();

            AddData<FakeForexTradeBarCustom>("NZDUSD", Resolution.Minute, true);
            Securities["NZDUSD"].Exchange = new ForexExchange();

            AddData<FakeEquityTradeBarCustom>("MSFT", Resolution.Minute, true);
            Securities["MSFT"].Exchange = new EquityExchange();

            AddData<FakeEquityTradeBarCustom>("SPY", Resolution.Minute, true);
            Securities["SPY"].Exchange = new EquityExchange();
        }

        private int lastHour = 0;
        private DateTime? last = null; 
        private readonly List<FakeTradeBarCustom> _data = new List<FakeTradeBarCustom>();
        public void OnData(FakeTradeBarCustom custom)
        {
            Console.WriteLine(custom.Time.ToString("o") + " FF " + (custom.IsFillForward ? "1" : "0") + " " + custom.Symbol);
            _data.Add(custom);
        }


        public static readonly List<string> StockSymbols = new List<string>
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
