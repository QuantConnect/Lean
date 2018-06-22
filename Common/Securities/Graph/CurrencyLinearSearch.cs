using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    class CurrencyListSearch : IShortestPathSearch
    {
        List<CurrencyEdge> _edges;

        Dictionary<string, CurrencyVertex> _vertices;

        public CurrencyListSearch()
        {
            _edges = new List<CurrencyEdge>();
            _vertices = new Dictionary<string, CurrencyVertex>();
        }

        public CurrencyEdge AddEdge(string currencyPair, SecurityType type)
        {
            string baseCode;
            string quoteCode;

            Util.DecomposeCurrencyPair(currencyPair, out baseCode, out quoteCode, type);

            return AddEdge(baseCode, quoteCode, type);
        }

        public CurrencyEdge AddEdge(string baseCode, string quoteCode, SecurityType type)
        {
            // search existing _edges list (also for inverses), and if anything found, maybe modify and return
            foreach (var edge in _edges)
            {
                if (edge.CompareTo(baseCode, quoteCode) == CurrencyEdge.Match.ExactMatch)
                    return edge;

                if (edge.CompareTo(baseCode, quoteCode) == CurrencyEdge.Match.InverseMatch)
                {
                    edge.MakeBidirectional();
                    return edge;
                }
            }

            // nothing was found, make new edge, add it to list and return
            CurrencyEdge newEdge = new CurrencyEdge(AddVertex(baseCode), AddVertex(quoteCode), type);

            _edges.Add(newEdge);

            return newEdge;
        }

        public CurrencyPath FindShortestPath(string startCode, string endCode)
        {
            throw new NotImplementedException();
        }

        private CurrencyVertex AddVertex(string code)
        {
            // Add new, if it doesn't already exist
            if (!_vertices.ContainsKey(code))
            {
                _vertices[code] = new CurrencyVertex(code);
            }

            return _vertices[code];
        }
    }
}
