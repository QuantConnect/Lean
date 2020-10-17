using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Samco symbols.
    /// </summary>
    public class SamcoSymbolMapper : ISymbolMapper
    {

        

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

        /// <summary>
        /// Symbols that are both active and delisted
        /// </summary>
        public static List<Symbol> KnownSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new SamcoSymbolMapper();
                foreach (var tp in KnownSymbolStrings)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), Market.NSE));
                }
                return symbols;
            }
        }

        /// <summary>
        /// The list of known Samco symbols.
        /// </summary>
        public static readonly HashSet<string> KnownSymbolStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JSWSTEEL","SBIN"
        };

        /// <summary>
        /// The list of active Samco symbols.
        /// </summary>
        public static List<string> ActiveSymbolStrings =
            KnownSymbolStrings
                .ToList();

        /***
         * public SamcoSymbolMapper()
        {
            
            StreamReader streamReader;
            var path = Path.Combine(Globals.DataFolder, "ScripMaster.csv");
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
                SaveStreamAsFile(Globals.DataFolder, resp.GetResponseStream(), "ScripMaster.csv");
            }
            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            var csv = new CsvReader(streamReader, configuration);
            var scrips = csv.GetRecords<ScripMaster>();
            char[] sep = { '-' };
            KnownSymbolStrings = scrips.Select(x => x.TradingSymbol.Split(sep)[0]).ToHashSet();
            var symbols = new List<Symbol>();
            var mapper = new SamcoSymbolMapper();

            foreach (var tp in scrips)
            {
                var securityType = SecurityType.Equity;
                var market = Market.NSE;
                OptionRight optionRight = 0;
                OptionStyle optionStyle = OptionStyle.European;
                switch (tp.Instrument)
                {
                    //Equities
                    case "EQ":
                        securityType = SecurityType.Equity;
                        break;
                    //Index Options
                    case "OPTIDX":
                        securityType = SecurityType.Option;
                        optionStyle = OptionStyle.European;
                        break;
                    //Stock Futures
                    case "FUTSTK":
                        securityType = SecurityType.Future;
                        break;
                    //Stock options
                    case "OPTSTK":
                        securityType = SecurityType.Option;
                        optionStyle = OptionStyle.European;
                        break;
                    //Commodity Futures
                    case "FUTCOM":
                        securityType = SecurityType.Future;
                        break;
                    //Commodity Options
                    case "OPTCOM":
                        securityType = SecurityType.Option;
                        optionStyle = OptionStyle.European;
                        break;

                    //Bullion Options
                    case "OPTBLN":

                        securityType = SecurityType.Option;
                        optionStyle = OptionStyle.European;
                        break;

                    //Energy Futures
                    case "FUTENR":

                        securityType = SecurityType.Future;
                        break;

                    //Currenty Options
                    case "OPTCUR":

                        securityType = SecurityType.Option;
                        optionStyle = OptionStyle.European;
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
                        optionStyle = OptionStyle.European;
                        break;
                    default:
                        securityType = SecurityType.Base;
                        break;
                }


                switch (tp.Exchange)
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


                
                string[] ticker = tp.TradingSymbol.Split(sep);
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
                }
                var strikePrice = tp.StrikePrice.IfNotNullOrEmpty(s => Convert.ToDecimal(s, CultureInfo.InvariantCulture));
                var expiryDate = tp.ExpiryDate.IfNotNullOrEmpty(s => DateTime.ParseExact(s, "yyyy-MM-dd HH:mm tt", CultureInfo.InvariantCulture));
                symbols.Add(mapper.GetLeanSymbol(ticker[0], securityType, market, expiryDate, strikePrice, optionRight, optionStyle));
            }
            KnownSymbols = symbols;
            
    }
        ***/
        /// <summary>
        /// Converts a Lean symbol instance to an Samco symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Samco symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            //if (symbol.ID.SecurityType != SecurityType.Equity)
            //    throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToSamcoSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

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
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0, OptionStyle optionStyle = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Samco symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Samco symbol: {brokerageSymbol}");

            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Commodity || securityType == SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (!Market.Encode(market).HasValue)
                throw new ArgumentException($"Invalid market: {market}");

            switch (securityType)
            {
                case SecurityType.Option:
                    return Symbol.CreateOption(ConvertSamcoSymbolToLeanSymbol(brokerageSymbol) , market, optionStyle, optionRight,strike,expirationDate);
                case SecurityType.Future:
                    return Symbol.CreateFuture(ConvertSamcoSymbolToLeanSymbol(brokerageSymbol), market,expirationDate);
                default:
                    return Symbol.Create(ConvertSamcoSymbolToLeanSymbol(brokerageSymbol), securityType, market);
            }

        }

        /// <summary>
        /// Converts an Samco symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            var symbol = KnownSymbols.Where(s => s.ID.Symbol == brokerageSymbol).FirstOrDefault();
            return GetLeanSymbol(brokerageSymbol, securityType, symbol.ID.Market);
        }

        /// <summary>
        /// Returns the security type for an Samco symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Samco symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Samco symbol: {brokerageSymbol}");
            //var symbol = KnownSymbols.Where(s => s.ID.Symbol == brokerageSymbol).FirstOrDefault();
            //TODO:Handle in better way
            return SecurityType.Equity;
            //return symbol.SecurityType;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToSamcoSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Samco
        /// </summary>
        /// <param name="brokerageSymbol">The Samco symbol</param>
        /// <returns>True if Samco supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return KnownSymbolStrings.Contains(brokerageSymbol);
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

            return IsKnownBrokerageSymbol(samcoSymbol) && GetBrokerageSecurityType(samcoSymbol) == symbol.ID.SecurityType;
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
