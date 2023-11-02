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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Per symbol we will have a fundamental class provider so the instances can be reused
    /// </summary>
    public class FundamentalInstanceProvider
    {
        private static readonly Dictionary<SecurityIdentifier, FundamentalInstanceProvider> _cache = new();

        private readonly FundamentalTimeProvider _timeProvider;
        private readonly FinancialStatements _financialStatements;
        private readonly OperationRatios _operationRatios;
        private readonly SecurityReference _securityReference;
        private readonly CompanyReference _companyReference;
        private readonly CompanyProfile _companyProfile;
        private readonly AssetClassification _assetClassification;
        private readonly ValuationRatios _valuationRatios;
        private readonly EarningRatios _earningRatios;
        private readonly EarningReports _earningReports;

        /// <summary>
        /// Get's the fundamental instance provider for the requested symbol
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        /// <returns>The unique instance provider</returns>
        public static FundamentalInstanceProvider Get(Symbol symbol)
        {
            FundamentalInstanceProvider result = null;
            lock (_cache)
            {
                _cache.TryGetValue(symbol.ID, out result);
            }

            if (result == null)
            {
                // we create the fundamental instance provider without holding the cache lock, this is because it uses the pygil
                // Deadlock case: if the main thread has PyGil and wants to take lock on cache (security.Fundamentals use case) and the data
                // stack thread takes the lock on the cache (creating new fundamentals) and next wants the pygil deadlock!
                result = new FundamentalInstanceProvider(symbol);
                lock (_cache)
                {
                    _cache[symbol.ID] = result;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates a new fundamental instance provider
        /// </summary>
        /// <param name="symbol">The target symbol</param>
        private FundamentalInstanceProvider(Symbol symbol)
        {
            _timeProvider = new();
            _financialStatements = new(_timeProvider, symbol.ID);
            _operationRatios = new(_timeProvider, symbol.ID);
            _securityReference = new(_timeProvider, symbol.ID);
            _companyReference = new(_timeProvider, symbol.ID);
            _companyProfile = new(_timeProvider, symbol.ID);
            _assetClassification = new(_timeProvider, symbol.ID);
            _valuationRatios = new(_timeProvider, symbol.ID);
            _earningRatios = new(_timeProvider, symbol.ID);
            _earningReports = new(_timeProvider, symbol.ID);
        }

        /// <summary>
        /// Returns the ValuationRatios instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValuationRatios GetValuationRatios(DateTime time)
        {
            _timeProvider.Time = time;
            return _valuationRatios;
        }

        /// <summary>
        /// Returns the EarningRatios instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EarningRatios GetEarningRatios(DateTime time)
        {
            _timeProvider.Time = time;
            return _earningRatios;
        }

        /// <summary>
        /// Returns the EarningReports instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EarningReports GetEarningReports(DateTime time)
        {
            _timeProvider.Time = time;
            return _earningReports;
        }

        /// <summary>
        /// Returns the OperationRatios instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OperationRatios GetOperationRatios(DateTime time)
        {
            _timeProvider.Time = time;
            return _operationRatios;
        }

        /// <summary>
        /// Returns the FinancialStatements instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FinancialStatements GetFinancialStatements(DateTime time)
        {
            _timeProvider.Time = time;
            return _financialStatements;
        }

        /// <summary>
        /// Returns the SecurityReference instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SecurityReference GetSecurityReference(DateTime time)
        {
            _timeProvider.Time = time;
            return _securityReference;
        }

        /// <summary>
        /// Returns the CompanyReference instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompanyReference GetCompanyReference(DateTime time)
        {
            _timeProvider.Time = time;
            return _companyReference;
        }

        /// <summary>
        /// Returns the CompanyProfile instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompanyProfile GetCompanyProfile(DateTime time)
        {
            _timeProvider.Time = time;
            return _companyProfile;
        }

        /// <summary>
        /// Returns the AssetClassification instance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetClassification GetAssetClassification(DateTime time)
        {
            _timeProvider.Time = time;
            return _assetClassification;
        }

        private class FundamentalTimeProvider : ITimeProvider
        {
            public DateTime Time;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DateTime GetUtcNow() => Time;
        }
    }
}
