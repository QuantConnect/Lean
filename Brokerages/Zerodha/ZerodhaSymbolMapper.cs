using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Zerodha symbols.
    /// </summary>
    public class ZerodhaSymbolMapper : ISymbolMapper
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
                var mapper = new ZerodhaSymbolMapper();
                foreach (var tp in KnownSymbolStrings)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), Market.NSE));
                }
                return symbols;
            }
        }

        /// <summary>
        /// The list of known Zerodha symbols.
        /// </summary>
        public static readonly HashSet<string> KnownSymbolStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JSWSTEEL","SBIN"
        };

        /// <summary>
        /// The list of active Zerodha symbols.
        /// </summary>
        public static List<string> ActiveSymbolStrings =
            KnownSymbolStrings
                .ToList();

        /***
         * public ZerodhaSymbolMapper()
        {
            
            StreamReader streamReader;
            //TODO: Append Date in filename to check the tradable scrips for the day?
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
            var mapper = new ZerodhaSymbolMapper();

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
        /// Converts a Lean symbol instance to an Zerodha symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Zerodha symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            //if (symbol.ID.SecurityType != SecurityType.Equity)
            //    throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        } 

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Zerodha symbol: {brokerageSymbol}");

            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Commodity || securityType == SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (!Market.Encode(market).HasValue)
                throw new ArgumentException($"Invalid market: {market}");

            switch (securityType)
            {
                case SecurityType.Option:
                    OptionStyle optionStyle = OptionStyle.European;
                    return Symbol.CreateOption(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol) , market, optionStyle, optionRight,strike,expirationDate);
                case SecurityType.Future:
                    return Symbol.CreateFuture(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol), market,expirationDate);
                default:
                    return Symbol.Create(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol), securityType, market);
            }

        }

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            var symbol = KnownSymbols.Where(s => s.ID.Symbol == brokerageSymbol).FirstOrDefault();
            return GetLeanSymbol(brokerageSymbol, securityType, symbol.ID.Market);
        }
        
        /// <summary>
        /// Returns the security type for an Zerodha symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Zerodha symbol: {brokerageSymbol}");
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
            return GetBrokerageSecurityType(ConvertLeanSymbolToZerodhaSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Zerodha
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return KnownSymbolStrings.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Zerodha
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var ZerodhaSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(ZerodhaSymbol) && GetBrokerageSecurityType(ZerodhaSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol string
        /// </summary>
        private static string ConvertZerodhaSymbolToLeanSymbol(string ZerodhaSymbol)
        {
            if (string.IsNullOrWhiteSpace(ZerodhaSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {ZerodhaSymbol}");

            // return as it is due to Zerodha has similar Symbol format
            return ZerodhaSymbol.ToUpperInvariant();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Zerodha symbol
        /// </summary>
        private static string ConvertLeanSymbolToZerodhaSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // return as it is due to Zerodha has similar Symbol format
            return leanSymbol.ToUpperInvariant();
        }
    }
}
