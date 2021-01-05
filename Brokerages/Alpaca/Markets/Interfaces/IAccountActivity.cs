/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates account activity information from Alpaca REST API.
    /// </summary>
    public interface IAccountActivity
    {
        /// <summary>
        /// Activity type.
        /// </summary>
        AccountActivityType ActivityType { get; }

        /// <summary>
        /// An ID for the activity, always in "{date}::{uuid}" format. Can be sent as page_token in requests to facilitate the paging of results.
        /// </summary>
        String ActivityId { get; }

        /// <summary>
        /// An activity timestamp (date and time) from <see cref="ActivityId"/>.
        /// </summary>
        DateTime ActivityDateTime { get; }

        /// <summary>
        /// An activity unique identifier from <see cref="ActivityId"/>.
        /// </summary>
        Guid ActivityGuid { get; }

        /// <summary>
        /// The symbol of the security involved with the activity. Not present for all activity types.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// The date on which the activity occurred or on which the transaction associated with the activity settled.
        /// </summary>
        DateTime? ActivityDate { get; }

        /// <summary>
        /// The net amount of money (positive or negative) associated with the activity.
        /// </summary>
        Decimal? NetAmount { get; }

        /// <summary>
        /// The number of shares that contributed to the transaction. Not present for all activity types.
        /// </summary>
        Int64? Quantity { get; }

        /// <summary>
        /// For dividend activities, the average amount paid per share. Not present for other activity types.
        /// </summary>
        Decimal? PerShareAmount { get; }

        /// <summary>
        /// The cumulative quantity of shares involved in the execution.
        /// </summary>
        Int64? CumulativeQuantity { get; }

        /// <summary>
        /// For partially_filled orders, the quantity of shares that are left to be filled.
        /// </summary>
        Int64? LeavesQuantity { get; }

        /// <summary>
        /// The per-share price that a trade was executed at.
        /// </summary>
        Decimal? Price { get; }

        /// <summary>
        /// The order side of a trade execution.
        /// </summary>
        OrderSide? Side { get; }

        /// <summary>
        /// The time at which an execution occurred.
        /// </summary>
        DateTime? TransactionTime { get; }

        /// <summary>
        /// The type of trade event associated with an execution.
        /// </summary>
        TradeEvent? Type { get; }
    }
}
