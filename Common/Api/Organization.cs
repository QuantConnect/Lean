using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QLNet;

namespace QuantConnect.Api
{
    /// <summary>
    /// Response wrapper for organizations/list
    /// TODO: The response objects in the array do not contain all Organization Properties; do we need another wrapper object? 
    /// </summary>
    public class OrganizationList : RestResponse
    {
        [JsonProperty(PropertyName = "organizations")]
        public List<Organization> List;
    }

    /// <summary>
    /// Object representation of Api Organization
    /// </summary>
    public class Organization : RestResponse
    {
        /// <summary>
        /// Organization ID; Used for API Calls
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Organization ID; Used for API Calls
        /// </summary>
        [JsonProperty(PropertyName = "seats")]
        public int Seats { get; set; }

        /// <summary>
        /// Organization ID; Used for API Calls
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public DataAgreement DataAgreement { get; set; }

        /// <summary>
        /// Organization Product Subscriptions
        /// </summary>
        public List<Product> Products { get; set; }

        /// <summary>
        /// Organization Product Subscriptions
        /// </summary>
        public Credit Credit { get; set; }
    }

    /// <summary>
    /// Organization Data Agreement
    /// </summary>
    public class DataAgreement
    {
        /// <summary>
        /// Time the Data Agreement was Signed
        /// </summary>
        [JsonProperty(PropertyName = "signedTime")]
        public string SignedTime { get; set; }

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
        /// TODO Rename to Transaction?
        /// </summary>
        public class Movement
        {
            /// <summary>
            /// Date of the change in credit
            /// </summary>
            [JsonProperty(PropertyName = "date")]
            public DateTime Date { get; set; }

            /// <summary>
            /// Credit decription
            /// </summary>
            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }

            /// <summary>
            /// Amount of change
            /// </summary>
            [JsonProperty(PropertyName = "amount")]
            public int Amount { get; set; }

            /// <summary>
            /// Ending Balance in QCC after Movement
            /// </summary>
            [JsonProperty(PropertyName = "balance")]
            public int Balance { get; set; }

            //TODO
            // Type and subtype of movement?? Maybe not needed
        }

        /// <summary>
        /// Current Balance USD
        /// </summary>
        [JsonProperty(PropertyName = "balance")]
        public decimal BalanceUSD { get; set; }

        /// <summary>
        /// Current Balance QCC
        /// </summary>
        public decimal BalanceQCC => BalanceUSD * 100;

        /// <summary>
        /// List of changes to Credit
        /// </summary>
        [JsonProperty(PropertyName = "movements")]
        public List<Movement> Movements { get; set; }
    }

    public class Product
    {
        /// <summary>
        /// Product Name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        //TODO: Items type varies so Object for now; likely need a unique json converter for products
        // Nodes/Data/Seats/etc
        [JsonProperty(PropertyName = "items")]
        public List<object> Items { get; set; }
    }
}
