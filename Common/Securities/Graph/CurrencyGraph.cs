using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public CurrencyVertex AddVertex(string Code)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            // Add new, if it doesn't already exist
            if (!_vertices.ContainsKey(Code))
                _vertices[Code] = new CurrencyVertex(Code);

            return _vertices[Code];
        }
        
        public CurrencyEdge AddEdge(string CurrencyPair, SecurityType Type)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            string baseCode = null;
            string quoteCode = null;

            Forex.Forex.DecomposeCurrencyPair(CurrencyPair, out baseCode, out quoteCode);

            return AddEdge(baseCode, quoteCode, Type);
        }

        public CurrencyEdge AddEdge(string BaseCode, string QuoteCode, SecurityType Type)
        {
            if (_locked)
                throw new ArgumentException("The graph has been locked, cannot modify the graph anymore!");

            // Search if existing edge already exists
            foreach (CurrencyEdge strEdge in Edges)
            {
                CurrencyEdge.Match match = strEdge.CompareTo(BaseCode, QuoteCode);

                if (match == CurrencyEdge.Match.ExactMatch)
                    return strEdge;

                if (match == CurrencyEdge.Match.InverseMatch)
                {
                    strEdge.MakeBidirectional();
                    return strEdge;
                }
            }
            
            // No existing edge, add new
            var vertexA = AddVertex(BaseCode);
            var vertexB = AddVertex(QuoteCode);

            var edge = new CurrencyEdge(vertexA, vertexB, Type);
            
            vertexA.AddEdge(edge);
            vertexB.AddEdge(edge);

            _edges.Add(edge);

            return edge;
        }

        /// <summary>
        /// Uses Breadth First Search to search through this graph
        /// </summary>
        /// <param name="StartCode">Start of currency code</param>
        /// <param name="EndCode">End of currency code</param>
        /// <returns></returns>
        public CurrencyPath FindShortedPath(string StartCode, string EndCode)
        {
            CurrencyVertex startVertex = _vertices[StartCode];

            HashSet<string> processedNodes = new HashSet<string>();

            Queue<CurrencyPath> pathsToExtend = new Queue<CurrencyPath>();
            
            pathsToExtend.Enqueue(new CurrencyPath(startVertex));

            while (pathsToExtend.Count > 0)
            {
                CurrencyPath nextPath = pathsToExtend.Dequeue();

                foreach (CurrencyEdge edge in nextPath.EndVertex.Edges)
                {

                    // check if node has been NOT visited
                    if (!processedNodes.Contains(nextPath.EndVertex.Code))
                    {
                        CurrencyPath newPath = nextPath.Extend(edge);

                        // if edge contains end, return the path
                        if (edge.ContainsOne(EndCode))
                        {
                            return newPath;
                        }

                        pathsToExtend.Enqueue(newPath);
                    }
                    // ignore the node
                }

                processedNodes.Add(nextPath.EndVertex.Code);
            }

            throw new ArgumentException($"No path found, graph does not contain EndCode: {EndCode}");
        }
        
        /// <summary>
        /// Locks the Graph from editing, makes it read only.
        /// </summary>
        public void LockPermamently()
        {
            _locked = true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Vertices:");
            foreach (var vertex in Vertices.Values)
                builder.AppendLine(vertex.ToString());
            
            builder.AppendLine("");
            builder.AppendLine("Edges:");
            
            foreach(var edge in Edges)
                builder.AppendLine(edge.ToString());
            

            return builder.ToString();
        }
    }
}