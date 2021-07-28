using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that fetches the list of contracts
    /// from the Samco StockNote API
    /// </summary>
    class SamcoLiveOptionChainProvider : IOptionChainProvider
    {
        private readonly SamcoSymbolMapper _symbolMapper;

        /// <summary>
        /// Static constructor for the <see cref="SamcoLiveOptionChainProvider"/> class
        /// </summary>
        public SamcoLiveOptionChainProvider(SamcoSymbolMapper symbolMapper)
        {
            _symbolMapper = symbolMapper;
        }

        /// <summary>
        /// Gets the option chain associated with the underlying Symbol
        /// </summary>
        /// <param name="underlyingSymbol">Underlying symbol to get the option chain for</param>
        /// <param name="date">Unused</param>
        /// <returns>Option chain</returns>
        public IEnumerable<Symbol> GetOptionContractList(Symbol underlyingSymbol, DateTime date)
        {
            var symbols = new List<Symbol>();
            foreach (var scripMaster in _symbolMapper.samcoTradableSymbolList)
            {
                symbols.Add(_symbolMapper.createLeanSymbol(scripMaster));
            }
            return symbols.Where(s => s.SecurityType == SecurityType.Option && s.ID.Symbol == underlyingSymbol.Value);
        }
    }
}
