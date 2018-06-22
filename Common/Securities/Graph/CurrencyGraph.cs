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
using System.Text;

namespace QuantConnect.Securities.Graph
{
    /// <summary>
    /// Currency Graph, holds currency codes such as "USD" and pairs such as"USDEUR"
    /// </summary>
    public class CurrencyGraph
    {
        public IReadOnlyDictionary<string, CurrencyVertex> Vertices => _vertices;
        public IReadOnlyList<CurrencyEdge> Edges => _edges;

        private Dictionary<string, CurrencyVertex> _vertices;
        private List<CurrencyEdge> _edges;

        private bool _locked = false;

        public CurrencyGraph()
        {
            _vertices = new Dictionary<string, CurrencyVertex>();
            _edges = new List<CurrencyEdge>();
        }

        public CurrencyVertex AddVertex(string code)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            // Add new, if it doesn't already exist
            if (!_vertices.ContainsKey(code))
            {
                _vertices[code] = new CurrencyVertex(code);
            }

            return _vertices[code];
        }

        public CurrencyEdge AddEdge(string currencyPair, SecurityType type)
        {
            if (_locked)
            {
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");
            }

            string baseCode = null;
            string quoteCode = null;

            Util.DecomposeCurrencyPair(currencyPair, out baseCode, out quoteCode);

            return AddEdge(baseCode, quoteCode, type);
        }

        public CurrencyEdge AddEdge(string baseCode, string quoteCode, SecurityType type)
        {
            if (_locked)
            {
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");
            }

            // Search if existing edge already exists
            foreach (CurrencyEdge strEdge in Edges)
            {
                CurrencyEdge.Match match = strEdge.CompareTo(baseCode, quoteCode);

                if (match == CurrencyEdge.Match.ExactMatch)
                {
                    return strEdge;
                }

                if (match == CurrencyEdge.Match.InverseMatch)
                {
                    strEdge.MakeBidirectional();
                    return strEdge;
                }
            }

            // No existing edge, add new
            var vertexA = AddVertex(baseCode);
            var vertexB = AddVertex(quoteCode);

            var edge = new CurrencyEdge(vertexA, vertexB, type);

            vertexA.AddEdge(edge);
            vertexB.AddEdge(edge);

            _edges.Add(edge);

            return edge;
        }

        /// <summary>
        /// Uses BFS to search through this graph
        /// </summary>
        /// <param name="startCode">Start of currency code</param>
        /// <param name="endCode">End of currency code</param>
        /// <returns></returns>
        public CurrencyPath FindShortestPath(string startCode, string endCode)
        {
            CurrencyVertex startVertex = null;

            _vertices.TryGetValue(startCode, out startVertex);


            if (startVertex == null)
            {
                throw new ArgumentException($"No path found, graph does not contain StartCode: {startCode}");
            }

            HashSet<string> processedNodes = new HashSet<string>();
            Queue<CurrencyPath> pathsToExtend = new Queue<CurrencyPath>();

            pathsToExtend.Enqueue(new CurrencyPath(startVertex));

            while (pathsToExtend.Count > 0)
            {
                CurrencyPath path = pathsToExtend.Dequeue();

                if (path.EndVertex.Code == endCode)
                {
                    return path;
                }

                processedNodes.Add(path.EndVertex.Code);

                // grow paths
                foreach (CurrencyEdge edge in path.EndVertex.Edges)
                {
                    //find other endpoint of edge
                    CurrencyVertex vertex = edge.Other(path.EndVertex);

                    // check if this endpoint has been NOT visited already then if, add the extended path to queue
                    if (!processedNodes.Contains(vertex.Code))
                        pathsToExtend.Enqueue(path.Extend(edge));
                }
            }

            throw new ArgumentException($"No path found, graph does not contain EndCode: {endCode}, or no possible path");
        }

        /// <summary>
        /// Locks the Graph from editing, makes it read only.
        /// </summary>
        public void Lock()
        {
            // also make vertices read-only
            foreach (var vertex in _vertices.Values)
            {
                vertex.Lock();
            }

            _locked = true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Vertices:");
            foreach (var vertex in Vertices.Values)
            {
                builder.AppendLine(vertex.ToString());
            }

            builder.AppendLine("");
            builder.AppendLine("Edges:");

            foreach (var edge in Edges)
            {
                builder.AppendLine(edge.ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Make a complete copy of the graph. The copy will be also unlocked, so it can be modified freely.
        /// </summary>
        /// <returns>Copy of this instance</returns>
        public CurrencyGraph Copy()
        {
            CurrencyGraph copy = new CurrencyGraph();

            foreach (var code in _vertices.Keys)
            {
                copy.AddVertex(code);
            }

            foreach (var edge in _edges)
            {
                copy.AddEdge(edge.PairSymbol, edge.Type);
            }

            return copy;
        }
    }
}