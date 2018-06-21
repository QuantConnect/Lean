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

namespace QuantConnect.Securities.Graph
{

    /// <summary>
    /// Represents a currency pair in CurrencyGraph. Even though edge is always bidirectional in graph terms, it can also be bidirectional in pair terms - both normal and inverse pairs can exist.
    /// </summary>
    public class CurrencyEdge
    {
        [Flags]
        public enum Match
        {
            NoMatch = 0,
            ExactMatch = 1,
            InverseMatch = 2
        }

        public CurrencyVertex Base { get; private set; }
        public CurrencyVertex Quote { get; private set; }

        public SecurityType Type { get; private set; }

        public bool Bidirectional { get; private set; }

        public string PairSymbol
        {
            get
            {
                return Base.Code + Quote.Code;
            }
        }

        public CurrencyEdge(CurrencyVertex Base, CurrencyVertex Quote, SecurityType Type)
        {
            this.Base = Base;
            this.Quote = Quote;
            this.Bidirectional = false;
            this.Type = Type;
        }

        public Match CompareTo(string BaseCode, string QuoteCode)
        {
            if (this.Base.Code == BaseCode && this.Quote.Code == QuoteCode)
            {
                return Match.ExactMatch;
            }

            if (this.Base.Code == QuoteCode && this.Quote.Code == BaseCode)
            {
                return Match.InverseMatch;
            }

            return Match.NoMatch;
        }

        public Match CompareTo(CurrencyEdge edge)
        {
            if (this.Base == edge.Base && this.Quote == edge.Quote)
            {
                return Match.ExactMatch;
            }

            if (this.Base == edge.Base && this.Quote == edge.Base)
            {
                return Bidirectional ? Match.ExactMatch : Match.InverseMatch;
            }

            return Match.NoMatch;
        }

        public bool ContainsOne(string Code)
        {
            return Base.Code == Code || Quote.Code == Code;
        }

        public CurrencyVertex Other(CurrencyVertex thisVertex)
        {
            if (this.Base == thisVertex)
            {
                return this.Quote;
            }

            if (this.Quote == thisVertex)
            {
                return this.Base;
            }

            throw new ArgumentException($"The vertex: {thisVertex.Code} is not present in edge {PairSymbol}!");
        }

        public void MakeBidirectional()
        {
            this.Bidirectional = true;
        }

        public override string ToString()
        {
            if (Bidirectional)
            {
                return $"Edge: {Base.Code}{Quote.Code} && {Quote.Code}{Base.Code}";
            }

            return $"Edge: {Base.Code}{Quote.Code}";
        }
    }
}
