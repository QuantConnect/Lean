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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Model for a TradierUser returned from the API.
    /// </summary>
    public class TradierUserContainer
    {
        /// User Profile Contents
        [JsonProperty(PropertyName = "profile")]
        public TradierUser Profile;

        /// Constructor: Create user from tradier data.
        public TradierUserContainer()
        { }
    }

    /// <summary>
    /// User profile array:
    /// </summary>
    public class TradierUser
    {
        /// Unique brokerage user id.
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// Name of user:
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// Array of user account details:
        [JsonProperty(PropertyName = "account")]
        [JsonConverter(typeof(SingleValueListConverter<TradierUserAccount>))]
        public List<TradierUserAccount> Accounts { get; set; }

        /// Empty Constructor
        public TradierUser() 
        {
            Id = "";
            Name = "";
            Accounts = new List<TradierUserAccount>();
        }
    }

    /// <summary>
    /// Account only settings for a tradier user:
    /// </summary>
    public class TradierUserAccount 
    {
        /// Users account number
        [JsonProperty(PropertyName = "account_number")]
        public long AccountNumber { get; set; }

        /// Pattern Trader:
        [JsonProperty(PropertyName = "day_trader")]
        public bool DayTrader { get; set; }

        /// Options level permissions on account.
        [JsonProperty(PropertyName = "option_level")]
        public int OptionLevel { get; set; }

        /// Cash or Margin Account:
        [JsonProperty(PropertyName = "type")]
        public TradierAccountType Type { get; set; }

        /// Date time of the last update:
        [JsonProperty(PropertyName = "last_update_date")]
        public DateTime LastUpdated { get; set; }

        /// Status of the users account:
        [JsonProperty(PropertyName = "status")]
        public TradierAccountStatus Status { get; set; }

        /// Type of user account
        [JsonProperty(PropertyName = "classification")]
        public TradierAccountClassification Classification { get; set; }

        /// <summary>
        /// Create a new account:
        /// </summary>
        public TradierUserAccount() 
        {
            AccountNumber = 0;
            DayTrader = false;
            OptionLevel = 1;
            Type = TradierAccountType.Cash;
            LastUpdated = new DateTime();
            Status = TradierAccountStatus.Closed;
            Classification = TradierAccountClassification.Individual;
        }
    }

}
