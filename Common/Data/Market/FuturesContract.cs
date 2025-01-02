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

using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a single futures contract at a specific expiration
    /// </summary>
    public class FuturesContract : BaseContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesContract"/> class
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        public FuturesContract(Symbol symbol)
            : base(symbol)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesContract"/> class
        /// </summary>
        /// <param name="contractData">The contract universe data</param>
        public FuturesContract(FutureUniverse contractData)
            : base(contractData.Symbol)
        {
            LastPrice = contractData.Close;
            AskPrice = contractData.Close;
            BidPrice = contractData.Close;
            Volume = (long)contractData.Volume;
            OpenInterest = contractData.OpenInterest;
        }

        /// <summary>
        /// Implicit conversion into <see cref="Symbol"/>
        /// </summary>
        /// <param name="contract">The option contract to be converted</param>
        public static implicit operator Symbol(FuturesContract contract)
        {
            return contract.Symbol;
        }

        /// <summary>
        /// Updates the future contract with the new data, which can be a <see cref="Tick"/> or <see cref="TradeBar"/> or <see cref="QuoteBar"/>
        /// </summary>
        internal override void Update(BaseData data)
        {
            switch (data)
            {
                case TradeBar tradeBar:
                    if (tradeBar.Close != 0m)
                    {
                        LastPrice = tradeBar.Close;
                        Volume = (long)tradeBar.Volume;
                    }
                   break;

                case QuoteBar quoteBar:
                    if (quoteBar.Ask != null && quoteBar.Ask.Close != 0m)
                    {
                        AskPrice = quoteBar.Ask.Close;
                        AskSize = (long)quoteBar.LastAskSize;
                    }
                    if (quoteBar.Bid != null && quoteBar.Bid.Close != 0m)
                    {
                        BidPrice = quoteBar.Bid.Close;
                        BidSize = (long)quoteBar.LastBidSize;
                    }
                    break;

                case Tick tick:
                    if (tick.TickType == TickType.Trade)
                    {
                        LastPrice = tick.Price;
                    }
                    else if (tick.TickType == TickType.Quote)
                    {
                        if (tick.AskPrice != 0m)
                        {
                            AskPrice = tick.AskPrice;
                            AskSize = (long)tick.AskSize;
                        }
                        if (tick.BidPrice != 0m)
                        {
                            BidPrice = tick.BidPrice;
                            BidSize = (long)tick.BidSize;
                        }
                    }
                    else if (tick.TickType == TickType.OpenInterest)
                    {
                        if (tick.Value != 0m)
                        {
                            OpenInterest = tick.Value;
                        }
                    }
                    break;
            }
        }
    }
}
