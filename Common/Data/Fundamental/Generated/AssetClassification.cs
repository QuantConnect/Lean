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
using Python.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the AssetClassification class
    /// </summary>
    public class AssetClassification : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The purpose of the Stock Types is to group companies according to the underlying fundamentals of their business. They answer the question: If I buy this stock, what kind of company am I buying? Unlike the style box, the emphasis with the Stock Types is on income statement, balance sheet, and cash-flow data-not price data or valuation multiples. We focus on the company, not the stock. Morningstar calculates this figure in-house on a monthly basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3000
        /// </remarks>
        [JsonProperty("3000")]
        public int StockType => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_StockType);

        /// <summary>
        /// The Morningstar Equity Style Box is a grid that provides a graphical representation of the investment style of stocks and portfolios. It classifies securities according to market capitalization (the vertical axis) and value-growth scores (the horizontal axis) and allows us to provide analysis on a 5-by-5 Style Box as well as providing the traditional style box assignment, which is the basis for the Morningstar Category. Two of the style categories, value and growth, are common to both stocks and portfolios. However, for stocks, the central column of the style box represents the core style (those stocks for which neither value nor growth characteristics dominate); for portfolios, it represents the blend style.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3001
        /// </remarks>
        [JsonProperty("3001")]
        public int StyleBox => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_StyleBox);

        /// <summary>
        /// The growth grade is based on the trend in revenue per share using data from the past five years. For the purpose of calculating revenue per share we use the past five years' revenue figures and corresponding year-end fully diluted shares outstanding; if year- end fully diluted shares outstanding is not available, we calculate this figure by dividing the company's reported net income applicable to common shareholders by the reported fully diluted earnings per share. A company must have a minimum of four consecutive years of positive and non-zero revenue, including the latest fiscal year, to qualify for a grade. In calculating the revenue per share growth rate, we calculate the slope of the regression line of historical revenue per share. We then divide the slope of the regression line by the arithmetic average of historical revenue per share figures. The result of the regression is a normalized historical increase or decrease in the rate of growth for sales per share. We then calculate a z-score by subtracting the universe mean revenue growth from the company's revenue growth, and dividing by the standard deviation of the universe's growth rates. Stocks are sorted based on the z-score of their revenue per share growth rate calculated above, from the most negative z-score to the most positive z-score. Stocks are then ranked based on their z-score from 1 to the total number of qualified stocks. We assign grades based on this ranking. Stocks are assigned A, B, C, D, or F. Morningstar calculates this figure in-house on a monthly basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3002
        /// </remarks>
        [JsonProperty("3002")]
        public string GrowthGrade => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_GrowthGrade);

        /// <summary>
        /// Instead of using accounting-based ratios to formulate a measure to reflect the financial health of a firm, we use structural or contingent claim models. Structural models take advantage of both market information and accounting financial information. The firm's equity in such models is viewed as a call option on the value of the firm's assets. If the value of the assets is not sufficient to cover the firm's liabilities (the strike price), default is expected to occur, and the call option expires worthless and the firm is turned over to its creditors. To estimate a distance to default, the value of the firm's liabilities is obtained from the firm's latest balance sheet and incorporated into the model. We then rank the calculated distance to default and award 10% of the universe A's, 20% B's, 40% C's, 20% D's, and 10% F's. Morningstar calculates this figure in-house on a daily basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3003
        /// </remarks>
        [JsonProperty("3003")]
        public string FinancialHealthGrade => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_FinancialHealthGrade);

        /// <summary>
        /// The profitability grade for all qualified companies in Morningstar's stock universe is based on valuation of return on shareholders' equity (ROE) using data from the past five years. Morningstar's universe of stocks is first filtered for adequacy of historical ROE figures. Companies with less than four years of consecutive ROE figures including the ROE figure for the latest fiscal year are tossed from calculations and are assigned "--" for the profitability grade. For the remaining qualified universe of stocks the profitability grade is based on the valuation of the following three components, which are assigned different weights; the historical growth rate of ROE, the average level of historical ROE, the level of ROE in the latest fiscal year of the company. Stocks are assigned A, B, C, D, or F. Morningstar calculates this figure in-house on a monthly basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3004
        /// </remarks>
        [JsonProperty("3004")]
        public string ProfitabilityGrade => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_ProfitabilityGrade);

        /// <summary>
        /// Equities are mapped into one of 148 industries, the one which most accurately reflects the underlying business of that company. This mapping is based on publicly available information about each company and uses annual reports, Form 10-Ks and Morningstar Equity Analyst input as its primary source. Other secondary sources of information may include company web sites, sell-side research (if available) and trade publications. By and large, equities are mapped into the industries that best reflect each company's largest source of revenue and income. If the company has more than three sources of revenue and income and there is no clear dominant revenue and income stream, the company is assigned to the Conglomerates industry. Based on Morningstar analyst research or other third party information, Morningstar may change industry assignments to more accurately reflect the changing businesses of companies.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3005
        /// </remarks>
        [JsonProperty("3005")]
        public int MorningstarIndustryCode => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_MorningstarIndustryCode);

        /// <summary>
        /// Industries are mapped into 69 industry groups based on their common operational characteristics. If a particular industry has unique operating characteristics-or simply lacks commonality with other industries-it would map into its own group. However, any industry group containing just one single industry does not necessarily imply that that industry is dominant or otherwise important. The assignment simply reflects the lack of a sufficient amount of shared traits among industries. See appendix for mappings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3006
        /// </remarks>
        [JsonProperty("3006")]
        public int MorningstarIndustryGroupCode => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_MorningstarIndustryGroupCode);

        /// <summary>
        /// Industry groups are consolidated into 11 sectors. See appendix for mappings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3007
        /// </remarks>
        [JsonProperty("3007")]
        public int MorningstarSectorCode => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_MorningstarSectorCode);

        /// <summary>
        /// Sectors are consolidated into three major economic spheres or Super Sectors: Cyclical, Defensive and Sensitive. See appendix for mappings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3008
        /// </remarks>
        [JsonProperty("3008")]
        public int MorningstarEconomySphereCode => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_MorningstarEconomySphereCode);

        /// <summary>
        /// Standard Industrial Classification System (SIC) is a system for classifying a business according to economic activity. See separate reference document for a list of Sic Codes/Mappings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3009
        /// </remarks>
        [JsonProperty("3009")]
        public int SIC => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_SIC);

        /// <summary>
        /// An acronym for North American Industry Classification System, it is a 6 digit numerical classification assigned to individual companies. Developed jointly by the U.S., Canada, and Mexico to provide new comparability in statistics about business activity across North America. It is intended to replace the U.S. Standard Industrial Classification (SIC) system. See separate reference document for a list of NAICS Codes/Mappings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3010
        /// </remarks>
        [JsonProperty("3010")]
        public int NAICS => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_NAICS);

        /// <summary>
        /// The scores for a stock's value and growth characteristics determine its horizontal placement. The Value-Growth Score is a reflection of the aggregate expectations of market participants for the future growth and required rate of return for a stock. We infer these expectations from the relation between current market prices and future growth and cost of capital expectations under the assumption of rational market participants and a simple model of stock value.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3011
        /// </remarks>
        [JsonProperty("3011")]
        public double StyleScore => FundamentalService.Get<double>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_StyleScore);

        /// <summary>
        /// Rather than a fixed number of large cap or small cap stocks, Morningstar uses a flexible system that isn't adversely affected by overall movements in the market. The Morningstar stock universe represents approximately 99% of the U.S. market for actively traded stocks. Giant-cap stocks are defined as the group that accounts for the top 40% of the capitalization of the Morningstar domestic stock universe; large-cap stocks represent the next 30%; mid-cap stocks represent the next 20%; small-cap stocks represent the next 7%; and micro-cap stocks represent the remaining 3%. Each stock is given a Size Score that ranges from -100 (very micro) to 400 (very giant). When classifying stocks to a Style Box, giant is included in large and micro is included in small.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3012
        /// </remarks>
        [JsonProperty("3012")]
        public double SizeScore => FundamentalService.Get<double>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_SizeScore);

        /// <summary>
        /// A high overall growth score indicates that a stock's per-share earnings, book value, revenues, and cash flow are expected to grow quickly relative to other stocks in the same scoring group. A weak growth orientation does not necessarily mean that a stock has a strong value orientation.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3013
        /// </remarks>
        [JsonProperty("3013")]
        public double GrowthScore => FundamentalService.Get<double>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_GrowthScore);

        /// <summary>
        /// A high value score indicates that a stock's price is relatively low, given the anticipated per-sharing earnings, book value, revenues, cash flow, and dividends that the stock provides to investors. A high price relative to these measures indicates that a stock's value orientation is weak, but it does not necessarily mean that the stock is growth-oriented.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3014
        /// </remarks>
        [JsonProperty("3014")]
        public double ValueScore => FundamentalService.Get<double>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_ValueScore);

        /// <summary>
        /// NACE is a European standard classification of economic activities maintained by Eurostat.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3015
        /// </remarks>
        [JsonProperty("3015")]
        public double NACE => FundamentalService.Get<double>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_NACE);

        /// <summary>
        /// Similar to NAICS (data point 3010, above), this is specifically for Canadian classifications. An acronym for North American Industry Classification System, it is a 6 digit numerical classification assigned to individual companies. Developed jointly by the U.S., Canada, and Mexico to provide new comparability in statistics about business activity across North America. It is intended to replace the U.S. Standard Industrial Classification (SIC) system. See separate reference document for a list of NAICS Codes/Mappings. The initial SIC and NAICS listed is the Primary based on revenue generation; followed by Secondary SIC and NAICS when applicable. Both SIC and NAICS are manually collected and assigned.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 3016
        /// </remarks>
        [JsonProperty("3016")]
        public int CANNAICS => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.AssetClassification_CANNAICS);

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public AssetClassification(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new AssetClassification(timeProvider, _securityIdentifier);
        }
    }
}
