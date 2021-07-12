using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration;
using QuantConnect.Brokerages.Samco.SamcoMessages;
using QuantConnect.Util;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Samco symbols.
    /// </summary>
    public class SamcoSymbolMapper : ISymbolMapper
    {
        public List<ScripMaster> samcoTradableSymbolList = new List<ScripMaster>();

        private void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(filePath, fileName);
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
            }
        }


        public SamcoSymbolMapper()
        {

            StreamReader streamReader;
            var csvFile = "SamcoInstruments-" + DateTime.Now.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture).Replace(" ", "-").Replace("/", "-") + ".csv";
            var path = Path.Combine(Globals.DataFolder, csvFile);

            if (File.Exists(path))
            {
                streamReader = new StreamReader(path);
            }
            else
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://developers.stocknote.com/doc/ScripMaster.csv");
                req.KeepAlive = false;
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                streamReader = new StreamReader(resp.GetResponseStream());
                SaveStreamAsFile(Globals.DataFolder, resp.GetResponseStream(), csvFile);
            }
            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            var csv = new CsvReader(streamReader, configuration);
            var scrips = csv.GetRecords<ScripMaster>();
            samcoTradableSymbolList = scrips.ToList();

        }

        public Symbol getSymbolfromList(ScripMaster scrip)
        {

            char[] sep = { '-' };

            var securityType = SecurityType.Equity;
            var market = Market.NSE;
            OptionRight optionRight = 0;
            switch (scrip.Instrument)
            {
                //Equities
                case "EQ":
                    securityType = SecurityType.Equity;
                    break;
                //Index Options
                case "OPTIDX":
                    securityType = SecurityType.Option;
                    break;
                //Stock Futures
                case "FUTSTK":
                    securityType = SecurityType.Future;
                    break;
                //Stock options
                case "OPTSTK":
                    securityType = SecurityType.Option;
                    break;
                //Commodity Futures
                case "FUTCOM":
                    securityType = SecurityType.Future;
                    break;
                //Commodity Options
                case "OPTCOM":
                    securityType = SecurityType.Option;
                    break;

                //Bullion Options
                case "OPTBLN":

                    securityType = SecurityType.Option;
                    break;

                //Energy Futures
                case "FUTENR":

                    securityType = SecurityType.Future;
                    break;

                //Currenty Options
                case "OPTCUR":

                    securityType = SecurityType.Option;
                    break;

                //Currency Futures
                case "FUTCUR":

                    securityType = SecurityType.Option;
                    break;

                //Bond Futures
                case "FUTIRC":

                    securityType = SecurityType.Future;
                    break;

                //Bond Futures
                case "FUTIRT":

                    securityType = SecurityType.Future;
                    break;

                //Bond Option
                case "OPTIRC":
                    securityType = SecurityType.Option;
                    break;
                default:
                    securityType = SecurityType.Base;
                    break;
            }


            switch (scrip.Exchange)
            {
                case "NSE":
                    market = Market.NSE;
                    break;
                case "NFO":
                    market = Market.NFO;
                    break;
                case "CDS":
                    market = Market.CDS;
                    break;
                case "BSE":
                    market = Market.BSE;
                    break;
                case "MFO":
                    market = Market.MCX;
                    break;
                default:
                    market = Market.NSE;
                    break;
            }

            string[] ticker = scrip.TradingSymbol.Split(sep);

            Symbol symbol = null;

            if (securityType == SecurityType.Option)
            {
                if (ticker[0].EndsWithInvariant("PE", true))
                {
                    optionRight = OptionRight.Put;
                }
                if (ticker[0].EndsWithInvariant("CE", true))
                {
                    optionRight = OptionRight.Call;
                }

                var strikePrice = Convert.ToDecimal(scrip.StrikePrice, CultureInfo.InvariantCulture);
                var expiryDate = DateTime.ParseExact(scrip.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                symbol = Symbol.CreateOption(ConvertSamcoSymbolToLeanSymbol(scrip.Name.Replace(" ", "").Trim()), market, OptionStyle.European, optionRight, strikePrice, expiryDate);
            }

            if (securityType == SecurityType.Future)
            {
                var expiryDate = DateTime.ParseExact(scrip.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                symbol=Symbol.CreateFuture(ConvertSamcoSymbolToLeanSymbol(ticker[0].Trim().Replace(" ", "")), market, expiryDate);
            }
            if (securityType == SecurityType.Equity)
            {
                symbol=Symbol.Create(ConvertSamcoSymbolToLeanSymbol(scrip.Name.Trim().Replace(" ", "")), securityType, market);
            }

            return symbol;
        }
        /// <summary>
        /// Converts a Lean symbol instance to an Samco symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Samco symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            var brokerageSymbol = ConvertLeanSymbolToSamcoSymbol(symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Samco symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            if (brokerageSymbol.Contains('-'))
            {
                brokerageSymbol = brokerageSymbol.Split('-')[0];
            }

            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Samco symbol: {brokerageSymbol}");

            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Commodity || securityType == SecurityType.Crypto)
                throw new ArgumentException($"Unsupported security type: {securityType}");

            if (!Market.Encode(market.ToLowerInvariant()).HasValue)
                throw new ArgumentException($"Invalid market: {market}");
            var scrip = samcoTradableSymbolList.Where(s => s.TradingSymbol == brokerageSymbol && s.Exchange == market).First();

            if(scrip == null)
            {
                throw new ArgumentException($"Invalid Samco symbol: {brokerageSymbol}");
            }

            return getSymbolfromList(scrip);
        }

        /// <summary>
        /// Returns the security type for an Samco symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol, string exchange)
        {
            if (brokerageSymbol.Contains('-'))
            {
                brokerageSymbol = brokerageSymbol.Split('-')[0];
            }
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Samco symbol: {brokerageSymbol}");

            var scrip = samcoTradableSymbolList.Where(s => s.TradingSymbol == brokerageSymbol && s.Exchange.ToUpperInvariant() == exchange.ToUpperInvariant()).FirstOrDefault();
            var symbol=getSymbolfromList(scrip);
            return symbol.SecurityType;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol, string exchange)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToSamcoSymbol(leanSymbol), exchange);
        }

        /// <summary>
        /// Checks if the symbol is supported by Samco
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <returns>True if Samco supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol, string exchange)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return samcoTradableSymbolList.Where(x => x.TradingSymbol.Contains(brokerageSymbol) && x.Exchange.ToUpperInvariant() == exchange.ToUpperInvariant()).IsNullOrEmpty();
        }

        /// <summary>
        /// Checks if the symbol is supported by Samco
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Samco supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var samcoSymbol = ConvertLeanSymbolToSamcoSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(samcoSymbol, symbol.ID.Market) && GetBrokerageSecurityType(samcoSymbol, symbol.ID.Market) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Samco symbol to a Lean symbol string
        /// </summary>
        private static string ConvertSamcoSymbolToLeanSymbol(string samcoSymbol)
        {
            if (string.IsNullOrWhiteSpace(samcoSymbol))
                throw new ArgumentException($"Invalid Samco symbol: {samcoSymbol}");

            // return as it is due to Samco has similar Symbol format
            return samcoSymbol.ToUpperInvariant();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Samco symbol
        /// </summary>
        private static string ConvertLeanSymbolToSamcoSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // return as it is due to Samco has similar Symbol format
            return leanSymbol.ToUpperInvariant();
        }
    }
}