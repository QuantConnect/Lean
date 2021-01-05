/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class JsonAccountConfiguration : IAccountConfiguration
    {
        [JsonProperty(PropertyName = "dtbp_check", Required = Required.Always)]
        public DayTradeMarginCallProtection DayTradeMarginCallProtection { get; set; }

        [JsonProperty(PropertyName = "trade_confirm_email", Required = Required.Always)]
        public TradeConfirmEmail TradeConfirmEmail { get; set; }

        [JsonProperty(PropertyName = "suspend_trade", Required = Required.Always)]
        public Boolean IsSuspendTrade { get; set; }

        [JsonProperty(PropertyName = "no_shorting", Required = Required.Always)]
        public Boolean IsNoShorting { get; set; }
    }
}
