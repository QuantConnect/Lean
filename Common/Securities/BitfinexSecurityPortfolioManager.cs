/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
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
