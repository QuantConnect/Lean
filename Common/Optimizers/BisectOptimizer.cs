using System.Collections.Generic;
using QuantConnect.Packets;

namespace QuantConnect.Optimizers
{
    /// <summary>
    ///  Bisect optimizer could bisect and skip large empty parts
    /// </summary>
    public class BisectOptimizer : IOptimizer
    {
        public Dictionary<string, string> Optimize(BacktestResult result)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            //TODO => Logic of optimization

            return parameters;
        }
    }
}
