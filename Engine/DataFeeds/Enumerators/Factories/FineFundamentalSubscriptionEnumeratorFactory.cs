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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> that reads
    /// an entire <see cref="SubscriptionDataSource"/> into a single <see cref="FineFundamental"/>
    /// to be emitted on the tradable date at midnight
    /// </summary>
    public class FineFundamentalSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FineFundamentalSubscriptionEnumeratorFactory"/> class.
        /// </summary>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to the enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public FineFundamentalSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null)
        {
            _tradableDaysProvider = tradableDaysProvider ?? (request => request.TradableDays);
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            var tradableDays = _tradableDaysProvider(request);
            
            var financialStatements = new FinancialStatements();
            var earningReports = new EarningReports();
            var operationRatios = new OperationRatios();
            var earningRatios = new EarningRatios();
            var valuationRatios = new ValuationRatios();

            var financialStatementsConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(FinancialStatements), request.Security.Symbol);
            var earningReportsConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(EarningReports), request.Security.Symbol);
            var operationRatiosConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(OperationRatios), request.Security.Symbol);
            var earningRatiosConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(EarningRatios), request.Security.Symbol);
            var valuationRatiosConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(ValuationRatios), request.Security.Symbol);

            return (
                from date in tradableDays

                let financialStatementsSource = financialStatements.GetSource(financialStatementsConfiguration, date, false)
                let earningReportsSource = earningReports.GetSource(earningReportsConfiguration, date, false)
                let operationRatiosSource = operationRatios.GetSource(operationRatiosConfiguration, date, false)
                let earningRatiosSource = earningRatios.GetSource(earningRatiosConfiguration, date, false)
                let valuationRatiosSource = valuationRatios.GetSource(valuationRatiosConfiguration, date, false)

                let financialStatementsFactory = SubscriptionDataSourceReader.ForSource(financialStatementsSource, financialStatementsConfiguration, date, false)
                let earningReportsFactory = SubscriptionDataSourceReader.ForSource(earningReportsSource, earningReportsConfiguration, date, false)
                let operationRatiosFactory = SubscriptionDataSourceReader.ForSource(operationRatiosSource, operationRatiosConfiguration, date, false)
                let earningRatiosFactory = SubscriptionDataSourceReader.ForSource(earningRatiosSource, earningRatiosConfiguration, date, false)
                let valuationRatiosFactory = SubscriptionDataSourceReader.ForSource(valuationRatiosSource, valuationRatiosConfiguration, date, false)

                let financialStatementsForDate = (FinancialStatements)financialStatementsFactory.Read(financialStatementsSource).FirstOrDefault()
                let earningReportsForDate = (EarningReports)earningReportsFactory.Read(earningReportsSource).FirstOrDefault()
                let operationRatiosForDate = (OperationRatios)operationRatiosFactory.Read(operationRatiosSource).FirstOrDefault()
                let earningRatiosForDate = (EarningRatios)earningRatiosFactory.Read(earningRatiosSource).FirstOrDefault()
                let valuationRatiosForDate = (ValuationRatios)valuationRatiosFactory.Read(valuationRatiosSource).FirstOrDefault()

                select new FineFundamental
                {
                    DataType = MarketDataType.Auxiliary,
                    Symbol = request.Configuration.Symbol,
                    Time = date,
                    EndTime = date,
                    FinancialStatements = UpdateFinancialStatements(financialStatementsForDate, ref financialStatements),
                    EarningReports = UpdateEarningReports(earningReportsForDate, ref earningReports),
                    OperationRatios = UpdateOperationRatios(operationRatiosForDate, ref operationRatios),
                    EarningRatios = UpdateEarningRatios(earningRatiosForDate, ref earningRatios),
                    ValuationRatios = UpdateValuationRatios(valuationRatiosForDate, ref valuationRatios)
                }
                ).GetEnumerator();
        }

        private static FinancialStatements UpdateFinancialStatements(FinancialStatements financialStatements, ref FinancialStatements previousFinancialStatements)
        {
            if (financialStatements == null) return previousFinancialStatements;

            financialStatements.UpdateValues(previousFinancialStatements);
            previousFinancialStatements = financialStatements;

            return financialStatements;
        }

        private static EarningReports UpdateEarningReports(EarningReports earningReports, ref EarningReports previousEarningReports)
        {
            if (earningReports == null) return previousEarningReports;

            earningReports.UpdateValues(previousEarningReports);
            previousEarningReports = earningReports;

            return earningReports;
        }

        private static OperationRatios UpdateOperationRatios(OperationRatios operationRatios, ref OperationRatios previousOperationRatios)
        {
            if (operationRatios == null) return previousOperationRatios;

            operationRatios.UpdateValues(previousOperationRatios);
            previousOperationRatios = operationRatios;

            return operationRatios;
        }

        private static EarningRatios UpdateEarningRatios(EarningRatios earningRatios, ref EarningRatios previousEarningRatios)
        {
            if (earningRatios == null) return previousEarningRatios;

            earningRatios.UpdateValues(previousEarningRatios);
            previousEarningRatios = earningRatios;

            return earningRatios;
        }

        private static ValuationRatios UpdateValuationRatios(ValuationRatios valuationRatios, ref ValuationRatios previousvaValuationRatios)
        {
            if (valuationRatios == null) return previousvaValuationRatios;

            valuationRatios.UpdateValues(previousvaValuationRatios);
            previousvaValuationRatios = valuationRatios;

            return valuationRatios;
        }

    }
}