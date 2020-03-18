/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Types of account activities
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AccountActivityType
    {
        /// <summary>
        /// Order fills (both partial and full fills)
        /// </summary>
        [EnumMember(Value = "FILL")]
        Fill,

        /// <summary>
        /// Cash transactions (both CSD and CSR)
        /// </summary>
        [EnumMember(Value = "TRANS")]
        Transaction,

        /// <summary>
        /// Miscellaneous or rarely used activity types (All types except those in TRANS, DIV, or FILL)
        /// </summary>
        [EnumMember(Value = "MISC")]
        Miscellaneous,

        /// <summary>
        /// ACATS IN/OUT (Cash)
        /// </summary>
        [EnumMember(Value = "ACATC")]
        ACATCash,

        /// <summary>
        /// ACATS IN/OUT (Securities)
        /// </summary>
        [EnumMember(Value = "ACATS")]
        ACATSecurities,

        /// <summary>
        /// Cash disbursement(+)
        /// </summary>
        [EnumMember(Value = "CSD")]
        CashDisbursement,

        /// <summary>
        /// Cash receipt(-)
        /// </summary>
        [EnumMember(Value = "CSR")]
        CashReceipt,

        /// <summary>
        /// Dividends
        /// </summary>
        [EnumMember(Value = "DIV")]
        Dividend,

        /// <summary>
        /// Dividend (capital gains long term)
        /// </summary>
        [EnumMember(Value = "DIVCGL")]
        DividendCapitalGainsLongTerm,

        /// <summary>
        /// Dividend (capital gain short term)
        /// </summary>
        [EnumMember(Value = "DIVCGS")]
        DividendCapitalGainsShortTerm,

        /// <summary>
        /// Dividend fee
        /// </summary>
        [EnumMember(Value = "DIVFEE")]
        DividendFee,

        /// <summary>
        /// Dividend adjusted (Foreign Tax Withheld)
        /// </summary>
        [EnumMember(Value = "DIVFT")]
        DividendForeignTaxWithheld,

        /// <summary>
        /// Dividend adjusted (NRA Withheld)
        /// </summary>
        [EnumMember(Value = "DIVNRA")]
        DividendNRAWithheld,

        /// <summary>
        /// Dividend return of capital
        /// </summary>
        [EnumMember(Value = "DIVROC")]
        DividendReturnOfCapital,

        /// <summary>
        /// Dividend adjusted (Tefra Withheld)
        /// </summary>
        [EnumMember(Value = "DIVTW")]
        DividendTefraWithheld,

        /// <summary>
        /// Dividend (tax exempt)
        /// </summary>
        [EnumMember(Value = "DIVTXEX")]
        DividendTaxExempt,

        /// <summary>
        /// Interest (credit/margin)
        /// </summary>
        [EnumMember(Value = "INT")]
        Interest,

        /// <summary>
        /// Interest adjusted (NRA Withheld)
        /// </summary>
        [EnumMember(Value = "INTNRA")]
        InterestNRAWithheld,

        /// <summary>
        /// Interest adjusted (Tefra Withheld)
        /// </summary>
        [EnumMember(Value = "INTTW")]
        InterestTefraWithheld,

        /// <summary>
        /// Journal entry
        /// </summary>
        [EnumMember(Value = "JNL")]
        JournalEntry,

        /// <summary>
        /// Journal entry (cash)
        /// </summary>
        [EnumMember(Value = "JNLC")]
        JournalEntryCash,

        /// <summary>
        /// Journal entry (stock)
        /// </summary>
        [EnumMember(Value = "JNLS")]
        JournalEntryStock,

        /// <summary>
        /// Merger/Acquisition
        /// </summary>
        [EnumMember(Value = "MA")]
        MergerAcquisition,

        /// <summary>
        /// Name change
        /// </summary>
        [EnumMember(Value = "NC")]
        NameChange,

        /// <summary>
        /// Pass Thru Charge
        /// </summary>
        [EnumMember(Value = "PTC")]
        PassThruCharge,

        /// <summary>
        /// Pass Thru Rebate
        /// </summary>
        [EnumMember(Value = "PTR")]
        PassThruRebate,

        /// <summary>
        /// Reorganization
        /// </summary>
        [EnumMember(Value = "REORG")]
        Reorg,

        /// <summary>
        /// Symbol change
        /// </summary>
        [EnumMember(Value = "SC")]
        SymbolChange,

        /// <summary>
        /// Stock spinoff
        /// </summary>
        [EnumMember(Value = "SSO")]
        StockSpinoff,

        /// <summary>
        /// Stock split
        /// </summary>
        [EnumMember(Value = "SSP")]
        StockSplit,
    }
}
