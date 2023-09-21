/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
 *
*/

using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the FineFundamental class
    /// </summary>
    public partial class FineFundamental : CoarseFundamental
    {
        /// <summary>
        /// The instance of the CompanyReference class
        /// </summary>
        public CompanyReference CompanyReference => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the SecurityReference class
        /// </summary>
        public SecurityReference SecurityReference => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the FinancialStatements class
        /// </summary>
        public FinancialStatements FinancialStatements => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the EarningReports class
        /// </summary>
        public EarningReports EarningReports => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the OperationRatios class
        /// </summary>
        public OperationRatios OperationRatios => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the EarningRatios class
        /// </summary>
        public EarningRatios EarningRatios => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the ValuationRatios class
        /// </summary>
        public ValuationRatios ValuationRatios => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the CompanyProfile class
        /// </summary>
        public CompanyProfile CompanyProfile => new(Time, Symbol.ID);

        /// <summary>
        /// The instance of the AssetClassification class
        /// </summary>
        public AssetClassification AssetClassification => new(Time, Symbol.ID);

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public FineFundamental()
        {
        }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public FineFundamental(DateTime time, Symbol symbol)
        {
            Time = time;
            Symbol = symbol;
        }
    }
}
