using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class LeanDataPathComponentsTests
    {
        public sealed class Arguments
        {
            public Symbol Symbol;
            public DateTime Date;
            public Resolution Resolution;
            public TickType TickType;
            public string Market;

            public Arguments(Symbol symbol, DateTime date, Resolution resolution, string market, TickType tickType)
            {
                Symbol = symbol;
                Date = date;
                Resolution = resolution;
                TickType = tickType;
                Market = market;
                if (symbol.ID.SecurityType != SecurityType.Option && (resolution == Resolution.Hour || resolution == Resolution.Daily))
                {
                    // for the time being this is true, eventually I'm sure we'd like to support hourly/daily quote data in other security types
                    TickType = TickType.Trade;
                }
            }
        }

        public TestCaseData[] GetTestCases()
        {
            var referenceDate = new DateTime(2016, 11, 1);

            var tickTypes = Enum.GetValues(typeof(TickType)).Cast<TickType>();
            var resolutions = Enum.GetValues(typeof (Resolution)).Cast<Resolution>();
            var securityTypes = Enum.GetValues(typeof (SecurityType)).Cast<SecurityType>();
            var markets = typeof (Market).GetFields().Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => (string) f.GetValue(null));

            var results = (
                from securityType in securityTypes
                where securityType != SecurityType.Commodity
                from market in markets
                from resolution in resolutions
                from tickType in tickTypes
                let date = resolution == Resolution.Hour || resolution == Resolution.Daily ? DateTime.MinValue : referenceDate
                let name = string.Format("{0}-{1}-{2}-{3}", securityType.ToLower(), market.ToLower(), resolution.ToLower(), tickType.ToLower())
                where TryInvoke(() => Symbol.Create(name, securityType, market))
                let symbol = securityType != SecurityType.Option
                    ? Symbol.Create(name, securityType, market)
                    : Symbol.CreateOption(name, market, OptionStyle.American, OptionRight.Put, 0, SecurityIdentifier.DefaultDate)
                select new TestCaseData(new Arguments(symbol, date, resolution, market, tickType))
                                .SetName(name)
                ).ToArray();

            return results;
        }

        [Test, TestCaseSource("GetTestCases")]
        public void DecomposesAccordingToLeanDataFileGeneration(Arguments args)
        {
            var sourceString = LeanData.GenerateRelativeZipFilePath(args.Symbol, args.Date, args.Resolution, args.TickType);
            var decomposed = LeanDataPathComponents.Parse(sourceString);

            var expectedFileName = LeanData.GenerateZipFileName(args.Symbol, args.Date, args.Resolution, args.TickType);

            Assert.AreEqual(args.Symbol, decomposed.Symbol);
            Assert.AreEqual(args.Date, decomposed.Date);
            Assert.AreEqual(expectedFileName, decomposed.Filename);
            Assert.AreEqual(args.Symbol.ID.SecurityType, decomposed.SecurityType);
            Assert.AreEqual(args.Symbol.ID.Market, decomposed.Market);
            Assert.AreEqual(args.TickType, decomposed.TickType);
            Assert.AreEqual(args.Market, decomposed.Market);
        }

        private static bool TryInvoke(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch
            {
                return false;
                throw;
            }
        }
    }
}
