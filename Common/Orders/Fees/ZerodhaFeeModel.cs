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

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/> Refer to https://www.samco.in/technology/brokerage_calculator
    /// </summary>
    public class ZerodhaFeeModel : IndiaFeeModel
    {
        /// <summary>
        /// Brokerage calculation Factor
        /// </summary>
        protected override decimal BrokerageMultiplier => 0.0003M;

        /// <summary>
        /// Maximum brokerage per order
        /// </summary>
        protected override decimal MaxBrokerage => 20;

        /// <summary>
        /// Securities Transaction Tax calculation Factor
        /// </summary>
        protected override decimal SecuritiesTransactionTaxTotalMultiplier => 0.00025M;

        /// <summary>
        /// Exchange Transaction Charge calculation Factor
        /// </summary>
        protected override decimal ExchangeTransactionChargeMultiplier => 0.0000345M;

        /// <summary>
        /// State Tax calculation Factor
        /// </summary>
        protected override decimal StateTaxMultiplier => 0.18M;

        /// <summary>
        /// Sebi Charges calculation Factor
        /// </summary>
        protected override decimal SebiChargesMultiplier => 0.000001M;

        /// <summary>
        /// Stamp Charges calculation Factor
        /// </summary>
        protected override decimal StampChargesMultiplier => 0.00003M;

        /// <summary>
        /// Checks if Stamp Charges is calculated from order valur or turnover
        /// </summary>
        protected override bool IsStampChargesFromOrderValue => true;
    }
}
