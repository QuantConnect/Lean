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

using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates how to submit orders to a Financial Advisor account group, allocation profile or a single managed account.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="financial advisor" />
    public class FinancialAdvisorDemoAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _symbol = AddEquity("SPY").Symbol;

            // The default order properties can be set here to choose the FA settings
            // to be automatically used in any order submission method (such as SetHoldings, Buy, Sell and Order)

            // Use a default FA Account Group with an Allocation Method
            DefaultOrderProperties = new InteractiveBrokersOrderProperties
            {
                // account group created manually in IB/TWS
                FaGroup = "TestGroupEQ",
                // supported allocation methods are: EqualQuantity, NetLiq, AvailableEquity, PctChange
                FaMethod = "EqualQuantity"
            };

            // set a default FA Allocation Profile
            //DefaultOrderProperties = new InteractiveBrokersOrderProperties
            //{
            //    // allocation profile created manually in IB/TWS
            //    FaProfile = "TestProfileP"
            //};

            // send all orders to a single managed account
            //DefaultOrderProperties = new InteractiveBrokersOrderProperties
            //{
            //    // a sub-account linked to the Financial Advisor master account
            //    Account = "DU123456"
            //};
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                // when logged into IB as a Financial Advisor, this call will use order properties
                // set in the DefaultOrderProperties property of QCAlgorithm
                SetHoldings(_symbol, 1);
            }
        }
    }
}
