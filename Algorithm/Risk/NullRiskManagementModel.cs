using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QCAlgorithmFramework = QuantConnect.Algorithm.QCAlgorithm;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that does nothing
    /// </summary>
    public class NullRiskManagementModel : RiskManagementModel
    {
        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
        {
            return Enumerable.Empty<IPortfolioTarget>();
        }
    }
}