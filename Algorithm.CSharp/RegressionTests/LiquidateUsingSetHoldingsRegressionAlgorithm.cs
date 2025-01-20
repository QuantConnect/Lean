using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    public class LiquidateUsingSetHoldingsRegressionAlgorithm : LiquidateRegressionAlgorithm
    {
        public override void PerformLiquidation()
        {
            SetHoldings(new List<PortfolioTarget>(), true);
        }
    }
}
