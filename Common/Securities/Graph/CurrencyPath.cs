using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
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
        public Queue<CurrencyEdge> Edges { get; }
        
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
            Edges = new Queue<CurrencyEdge>();
        }

        public CurrencyPath(CurrencyVertex startVertex, CurrencyVertex endVertex, IEnumerable<CurrencyEdge> collection)
        {
            StartVertex = startVertex;
            EndVertex = endVertex;
            Edges = new Queue<CurrencyEdge>(collection);
        }

        /// <summary>
        /// Extends the path with new vertex
        /// </summary>
        /// <param name="newEdge"></param>
        /// <returns></returns>
        public CurrencyPath Extend(CurrencyEdge newEdge)
        {
            CurrencyVertex end = newEdge.Other(EndVertex);
            
            // error checking, path shouldn't have loops
            foreach(var edge in Edges)
            {
                if (edge.ContainsOne(end.Code))
                    throw new ArgumentException($"The path already contains symbol {end.Code}");
            }

            CurrencyPath newPath = new CurrencyPath(this.StartVertex, end, this.Edges);

            newPath.Edges.Enqueue(newEdge);
            return newPath;
        }

        /// <summary>
        /// Used for easier iterating the CurrencyPath, Step class contains Edge and Inverted (boolean).
        /// </summary>
        public IEnumerable<Step> Steps 
        {
            get
            {
                CurrencyVertex BaseVertex = StartVertex;

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
                        // edge is inverted
                        BaseVertex = edge.Base;
                        yield return new Step(edge, edge.Bidirectional? false : true);
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"CurrencyPath with length {Length} and path:");
            builder.AppendLine("StartVertex:" + StartVertex.Code);

            foreach (var step in Steps)
                builder.AppendLine($"{step.Edge}, Inverted: {step.Inverted}");

            builder.AppendLine("EndVertex:" + EndVertex.Code);

            return builder.ToString();
        }

    }
}
