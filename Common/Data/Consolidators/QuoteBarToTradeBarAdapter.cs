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
    /// <see cref="TradeBar"/> with zero volume before being forwarded to the wrapped consolidator.
    /// Used by <see cref="SubscriptionManager.AddConsolidator(Symbol,IDataConsolidator,TickType?)"/>
    /// so trade bar consolidators and indicators work out of the box on quote-only security types.
    /// </summary>
    public class QuoteBarToTradeBarAdapter : IDataConsolidator
    {
        /// <summary>
        /// The wrapped consolidator receiving the collapsed trade bars
        /// </summary>
        public IDataConsolidator Consolidator { get; }

        /// <summary>
        /// Gets the most recently consolidated piece of data produced by the wrapped consolidator
        /// </summary>
        public IBaseData Consolidated => Consolidator.Consolidated;

        /// <summary>
        /// Gets a clone of the data being currently consolidated by the wrapped consolidator
        /// </summary>
        public IBaseData WorkingData => Consolidator.WorkingData;

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(QuoteBar);

        /// <summary>
        /// Gets the type produced by the wrapped consolidator
        /// </summary>
        public Type OutputType => Consolidator.OutputType;

        /// <summary>
        /// Event handler that fires when the wrapped consolidator produces a new piece of data
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated
        {
            add { Consolidator.DataConsolidated += value; }
            remove { Consolidator.DataConsolidated -= value; }
        }

        /// <summary>
        /// Creates a new adapter feeding collapsed quote bars into the given consolidator
        /// </summary>
        /// <param name="consolidator">The consolidator to adapt, must consume <see cref="TradeBar"/> input</param>
        public QuoteBarToTradeBarAdapter(IDataConsolidator consolidator)
        {
            if (!consolidator.InputType.IsAssignableFrom(typeof(TradeBar)))
            {
                throw new ArgumentException($"{nameof(QuoteBarToTradeBarAdapter)} requires a consolidator that accepts {nameof(TradeBar)} input " +
                    $"but was given one with input type {consolidator.InputType.Name}");
            }
            Consolidator = consolidator;
        }

        /// <summary>
        /// Updates the wrapped consolidator with the collapsed version of the specified quote bar
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            Consolidator.Update(((QuoteBar)data).Collapse());
        }

        /// <summary>
        /// Scans the wrapped consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
            Consolidator.Scan(currentLocalTime);
        }

        /// <summary>
        /// Resets the wrapped consolidator
        /// </summary>
        public void Reset()
        {
            Consolidator.Reset();
        }

        /// <summary>
        /// Disposes the wrapped consolidator
        /// </summary>
        public void Dispose()
        {
            Consolidator.Dispose();
        }
    }
}
