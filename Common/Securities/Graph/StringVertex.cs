using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Graph
{
    public class CurrencyVertex
    {
        public string Code;

        public List<CurrencyEdge> Edges;

        public CurrencyVertex(string Code)
        {
            this.Code = Code;
            Edges = new List<CurrencyEdge>();
        }
    }

}
