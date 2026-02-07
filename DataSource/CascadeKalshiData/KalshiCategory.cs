/*
 * Cascade Labs - Kalshi Category Enum
 * Known market categories for filtering
 */

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Known Kalshi market categories for filtering
    /// </summary>
    public enum KalshiCategory
    {
        /// <summary>Weather-related markets (temperature, precipitation, etc.)</summary>
        Weather,

        /// <summary>Financial markets (S&P 500, NASDAQ, etc.)</summary>
        Finance,

        /// <summary>Political markets (elections, policy, etc.)</summary>
        Politics,

        /// <summary>Economic indicator markets (CPI, GDP, etc.)</summary>
        Economics,

        /// <summary>Sports-related markets</summary>
        Sports,

        /// <summary>Entertainment markets (awards, shows, etc.)</summary>
        Entertainment,

        /// <summary>Science-related markets</summary>
        Science,

        /// <summary>Technology markets</summary>
        Technology,

        /// <summary>Climate-related markets</summary>
        Climate
    }
}
