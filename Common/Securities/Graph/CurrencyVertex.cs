using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    public class CurrencyVertex
    {
        public string Code { get; private set; }
        public IReadOnlyList<CurrencyEdge> Edges { get { return _edges; } }

        private List<CurrencyEdge> _edges;

        public CurrencyVertex(string Code)
        {
            this.Code = Code;
            _edges = new List<CurrencyEdge>();
        }

        public void AddEdge(CurrencyEdge edge)
        {
            _edges.Add(edge);
        }

        public override string ToString()
        {
            return $"Vertex: {Code}, Edges count: {Edges.Count}";
        }
    }

}
