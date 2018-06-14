using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    public class CurrencyPath
    {
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

        public CurrencyVertex StartVertex { get; }

        public CurrencyVertex EndVertex { get; private set; }

        public Queue<CurrencyEdge> Edges { get; }
        
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

        public CurrencyPath Extend(CurrencyEdge newEdge)
        {
            CurrencyVertex end = newEdge.Other(EndVertex);

            CurrencyPath newPath = new CurrencyPath(this.StartVertex, end, this.Edges);

            newPath.Edges.Enqueue(newEdge);
            return newPath;
        }

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
                        yield return new Step(edge, true);
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
