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

using System.Linq;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    public class TastytradeBrokerageOptionStrategyBullCallSpread : QCAlgorithm
    {
        private Symbol _applOption;

        public override void Initialize()
        {
            SetBrokerageModel(Brokerages.BrokerageName.Tastytrade);

            var aapl = AddEquity("AAPL").Symbol;
            var aaplOption = AddOption(aapl);
            aaplOption.SetFilter(o => o.Strikes(-5, 5).Expiration(0, 30));

            _applOption = aaplOption.Symbol;
        }

        private bool _wasPlaced;

        public override void OnData(Slice slice)
        {
            LogData("OnData", slice);

            if (_wasPlaced)
            {
                return;
            }

            if (!slice.OptionChains.TryGetValue(_applOption, out var chain))
            {
                return;
            }

            // Get the nearest expiry date of the contracts
            var expiry = chain.Min(x => x.Expiry);

            // Select the call Option contracts with the nearest expiry and sort by strike price
            var calls = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Call).OrderBy(x => x.Strike).ToArray();

            if (calls.Length < 2)
            {
                return;
            }

            // Buy the bull call spread
            var bullCallSpread = OptionStrategies.BullCallSpread(_applOption, calls[0].Strike, calls[^1].Strike, expiry);
            Buy(bullCallSpread, 1);

            _wasPlaced = true;
        }

        private static void LogData(string name, Slice slice)
        {
            var allData = new StringBuilder("**********" + name + "**********\n");
            for (var i = 0; i < slice.AllData.Count; i++)
            {
                var item = slice.AllData[i];
                switch (item)
                {
                    case TradeBar tradeBar:
                        allData.AppendLine($"#{i} Data Type: {item.DataType} | " + tradeBar.ToString() + $" Time: {tradeBar.Time}, EndTime: {tradeBar.EndTime}");
                        break;
                    case QuoteBar quoteBar:
                        allData.AppendLine($"#{i} Data Type: {item.DataType} | " + quoteBar.ToString() + $" Time: {quoteBar.Time}, EndTime: {quoteBar.EndTime}");
                        break;
                    default:
                        allData.AppendLine($"DEFAULT: #{i}: Data Type: {item.DataType} | Time: {item.Time} | End Time: {item.EndTime} | Symbol: {item.Symbol} | Price: {item.Price} | IsFillForward: {item.IsFillForward}");
                        break;
                }
            }

            Logging.Log.Trace(allData.ToString());
        }
    }
}
