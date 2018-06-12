using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Graph
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

        public CurrencyGraph()
        {
            _vertices = new Dictionary<string, CurrencyVertex>();
            _edges = new List<CurrencyEdge>();
        }

        public CurrencyVertex AddVertex(string Code)
        {
            // Add new, if it doesn't already exist
            if (!_vertices.ContainsKey(Code))
                _vertices[Code] = new CurrencyVertex(Code);

            return _vertices[Code];
        }
        
        public CurrencyEdge AddEdge(string BaseCode, string QuoteCode)
        {
            // Search if existing edge already exists
            foreach (CurrencyEdge strEdge in Edges)
            {
                if(strEdge.CompareTo(BaseCode, QuoteCode) == CurrencyEdge.Match.ExactMatch)
                    return strEdge;
            }

            // No existing edge, add new
            var vertexA = AddVertex(BaseCode);
            var vertexB = AddVertex(QuoteCode);

            var edge = new CurrencyEdge(vertexA, vertexB);
            
            vertexA.Edges.Add(edge);
            vertexB.Edges.Add(edge);

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
                
                foreach(CurrencyEdge edge in nextPath.Edges)
                {
                    // if edge contains end, return the path
                    if(edge.ContainsOne(EndCode))
                        return nextPath;
                    
                    // check if node has been visited
                    if(!processedNodes.Contains(nextPath.EndVertex.Code))
                    {
                        CurrencyPath newPath = nextPath.Extend(edge);

                        pathsToExtend.Enqueue(newPath);
                    }
                }
            }

            throw new ArgumentException($"No path found, graph does not contain EndCode: {EndCode}");
        }
    }
}