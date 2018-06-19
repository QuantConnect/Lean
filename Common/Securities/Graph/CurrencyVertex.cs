using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
