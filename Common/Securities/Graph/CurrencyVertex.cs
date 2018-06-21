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

namespace QuantConnect.Securities.Graph
{
    /// <summary>
    /// Represents one currency and all it's connections to other currencies
    /// </summary>
    public class CurrencyVertex
    {
        public string Code { get; private set; }
        public IReadOnlyList<CurrencyEdge> Edges { get { return _edges; } }

        private List<CurrencyEdge> _edges;

        private bool _locked = false;

        public CurrencyVertex(string Code)
        {
            this.Code = Code;
            _edges = new List<CurrencyEdge>();
        }

        /// <summary>
        /// Add new currency pair to this vertex
        /// </summary>
        /// <param name="edge">Currency pair</param>
        public void AddEdge(CurrencyEdge edge)
        {
            if (_locked)
                throw new ArgumentException("The vertex has been locked, cannot modify the vertex anymore!");

            _edges.Add(edge);
        }

        /// <summary>
        /// Make the vertex read only
        /// </summary>
        public void Lock()
        {
            _locked = true;
        }

        public override string ToString()
        {
            return $"Vertex: {Code}, Edges count: {Edges.Count}";
        }

    }

}
