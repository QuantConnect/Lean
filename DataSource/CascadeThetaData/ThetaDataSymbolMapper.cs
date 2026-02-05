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

using QuantConnect.Brokerages;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Enums;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Index Option Tickers: https://http-docs.thetadata.us/docs/theta-data-rest-api-v2/s1ezbyfni6rw0-index-option-tickers
    /// </summary>
    public class ThetaDataSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// docs: https://http-docs.thetadata.us/docs/theta-data-rest-api-v2/1872cab32381d-the-si-ps#options-opra
        /// </summary>
        private const string MARKET = Market.USA;

        /// <summary>
        /// Represents a set of supported security types.
        /// </summary>
        /// <remarks>
        /// This HashSet contains the supported security types that are allowed within the system.
        /// </remarks>
        public readonly HashSet<SecurityType> SupportedSecurityType = new() { SecurityType.Equity, SecurityType.Index, SecurityType.Option, SecurityType.IndexOption };

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol.
        /// </summary>
        /// <param name="symbol">The Lean symbol instance to be converted.</param>
        /// <returns>
        /// For Equity or Index symbols, returns the actual symbol's ticker.
        /// For Option or IndexOption symbols, returns a formatted string: "Ticker,yyyyMMdd,strikePrice,optionRight".
        /// Example: For symbol AAPL expiry DateTime(2024, 03, 28), strikePrice = 100m, OptionRight.Call => "AAPL,20240328,100000,C".
        /// </returns>
        /// <exception cref="NotImplementedException">Thrown when the specified securityType is not supported.</exception>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.Equity:
                case SecurityType.Index:
                    return GetDataProviderTicker(ContractSecurityType.Equity, symbol.Value);
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    return GetDataProviderTicker(
                        ContractSecurityType.Option,
                        symbol.ID.Symbol,
                        symbol.ID.Date.ConvertToThetaDataDateFormat(),
                        ConvertStrikePriceToThetaDataFormat(symbol.ID.StrikePrice),
                        symbol.ID.OptionRight == OptionRight.Call ? "C" : "P");
                default:
                    throw new NotSupportedException($"{nameof(ThetaDataSymbolMapper)}.{nameof(GetBrokerageSymbol)}: The security type '{symbol.SecurityType}' is not supported by {nameof(CascadeThetaDataProvider)}.");
            }
        }

        /// <summary>
        /// Constructs a Lean symbol based on the provided parameters.
        /// </summary>
        /// <param name="root">The root symbol for the Lean symbol.</param>
        /// <param name="contractSecurityType">The type of contract security (e.g., Option, Equity).</param>
        /// <param name="dataProviderDate">The date string formatted according to the data provider's requirements.</param>
        /// <param name="strike">The strike price for options contracts.</param>
        /// <param name="right">The option right for options contracts (Call or Put).</param>
        /// <returns>
        /// A Lean symbol constructed using the provided parameters.
        /// </returns>
        public Symbol GetLeanSymbol(string root, ContractSecurityType contractSecurityType, string dataProviderDate, decimal strike, string right)
        {
            switch (contractSecurityType)
            {
                case ContractSecurityType.Option:
                    return GetLeanSymbol(root, SecurityType.Option, MARKET, dataProviderDate.ConvertFromThetaDataDateFormat(), strike, ConvertContractOptionRightFromThetaDataFormat(right));
                case ContractSecurityType.Equity:
                    return GetLeanSymbol(root, SecurityType.Equity, MARKET);
                case ContractSecurityType.Index:
                    return GetLeanSymbol(root, SecurityType.Index, MARKET);
                default:
                    throw new NotImplementedException($"{nameof(ThetaDataSymbolMapper)}.{nameof(GetLeanSymbol)}: The contract security type '{contractSecurityType}' is not implemented.");
            }
        }

        /// <summary>
        /// Constructs a Lean symbol based on the provided parameters.
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol representing the security.</param>
        /// <param name="securityType">The type of security (e.g., Equity, Option).</param>
        /// <param name="market">The market/exchange where the security is traded.</param>
        /// <param name="expirationDate">The expiration date for options contracts. Default is DateTime.MinValue.</param>
        /// <param name="strike">The strike price for options contracts. Default is 0.</param>
        /// <param name="optionRight">The option right for options contracts (Call or Put). Default is Call.</param>
        /// <returns>
        /// A Lean symbol constructed using the provided parameters.
        /// </returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default, decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            return GetLeanSymbol(brokerageSymbol, securityType, market, OptionStyle.American, expirationDate, strike, optionRight);
        }

        /// <summary>
        /// Constructs a Lean symbol based on the provided parameters.
        /// </summary>
        /// <param name="dataProviderTicker">The ticker symbol formatted according to the data provider's requirements.</param>
        /// <param name="securityType">The type of security (e.g., Equity, Option).</param>
        /// <param name="market">The market/exchange where the security is traded.</param>
        /// <param name="optionStyle">The option style for options contracts (e.g., American, European).</param>
        /// <param name="expirationDate">The expiration date for options contracts. Default is DateTime.MinValue.</param>
        /// <param name="strike">The strike price for options contracts. Default is 0.</param>
        /// <param name="optionRight">The option right for options contracts (Call or Put). Default is Call.</param>
        /// <param name="underlying">The underlying symbol for options contracts. Default is null.</param>
        /// <returns>
        /// A Lean symbol constructed using the provided parameters.
        /// </returns>
        public Symbol GetLeanSymbol(string dataProviderTicker, SecurityType securityType, string market, OptionStyle optionStyle,
            DateTime expirationDate = new DateTime(), decimal strike = 0, OptionRight optionRight = OptionRight.Call,
            Symbol? underlying = null)
        {
            if (string.IsNullOrWhiteSpace(dataProviderTicker))
            {
                throw new ArgumentException("Invalid symbol: " + dataProviderTicker);
            }

            var underlyingSymbolStr = underlying?.Value ?? dataProviderTicker;
            var leanSymbol = default(Symbol);

            if (strike != 0m)
            {
                strike = ConvertStrikePriceFromThetaDataFormat(strike);
            }

            switch (securityType)
            {
                case SecurityType.Option:
                    leanSymbol = Symbol.CreateOption(underlyingSymbolStr, market, optionStyle, optionRight, strike, expirationDate);
                    break;

                case SecurityType.IndexOption:
                    underlying ??= Symbol.Create(underlyingSymbolStr, SecurityType.Index, market);
                    leanSymbol = Symbol.CreateOption(underlying, dataProviderTicker, market, optionStyle, optionRight, strike, expirationDate);
                    break;

                case SecurityType.Equity:
                    leanSymbol = Symbol.Create(dataProviderTicker, securityType, market);
                    break;

                case SecurityType.Index:
                    leanSymbol = Symbol.Create(dataProviderTicker, securityType, market);
                    break;

                default:
                    throw new Exception($"{nameof(ThetaDataSymbolMapper)}.{nameof(GetLeanSymbol)}: unsupported security type: {securityType}");
            }

            return leanSymbol;
        }

        /// <summary>
        /// Gets the ticker for the data provider based on the contract security type.
        /// </summary>
        /// <param name="contractSecurityType">The type of contract security (e.g., Option, Equity).</param>
        /// <param name="ticker">The ticker symbol.</param>
        /// <param name="expirationDate">The expiration date for options contracts. Default is null.</param>
        /// <param name="strikePrice">The strike price for options contracts. Default is null.</param>
        /// <param name="optionRight">The option right for options contracts. Default is null.</param>
        /// <returns>
        /// The ticker string formatted according to the data provider's requirements.
        /// For options contracts, the format is "Ticker,ExpirationDate,StrikePrice,OptionRight".
        /// For equity contracts, the ticker is returned directly.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the provided contractSecurityType is not supported.
        /// </exception>
        private string GetDataProviderTicker(ContractSecurityType contractSecurityType, string ticker, string? expirationDate = null, string? strikePrice = null, string? optionRight = null)
        {
            switch (contractSecurityType)
            {
                case ContractSecurityType.Option:
                    return $"{ticker},{expirationDate},{strikePrice},{optionRight}";
                case ContractSecurityType.Index:
                case ContractSecurityType.Equity:
                    return ticker;
                default:
                    throw new NotSupportedException();
            }

        }

        /// <summary>
        /// Converts an option right from ThetaData format to the corresponding Lean format.
        /// </summary>
        /// <param name="contractOptionRight">The option right in ThetaData format ("C" for Call, "P" for Put).</param>
        /// <returns>
        /// The corresponding Lean OptionRight enum value.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the provided contractOptionRight is not a recognized ThetaData option right ("C" for Call or "P" for Put).
        /// </exception>
        private OptionRight ConvertContractOptionRightFromThetaDataFormat(string contractOptionRight) => contractOptionRight switch
        {
            "C" => OptionRight.Call,
            "P" => OptionRight.Put,
            _ => throw new ArgumentException($"{nameof(ThetaDataSymbolMapper)}.{nameof(ConvertContractOptionRightFromThetaDataFormat)}:The provided contractOptionRight is not a recognized ThetaData option right. Expected values are 'C' for Call or 'P' for Put.")
        };

        /// <summary>
        /// Converts an option strike price to ThetaData format, where strike prices are formatted in 10ths of a cent.
        /// </summary>
        /// <param name="value">The option strike price.</param>
        /// <returns>
        /// The strike price in ThetaData format.
        /// For example, if the input strike price is 100.00m, the returned value would be "100_000".
        /// </returns>
        private string ConvertStrikePriceToThetaDataFormat(decimal value) => Math.Truncate(value * 1000m).ToStringInvariant();

        /// <summary>
        /// Converts an option strike price from ThetaData format to Lean format, where strike prices are formatted in 10ths of a cent.
        /// </summary>
        /// <param name="value">The option strike price in ThetaData format.</param>
        /// <returns>
        /// The strike price in Lean format.
        /// For example, if the input strike price is "100000", the returned value would be 100m.
        /// </returns>
        private decimal ConvertStrikePriceFromThetaDataFormat(decimal value) => value / 1000m;
    }
}
