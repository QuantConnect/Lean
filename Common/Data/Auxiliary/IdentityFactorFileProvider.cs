/*
 * Cascade Labs - Identity Factor File Provider
 * Returns identity factors (1.0) for all symbols, disabling split/dividend adjustments.
 * Use this for control tests where you want to rely on pre-adjusted data from the source.
 */

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Logging;
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Factor file provider that returns identity factors (1.0) for all symbols.
    /// This disables split and dividend adjustments, useful when using pre-adjusted data sources.
    /// </summary>
    public class IdentityFactorFileProvider : IFactorFileProvider
    {
        private static int _wroteTraceStatement;
        private readonly ConcurrentDictionary<Symbol, IFactorProvider> _cache;

        /// <summary>
        /// Creates a new instance of the <see cref="IdentityFactorFileProvider"/>
        /// </summary>
        public IdentityFactorFileProvider()
        {
            _cache = new ConcurrentDictionary<Symbol, IFactorProvider>();
        }

        /// <summary>
        /// Initializes the provider (no-op for identity provider)
        /// </summary>
        public void Initialize(IMapFileProvider mapFileProvider, IDataProvider dataProvider)
        {
            if (Interlocked.CompareExchange(ref _wroteTraceStatement, 1, 0) == 0)
            {
                Log.Trace("IdentityFactorFileProvider: Using identity factors (no split/dividend adjustments)");
            }
        }

        /// <summary>
        /// Gets an identity factor provider for the specified symbol.
        /// Always returns factors of 1.0 (no adjustments).
        /// </summary>
        public IFactorProvider Get(Symbol symbol)
        {
            symbol = symbol.GetFactorFileSymbol();
            return _cache.GetOrAdd(symbol, CreateIdentityFactorProvider);
        }

        private static IFactorProvider CreateIdentityFactorProvider(Symbol symbol)
        {
            // Create a factor file with a single identity row (factor = 1.0)
            var rows = new List<CorporateFactorRow>
            {
                new CorporateFactorRow(new DateTime(1998, 1, 2), 1m, 1m, 0m),
                new CorporateFactorRow(Time.EndOfTime, 1m, 1m, 0m)
            };
            return new CorporateFactorProvider(symbol.Value, rows);
        }
    }
}
