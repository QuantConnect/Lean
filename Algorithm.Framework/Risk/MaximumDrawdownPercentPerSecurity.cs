using System;
using System.Linq;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that limits the drawdown
    /// per holding to the specified percentage
    /// </summary>
    public class MaximumDrawdownPercentPerSecurity : IRiskManagementModel
    {
        private readonly decimal _maximumDrawdownPercent;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaximumDrawdownPercentPerSecurity"/> class
        /// </summary>
        /// <param name="maximumDrawdownPercent">The maximum percentage drawdown allowed for any single security holding</param>
        public MaximumDrawdownPercentPerSecurity(decimal maximumDrawdownPercent)
        {
            _maximumDrawdownPercent = -Math.Abs(maximumDrawdownPercent);
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public void ManageRisk(QCAlgorithmFramework algorithm)
        {
            foreach (var security in algorithm.Securities.Select(x => x.Value))
            {
                if (!security.Invested)
                {
                    continue;
                }

                var pnl = security.Holdings.UnrealizedProfitPercent;
                if (pnl < _maximumDrawdownPercent)
                {
                    algorithm.Liquidate(security.Symbol);
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
        }
    }
}