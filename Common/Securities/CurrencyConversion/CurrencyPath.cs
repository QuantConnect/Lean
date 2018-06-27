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

namespace QuantConnect.Securities.CurrencyConversion
{
    /// <summary>
    /// A class that represents currency conversion path
    /// </summary>
    public class CurrencyPath
    {
        /// <summary>
        /// This class is Used in foreach loop
        /// </summary>
        public class Step
        {
            public CurrencyEdge Edge;
            public bool Inverted;

            public Step(CurrencyEdge Edge, bool Inverted)
            {
                this.Edge = Edge;
                this.Inverted = Inverted;
            }
        }

        /// <summary>
        /// Start vertex of the path
        /// </summary>
        public CurrencyVertex StartVertex { get; }

        /// <summary>
        /// End vertex of the path
        /// </summary>
        public CurrencyVertex EndVertex { get; private set; }

        /// <summary>
        /// Edges in order
        /// </summary>
        public List<CurrencyEdge> Edges { get; }

        /// <summary>
        /// Number of the edges
        /// </summary>
        public int Length
        {
            get
            {
                return Edges.Count;
            }
        }

        public CurrencyPath(CurrencyVertex startVertex)
        {
            StartVertex = startVertex;
            EndVertex = startVertex;
            Edges = new List<CurrencyEdge>();
        }

        public CurrencyPath(CurrencyVertex startVertex, CurrencyVertex endVertex, IEnumerable<CurrencyEdge> collection)
        {
            StartVertex = startVertex;
            EndVertex = endVertex;
            Edges = new List<CurrencyEdge>(collection);

            var edgesArray = Edges.ToArray();

            // Validation
            if(!edgesArray[0].ContainsOne(startVertex.Code) || !edgesArray[edgesArray.Length-1].ContainsOne(endVertex.Code))
            {
                throw new Exception("The path provided is invalid!");
            }
        }

        /// <summary>
        /// Extends the path with new vertex
        /// </summary>
        /// <param name="newEdge"></param>
        /// <returns></returns>
        public CurrencyPath Extend(CurrencyEdge newEdge)
        {
            var end = newEdge.GetOtherVertex(EndVertex);

            // error checking, path shouldn't have loops
            foreach(var edge in Edges)
            {
                if (edge.ContainsOne(end.Code))
                {
                    throw new ArgumentException($"The path already contains symbol {end.Code}");
                }
            }

            var newPath = new CurrencyPath(this.StartVertex, end, this.Edges);

            newPath.Edges.Add(newEdge);
            return newPath;
        }

        /// <summary>
        /// Used for easier iterating the CurrencyPath, Step class contains Edge and Inverted (boolean).
        /// </summary>
        public IEnumerable<Step> Steps
        {
            get
            {
                var BaseVertex = StartVertex;

                foreach (var edge in Edges)
                {
                    // edge is not inverted
                    if (BaseVertex.Code == edge.Base.Code)
                    {
                        BaseVertex = edge.Quote;
                        yield return new Step(edge, false);
                    }
                    else
                    {
                        if(edge.Bidirectional)
                        {
                            if(BaseVertex == edge.Base)
                            {
                                yield return new Step(edge, false);
                            }
                            else
                            {
                                yield return new Step(edge, true);
                            }
                        }
                        else
                        {
                            yield return new Step(edge, true);
                        }

                        // edge is inverted
                        BaseVertex = edge.Base;
                    }
                }
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"CurrencyPath with length {Length} and path:");
            builder.AppendLine("StartVertex:" + StartVertex.Code);

            foreach (var step in Steps)
            {
                builder.AppendLine($"{step.Edge}, Inverted: {step.Inverted}");
            }

            builder.AppendLine("EndVertex:" + EndVertex.Code);

            return builder.ToString();
        }

    }
}
