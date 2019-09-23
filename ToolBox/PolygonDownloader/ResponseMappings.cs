using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuantConnect.ToolBox.PolygonDownloader
{
    /// <summary>
    /// Provides information on available exchanges and other hard coded information 
    /// as well as means of matching them with Lean global catalogues
    /// </summary>
    public static class Mapping
    {
        /// <summary>
        /// Dictionary that fulfills the mapping from exchange id to exchange tape symbol
        /// </summary>
        public static ReadOnlyDictionary<int, string> IdToTape;

        /// <summary>
        /// Initialize static fields
        /// </summary>
        static Mapping()
        {
            // Init readonly dict.
            IdToTape = new ReadOnlyDictionary<int, string>(Exchanges.ToDictionary( k => k.Id, v => v.Tape));
        }

        /// <summary>
        /// Summary of available exchanges
        /// </summary>
        public static List<ExchangeTexture> Exchanges = new List<ExchangeTexture>
        {
            new ExchangeTexture()
            {
                Id = 0,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "TFF",
                Name = "Multiple",
                Tape = "-"
            },
            new ExchangeTexture()
            {
                Id = 1,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XASE",
                Name = "NYSE American (AMEX)",
                Tape = "A",
                Code = "AMX"
            },
            new ExchangeTexture()
            {
                Id = 2,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XNAS",
                Name = "NASDAQ OMX BX",
                Tape = "B",
                Code = "NSD"
            },
            new ExchangeTexture()
            {
                Id = 3,
                Infrastructure = "TRF",
                Market = "equities",
                Name = "National Stock Exchange",
                Mic = "XCIS",
                Tape = "C"
            },
            new ExchangeTexture()
            {
                Id = 4,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "FINR",
                Name = "FINRA",
                Tape = "D"
            },
            new ExchangeTexture()
            {
                Id = 5,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "CQS",
                Name = "Consolidated Quote System",
                Tape = "E"
            },
            new ExchangeTexture()
            {
                Id = 6,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "XISX",
                Name = "International Securities Exchange",
                Tape = "I",
                Code = "IOE"
            },
            new ExchangeTexture()
            {
                Id = 7,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "EDGA",
                Name = "Cboe EDGA",
                Tape = "J",
                Code = "XCBO"
            },
            new ExchangeTexture()
            {
                Id = 8,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "EDGX",
                Name = "Cboe EDGX",
                Tape = "K",
                Code = "EDGX"
            },
            new ExchangeTexture()
            {
                Id = 9,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XCHI",
                Name = "Chicago Stock Exchange, Inc",
                Tape = "M"
            },
            new ExchangeTexture()
            {
                Id = 10,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XNYS",
                Name = "New York Stock Exchange",
                Tape = "N",
                Code = "NYE"
            },
            new ExchangeTexture()
            {
                Id = 11,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "ARCX",
                Name = "NYSE Arca",
                Tape = "P",
                Code = "ARCA"
            },
            new ExchangeTexture()
            {
                Id = 12,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XNGS",
                Name = "Nasdaq",
                Tape = "T",
                Code = "NSD"
            },
            new ExchangeTexture()
            {
                Id = 13,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "CTS",
                Name = "Consolidated Tape System",
                Tape = "S"
            },
            new ExchangeTexture()
            {
                Id = 14,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "OOTC",
                Name = "OTC Bulletin Board",
                Tape = "U"
            },
            new ExchangeTexture()
            {
                Id = 141,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "XOTC",
                Name = "OTC Bulletin Board",
                Tape = "U"
            },
            new ExchangeTexture()
            {
                Id = 142,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "PSGM",
                Name = "OTC Bulletin Board",
                Tape = "U",
                Code = "GREY"
            },
            new ExchangeTexture()
            {
                Id = 143,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "PINX",
                Name = "OTC Bulletin Board",
                Tape = "U",
                Code = "OTO"
            },
            new ExchangeTexture()
            {
                Id = 144,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "OTCB",
                Name = "OTC Bulletin Board",
                Tape = "U",
                Code = "OTCQB"
            },
            new ExchangeTexture()
            {
                Id = 145,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "OTCQ",
                Name = "OTC Bulletin Board",
                Tape = "U",
                Code = "OTCQX"
            },
            new ExchangeTexture()
            {
                Id = 15,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "IEXG",
                Name = "IEX",
                Tape = "V",
                Code = "IEXG"
            },
            new ExchangeTexture()
            {
                Id = 16,
                Infrastructure = "TRF",
                Market = "equities",
                Mic = "XCBO",
                Name = "Chicago Board Options Exchange",
                Tape = "W",
                Code = "CBO"
            },
            new ExchangeTexture()
            {
                Id = 17,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "PHLX",
                Name = "Nasdaq PSX",
                Tape = "X"
            },
            new ExchangeTexture()
            {
                Id = 18,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "BATY",
                Name = "Cboe BYX",
                Tape = "Y",
                Code = "BATS"
            },
            new ExchangeTexture()
            {
                Id = 19,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "BATS",
                Name = "Cboe BZX",
                Tape = "Z",
                Code = "BATS"
            },
            new ExchangeTexture()
            {
                Id = 20,
                Infrastructure = "banking",
                Market = "currencies",
                Name = "Currency Banks 1"
            },
            new ExchangeTexture()
            {
                Id = 33,
                Infrastructure = "exchange",
                Market = "equities",
                Mic = "XBOS",
                Name = "NASDAQ BX Options/ETF",
                Tape = "B"
            },
            new ExchangeTexture()
            {
                Id = 36,
                Infrastructure = "exchange",
                Market = "index",
                Name = "CME S&P Complete Indices",
                Code = "SPIC"
            },
            new ExchangeTexture()
            {
                Id = 37,
                Infrastructure = "exchange",
                Market = "index",
                Name = "Russell Tick Indices",
                Code = "RUS"
            },
            new ExchangeTexture()
            {
                Id = 38,
                Infrastructure = "exchange",
                Market = "index",
                Name = "CSMI Indices Exchange",
                Code = "MDX"
            },
            new ExchangeTexture()
            {
                Id = 39,
                Infrastructure = "exchange",
                Market = "index",
                Name = "CME S&P Base Indices",
                Code = "SPIB"
            },
            new ExchangeTexture()
            {
                Id = 40,
                Infrastructure = "exchange",
                Market = "index",
                Name = "Dow Jones Indexes",
                Code = "DJI"
            },
            new ExchangeTexture()
            {
                Id = 44,
                Infrastructure = "banking",
                Market = "currencies",
                Name = "Currency Banks 2"
            },
            new ExchangeTexture()
            {
                Id = 60,
                Infrastructure = "banking",
                Market = "currencies",
                Name = "Currency Banks 3"
            }
        };
    }
}
