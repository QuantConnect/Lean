using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    public class CurrencyEdge
    {
        [Flags]
        public enum Match
        {
            NoMatch = 0,
            ExactMatch = 1,
            InverseMatch = 2
        }

        public CurrencyVertex Base;
        public CurrencyVertex Quote;

        public string PairSymbol
        {
            get
            {
                return Base.Code + Quote.Code;
            }
        }

        public CurrencyEdge(CurrencyVertex Base, CurrencyVertex Quote) 
        {
            this.Base = Base;
            this.Quote = Quote;
        }
        
        public Match CompareTo(string BaseCode, string QuoteCode)
        {
            if (this.Base.Code == BaseCode && this.Quote.Code == QuoteCode)
                return Match.ExactMatch;

            if (this.Base.Code == QuoteCode && this.Quote.Code == BaseCode)
                return Match.InverseMatch;

            return Match.NoMatch;
        }

        public Match CompareTo(CurrencyEdge edge)
        {
            if (this.Base == edge.Base && this.Quote == edge.Quote)
                return Match.ExactMatch;

            if (this.Base == edge.Base && this.Quote == edge.Base)
                return Match.InverseMatch;

            return Match.NoMatch;
        }

        public bool ContainsOne(string Code)
        {
            return Base.Code == Code || Quote.Code == Code;
        }
        
        public CurrencyVertex Other(CurrencyVertex thisVertex)
        {
            if (this.Base == thisVertex)
                return this.Quote;

            if (this.Quote == thisVertex)
                return this.Base;

            throw new ArgumentException($"The vertex: {thisVertex.Code} is not present in edge {PairSymbol}!");
        }
        
    }
}
