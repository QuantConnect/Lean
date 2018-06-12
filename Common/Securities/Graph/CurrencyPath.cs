using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    public class CurrencyPath
    {
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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"CurrencyPath with length {Length} and path:");

            builder.AppendLine("StartVertex:" + StartVertex.Code);
            
            foreach (var edge in Edges)
                builder.AppendLine(edge.ToString());
            
            builder.AppendLine("EndVertex:" + EndVertex.Code);

            return builder.ToString();
        }
    }
}
