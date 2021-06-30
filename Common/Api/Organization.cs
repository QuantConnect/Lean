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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Api.Serialization;

// Collection of response objects for QuantConnect Organization/ endpoints
namespace QuantConnect.Api
{
    /// <summary>
    /// Response wrapper for Organizations/List
    /// TODO: The response objects in the array do not contain all Organization Properties; do we need another wrapper object?
    /// </summary>
    public class OrganizationResponseList : RestResponse
    {
        /// <summary>
        /// List of organizations in the response
        /// </summary>
        [JsonProperty(PropertyName = "organizations")]
        public List<Organization> List { get; set; }
    }

    /// <summary>
    /// Response wrapper for Organizations/Read
    /// </summary>
    public class OrganizationResponse : RestResponse
    {
        /// <summary>
        /// Organization read from the response
        /// </summary>
        [JsonProperty(PropertyName = "organization")]
        public Organization Organization { get; set; }
    }

    /// <summary>
    /// Object representation of Organization from QuantConnect Api
    /// </summary>
    public class Organization
    {
        /// <summary>
        /// Organization ID; Used for API Calls
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Seats in Organization
        /// </summary>
        [JsonProperty(PropertyName = "seats")]
        public int Seats { get; set; }

        /// <summary>
        /// Data Agreement information
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public DataAgreement DataAgreement { get; set; }

        /// <summary>
        /// Organization Product Subscriptions
        /// </summary>
        [JsonProperty(PropertyName = "products")]
        public List<Product> Products { get; set; }

        /// <summary>
        /// Organization Credit Balance and Transactions
        /// </summary>
        [JsonProperty(PropertyName = "credit")]
        public Credit Credit { get; set; }
    }

    /// <summary>
    /// Organization Data Agreement
    /// </summary>
    public class DataAgreement
    {
        /// <summary>
        /// Epoch time the Data Agreement was Signed
        /// </summary>
        [JsonProperty(PropertyName = "signedTime")]
        public long? EpochSignedTime { get; set; }

        /// <summary>
        /// DateTime the agreement was signed.
        /// Uses EpochSignedTime converted to a standard datetime.
        /// </summary>
        public DateTime? SignedTime => EpochSignedTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(EpochSignedTime.Value).DateTime : null;

        /// <summary>
        /// True/False if it is currently signed
        /// </summary>
        [JsonProperty(PropertyName = "current")]
        public bool Signed { get; set; }
    }

    /// <summary>
    /// Organization Credit Object
    /// </summary>
    public class Credit
    {
        /// <summary>
        /// Represents a change in organization credit
        /// </summary>
        public class Movement
        {
            /// <summary>
            /// Date of the change in credit
            /// </summary>
            [JsonProperty(PropertyName = "date")]
            public DateTime Date { get; set; }

            /// <summary>
            /// Credit description
            /// </summary>
            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }

            /// <summary>
            /// Amount of change
            /// </summary>
            [JsonProperty(PropertyName = "amount")]
            public decimal Amount { get; set; }

            /// <summary>
            /// Ending Balance in QCC after Movement
            /// </summary>
            [JsonProperty(PropertyName = "balance")]
            public decimal Balance { get; set; }
        }

        /// <summary>
        /// QCC Current Balance
        /// </summary>
        [JsonProperty(PropertyName = "balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// List of changes to Credit
        /// </summary>
        [JsonProperty(PropertyName = "movements")]
        public List<Movement> Movements { get; set; }
    }

    /// <summary>
    /// QuantConnect Products
    /// </summary>
    [JsonConverter(typeof(ProductJsonConverter))]
    public class Product
    {
        /// <summary>
        /// Product Type
        /// </summary>
        public ProductType Type { get; set; }

        /// <summary>
        /// Collection of item subscriptions
        /// Nodes/Data/Seats/etc
        /// </summary>
        public List<ProductItem> Items { get; set; }
    }

    /// <summary>
    /// QuantConnect ProductItem 
    /// </summary>
    public class ProductItem
    {
        /// <summary>
        /// Product Type
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Collection of item subscriptions
        /// Nodes/Data/Seats/etc
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// USD Unit price for this item
        /// </summary>
        [JsonProperty(PropertyName = "unitPrice")]
        public int UnitPrice { get; set; }

        /// <summary>
        /// USD Total price for this product
        /// </summary>
        [JsonProperty(PropertyName = "total")]
        public int TotalPrice { get; set; }

        /// <summary>
        /// ID for this product
        /// </summary>
        [JsonProperty(PropertyName = "productId")]
        public int Id { get; set; }
    }

    /// <summary>
    /// Product types offered by QuantConnect
    /// Used by Product class
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Professional Seats Subscriptions
        /// </summary>
        ProfessionalSeats,

        /// <summary>
        /// Backtest Nodes Subscriptions
        /// </summary>
        BacktestNode,

        /// <summary>
        /// Research Nodes Subscriptions
        /// </summary>
        ResearchNode,

        /// <summary>
        /// Live Trading Nodes Subscriptions
        /// </summary>
        LiveNode,

        /// <summary>
        /// Support Subscriptions
        /// </summary>
        Support,

        /// <summary>
        /// Data Subscriptions
        /// </summary>
        Data,

        /// <summary>
        /// Modules Subscriptions
        /// </summary>
        Modules
    }
}
