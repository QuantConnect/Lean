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
 *
*/
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis.Utils
{
    /// <summary>
    /// Maps LEAN / QCAlgorithm member names to their Python or C# spelling.
    /// Mirrors the <c>code_by_language.py</c> dictionaries.
    /// </summary>
    public static class CodeByLanguage
    {
        // ── QCAlgorithm ──────────────────────────────────────────────────────────

        public static readonly Dictionary<Language, string> Initialize = new()
        {
            [Language.Python] = "initialize",
            [Language.CSharp] = "Initialize",
        };

        public static readonly Dictionary<Language, string> PostInitialize = new()
        {
            [Language.Python] = "post_initialize",
            [Language.CSharp] = "PostInitialize",
        };

        public static readonly Dictionary<Language, string> SetHoldings = new()
        {
            [Language.Python] = "set_holdings",
            [Language.CSharp] = "SetHoldings",
        };

        public static readonly Dictionary<Language, string> CalculateOrderQuantity = new()
        {
            [Language.Python] = "calculate_order_quantity",
            [Language.CSharp] = "CalculateOrderQuantity",
        };

        public static readonly Dictionary<Language, string> MarketOrder = new()
        {
            [Language.Python] = "market_order",
            [Language.CSharp] = "MarketOrder",
        };

        public static readonly Dictionary<Language, string> IsWarmingUp = new()
        {
            [Language.Python] = "is_warming_up",
            [Language.CSharp] = "IsWarmingUp",
        };

        public static readonly Dictionary<Language, string> OnWarmupFinished = new()
        {
            [Language.Python] = "on_warmup_finished",
            [Language.CSharp] = "OnWarmupFinished",
        };

        public static readonly Dictionary<Language, string> OnMarginCallWarning = new()
        {
            [Language.Python] = "on_margin_call_warning",
            [Language.CSharp] = "OnMarginCallWarning",
        };

        public static readonly Dictionary<Language, string> SetStartDate = new()
        {
            [Language.Python] = "set_start_date",
            [Language.CSharp] = "SetStartDate",
        };

        public static readonly Dictionary<Language, string> SetEndDate = new()
        {
            [Language.Python] = "set_end_date",
            [Language.CSharp] = "SetEndDate",
        };

        public static readonly Dictionary<Language, string> SetCash = new()
        {
            [Language.Python] = "set_cash",
            [Language.CSharp] = "SetCash",
        };

        public static readonly Dictionary<Language, string> Log = new()
        {
            [Language.Python] = "log",
            [Language.CSharp] = "Log",
        };

        public static readonly Dictionary<Language, string> Debug = new()
        {
            [Language.Python] = "debug",
            [Language.CSharp] = "Debug",
        };

        // ── AlgorithmSettings ────────────────────────────────────────────────────

        public static readonly Dictionary<Language, string> DailyPreciseEndTime = new()
        {
            [Language.Python] = "daily_precise_end_time",
            [Language.CSharp] = "DailyPreciseEndTime",
        };

        public static readonly Dictionary<Language, string> FreePortfolioValue = new()
        {
            [Language.Python] = "free_portfolio_value",
            [Language.CSharp] = "FreePortfolioValue",
        };

        public static readonly Dictionary<Language, string> FreePortfolioValuePercentage = new()
        {
            [Language.Python] = "free_portfolio_value_percentage",
            [Language.CSharp] = "FreePortfolioValuePercentage",
        };

        public static readonly Dictionary<Language, string> MinimumOrderMarginPortfolioPercentage = new()
        {
            [Language.Python] = "minimum_order_margin_portfolio_percentage",
            [Language.CSharp] = "MinimumOrderMarginPortfolioPercentage",
        };

        public static readonly Dictionary<Language, string> SeedInitialPrices = new()
        {
            [Language.Python] = "seed_initial_prices",
            [Language.CSharp] = "SeedInitialPrices",
        };

        public static readonly Dictionary<Language, string> StalePriceTimeSpan = new()
        {
            [Language.Python] = "stale_price_time_span",
            [Language.CSharp] = "StalePriceTimeSpan",
        };

        // ── TradierOrderProperties ───────────────────────────────────────────────

        public static readonly Dictionary<Language, string> OutsideRegularTradingHours = new()
        {
            [Language.Python] = "outside_regular_trading_hours",
            [Language.CSharp] = "OutsideRegularTradingHours",
        };
    }
}
