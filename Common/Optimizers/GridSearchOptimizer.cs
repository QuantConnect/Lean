using System.Collections.Generic;
using QuantConnect.Packets;

namespace QuantConnect.Optimizers
{
    /// <summary>
    /// "GridSearchOptimization" would be called once with no results at the start; 
    /// then it would return linear steps across each of the parameter variations
    ///  to make up all possible combinations to backtest.
    ///  A GenericOptimizer would remember breed its parameter sets as to get the best 
    /// results and generate new ones. Bisect optimizer could bisect and skip large empty parts
    /// </summary>
    public class GridSearchOptimizer : IOptimizer
    {
        /// <summary>
        /// GridSearchOptimizer would take in the last set of results; and return a new parameter set to backtest over.
        /// </summary>
        /// <param name="result"> Last set of results
        /// <returns> New parameter set to backtest over.</returns>
        public Dictionary<string, string> Optimize(BacktestResult result)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            //TODO => Logic of optimization

            return parameters;
        }
    }
}
