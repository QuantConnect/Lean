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
using QuantConnect.Util;

namespace QuantConnect.Data.Test
{
    /// <summary>
    /// This type is used to simulate custom data moving through the system. It grabs its data the same was TradeBar does
    /// </summary>
    public abstract class FakeTradeBarCustom : TradeBar
    {
        private readonly SecurityType _type;
        private static readonly Random _random = new Random();

        /// <summary>
        /// Constructor for fake tradebar data by type
        /// </summary>
        /// <param name="type">Asset type</param>
        protected FakeTradeBarCustom(SecurityType type)
        {
            _type = type;
        }

        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            if (_random.NextDouble() < 0.01)
            {
                // this is simulating that we don't have data for this time period for fill forward testing
                return null;
            }

            config.Security = _type;
            var tradeBar  = (TradeBar)base.Reader(config, line, date, datafeed);
            return CreateFromTradeBar(tradeBar);
        }

        /// <summary>
        /// Create the tradebar required in inherited classes
        /// </summary>
        protected abstract BaseData CreateFromTradeBar(TradeBar tradeBar);

        /// <summary>
        /// Get Source for Fake Data
        /// </summary>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            // this is really a base security type, but we're pulling data from the equities/forex
            config.Security = _type;
            var file = base.GetSource(config, date, datafeed);
            return file;
        }

        /// <summary>
        /// Cloner for tradebar
        /// </summary>
        public override BaseData Clone()
        {
            return ObjectActivator.Clone(this) as FakeTradeBarCustom;
        }
    }

    /// <summary>
    /// Fake equity data
    /// </summary>
    public class FakeEquityTradeBarCustom : FakeTradeBarCustom
    {
        /// <summary>
        /// Constructor for creating fake equity data
        /// </summary>
        public FakeEquityTradeBarCustom() 
            : base(SecurityType.Equity)
        {
        }

        /// <summary>
        /// Create the tradebar required in inherited classes
        /// </summary>
        protected override BaseData CreateFromTradeBar(TradeBar tradeBar)
        {
            return new FakeEquityTradeBarCustom
            {
                Close = tradeBar.Close,
                DataType = MarketDataType.Base,
                High = tradeBar.High,
                Low = tradeBar.Low,
                Open = tradeBar.Open,
                Symbol = tradeBar.Symbol,
                Time = tradeBar.Time,
                Value = tradeBar.Value,
                Volume = tradeBar.Volume
            };
        }
    }

    /// <summary>
    /// Fake forex data
    /// </summary>
    public class FakeForexTradeBarCustom : FakeTradeBarCustom
    {
        /// <summary>
        /// Forex Type Fake Tradebar Constructor
        /// </summary>
        public FakeForexTradeBarCustom() 
            : base(SecurityType.Forex)
        {
        }

        /// <summary>
        /// Create the tradebar required in inherited classes
        /// </summary>
        protected override BaseData CreateFromTradeBar(TradeBar tradeBar)
        {
            return new FakeForexTradeBarCustom
            {
                Close = tradeBar.Close,
                DataType = MarketDataType.Base,
                High = tradeBar.High,
                Low = tradeBar.Low,
                Open = tradeBar.Open,
                Symbol = tradeBar.Symbol,
                Time = tradeBar.Time,
                Value = tradeBar.Value,
                Volume = tradeBar.Volume
            };
        }
    }
}
