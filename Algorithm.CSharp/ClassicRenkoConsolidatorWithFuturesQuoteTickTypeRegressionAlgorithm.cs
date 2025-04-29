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
 *
*/

using System;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests the Classic Renko Consolidator with future quote tick data.
    /// It checks if valid quote data (non-zero Bid/Ask prices) is received during the algorithm's execution.
    /// If consolidation does not happen, a RegressionTestException is thrown
    /// </summary>
    public class ClassicRenkoConsolidatorWithFuturesQuoteTickTypeRegressionAlgorithm : ClassicRenkoConsolidatorWithFuturesTickTypesRegressionAlgorithm
    {
        private bool _hasNonZeroBidPrice;
        private bool _hasNonZeroAskPrice;
        protected override TickType TickType => TickType.Quote;
        protected override ClassicRenkoConsolidator GetConsolidator()
        {
            Func<IBaseData, decimal> selector = data =>
            {
                var tick = data as Tick;
                _hasNonZeroBidPrice |= tick.BidPrice != 0;
                _hasNonZeroAskPrice |= tick.AskPrice != 0;
                if (tick.TickType != TickType)
                {
                    throw new RegressionTestException("The tick type should be quote");
                }
                WasSelectorExecuted = true;
                return tick.AskPrice * 10;
            };

            var consolidator = new ClassicRenkoConsolidator(BucketSize, selector);
            return consolidator;
        }

        public override void AddConsolidator(ClassicRenkoConsolidator consolidator)
        {
            SubscriptionManager.AddConsolidator(GoldFuture.Mapped, consolidator, TickType);
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_hasNonZeroBidPrice || !_hasNonZeroAskPrice)
            {
                throw new RegressionTestException("No valid Quote tick data found: fields (Bid/Ask) were zero.");
            }
            base.OnEndOfAlgorithm();
        }
    }
}