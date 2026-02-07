/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ContractSecurityType
{
    [EnumMember(Value = "OPTION")]
    Option = 0,

    [EnumMember(Value = "STOCK")]
    Equity = 1,

    [EnumMember(Value = "INDEX")]
    Index = 2
}
