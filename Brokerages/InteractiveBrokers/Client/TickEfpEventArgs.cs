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

using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.TickEfp"/> event
    /// </summary>
    public sealed class TickEfpEventArgs : EventArgs
    {
        /// <summary>
        /// The request's unique identifier.
        /// </summary>
        public int TickerId { get; private set; }

        /// <summary>
        /// Specifies the type of tick being received.
        /// </summary>
        public int TickType { get; private set; }

        /// <summary>
        /// Annualized basis points, which is representative of the financing rate that can be directly compared to broker rates.
        /// </summary>
        public double BasisPoints { get; private set; }

        /// <summary>
        /// Annualized basis points as a formatted string that depicts them in percentage form.
        /// </summary>
        public string FormattedBasisPoints { get; private set; }

        /// <summary>
        /// Implied futures price.
        /// </summary>
        public double ImpliedFuture { get; private set; }

        /// <summary>
        /// The number of hold days until the expiry of the EFP.
        /// </summary>
        public int HoldDays { get; private set; }

        /// <summary>
        /// The expiration date of the single stock future.
        /// </summary>
        public string FutureExpiry { get; private set; }

        /// <summary>
        /// The dividend impact upon the annualized basis points interest rate.
        /// </summary>
        public double DividendImpact { get; private set; }

        /// <summary>
        /// The dividends expected until the expiration of the single stock future.
        /// </summary>
        public double DividendsToExpiry { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickEfpEventArgs"/> class
        /// </summary>
        public TickEfpEventArgs(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            TickerId = tickerId;
            TickType = tickType;
            BasisPoints = basisPoints;
            FormattedBasisPoints = formattedBasisPoints;
            ImpliedFuture = impliedFuture;
            HoldDays = holdDays;
            FutureExpiry = futureExpiry;
            DividendImpact = dividendImpact;
            DividendsToExpiry = dividendsToExpiry;
        }
    }
}