using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Conversion
{
    /// <summary>
    /// This class describes path of conversion from asset X to asset Y
    /// </summary>
    public class Path
    {
        public Asset X { get; private set; }
        public Asset Y { get; private set; }

        public List<Connection> Steps { get; private set; }

        public Path(Asset startAsset)
        {
            X = startAsset;
            Y = startAsset;

            Steps = new List<Connection>();
        }

        /// <summary>
        /// Add next step to path
        /// </summary>
        /// <param name="connection"></param>
        public void AddStep(Asset nextAsset)
        {
            Connection nextConnection = Y.Connections.Find(c => c.Contains(nextAsset));
            Steps.Add(nextConnection);
            Y = nextAsset;
        }

        public decimal CalculateRate()
        {
            decimal Rate = 1m;

            Asset steppingAsset = X;

            foreach (Connection connection in Steps)
            {
                if (!connection.Invert(steppingAsset))
                {
                    steppingAsset = connection.Quote;
                    Rate *= connection.Rate;
                }
                else
                {
                    steppingAsset = connection.Base;
                    Rate /= connection.Rate;
                }

            }

            return Rate;
        }
    }

}
