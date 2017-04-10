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

using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using System.Collections.Generic;

namespace QuantConnect.Python.Wrappers
{
    /// <summary>
    /// Wrapper for an <see cref = "ITradeBuilder"/> instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    public class TradeBuilderPythonWrapper : ITradeBuilder
    {
        private ITradeBuilder _tradeBuilder;

        /// <summary>
        /// <see cref = "TradeBuilderPythonWrapper"/> constructor.
        /// Wraps the <see cref = "ITradeBuilder"/> object.  
        /// </summary>
        /// <param name="tradeBuilder"><see cref = "ITradeBuilder"/> object to be wrapped</param>
        public TradeBuilderPythonWrapper(ITradeBuilder tradeBuilder)
        {
            _tradeBuilder = tradeBuilder;
        }

        /// <summary>
        /// Wrapper for <see cref = "ITradeBuilder.ClosedTrades" /> in Python
        /// </summary>
        public List<Trade> ClosedTrades
        {
            get
            {
                using (Py.GIL())
                {
                    return _tradeBuilder.ClosedTrades;
                }
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "ITradeBuilder.HasOpenPosition" /> in Python
        /// </summary>
        public bool HasOpenPosition(Symbol symbol)
        {
            using (Py.GIL())
            {
                return _tradeBuilder.HasOpenPosition(symbol);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "ITradeBuilder.ProcessFill" /> in Python
        /// </summary>
        public void ProcessFill(OrderEvent fill, decimal conversionRate, decimal multiplier)
        {
            using (Py.GIL())
            {
                _tradeBuilder.ProcessFill(fill, conversionRate, multiplier);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "ITradeBuilder.SetLiveMode" /> in Python
        /// </summary>
        public void SetLiveMode(bool live)
        {
            using (Py.GIL())
            {
                _tradeBuilder.SetLiveMode(live);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "ITradeBuilder.SetMarketPrice" /> in Python
        /// </summary>
        public void SetMarketPrice(Symbol symbol, decimal price)
        {
            using (Py.GIL())
            {
                _tradeBuilder.SetMarketPrice(symbol, price);
            }
        }
    }
}