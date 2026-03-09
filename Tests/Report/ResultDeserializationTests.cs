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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Report;
using QuantConnect.Report.ReportElements;
using System;
using System.Collections.Generic;
using System.Linq;
using static QuantConnect.Report.Report;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ResultDeserializationTests
    {
        private const string OrderStringReplace = "{{orderStringReplace}}";
        private const string OrderTypeStringReplace = "{{marketOrderType}}";
        private const string EmptyJson = "{}";

        private const string ValidBacktestResultJson2 = "{\"backtest\":{\"note\":null,\"name\":\"Emotional Black Mosquito\",\"organizationId\":99568,\"projectId\":15863659,\"completed\":true,\"optimizationId\":null,\"backtestId\"" +
            ":\"2d5e2342b73ffb04bfa6ff4a505e3cad\",\"tradeableDates\":381,\"researchGuide\":{\"minutes\":1,\"backtestCount\":1,\"parameters\":0},\"backtestStart\":\"2022-03-10 00:00:00\",\"backtestEnd\":\"2023-09-14 23:59:59\"," +
            "\"created\":\"2023-09-15 16:23:31\",\"snapshotId\":15863663,\"status\":\"Completed.\",\"error\":null,\"stacktrace\":null,\"progress\":1,\"profitLoss\":[],\"hasInitializeError\":false,\"charts\":{\"Portfolio Turnover\":" +
            "{\"Name\":\"Portfolio Turnover\"},\"Drawdown\":{\"Name\":\"Drawdown\",\"ChartType\":0,\"Series\":{\"Equity Drawdown\":{\"Name\":\"Equity Drawdown\",\"Unit\":\"%\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1646888400,\"y\":0}" +
            ",{\"x\":1646974800,\"y\":0}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Exposure\":{\"Name\":\"Exposure\"},\"Universe Analytics\":{\"Name\":\"Universe Analytics\"},\"Strategy Equity\":{\"Name\":\"Strategy Equity\"" +
            ",\"LastValue\":null},\"Capacity\":{\"Name\":\"Capacity\"},\"Benchmark\":{\"Name\":\"Benchmark\"}},\"parameterSet\":[],\"alphaRuntimeStatistics\":null,\"runtimeStatistics\":{\"Equity\":\"$100,000.00\",\"Fees\":\"-$0.00\"," +
            "\"Holdings\":\"$0.00\",\"Net Profit\":\"$0.00\",\"Probabilistic Sharpe Ratio\":\"0%\",\"Return\":\"0.00 %\",\"Unrealized\":\"$0.00\",\"Volume\":\"$0.00\"},\"statistics\":{\"Total Trades\":\"0\",\"Average Win\":\"0%\"," +
            "\"Average Loss\":\"0%\",\"Compounding Annual Return\":\"0%\",\"Drawdown\":\"0%\",\"Expectancy\":\"0\",\"Net Profit\":\"0%\",\"Sharpe Ratio\":\"0\",\"Probabilistic Sharpe Ratio\":\"0%\",\"Loss Rate\":\"0%\",\"Win Rate\"" +
            ":\"0%\",\"Profit-Loss Ratio\":\"0\",\"Alpha\":\"0\",\"Beta\":\"0\",\"Annual Standard Deviation\":\"0\",\"Annual Variance\":\"0\",\"Information Ratio\":\"-0.309\",\"Tracking Error\":\"0.169\",\"Treynor Ratio\":\"0\"" +
            ",\"Total Fees\":\"$0.00\",\"Estimated Strategy Capacity\":\"$0\",\"Lowest Capacity Asset\":\"\",\"Portfolio Turnover\":\"0%\"},\"totalPerformance\":{\"TradeStatistics\":{\"StartDateTime\":null,\"EndDateTime\":null," +
            "\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\"" +
            ":\"0\",\"AverageProfit\":\"0\",\"AverageLoss\":\"0\",\"AverageTradeDuration\":\"00:00:00\",\"AverageWinningTradeDuration\":\"00:00:00\",\"AverageLosingTradeDuration\":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\"," +
            "\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0,\"ProfitLossRatio\":\"0\",\"WinLossRatio\":\"0\",\"WinRate\":\"0\"," +
            "\"LossRate\":\"0\",\"AverageMAE\":\"0\",\"AverageMFE\":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\"," +
            "\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\",\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":\"0\",\"MaximumEndTradeDrawdown\":\"0\",\"AverageEndTradeDrawdown\":\"0\",\"MaximumDrawdownDuration\"" +
            ":\"00:00:00\",\"TotalFees\":\"0\"},\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":\"0\"" +
            ",\"Drawdown\":\"0\",\"TotalNetProfit\":\"0\",\"SharpeRatio\":\"0\",\"ProbabilisticSharpeRatio\":\"0\",\"Alpha\":\"0\",\"Beta\":\"0\",\"AnnualStandardDeviation\":\"0\",\"AnnualVariance\":\"0\",\"InformationRatio\":\"-0.3094\"," +
            "\"TrackingError\":\"0.1686\",\"TreynorRatio\":\"0\",\"PortfolioTurnover\":\"0\"},\"ClosedTrades\":[]},\"signals\":null,\"nodeName\":\"B8-16 node 1ed393c8\"},\"success\":true}";

        private const string ValidBacktestResultJson = "{\"RollingWindow\":{\"M1_20131011\":{\"TradeStatistics\":{\"StartDateTime\":null,\"EndDateTime\":null,\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\":\"0\"," +
            "\"AverageProfit\":\"0\",\"AverageLoss\":\"0\",\"AverageTradeDuration\":\"00:00:00\",\"AverageWinningTradeDuration\":\"00:00:00\",\"AverageLosingTradeDuration\":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\",\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0," +
            "\"ProfitLossRatio\":\"0\",\"WinLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"AverageMAE\":\"0\",\"AverageMFE\":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\",\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\"," +
            "\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":\"0\",\"MaximumEndTradeDrawdown\":\"0\",\"AverageEndTradeDrawdown\":\"0\",\"MaximumDrawdownDuration\":\"00:00:00\",\"TotalFees\":\"0\"},\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":" +
            "\"2.7145\",\"Drawdown\":\"0.022\",\"TotalNetProfit\":\"0.0169\",\"SharpeRatio\":\"8.8881\",\"ProbabilisticSharpeRatio\":\"0.6761\",\"Alpha\":\"-0.0049\",\"Beta\":\"0.9961\",\"AnnualStandardDeviation\":\"0.2217\",\"AnnualVariance\":\"0.0491\",\"InformationRatio\":\"-14.5651\",\"TrackingError\":\"0.0009\",\"TreynorRatio\":\"1.9780\",\"PortfolioTurnover\":\"0.1993\"}," +
            "\"ClosedTrades\":[]},\"M3_20131011\":{\"TradeStatistics\":{\"StartDateTime\":null,\"EndDateTime\":null,\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\":\"0\",\"AverageProfit\":\"0\",\"AverageLoss\":" +
            "\"0\",\"AverageTradeDuration\":\"00:00:00\",\"AverageWinningTradeDuration\":\"00:00:00\",\"AverageLosingTradeDuration\":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\",\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0,\"ProfitLossRatio\":\"0\",\"WinLossRatio\":" +
            "\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"AverageMAE\":\"0\",\"AverageMFE\":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\",\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\",\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":" +
            "\"0\",\"MaximumEndTradeDrawdown\":\"0\",\"AverageEndTradeDrawdown\":\"0\",\"MaximumDrawdownDuration\":\"00:00:00\",\"TotalFees\":\"0\"},\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":\"2.7145\",\"Drawdown\":\"0.022\",\"TotalNetProfit\"" +
            ":\"0.0169\",\"SharpeRatio\":\"8.8881\",\"ProbabilisticSharpeRatio\":\"0.6761\",\"Alpha\":\"-0.0049\",\"Beta\":\"0.9961\",\"AnnualStandardDeviation\":\"0.2217\",\"AnnualVariance\":\"0.0491\",\"InformationRatio\":\"-14.5651\",\"TrackingError\":\"0.0009\",\"TreynorRatio\":\"1.9780\",\"PortfolioTurnover\":\"0.1993\"},\"ClosedTrades\":[]},\"M6_20131011\":{\"TradeStatistics\"" +
            ":{\"StartDateTime\":null,\"EndDateTime\":null,\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\":\"0\",\"AverageProfit\":\"0\",\"AverageLoss\":\"0\",\"AverageTradeDuration\":\"00:00:00\"," +
            "\"AverageWinningTradeDuration\":\"00:00:00\",\"AverageLosingTradeDuration\":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\",\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0,\"ProfitLossRatio\":\"0\",\"WinLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\"," +
            "\"AverageMAE\":\"0\",\"AverageMFE\":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\",\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\",\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":\"0\",\"MaximumEndTradeDrawdown\"" +
            ":\"0\",\"AverageEndTradeDrawdown\":\"0\",\"MaximumDrawdownDuration\":\"00:00:00\",\"TotalFees\":\"0\"},\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":\"2.7145\",\"Drawdown\":\"0.022\",\"TotalNetProfit\":\"0.0169\",\"SharpeRatio\":" +
            "\"8.8881\",\"ProbabilisticSharpeRatio\":\"0.6761\",\"Alpha\":\"-0.0049\",\"Beta\":\"0.9961\",\"AnnualStandardDeviation\":\"0.2217\",\"AnnualVariance\":\"0.0491\",\"InformationRatio\":\"-14.5651\",\"TrackingError\":\"0.0009\",\"TreynorRatio\":\"1.9780\",\"PortfolioTurnover\":\"0.1993\"},\"ClosedTrades\":[]},\"M12_20131011\":{\"TradeStatistics\":{\"StartDateTime\"" +
            ":null,\"EndDateTime\":null,\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\":\"0\",\"AverageProfit\":\"0\",\"AverageLoss\":\"0\",\"AverageTradeDuration\":\"00:00:00\",\"AverageWinningTradeDuration\":" +
            "\"00:00:00\",\"AverageLosingTradeDuration\":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\",\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0,\"ProfitLossRatio\":\"0\",\"WinLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"AverageMAE\":\"0\",\"AverageMFE\"" +
            ":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\",\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\",\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":\"0\",\"MaximumEndTradeDrawdown\":\"0\",\"AverageEndTradeDrawdown\":\"0\"," +
            "\"MaximumDrawdownDuration\":\"00:00:00\",\"TotalFees\":\"0\"},\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":\"2.7145\",\"Drawdown\":\"0.022\",\"TotalNetProfit\":\"0.0169\",\"SharpeRatio\":\"8.8881\",\"ProbabilisticSharpeRatio\"" +
            ":\"0.6761\",\"Alpha\":\"-0.0049\",\"Beta\":\"0.9961\",\"AnnualStandardDeviation\":\"0.2217\",\"AnnualVariance\":\"0.0491\",\"InformationRatio\":\"-14.5651\",\"TrackingError\":\"0.0009\",\"TreynorRatio\":\"1.9780\",\"PortfolioTurnover\":\"0.1993\"},\"ClosedTrades\":[]}},\"TotalPerformance\":{\"TradeStatistics\":{\"StartDateTime\":null,\"EndDateTime\":null," +
            "\"TotalNumberOfTrades\":0,\"NumberOfWinningTrades\":0,\"NumberOfLosingTrades\":0,\"TotalProfitLoss\":\"0\",\"TotalProfit\":\"0\",\"TotalLoss\":\"0\",\"LargestProfit\":\"0\",\"LargestLoss\":\"0\",\"AverageProfitLoss\":\"0\",\"AverageProfit\":\"0\",\"AverageLoss\":\"0\",\"AverageTradeDuration\":\"00:00:00\",\"AverageWinningTradeDuration\":\"00:00:00\",\"AverageLosingTradeDuration\"" +
            ":\"00:00:00\",\"MedianTradeDuration\":\"00:00:00\",\"MedianWinningTradeDuration\":\"00:00:00\",\"MedianLosingTradeDuration\":\"00:00:00\",\"MaxConsecutiveWinningTrades\":0,\"MaxConsecutiveLosingTrades\":0,\"ProfitLossRatio\":\"0\",\"WinLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"AverageMAE\":\"0\",\"AverageMFE\":\"0\",\"LargestMAE\":\"0\",\"LargestMFE\":\"0\"" +
            ",\"MaximumClosedTradeDrawdown\":\"0\",\"MaximumIntraTradeDrawdown\":\"0\",\"ProfitLossStandardDeviation\":\"0\",\"ProfitLossDownsideDeviation\":\"0\",\"ProfitFactor\":\"0\",\"SharpeRatio\":\"0\",\"SortinoRatio\":\"0\",\"ProfitToMaxDrawdownRatio\":\"0\",\"MaximumEndTradeDrawdown\":\"0\",\"AverageEndTradeDrawdown\":\"0\",\"MaximumDrawdownDuration\":\"00:00:00\",\"TotalFees\":\"0\"}" +
            ",\"PortfolioStatistics\":{\"AverageWinRate\":\"0\",\"AverageLossRate\":\"0\",\"ProfitLossRatio\":\"0\",\"WinRate\":\"0\",\"LossRate\":\"0\",\"Expectancy\":\"0\",\"CompoundingAnnualReturn\":\"2.7145\",\"Drawdown\":\"0.022\",\"TotalNetProfit\":\"0.0169\",\"SharpeRatio\":\"8.8881\",\"ProbabilisticSharpeRatio\":\"0.6761\",\"Alpha\":\"-0.0049\",\"Beta\":\"0.9961\",\"AnnualStandardDeviation\"" +
            ":\"0.2217\",\"AnnualVariance\":\"0.0491\",\"InformationRatio\":\"-14.5651\",\"TrackingError\":\"0.0009\",\"TreynorRatio\":\"1.9780\",\"PortfolioTurnover\":\"0.1993\"},\"ClosedTrades\":[]},\"Charts\":{\"Drawdown\":{\"Name\":\"Drawdown\",\"ChartType\":0,\"Series\":{\"Equity Drawdown\":{\"Name\":\"Equity Drawdown\",\"Unit\":\"%\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381118400" +
            ",\"y\":0.0},{\"x\":1381204800,\"y\":-0.02},{\"x\":1381291200,\"y\":-1.18},{\"x\":1381377600,\"y\":-1.12},{\"x\":1381464000,\"y\":0.0},{\"x\":1381521600,\"y\":0.0}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Strategy Equity\":{\"Name\":\"Strategy Equity\",\"ChartType\":0,\"Series\":{\"Equity\":{\"Name\":\"Equity\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":2,\"Values\":" +
            "[[1381118400,100000.0,100000.0,100000.0,100000.0],[1381152660,99990.6114,99990.6114,99990.6114,99990.6114],[1381152960,99948.971,100056.0463,99895.4333,100056.0463],[1381153260,99984.6627,100020.3545,99984.6627,100008.4573],[1381153560,100073.8922,100127.4299,100073.8922,100097.6867],[1381153860,100139.3271,100234.5052,100139.3271,100234.5052],[1381154160,100204.7621,100204.7621,100157.173,100157.173]," +
            "[1381154460,100139.3271,100180.9676,100139.3271,100169.0703],[1381154760,100115.5326,100145.2758,100103.6354,100145.2758],[1381155060,100210.7107,100371.3238,100210.7107,100365.3751],[1381155360,100359.4265,100424.8614,100359.4265,100401.0669],[1381155660,100395.1183,100442.7073,100377.2724,100442.7073],[1381155960,100436.7587,100490.2964,100407.0155,100466.5018],[1381156260,100496.245,100514.0909" +
            ",100460.5532,100514.0909],[1381156560,100537.8854,100537.8854,100430.8101,100430.8101],[1381156860,100407.0155,100424.8614,100407.0155,100418.9128],[1381157160,100407.0155,100407.0155,100383.221,100383.221],[1381157460,100424.8614,100454.6046,100418.9128,100424.8614],[1381157760,100442.7073,100484.3477,100442.7073,100460.5532],[1381158060,100424.8614,100436.7587,100424.8614,100424.8614]," +
            "[1381158360,100341.5806,100353.4779,100323.7347,100341.5806],[1381158660,100347.5292,100395.1183,100335.632,100395.1183],[1381158960,100418.9128,100418.9128,100329.6833,100329.6833],[1381159260,100341.5806,100341.5806,100288.0429,100311.8374],[1381159560,100341.5806,100359.4265,100341.5806,100359.4265],[1381159860,100347.5292,100359.4265,100329.6833,100341.5806],[1381160160,100329.6833,100472.4505," +
            "100329.6833,100472.4505],[1381160460,100466.5018,100466.5018,100424.8614,100436.7587],[1381160760,100424.8614,100484.3477,100407.0155,100484.3477],[1381161060,100496.245,100537.8854,100484.3477,100537.8854],[1381161360,100525.9882,100525.9882,100472.4505,100484.3477],[1381161660,100484.3477,100490.2964,100430.8101,100430.8101],[1381161960,100401.0669,100424.8614,100395.1183,100407.0155],[1381162260," +
            "100430.8101,100460.5532,100418.9128,100436.7587],[1381162560,100424.8614,100430.8101,100418.9128,100430.8101],[1381162860,100436.7587,100436.7587,100424.8614,100430.8101],[1381163160,100377.2724,100395.1183,100377.2724,100377.2724],[1381163460,100359.4265,100359.4265,100228.5566,100228.5566],[1381163760,100222.608,100293.9916,100222.608,100264.2484],[1381164060,100282.0943,100282.0943,100258.2998," +
            "100258.2998],[1381164360,100282.0943,100323.7347,100276.1457,100323.7347],[1381164660,100335.632,100335.632,100299.9402,100323.7347],[1381164960,100341.5806,100341.5806,100317.7861,100317.7861],[1381165260,100323.7347,100323.7347,100216.6593,100276.1457],[1381165560,100317.7861,100317.7861,100264.2484,100264.2484],[1381165860,100246.4025,100299.9402,100240.4539,100299.9402],[1381166160,100365.3751" +
            ",100365.3751,100341.5806,100365.3751],[1381166460,100395.1183,100395.1183,100377.2724,100383.221],[1381166760,100383.221,100383.221,100365.3751,100377.2724],[1381167060,100365.3751,100407.0155,100335.632,100407.0155],[1381167360,100395.1183,100395.1183,100359.4265,100389.1696],[1381167660,100365.3751,100365.3751,100293.9916,100317.7861],[1381167960,100311.8374,100335.632,100299.9402,100323.7347]" +
            ",[1381168260,100347.5292,100359.4265,100329.6833,100329.6833],[1381168560,100353.4779,100383.221,100347.5292,100383.221],[1381168860,100418.9128,100514.0909,100418.9128,100514.0909],[1381169160,100514.0909,100543.8341,100496.245,100496.245],[1381169460,100430.8101,100430.8101,100389.1696,100424.8614],[1381169760,100454.6046,100454.6046,100407.0155,100418.9128],[1381170060,100418.9128,100454.6046" +
            ",100383.221,100436.7587],[1381170360,100412.9642,100430.8101,100412.9642,100424.8614],[1381170660,100412.9642,100448.656,100383.221,100436.7587],[1381170960,100448.656,100454.6046,100442.7073,100454.6046],[1381171260,100496.245,100496.245,100442.7073,100442.7073],[1381171560,100430.8101,100430.8101,100359.4265,100383.221],[1381171860,100395.1183,100442.7073,100395.1183,100442.7073],[1381172160" +
            ",100418.9128,100472.4505,100401.0669,100472.4505],[1381172460,100484.3477,100484.3477,100454.6046,100454.6046],[1381172760,100430.8101,100472.4505,100430.8101,100436.7587],[1381173060,100436.7587,100454.6046,100418.9128,100454.6046],[1381173360,100448.656,100448.656,100430.8101,100448.656],[1381173660,100418.9128,100436.7587,100383.221,100383.221],[1381173960,100341.5806,100341.5806,100299.9402" +
            ",100299.9402],[1381174260,100288.0429,100305.8888,100288.0429,100293.9916],[1381174560,100276.1457,100293.9916,100228.5566,100228.5566],[1381174860,100240.4539,100246.4025,100198.8135,100198.8135],[1381175160,100169.0703,100228.5566,100169.0703,100180.9676],[1381175460,100121.4813,100145.2758,100103.6354,100139.3271],[1381175760,100121.4813,100127.4299,100044.149,100044.149],[1381204800,100109.584" +
            ",100127.4299,99978.7141,99978.7141],[1381239060,100026.3032,100026.3032,100026.3032,100026.3032],[1381239360,99978.7141,100008.4573,99919.2278,99919.2278],[1381239660,99895.4333,99895.4333,99776.4607,99776.4607],[1381239960,99806.2038,100014.4059,99806.2038,100014.4059],[1381240260,100014.4059,100014.4059,99895.4333,99895.4333],[1381240560,99853.7929,100061.9949,99853.7929,100061.9949],[1381240860" +
            ",100038.2004,100050.0977,100020.3545,100020.3545],[1381241160,99907.3305,99978.7141,99877.5874,99978.7141],[1381241460,99943.0223,99943.0223,99829.9983,99829.9983],[1381241760,99883.536,99931.1251,99883.536,99931.1251],[1381242060,99937.0737,100014.4059,99937.0737,100014.4059],[1381242360,99978.7141,99996.56,99966.8168,99966.8168],[1381242660,99960.8682,100014.4059,99954.9196,99954.9196],[1381242960" +
            ",99895.4333,99907.3305,99877.5874,99895.4333],[1381243260,99913.2792,99913.2792,99841.8956,99841.8956],[1381243560,99853.7929,99907.3305,99853.7929,99907.3305],[1381243860,99806.2038,99806.2038,99586.1045,99586.1045],[1381244160,99633.6935,99633.6935,99574.2072,99574.2072],[1381244460,99568.2586,99586.1045,99532.5668,99532.5668],[1381244760,99520.6695,99520.6695,99419.5428,99490.9264],[1381245060" +
            ",99508.7723,99609.899,99508.7723,99609.899],[1381245360,99592.0531,99627.7449,99562.3099,99562.3099],[1381245660,99580.1558,99580.1558,99532.5668,99550.4127],[1381245960,99562.3099,99598.0017,99520.6695,99598.0017],[1381246260,99550.4127,99550.4127,99490.9264,99490.9264],[1381246560,99461.1832,99473.0805,99366.0051,99366.0051],[1381246860,99395.7483,99395.7483,99312.4674,99342.2106],[1381247160" +
            ",99294.6215,99360.0565,99294.6215,99318.4161],[1381247460,99252.9811,99258.9298,99205.3921,99258.9298],[1381247760,99282.7243,99306.5188,99270.827,99300.5702],[1381248060,99300.5702,99342.2106,99270.827,99270.827],[1381248360,99258.9298,99306.5188,99258.9298,99282.7243],[1381248660,99264.8784,99306.5188,99223.238,99270.827],[1381248960,99229.1866,99270.827,99223.238,99223.238],[1381249260,99235.1352," +
            "99235.1352,99193.4948,99193.4948],[1381249560,99181.5976,99193.4948,99134.0085,99134.0085],[1381249860,99145.9058,99199.4434,99128.0599,99128.0599],[1381250160,99116.1626,99116.1626,99074.5222,99104.2653],[1381250460,99169.7003,99175.6489,99151.8544,99151.8544],[1381250760,99169.7003,99318.4161,99169.7003,99318.4161],[1381251060,99312.4674,99324.3647,99288.6729,99288.6729],[1381251360,99247.0325" +
            ",99247.0325,99211.3407,99229.1866],[1381251660,99247.0325,99312.4674,99247.0325,99312.4674],[1381251960,99348.1592,99348.1592,99258.9298,99258.9298],[1381252260,99247.0325,99247.0325,99199.4434,99211.3407],[1381252560,99229.1866,99270.827,99181.5976,99181.5976],[1381252860,99187.5462,99193.4948,99122.1112,99122.1112],[1381253160,99122.1112,99193.4948,99122.1112,99187.5462],[1381253460,99235.1352" +
            ",99288.6729,99169.7003,99288.6729],[1381253760,99306.5188,99306.5188,99241.0839,99241.0839],[1381254060,99187.5462,99217.2893,99187.5462,99193.4948],[1381254360,99199.4434,99211.3407,99157.803,99169.7003],[1381254660,99151.8544,99151.8544,99092.3681,99134.0085],[1381254960,99122.1112,99151.8544,99110.214,99151.8544],[1381255260,99157.803,99199.4434,99062.6249,99062.6249],[1381255560,99044.779" +
            ",99044.779,99003.1386,99032.8818],[1381255860,99015.0359,99026.9331,98967.4468,99026.9331],[1381256160,99009.0873,99050.7277,99003.1386,99003.1386],[1381256460,98955.5496,99009.0873,98937.7037,98937.7037],[1381256760,98937.7037,99116.1626,98937.7037,99098.3167],[1381257060,99157.803,99169.7003,99116.1626,99163.7517],[1381257360,99169.7003,99169.7003,99038.8304,99038.8304],[1381257660,99032.8818" +
            ",99080.4708,99026.9331,99074.5222],[1381257960,99032.8818,99086.4195,99020.9845,99020.9845],[1381258260,98991.2414,99020.9845,98967.4468,98967.4468],[1381258560,99020.9845,99020.9845,98955.5496,98955.5496],[1381258860,98979.3441,99086.4195,98979.3441,99086.4195],[1381259160,99092.3681,99128.0599,99092.3681,99092.3681],[1381259460,99116.1626,99116.1626,99009.0873,99026.9331],[1381259760,98973.3955" +
            ",99020.9845,98967.4468,98973.3955],[1381260060,99003.1386,99086.4195,99003.1386,99086.4195],[1381260360,99139.9571,99235.1352,99139.9571,99217.2893],[1381260660,99247.0325,99264.8784,99217.2893,99264.8784],[1381260960,99270.827,99270.827,99092.3681,99092.3681],[1381261260,99080.4708,99169.7003,99080.4708,99169.7003],[1381261560,99151.8544,99163.7517,99104.2653,99104.2653],[1381261860,99038.8304,99050.7277" +
            ",98979.3441,99050.7277],[1381262160,99003.1386,99020.9845,98890.1146,98890.1146],[1381291200,98836.577,98919.8578,98824.6797,98824.6797],[1381325460,98985.2927,98985.2927,98985.2927,98985.2927],[1381325760,99015.0359,99015.0359,98878.2174,98890.1146],[1381326060,98937.7037,98973.3955,98907.9605,98907.9605],[1381326360,98842.5256,98842.5256,98753.2961,98794.9365],[1381326660,98866.3201,98866.3201,98658.118,98658.118]" +
            ",[1381326960,98723.553,98777.0906,98699.7584,98753.2961],[1381327260,98854.4228,98907.9605,98854.4228,98872.2687],[1381327560,98818.7311,98830.6283,98729.5016,98729.5016],[1381327860,98729.5016,98836.577,98729.5016,98777.0906],[1381328160,98741.3989,98818.7311,98717.6043,98818.7311],[1381328460,98800.8852,98800.8852,98741.3989,98794.9365],[1381328760,98747.3475,98985.2927,98747.3475,98985.2927],[1381329060," +
            "98907.9605,98943.6523,98907.9605,98919.8578],[1381329360,98931.755,99009.0873,98878.2174,98878.2174],[1381329660,98836.577,98890.1146,98836.577,98890.1146],[1381329960,98907.9605,98907.9605,98818.7311,98818.7311],[1381330260,98687.8612,98759.2448,98687.8612,98759.2448],[1381330560,98753.2961,98753.2961,98580.7858,98592.6831],[1381330860,98616.4776,98634.3235,98539.1454,98574.8372],[1381331160,98634.3235," +
            "98664.0667,98622.4262,98628.3749],[1381331460,98604.5803,98604.5803,98449.9159,98449.9159],[1381331760,98485.6077,98485.6077,98402.3269,98402.3269],[1381332060,98390.4296,98420.1728,98342.8406,98342.8406],[1381332360,98283.3543,98336.8919,98283.3543,98336.8919],[1381332660,98390.4296,98426.1214,98390.4296,98426.1214],[1381332960,98473.7105,98551.0427,98473.7105,98491.5564]" +
            ",[1381333260,98503.4536,98539.1454,98461.8132,98485.6077]]},\"Daily Performance\":{\"Name\":\"Daily Performance\",\"Unit\":\"%\",\"Index\":1,\"SeriesType\":3,\"Values\":[{\"x\":1381118400,\"y\":0.0},{\"x\":1381204800,\"y\":-0.02128589},{\"x\":1381291200,\"y\":-1.15428},{\"x\":1381377600,\"y\":0.0541744},{\"x\":1381464000,\"y\":2.207916},{\"x\":1381521600,\"y\":0.6239327}],\"Color\"" +
            ":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Benchmark\":{\"Name\":\"Benchmark\",\"ChartType\":0,\"Series\":{\"Benchmark\":{\"Name\":\"Benchmark\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381118400,\"y\":146.0268},{\"x\":1381204800,\"y\":144.7558},{\"x\":1381291200,\"y\":143.0784},{\"x\":1381377600,\"y\":143.1562},{\"x\":1381464000,\"y\":146.3294},{\"x\":1381521600," +
            "\"y\":147.2459}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Capacity\":{\"Name\":\"Capacity\",\"ChartType\":0,\"Series\":{\"Strategy Capacity\":{\"Name\":\"Strategy Capacity\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381118400,\"y\":0.0},{\"x\":1381204800,\"y\":0.0},{\"x\":1381291200,\"y\":0.0},{\"x\":1381377600,\"y\":0.0},{\"x\":1381464000,\"y\":0.0}," +
            "{\"x\":1381521600,\"y\":56181000.0}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Portfolio Turnover\":{\"Name\":\"Portfolio Turnover\",\"ChartType\":0,\"Series\":{\"Portfolio Turnover\":{\"Name\":\"Portfolio Turnover\",\"Unit\":\"%\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381204800,\"y\":0.9963103},{\"x\":1381291200,\"y\":0.0},{\"x\":1381377600,\"y\":0.0},{\"x\":1381464000,\"y\":0.0}" +
            ",{\"x\":1381521600,\"y\":0.0}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}},\"Exposure\":{\"Name\":\"Exposure\",\"ChartType\":0,\"Series\":{\"Equity - Long Ratio\":{\"Name\":\"Equity - Long Ratio\",\"Unit\":\"\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381118400,\"y\":0.0},{\"x\":1381204800,\"y\":0.9961},{\"x\":1381291200,\"y\":0.9961},{\"x\":1381377600,\"y\":0.9961},{\"x\":1381464000,\"y\":0.9962}" +
            ",{\"x\":1381521600,\"y\":0.9962}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"},\"Equity - Short Ratio\":{\"Name\":\"Equity - Short Ratio\",\"Unit\":\"\",\"Index\":0,\"SeriesType\":0,\"Values\":[{\"x\":1381118400,\"y\":0.0},{\"x\":1381204800,\"y\":0.0},{\"x\":1381291200,\"y\":0.0},{\"x\":1381377600,\"y\":0.0},{\"x\":1381464000,\"y\":0.0},{\"x\":1381521600,\"y\":0.0}],\"Color\":\"\"" +
            ",\"ScatterMarkerSymbol\":\"none\"}}},\"Assets Sales Volume\":{\"Name\":\"Assets Sales Volume\",\"ChartType\":0,\"Series\":{\"SPY\":{\"Name\":\"SPY\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":7,\"Values\":[{\"x\":1381204800,\"y\":99609.8262},{\"x\":1381291200,\"y\":99609.8262},{\"x\":1381377600,\"y\":99609.8262},{\"x\":1381464000,\"y\":99609.8262},{\"x\":1381521600,\"y\":99609.8262}],\"Color\":\"\"" +
            ",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":{\"1\":{\"Type\":0,\"Id\":1,\"ContingentId\":0,\"BrokerId\":[\"1\"],\"Symbol\":{\"Value\":\"SPY\",\"ID\":\"SPY R735QTJ8XC9X\",\"Permtick\":\"SPY\"},\"Price\":144.78172417,\"PriceCurrency\":\"USD\",\"Time\":\"2013-10-07T13:31:00Z\",\"CreatedTime\":\"2013-10-07T13:31:00Z\",\"LastFillTime\":\"2013-10-07T13:31:00Z\",\"Quantity\":688.0,\"Status\":3,\"Properties\"" +
            ":{\"TimeInForce\":{}},\"SecurityType\":1,\"Direction\":0,\"Value\":99609.82622896,\"OrderSubmissionData\":{\"BidPrice\":144.77307790400,\"AskPrice\":144.78172417000,\"LastPrice\":144.77307790400},\"IsMarketable\":true}},\"ProfitLoss\":{},\"Statistics\":{\"Total Trades\":\"1\",\"Average Win\":\"0%\",\"Average Loss\":\"0%\",\"Compounding Annual Return\":\"271.453%\",\"Drawdown\":\"2.200%\",\"Expectancy\":\"0\"" +
            ",\"Net Profit\":\"1.692%\",\"Sharpe Ratio\":\"8.888\",\"Probabilistic Sharpe Ratio\":\"67.609%\",\"Loss Rate\":\"0%\",\"Win Rate\":\"0%\",\"Profit-Loss Ratio\":\"0\",\"Alpha\":\"-0.005\",\"Beta\":\"0.996\",\"Annual Standard Deviation\":\"0.222\",\"Annual Variance\":\"0.049\",\"Information Ratio\":\"-14.565\",\"Tracking Error\":\"0.001\",\"Treynor Ratio\":\"1.978\",\"Total Fees\":\"$3.44\"" +
            ",\"Estimated Strategy Capacity\":\"$56000000.00\",\"Lowest Capacity Asset\":\"SPY R735QTJ8XC9X\",\"Portfolio Turnover\":\"19.93%\"},\"RuntimeStatistics\":{\"Equity\":\"$101,691.92\",\"Fees\":\"-$3.44\",\"Holdings\":\"$101,305.19\",\"Net Profit\":\"$-3.44\",\"Probabilistic Sharpe Ratio\":\"67.609%\",\"Return\":\"1.69 %\",\"Unrealized\":\"$1,656.23\",\"Volume\":\"$99,609.83\"},\"State\":{\"StartTime\":" +
            "\"2023-08-09 17:14:06\",\"EndTime\":\"2023-08-09 17:14:07\",\"RuntimeError\":\"\",\"StackTrace\":\"\",\"LogCount\":\"4\",\"OrderCount\":\"1\",\"InsightCount\":\"0\",\"Name\":\"local\",\"Hostname\":\"MM\",\"Status\":\"Completed\"},\"AlgorithmConfiguration\":{\"AccountCurrency\":\"USD\",\"Brokerage\":0,\"AccountType\":0,\"Parameters\":{\"intrinio-username\":\"\",\"intrinio-password\":\"\",\"ema-fast\":\"10\"," +
            "\"ema-slow\":\"20\"}}}";
        private const string InvalidBacktestResultJson = "{\"RollingWindow\":{},\"TotalPerformance\":null,\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583704925,\"y\":5.0},{\"x\":1583791325,\"y\":null},{\"x\":1583877725,\"y\":7.0},{\"x\":1583964125,\"y\":8.0},{\"x\":1584050525,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        private const string InvalidLiveResultJson = "{\"Holdings\":{},\"Cash\":{\"USD\":{\"SecuritySymbol\":{\"Value\":\"\",\"ID\":\" 0\",\"Permtick\":\"\"},\"Symbol\":\"USD\",\"Amount\":0.0,\"ConversionRate\":1.0,\"CurrencySymbol\":\"$\",\"ValueInAccountCurrency\":0.0}},\"ServerStatistics\":{\"CPU Usage\":\"0.0%\",\"Used RAM (MB)\":\"68\",\"Total RAM (MB)\":\"\",\"Used Disk Space (MB)\":\"1\",\"Total Disk Space (MB)\":\"5\",\"Hostname\":\"LEAN\",\"LEAN Version\":\"v2.4.0.0\"},\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583705127,\"y\":5.0},{\"x\":1583791527,\"y\":null},{\"x\":1583877927,\"y\":7.0},{\"x\":1583964327,\"y\":8.0},{\"x\":1584050727,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        private const string OrderJson = @"{'1': {
    'Type':" + OrderTypeStringReplace + @",
    'Value':99986.827413672,
    'Id':1,
    'ContingentId':0,
    'BrokerId':[1],
    'Symbol':{'Value':'SPY',
    'Permtick':'SPY'},
    'Price':100.086914328,
    'Time':'2010-03-04T14:31:00Z',
    'Quantity':999,
    'Status':3,
    'Duration':2,
    'DurationValue':'2010-04-04T14:31:00Z',
    'Tag':'',
    'SecurityType':1,
    'Direction':0,
    'AbsoluteQuantity':999,
    'GroupOrderManager': {
        'Id': 1,
        'Count': 3,
        'Quantity': 10,
        'LimitPrice': 123.456,
        'OrderIds': [1, 2, 3]
    }
}}";
        [TestCase("charts")]
        [TestCase("Charts")]
        public void ValidBacktestResultDefaultSerializer(string chartKey)
        {
            var result = JsonConvert.DeserializeObject<BacktestResponseWrapper>(ValidBacktestResultJson2.Replace("charts", chartKey, StringComparison.InvariantCulture)).Backtest;

            Assert.AreEqual(7, result.Charts.Count);
            Assert.IsTrue(result.Charts.Where(x => x.Key == "Drawdown").All(kvp => !kvp.Value.IsEmpty()));
        }

        [Test]
        public void ValidBacktestResult()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new NullResultValueTypeJsonConverter<BacktestResult>() },
                FloatParseHandling = FloatParseHandling.Decimal
            };

            var result = JsonConvert.DeserializeObject<BacktestResult>(ValidBacktestResultJson, settings);

            Assert.AreEqual(7, result.Charts.Count);
            Assert.IsTrue(result.Charts.All(kvp => !kvp.Value.IsEmpty()));
        }

        [Test]
        public void BacktestResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<BacktestResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            Assert.AreEqual(1, noConverterPoints.Count(kvp => !kvp.Value.HasValue));
            Assert.AreEqual(5, noConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<BacktestResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void LiveResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<LiveResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            Assert.AreEqual(1, noConverterPoints.Count(kvp => !kvp.Value.HasValue));
            Assert.AreEqual(5, noConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<LiveResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void NullCandleStickPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<LiveResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var nullCandlestick = "{\"Charts\":{\"Strategy Equity\":{\"Name\":\"Strategy Equity\",\"ChartType\":0,\"Series\":{\"Equity\":{\"Name\":\"Equity\",\"Values\":[[1695991900,null,null,null,null],[1695992500,10000000,10000000,10000000,10000000]],\"SeriesType\":2,\"Index\":2,\"Unit\":\"\"}}}}}";
            var deWithoutConverter = JsonConvert.DeserializeObject<LiveResult>(nullCandlestick, settings);
            var deWithConverter = JsonConvert.DeserializeObject<LiveResult>(nullCandlestick, converter);

            var noConverterPoints = deWithoutConverter.Charts["Strategy Equity"].Series["Equity"].Values.Cast<Candlestick>().ToList();
            var withConverterPoints = deWithConverter.Charts["Strategy Equity"].Series["Equity"].Values.Cast<Candlestick>().ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Close > 0));
            Assert.AreEqual(1, withConverterPoints.Count);

            Assert.AreEqual(1, noConverterPoints.Count(kvp => !kvp.Close.HasValue));
            Assert.AreEqual(2, noConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<LiveResult>(convertedSerialized);
            var roundTripCandlestick = roundtripDeserialization.Charts["Strategy Equity"].Series["Equity"].Values.Cast<Candlestick>();

            Assert.IsTrue(withConverterPoints.Select(x => x.Close).SequenceEqual(roundTripCandlestick.Select(x => x.Close)));
        }

        [Test]
        public void OrderTypeEnumStringAndValueDeserialization()
        {

            var settings = new JsonSerializerSettings
            {
                Converters = new[] { new NullResultValueTypeJsonConverter<LiveResult>() }
            };

            foreach (var orderType in (OrderType[])Enum.GetValues(typeof(OrderType)))
            {
                //var orderObjectType = OrderTypeNormalizingJsonConverter.TypeFromOrderTypeEnum(orderType);
                var intValueJson = OrderJson.Replace(OrderTypeStringReplace, ((int)orderType).ToStringInvariant());
                var upperCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToUpperInvariant()}'");
                var camelCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToCamelCase()}'");

                var intValueLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, intValueJson);
                var upperCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, upperCaseJson);
                var camelCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, camelCaseJson);

                var intInstance = JsonConvert.DeserializeObject<LiveResult>(intValueLiveResult, settings).Orders.Values.Single();
                var upperCaseInstance = JsonConvert.DeserializeObject<LiveResult>(upperCaseLiveResult, settings).Orders.Values.Single();
                var camelCaseInstance = JsonConvert.DeserializeObject<LiveResult>(camelCaseLiveResult, settings).Orders.Values.Single();

                CollectionAssert.AreEqual(intInstance.BrokerId, upperCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, upperCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, upperCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, upperCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, upperCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, upperCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, upperCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, upperCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, upperCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, upperCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, upperCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, upperCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, upperCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, upperCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, upperCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, upperCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, upperCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, upperCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, upperCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, upperCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, upperCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, upperCaseInstance.OrderSubmissionData?.LastPrice);

                CollectionAssert.AreEqual(intInstance.BrokerId, camelCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, camelCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, camelCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, camelCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, camelCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, camelCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, camelCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, camelCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, camelCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, camelCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, camelCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, camelCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, camelCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, camelCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, camelCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, camelCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, camelCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, camelCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, camelCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, camelCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, camelCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, camelCaseInstance.OrderSubmissionData?.LastPrice);
            }
        }

        [TestCaseSource(nameof(CreatesReportParametersTableCorrectlyTestCases))]
        public void CreatesReportParametersTableCorrectly(string parametersTemplate, Dictionary<string, string> parameters, string expectedParametersTable)
        {
            parametersTemplate = parametersTemplate.Replace("\r", string.Empty);
            var algorithmConfiguration = new AlgorithmConfiguration { Parameters = parameters };
            var parametersReportElment = new ParametersReportElement("parameters", "", algorithmConfiguration, null, parametersTemplate);
            var parametersTable = parametersReportElment.Render();
            expectedParametersTable = expectedParametersTable.Replace("\r", string.Empty);
            Assert.AreEqual(expectedParametersTable, parametersTable);
        }

        [TestCase(htmlExampleCode + @"

<!--crisis
<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
crisis-->
",
            @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->", @"<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
")]
        [TestCase(htmlExampleCode + @"
<!--parameters
<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
parameters-->",
            @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->", @"<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
")]
        [TestCase(@"<!--crisis<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>crisis-->",
            @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->", @"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>")]
        [TestCase(@"<!--parameters<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>parameters-->",
            @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->", @"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>")]
        public void GetsExpectedCrisisAndParametersHTMLCodes(string input, string pattern, string expected)
        {
            var htmlCode = GetRegexInInput(pattern, input);
            Assert.IsNotNull(htmlCode);
            Assert.AreEqual(expected, htmlCode);
        }

        [TestCase(htmlExampleCode + @"

<!--crisis
<div class=""col-xs-4"">
    <table class=""crisis-chart table compact"">
        <thead>
        <tr>
            <th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th>
        </tr>
        </thead>
        <tbody>
        <tr>
            <td style=""padding:0;"">
                <img src=""{{$PLOT-CRISIS-CONTENT}}"">
            </td>
        </tr>
        </tbody>
    </table>
</div>
crisis-->
", @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(htmlExampleCode + @"
<!--parameters
<tr>
	<td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td>
	<td class = ""title""> {{$SECOND-KPI-NAME}} </td><td> {{$SECOND-KPI-VALUE}} </td>
</tr>
parameters-->", @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"", @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"", @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>crisis-->",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<!--crisis<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<div class=""col-xs-4""><table class=""crisis-chart table compact""><thead><tr><th style=""display: block; height: 75px;"">{{$TEXT-CRISIS-TITLE}}</th></tr></thead><tbody><tr><td style=""padding:0;""><img src=""{{$PLOT-CRISIS-CONTENT}}""></td></tr></tbody></table></div>",
    @"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->")]
        [TestCase(@"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>parameters-->",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<!--parameters<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        [TestCase(@"<tr><td class = ""title""> {{$FIRST-KPI-NAME}} </td><td> {{$FIRST-KPI-VALUE}} </td></tr>",
    @"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->")]
        public void FindsNoMatchingForCrisisAndParametersInGivenInput(string input, string pattern)
        {
            var matching = GetRegexInInput(pattern, input);
            Assert.IsNull(matching);
        }

        public IEnumerable<KeyValuePair<long, decimal?>> GetChartPoints(Result result)
        {
            return result.Charts["Equity"].Series["Performance"].Values.Cast<ChartPoint>().Select(point => new KeyValuePair<long, decimal?>(point.x, point.y));
        }

        private const string htmlExampleCode = @"            <div class=""page"" style=""{{$CSS-CRISIS-PAGE-STYLE}}"">
                <div class=""header"">
                    <div class=""header-left"">
                        <img src=""https://cdn.quantconnect.com/web/i/logo.png"">
                    </div>
                    <div class=""header-right"">Strategy Report Summary: {{$TEXT-STRATEGY-NAME}} {{$TEXT-STRATEGY-VERSION}}</div>
                </div>
                <div class=""content"">
                    <div class=""container-row"">
                        {{$HTML-CRISIS-PLOTS}}
                    </div>
                </div>
            </div>
			<div class=""page"" id=""parameters"" style=""{{$CSS-PARAMETERS-PAGE-STYLE}}"">
                <div class=""header"">
                    <div class=""header-left"">
                        <img src=""https://cdn.quantconnect.com/web/i/logo.png"">
                    </div>
                    <div class=""header-right"">Strategy Report Summary: {{$TEXT-STRATEGY-NAME}} {{$TEXT-STRATEGY-VERSION}}</div>
                </div>
                <div class=""content"">
                    <div class=""container-row"">
                        <div class=""col-xs-12"">
                            <table id=""key-characteristics"" class=""table compact"">
                                <thead>
                                <tr>
                                    <th class=""title"">Parameters</th>
                                </tr>
                                </thead>
                                <tbody>
                                    {{$PARAMETERS}}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
    </body>
    </html>";

        public static object[] CreatesReportParametersTableCorrectlyTestCases = new object[]
        {
            // Happy test cases
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } },
                @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
</tr>
<tr>
	<td class = ""title""> test-key-two </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> test-key-three </td><td> three </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
	<td class = ""title""> test-key-two </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> test-key-three </td><td> three </td>
	<td class = ""title"">  </td><td>  </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" } }, @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
	<td class = ""title""> test-key-two </td><td> 2 </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"} }, @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
	<td class = ""title""> test-key-two </td><td> 2 </td>
    <td class = ""title""> test-key-three </td><td> three </td>
</tr>
<tr>
	<td class = ""title""> test-key-four </td><td> 4 </td>
	<td class = ""title"">  </td><td>  </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"}, { "test-key-five", "5"} }, @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
	<td class = ""title""> test-key-two </td><td> 2 </td>
    <td class = ""title""> test-key-three </td><td> three </td>
</tr>
<tr>
	<td class = ""title""> test-key-four </td><td> 4 </td>
	<td class = ""title""> test-key-five </td><td> 5 </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE0}} </td>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
    <td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
    <td class = ""title""> {{$KEY4}} </td><td> {{$VALUE4}} </td>
    <td class = ""title""> {{$KEY5}} </td><td> {{$VALUE5}} </td>
    <td class = ""title""> {{$KEY6}} </td><td> {{$VALUE6}} </td>
    <td class = ""title""> {{$KEY7}} </td><td> {{$VALUE7}} </td>
    <td class = ""title""> {{$KEY8}} </td><td> {{$VALUE8}} </td>
    <td class = ""title""> {{$KEY9}} </td><td> {{$VALUE9}} </td>
    <td class = ""title""> {{$KEY10}} </td><td> {{$VALUE10}} </td>
    <td class = ""title""> {{$KEY11}} </td><td> {{$VALUE11}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4" }, { "test-key-five", "5" },
                { "test-key-six", "6" }, { "test-key-seven", "7" }, { "test-key-eight", "8" }, { "test-key-nine", "9" }, { "test-key-ten", "10"}, { "test-key-eleven", "11"}}, @"<tr>
	<td class = ""title""> test-key-one </td><td> 1 </td>
	<td class = ""title""> test-key-two </td><td> 2 </td>
    <td class = ""title""> test-key-three </td><td> three </td>
    <td class = ""title""> test-key-four </td><td> 4 </td>
    <td class = ""title""> test-key-five </td><td> 5 </td>
    <td class = ""title""> test-key-six </td><td> 6 </td>
    <td class = ""title""> test-key-seven </td><td> 7 </td>
    <td class = ""title""> test-key-eight </td><td> 8 </td>
    <td class = ""title""> test-key-nine </td><td> 9 </td>
    <td class = ""title""> test-key-ten </td><td> 10 </td>
    <td class = ""title""> test-key-eleven </td><td> 11 </td>
    <td class = ""title"">  </td><td>  </td>
</tr>"},
            // Sad test cases
            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY0}} </td><td> {{$VALUE}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> test-key-one </td><td> {{$VALUE}} </td>
</tr>
<tr>
	<td class = ""title""> test-key-two </td><td> {{$VALUE}} </td>
</tr>
<tr>
	<td class = ""title""> test-key-three </td><td> {{$VALUE}} </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE0}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" } }, @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> 1 </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> 2 </td>
</tr>
<tr>
	<td class = ""title""> {{$KEY1}} </td><td> three </td>
</tr>"},

            new object[] { @"<tr>
	<td class = ""title""> {{$KEY1}} </td><td> {{$VALUE1}} </td>
	<td class = ""title""> {{$KEY2}} </td><td> {{$VALUE2}} </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>", new Dictionary<string, string>() { { "test-key-one", "1" }, { "test-key-two", "2" }, { "test-key-three", "three" }, { "test-key-four", "4"} }, @"<tr>
	<td class = ""title""> test-key-two </td><td> 2 </td>
	<td class = ""title""> test-key-three </td><td> three </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>
<tr>
	<td class = ""title"">  </td><td>  </td>
	<td class = ""title"">  </td><td>  </td>
    <td class = ""title""> {{$KEY3}} </td><td> {{$VALUE3}} </td>
</tr>"},
        };
    }
}
