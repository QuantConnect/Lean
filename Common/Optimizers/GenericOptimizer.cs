using System.Collections.Generic;
using QuantConnect.Packets;

namespace QuantConnect.Optimizers
{
    /// <summary>
    ///  A GenericOptimizer would remember breed its parameter sets as to get the best 
    /// results and generate new ones.
    /// </summary>
    public class GenericOptimizer : IOptimizer
    {
        public Dictionary<string, string> Optimize(BacktestResult result)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            //TODO => Logic of optimization

            return parameters;
        }
    }
}
