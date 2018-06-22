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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.CurrencyConversion.PathProvider
{
    class LinearMax2SearchProvider : ICurrencyPathProvider
    {
        List<CurrencyEdge> _edges;

        Dictionary<string, CurrencyVertex> _vertices;

        public LinearMax2SearchProvider()
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
                {
                    return edge;
                }

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
            foreach (CurrencyEdge edge1 in _edges)
            {
                if (edge1.ContainsOne(startCode))
                {
                    string midCode = edge1.Other(startCode);

                    if (midCode == endCode)
                    {
                        return new CurrencyPath(_vertices[startCode], _vertices[endCode], new List<CurrencyEdge>() { edge1 });
                    }

                    foreach (CurrencyEdge edge2 in _edges)
                    {
                        if(edge2.ContainsOne(midCode))
                        {

                            if(edge2.Other(midCode) == endCode)
                            {
                                return new CurrencyPath(_vertices[startCode], _vertices[endCode], new List<CurrencyEdge>() { edge1, edge2 });
                            }
                        }
                    }
                }
            }

            throw new ArgumentException($"No path found, linear search does not contain sufficient pairs for {startCode+endCode} pair, or path is too long.");
        }

        public ICurrencyPathProvider Copy()
        {
            LinearMax2SearchProvider copy = new LinearMax2SearchProvider();

            foreach(var edge in _edges)
            {
                copy.AddEdge(edge.Base.Code, edge.Quote.Code, edge.Type);
            }

            return copy;
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
