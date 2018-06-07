using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    /// <summary>
    /// This class describes path of conversion from asset X to asset Y through DAG, might get deprecated probably
    /// </summary>
    public class ConversionRatePath
    {
        public class UndirectedEdge
        {
            public Cash Cash;
            public List<UndirectedEdge> Paths;
        }

        // Directed Acyclic Graph, Path
        private Dictionary<Cash, List<Cash>> DAG = new Dictionary<Cash, List<Cash>>();

        public ConversionRatePath(Asset startAsset)
        {       
            //Steps = new List<Edge>();
        }

        /// <summary>
        /// Add next step to path
        /// </summary>
        /// <param name="connection"></param>
        /*public void AddStep(Asset nextAsset)
        {
            Edge nextConnection = Y.Edges.Find(c => c.Contains(nextAsset));
            Steps.Add(nextConnection);
            Y = nextAsset;
        }

        public decimal CalculateRate()
        {
            decimal Rate = 1m;

            Asset steppingAsset = X;

            foreach (Edge edge in Steps)
            {
                if (edge.NormalOrientation(steppingAsset))
                {
                    steppingAsset = edge.Quote;
                    Rate *= edge.Rate;
                }
                else
                {
                    steppingAsset = edge.Base;
                    Rate /= edge.Rate;
                }

            }

            return Rate;
        }*/
    }

}
