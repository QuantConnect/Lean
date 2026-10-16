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
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Adapts a consolidator with <see cref="TradeBar"/> input so it can be fed from a quote-only
    /// subscription (forex, cfd): each incoming <see cref="QuoteBar"/> is collapsed into a mid-point
    /// <see cref="TradeBar"/> without volume (reading it throws) before being forwarded to the wrapped consolidator.
    /// Used by <see cref="SubscriptionManager.AddConsolidator(Symbol,IDataConsolidator,TickType?)"/>
    /// so trade bar consolidators and indicators work out of the box on quote-only security types.
    /// </summary>
    public class QuoteBarToTradeBarAdapter : ConsolidatorAdapterBase
    {
        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public override Type InputType => typeof(QuoteBar);

        /// <summary>
        /// Creates a new adapter feeding collapsed quote bars into the given consolidator
        /// </summary>
        /// <param name="consolidator">The consolidator to adapt, must consume <see cref="TradeBar"/> input</param>
        public QuoteBarToTradeBarAdapter(IDataConsolidator consolidator)
            : base(consolidator)
        {
            if (!consolidator.InputType.IsAssignableFrom(typeof(TradeBar)))
            {
                throw new ArgumentException($"{nameof(QuoteBarToTradeBarAdapter)} requires a consolidator that accepts {nameof(TradeBar)} input " +
                    $"but was given one with input type {consolidator.InputType.Name}");
            }
        }

        /// <summary>
        /// Updates the wrapped consolidator with the collapsed version of the specified quote bar
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            Consolidator.Update(Collapse((QuoteBar)data));
        }

        /// <summary>
        /// Collapses the quote bar into a mid-point trade bar that has no volume: quote data carries
        /// no trade volume, so reading <see cref="TradeBar.Volume"/> on the result throws
        /// </summary>
        /// <param name="quoteBar">The quote bar to collapse</param>
        /// <returns>The collapsed trade bar</returns>
        public static TradeBar Collapse(QuoteBar quoteBar)
        {
            return new QuoteDerivedTradeBar(quoteBar);
        }
    }
}
