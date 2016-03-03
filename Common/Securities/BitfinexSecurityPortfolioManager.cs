using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities
{
    public class BitfinexSecurityPortfolioManager : SecurityPortfolioManager
    {

        public BitfinexSecurityPortfolioManager(SecurityManager securityManager, SecurityTransactionManager transactions)
            : base(securityManager, transactions)
        { }

        public override List<SubmitOrderRequest> ScanForMarginCall(out bool issueMarginCallWarning)
        {
            issueMarginCallWarning = false;

            var totalMarginUsed = TotalMarginUsed;

            // don't issue a margin call if we're not using margin
            if (totalMarginUsed <= 0)
            {
                return new List<SubmitOrderRequest>();
            }

            // don't issue a margin call if we're under 1x implied leverage on the whole portfolio's holdings
            var averageHoldingsLeverage = TotalAbsoluteHoldingsCost / totalMarginUsed;
            if (averageHoldingsLeverage <= 1.0m)
            {
                return new List<SubmitOrderRequest>();
            }

            //Will liquidate if position to equity ratio > 15%. We begin to issue warnings when Ticker to Position price ratio > 1.13            
            foreach (var security in Securities.Values.Where(x => x.Holdings.Quantity != 0 && x.Price != 0))
            {
                decimal ratio = security.Holdings.Price / security.Holdings.AveragePrice;
                if ((security.Holdings.IsShort && ratio > 1.13m) || (security.Holdings.IsLong && ratio < 0.87m))
                {
                    issueMarginCallWarning = true;
                }
            }


            return new List<SubmitOrderRequest>();
        }

    }
}
