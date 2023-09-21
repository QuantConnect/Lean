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
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the ValuationRatios class
    /// </summary>
    public readonly struct ValuationRatios
    {
        /// <summary>
        /// Dividend per share / Diluted earnings per share
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14000
        /// </remarks>
        [JsonProperty("14000")]
        public double PayoutRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PayoutRatio");

        /// <summary>
        /// ROE * (1 - Payout Ratio)
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14001
        /// </remarks>
        [JsonProperty("14001")]
        public double SustainableGrowthRate => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.SustainableGrowthRate");

        /// <summary>
        /// Refers to the ratio of free cash flow to enterprise value. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: FCF /Enterprise Value.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14002
        /// </remarks>
        [JsonProperty("14002")]
        public double CashReturn => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.CashReturn");

        /// <summary>
        /// Sales / Average Diluted Shares Outstanding
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14003
        /// </remarks>
        [JsonProperty("14003")]
        public double SalesPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.SalesPerShare");

        /// <summary>
        /// Common Shareholder's Equity / Diluted Shares Outstanding
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14004
        /// </remarks>
        [JsonProperty("14004")]
        public double BookValuePerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.BookValuePerShare");

        /// <summary>
        /// Cash Flow from Operations / Average Diluted Shares Outstanding
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14005
        /// </remarks>
        [JsonProperty("14005")]
        public double CFOPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.CFOPerShare");

        /// <summary>
        /// Free Cash Flow / Average Diluted Shares Outstanding
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14006
        /// </remarks>
        [JsonProperty("14006")]
        public double FCFPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.FCFPerShare");

        /// <summary>
        /// Diluted EPS / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14007
        /// </remarks>
        [JsonProperty("14007")]
        public double EarningYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EarningYield");

        /// <summary>
        /// Adjusted Close Price/ EPS. If the result is negative, zero, &gt;10,000 or &lt;0.001, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14008
        /// </remarks>
        [JsonProperty("14008")]
        public double PERatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio");

        /// <summary>
        /// SalesPerShare / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14009
        /// </remarks>
        [JsonProperty("14009")]
        public double SalesYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.SalesYield");

        /// <summary>
        /// Adjusted close price / Sales Per Share. If the result is negative or zero, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14010
        /// </remarks>
        [JsonProperty("14010")]
        public double PSRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio");

        /// <summary>
        /// BookValuePerShare / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14011
        /// </remarks>
        [JsonProperty("14011")]
        public double BookValueYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.BookValueYield");

        /// <summary>
        /// Adjusted close price / Book Value Per Share. If the result is negative or zero, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14012
        /// </remarks>
        [JsonProperty("14012")]
        public double PBRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio");

        /// <summary>
        /// CFOPerShare / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14013
        /// </remarks>
        [JsonProperty("14013")]
        public double CFYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.CFYield");

        /// <summary>
        /// Adjusted close price /Cash Flow Per Share. If the result is negative or zero, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14014
        /// </remarks>
        [JsonProperty("14014")]
        public double PCFRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PCFRatio");

        /// <summary>
        /// FCFPerShare / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14015
        /// </remarks>
        [JsonProperty("14015")]
        public double FCFYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.FCFYield");

        /// <summary>
        /// Adjusted close price/ Free Cash Flow Per Share. If the result is negative or zero, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14016
        /// </remarks>
        [JsonProperty("14016")]
        public double FCFRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.FCFRatio");

        /// <summary>
        /// Dividends Per Share over the trailing 12 months / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14017
        /// </remarks>
        [JsonProperty("14017")]
        public double TrailingDividendYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TrailingDividendYield");

        /// <summary>
        /// (Current Dividend Per Share * Payout Frequency) / Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14018
        /// </remarks>
        [JsonProperty("14018")]
        public double ForwardDividendYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardDividendYield");

        /// <summary>
        /// Estimated Earnings Per Share / Price Note: a) The "Next" Year's EPS Estimate is used; For instance, if today's actual date is March 1, 2009, the "Current" EPS Estimate for MSFT is June 2009, and the "Next" EPS Estimate for MSFT is June 2010; the latter is used. b) The eps estimated data is sourced from a third party.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14019
        /// </remarks>
        [JsonProperty("14019")]
        public double ForwardEarningYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardEarningYield");

        /// <summary>
        /// 1 / ForwardEarningYield If result is negative, then null
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14020
        /// </remarks>
        [JsonProperty("14020")]
        public double ForwardPERatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardPERatio");

        /// <summary>
        /// ForwardPERatio / Long-term Average Earning Growth Rate
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14021
        /// </remarks>
        [JsonProperty("14021")]
        public double PEGRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PEGRatio");

        /// <summary>
        /// The number of years it would take for a company's cumulative earnings to equal the stock's current trading price, assuming that the company continues to increase its annual earnings at the growth rate used to calculate the PEG ratio. [ Log (PG/E + 1) / Log (1 + G) ] - 1 Where P=Price E=Next Fiscal Year's Estimated EPS G=Long-term Average Earning Growth
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14022
        /// </remarks>
        [JsonProperty("14022")]
        public double PEGPayback => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PEGPayback");

        /// <summary>
        /// The company's total book value less the value of any intangible assets dividend by number of shares.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14023
        /// </remarks>
        [JsonProperty("14023")]
        public double TangibleBookValuePerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TangibleBookValuePerShare");

        /// <summary>
        /// The three year average for tangible book value per share.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14024
        /// </remarks>
        [JsonProperty("14024")]
        public double TangibleBVPerShare3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TangibleBVPerShare3YrAvg");

        /// <summary>
        /// The five year average for tangible book value per share.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14025
        /// </remarks>
        [JsonProperty("14025")]
        public double TangibleBVPerShare5YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TangibleBVPerShare5YrAvg");

        /// <summary>
        /// Latest Dividend * Frequency
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14026
        /// </remarks>
        [JsonProperty("14026")]
        public double ForwardDividend => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardDividend");

        /// <summary>
        /// (Current Assets - Current Liabilities)/number of shares
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14027
        /// </remarks>
        [JsonProperty("14027")]
        public double WorkingCapitalPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.WorkingCapitalPerShare");

        /// <summary>
        /// The three year average for working capital per share.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14028
        /// </remarks>
        [JsonProperty("14028")]
        public double WorkingCapitalPerShare3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.WorkingCapitalPerShare3YrAvg");

        /// <summary>
        /// The five year average for working capital per share.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14029
        /// </remarks>
        [JsonProperty("14029")]
        public double WorkingCapitalPerShare5YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.WorkingCapitalPerShare5YrAvg");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of EBITDA generated.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14030
        /// </remarks>
        [JsonProperty("14030")]
        public double EVToEBITDA => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA");

        /// <summary>
        /// The net repurchase of shares outstanding over the market capital of the company. It is a measure of shareholder return.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14031
        /// </remarks>
        [JsonProperty("14031")]
        public double BuyBackYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.BuyBackYield");

        /// <summary>
        /// The total yield that shareholders can expect, by summing Dividend Yield and Buyback Yield.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14032
        /// </remarks>
        [JsonProperty("14032")]
        public double TotalYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TotalYield");

        /// <summary>
        /// The five-year average of the company's price-to-earnings ratio.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14033
        /// </remarks>
        [JsonProperty("14033")]
        public double RatioPE5YearAverage => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.RatioPE5YearAverage");

        /// <summary>
        /// Price change this month, expressed as latest price/last month end price.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14034
        /// </remarks>
        [JsonProperty("14034")]
        public double PriceChange1M => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PriceChange1M");

        /// <summary>
        /// Adjusted Close Price/ Normalized EPS. Normalized EPS removes onetime and unusual items from net EPS, to provide investors with a more accurate measure of the company's true earnings. If the result is negative, zero, &gt;10,000 or &lt;0.001, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14035
        /// </remarks>
        [JsonProperty("14035")]
        public double NormalizedPERatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.NormalizedPERatio");

        /// <summary>
        /// Adjusted close price/EBITDA Per Share. If the result is negative or zero, then null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14036
        /// </remarks>
        [JsonProperty("14036")]
        public double PricetoEBITDA => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PricetoEBITDA");

        /// <summary>
        /// Average of the last 60 monthly observations of trailing dividend yield in the last 5 years.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14037
        /// </remarks>
        [JsonProperty("14037")]
        public double DivYield5Year => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.DivYield5Year");

        /// <summary>
        /// Estimated EPS/Book Value Per Share
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14038
        /// </remarks>
        [JsonProperty("14038")]
        public double ForwardROE => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardROE");

        /// <summary>
        /// Estimated EPS/Total Assets Per Share
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14039
        /// </remarks>
        [JsonProperty("14039")]
        public double ForwardROA => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ForwardROA");

        /// <summary>
        /// 2 Years Forward Estimated EPS / Adjusted Close Price
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14040
        /// </remarks>
        [JsonProperty("14040")]
        public double TwoYearsForwardEarningYield => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TwoYearsForwardEarningYield");

        /// <summary>
        /// Adjusted Close Price/2 Years Forward Estimated EPS
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14041
        /// </remarks>
        [JsonProperty("14041")]
        public double TwoYearsForwardPERatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TwoYearsForwardPERatio");

        /// <summary>
        /// Indicates the method used to calculate Forward Dividend. There are three options: Annual, Look-back and Manual.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14042
        /// </remarks>
        [JsonProperty("14042")]
        public string ForwardCalculationStyle => FundamentalService.Get<string>(_time, _securityIdentifier, "ValuationRatios.ForwardCalculationStyle");

        /// <summary>
        /// Used to collect the forward dividend for companies where our formula will not produce the correct value.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14043
        /// </remarks>
        [JsonProperty("14043")]
        public double ActualForwardDividend => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ActualForwardDividend");

        /// <summary>
        /// Indicates the method used to calculate Trailing Dividend. There are two options: Look-back and Manual.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14044
        /// </remarks>
        [JsonProperty("14044")]
        public string TrailingCalculationStyle => FundamentalService.Get<string>(_time, _securityIdentifier, "ValuationRatios.TrailingCalculationStyle");

        /// <summary>
        /// Used to collect the trailing dividend for companies where our formula will not produce the correct value.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14045
        /// </remarks>
        [JsonProperty("14045")]
        public double ActualTrailingDividend => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ActualTrailingDividend");

        /// <summary>
        /// Total Assets / Diluted Shares Outstanding
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14046
        /// </remarks>
        [JsonProperty("14046")]
        public double TotalAssetPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TotalAssetPerShare");

        /// <summary>
        /// The growth rate from the TrailingDividend to the Forward Dividend: {(Forward Dividend/Trailing Dividend) - 1}*100.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14047
        /// </remarks>
        [JsonProperty("14047")]
        public double ExpectedDividendGrowthRate => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.ExpectedDividendGrowthRate");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of revenue generated.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14048
        /// </remarks>
        [JsonProperty("14048")]
        public double EVtoRevenue => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVtoRevenue");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of Pretax Income generated.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14049
        /// </remarks>
        [JsonProperty("14049")]
        public double EVtoPreTaxIncome => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVtoPreTaxIncome");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of asset value; should be the default EV multiple used in an asset driven business.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14050
        /// </remarks>
        [JsonProperty("14050")]
        public double EVtoTotalAssets => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVtoTotalAssets");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of free cash flow generated.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14051
        /// </remarks>
        [JsonProperty("14051")]
        public double EVtoFCF => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVtoFCF");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of EBIT generated.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14052
        /// </remarks>
        [JsonProperty("14052")]
        public double EVtoEBIT => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVtoEBIT");

        /// <summary>
        /// Funds from operations per share; populated only for real estate investment trusts (REITs), defined as the sum of net income, gain/loss (realized and unrealized) on investment securities, asset impairment charge, depreciation and amortization and gain/ loss on the sale of business and property plant and equipment, divided by shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14053
        /// </remarks>
        [JsonProperty("14053")]
        public double FFOPerShare => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.FFOPerShare");

        /// <summary>
        /// The ratio of a stock's price to its cash flow per share.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14054
        /// </remarks>
        [JsonProperty("14054")]
        public double PricetoCashRatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PricetoCashRatio");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of estimated EBITDA.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14055
        /// </remarks>
        [JsonProperty("14055")]
        public double EVToForwardEBITDA => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToForwardEBITDA");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of estimated revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14056
        /// </remarks>
        [JsonProperty("14056")]
        public double EVToForwardRevenue => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToForwardRevenue");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of estimated EBIT.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14057
        /// </remarks>
        [JsonProperty("14057")]
        public double EVToForwardEBIT => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToForwardEBIT");

        /// <summary>
        /// The one-year growth in the company's EV to EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14058
        /// </remarks>
        [JsonProperty("14058")]
        public double EVToEBITDA1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's EV to free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash flow from operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14059
        /// </remarks>
        [JsonProperty("14059")]
        public double EVToFCF1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's EV to revenue on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14060
        /// </remarks>
        [JsonProperty("14060")]
        public double EVToRevenue1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's EV to total assets on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by total assets reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14061
        /// </remarks>
        [JsonProperty("14061")]
        public double EVToTotalAssets1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's price to free cash flow ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the free cash flow reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14062
        /// </remarks>
        [JsonProperty("14062")]
        public double PFCFRatio1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's price to book ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14063
        /// </remarks>
        [JsonProperty("14063")]
        public double PBRatio1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's PE ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14064
        /// </remarks>
        [JsonProperty("14064")]
        public double PERatio1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio1YearGrowth");

        /// <summary>
        /// The one-year growth in the company's price to sales ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the sales per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14065
        /// </remarks>
        [JsonProperty("14065")]
        public double PSRatio1YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio1YearGrowth");

        /// <summary>
        /// The three-year average for a company's EV to EBIT ratio: EV (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBIT (earnings minus expenses excluding interest and tax expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14066
        /// </remarks>
        [JsonProperty("14066")]
        public double EVToEBIT3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBIT3YrAvg");

        /// <summary>
        /// The three-year average for a company's EV to EBITDA ratio: EV (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14067
        /// </remarks>
        [JsonProperty("14067")]
        public double EVToEBITDA3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA3YrAvg");

        /// <summary>
        /// The three-year average for a company's EV to free cash flow ratio: EV (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash Flow from Operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14068
        /// </remarks>
        [JsonProperty("14068")]
        public double EVToFCF3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF3YrAvg");

        /// <summary>
        /// The three-year average for a company's EV to revenue ratio: EV (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14069
        /// </remarks>
        [JsonProperty("14069")]
        public double EVToRevenue3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue3YrAvg");

        /// <summary>
        /// The three-year average for a company's EV to total assets ratio: EV (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Assets reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14070
        /// </remarks>
        [JsonProperty("14070")]
        public double EVToTotalAssets3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets3YrAvg");

        /// <summary>
        /// The growth in the three-year average for a company's EV to EBIT ratio. Morningstar calculates the growth percentage based on the EV to EBIT ratio ((Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBIT (earnings minus expenses excluding interest and tax expenses) reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14071
        /// </remarks>
        [JsonProperty("14071")]
        public double EVToEBIT3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBIT3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's EV to EBITDA ratio. Morningstar calculates the growth percentage based on the EV to EBITDA ratio ((Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax depreciation and amortization expenses) reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14072
        /// </remarks>
        [JsonProperty("14072")]
        public double EVToEBITDA3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's EV to free cash flow ratio. Morningstar calculates the growth percentage based on the EV to free cash flow ratio ((Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash Flow from Operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14073
        /// </remarks>
        [JsonProperty("14073")]
        public double EVToFCF3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's EV to revenue ratio. Morningstar calculates the growth percentage based on the EV to revenue ratio ((Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14074
        /// </remarks>
        [JsonProperty("14074")]
        public double EVToRevenue3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's EV to total assets ratio. Morningstar calculates the growth percentage based on the EV to total assets ratio ((Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by total assets reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14075
        /// </remarks>
        [JsonProperty("14075")]
        public double EVToTotalAssets3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets3YrAvgChange");

        /// <summary>
        /// The three-year average for a company's price to free cash flow ratio (the adjusted close price divided by the free cash flow per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14076
        /// </remarks>
        [JsonProperty("14076")]
        public double PFCFRatio3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio3YrAvg");

        /// <summary>
        /// The three-year average for a company's price to book ratio (the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14077
        /// </remarks>
        [JsonProperty("14077")]
        public double PBRatio3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio3YrAvg");

        /// <summary>
        /// The three-year average for a company's price to sales ratio (the adjusted close price divided by the total sales per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14078
        /// </remarks>
        [JsonProperty("14078")]
        public double PSRatio3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio3YrAvg");

        /// <summary>
        /// The three-year average for a company's price to cash ratio (the adjusted close price divided by the cash flow per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14079
        /// </remarks>
        [JsonProperty("14079")]
        public double PCashRatio3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PCashRatio3YrAvg");

        /// <summary>
        /// The three-year average for a company's PE ratio (the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14080
        /// </remarks>
        [JsonProperty("14080")]
        public double PERatio3YrAvg => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio3YrAvg");

        /// <summary>
        /// The growth in the three-year average for a company's price to free cash flow ratio. Morningstar calculates the growth percentage based on the adjusted close price divided by the free cash flow per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14081
        /// </remarks>
        [JsonProperty("14081")]
        public double PFCFRatio3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's price to book ratio. Morningstar calculates the growth percentage based on the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14082
        /// </remarks>
        [JsonProperty("14082")]
        public double PBRatio3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's price to sales ratio. Morningstar calculates the growth percentage based on the adjusted close price divided by the total sales per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14083
        /// </remarks>
        [JsonProperty("14083")]
        public double PSRatio3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio3YrAvgChange");

        /// <summary>
        /// The growth in the three-year average for a company's PE ratio. Morningstar calculates the growth percentage based on the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14084
        /// </remarks>
        [JsonProperty("14084")]
        public double PERatio3YrAvgChange => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio3YrAvgChange");

        /// <summary>
        /// The one-year high for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14085
        /// </remarks>
        [JsonProperty("14085")]
        public double PERatio1YearHigh => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio1YearHigh");

        /// <summary>
        /// The one-year low for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14086
        /// </remarks>
        [JsonProperty("14086")]
        public double PERatio1YearLow => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio1YearLow");

        /// <summary>
        /// The one-year average for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14087
        /// </remarks>
        [JsonProperty("14087")]
        public double PERatio1YearAverage => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio1YearAverage");

        /// <summary>
        /// The five-year high for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14088
        /// </remarks>
        [JsonProperty("14088")]
        public double PERatio5YearHigh => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio5YearHigh");

        /// <summary>
        /// The five-year low for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14089
        /// </remarks>
        [JsonProperty("14089")]
        public double PERatio5YearLow => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio5YearLow");

        /// <summary>
        /// The five-year average for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14090
        /// </remarks>
        [JsonProperty("14090")]
        public double PERatio5YearAverage => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio5YearAverage");

        /// <summary>
        /// The ten-year high for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14091
        /// </remarks>
        [JsonProperty("14091")]
        public double PERatio10YearHigh => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio10YearHigh");

        /// <summary>
        /// The ten-year low for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14092
        /// </remarks>
        [JsonProperty("14092")]
        public double PERatio10YearLow => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio10YearLow");

        /// <summary>
        /// The ten-year average for a company's PE ratio (adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14093
        /// </remarks>
        [JsonProperty("14093")]
        public double PERatio10YearAverage => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio10YearAverage");

        /// <summary>
        /// The cyclically adjusted PE ratio for a company; adjusted close price divided by earnings per share. If the result is negative, zero, &gt;10,000 or &lt;0.001, then null. Morningstar uses the CPI index for US companies and Indexes from the World Bank for the rest of the global markets.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14094
        /// </remarks>
        [JsonProperty("14094")]
        public double CAPERatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.CAPERatio");

        /// <summary>
        /// The three-year growth in the company's EV to EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14095
        /// </remarks>
        [JsonProperty("14095")]
        public double EVToEBITDA3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's EV to free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash flow from operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14096
        /// </remarks>
        [JsonProperty("14096")]
        public double EVToFCF3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's EV to revenue on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14097
        /// </remarks>
        [JsonProperty("14097")]
        public double EVToRevenue3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's EV to total assets on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by total assets reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14098
        /// </remarks>
        [JsonProperty("14098")]
        public double EVToTotalAssets3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's price to free cash flow ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the free cash flow reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14099
        /// </remarks>
        [JsonProperty("14099")]
        public double PFCFRatio3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's price to book ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14100
        /// </remarks>
        [JsonProperty("14100")]
        public double PBRatio3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's PE ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14101
        /// </remarks>
        [JsonProperty("14101")]
        public double PERatio3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio3YearGrowth");

        /// <summary>
        /// The three-year growth in the company's price to sales ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the sales per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14102
        /// </remarks>
        [JsonProperty("14102")]
        public double PSRatio3YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio3YearGrowth");

        /// <summary>
        /// The five-year growth in the company's EV to EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14103
        /// </remarks>
        [JsonProperty("14103")]
        public double EVToEBITDA5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's EV to free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash flow from operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14104
        /// </remarks>
        [JsonProperty("14104")]
        public double EVToFCF5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's EV to revenue on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14105
        /// </remarks>
        [JsonProperty("14105")]
        public double EVToRevenue5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's EV to total assets on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by total assets reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14106
        /// </remarks>
        [JsonProperty("14106")]
        public double EVToTotalAssets5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's price to free cash flow ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the free cash flow reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14107
        /// </remarks>
        [JsonProperty("14107")]
        public double PFCFRatio5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's price to book ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14108
        /// </remarks>
        [JsonProperty("14108")]
        public double PBRatio5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's PE ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14109
        /// </remarks>
        [JsonProperty("14109")]
        public double PERatio5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio5YearGrowth");

        /// <summary>
        /// The five-year growth in the company's price to sales ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the sales per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14110
        /// </remarks>
        [JsonProperty("14110")]
        public double PSRatio5YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio5YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's EV to EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by EBITDA (earnings minus expenses excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14111
        /// </remarks>
        [JsonProperty("14111")]
        public double EVToEBITDA10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToEBITDA10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's EV to free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by free cash flow (Cash flow from operations - Capital Expenditures) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14112
        /// </remarks>
        [JsonProperty("14112")]
        public double EVToFCF10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToFCF10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's EV to revenue on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by Total Revenue reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14113
        /// </remarks>
        [JsonProperty("14113")]
        public double EVToRevenue10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToRevenue10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's EV to total assets on a percentage basis. Morningstar calculates the growth percentage based on the enterprise value (Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed) divided by total assets reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14114
        /// </remarks>
        [JsonProperty("14114")]
        public double EVToTotalAssets10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.EVToTotalAssets10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's price to free cash flow ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the free cash flow reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14115
        /// </remarks>
        [JsonProperty("14115")]
        public double PFCFRatio10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PFCFRatio10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's price to book ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the book value per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14116
        /// </remarks>
        [JsonProperty("14116")]
        public double PBRatio10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PBRatio10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's PE ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the earnings per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14117
        /// </remarks>
        [JsonProperty("14117")]
        public double PERatio10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PERatio10YearGrowth");

        /// <summary>
        /// The ten-year growth in the company's price to sales ratio on a percentage basis. Morningstar calculates the growth percentage based on the adjusted close price divided by the sales per share reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14118
        /// </remarks>
        [JsonProperty("14118")]
        public double PSRatio10YearGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.PSRatio10YearGrowth");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of estimated EBIT in year 2.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14119
        /// </remarks>
        [JsonProperty("14119")]
        public double TwoYrsEVToForwardEBIT => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TwoYrsEVToForwardEBIT");

        /// <summary>
        /// Indicates what is a company being valued per each dollar of estimated EBITDA in year 2.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14120
        /// </remarks>
        [JsonProperty("14120")]
        public double TwoYrsEVToForwardEBITDA => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.TwoYrsEVToForwardEBITDA");

        /// <summary>
        /// EPS Growth Ratio: (Estimated EPS Year 1) / (TTM Normalized diluted EPS
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14121
        /// </remarks>
        [JsonProperty("14121")]
        public double FirstYearEstimatedEPSGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.FirstYearEstimatedEPSGrowth");

        /// <summary>
        /// EPS Growth Ratio: (Estimated EPS Year 2) / (Estimated EPS Year 1)
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14122
        /// </remarks>
        [JsonProperty("14122")]
        public double SecondYearEstimatedEPSGrowth => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.SecondYearEstimatedEPSGrowth");

        /// <summary>
        /// Normalized ForwardPERatio / Long-term Average Normalized Earnings Growth Rate
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 14123
        /// </remarks>
        [JsonProperty("14123")]
        public double NormalizedPEGatio => FundamentalService.Get<double>(_time, _securityIdentifier, "ValuationRatios.NormalizedPEGatio");

        private readonly DateTime _time;
        private readonly SecurityIdentifier _securityIdentifier;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public ValuationRatios(DateTime time, SecurityIdentifier securityIdentifier)
        {
            _time = time;
            _securityIdentifier = securityIdentifier;
        }
    }
}
