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

        public Stack<CurrencyEdge> Edges { get; }
        
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
            Edges = new Stack<CurrencyEdge>();
        }

        public CurrencyPath(CurrencyVertex startVertex, CurrencyVertex endVertex, IEnumerable<CurrencyEdge> collection)
        {
            StartVertex = startVertex;
            EndVertex = endVertex;
            Edges = new Stack<CurrencyEdge>(collection);
        }

        public CurrencyPath Extend(CurrencyEdge newEdge)
        {
            CurrencyVertex end = newEdge.Other(EndVertex);

            CurrencyPath newPath = new CurrencyPath(this.StartVertex, end, this.Edges);

            newPath.Edges.Push(newEdge);
            return newPath;
        }
    }
}
