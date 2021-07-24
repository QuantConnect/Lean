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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CsvHelper.Configuration.Attributes;

namespace QuantConnect.Brokerages.Zerodha.Messages
{
#pragma warning disable 1591
    /// <summary>
    /// Tick data structure
    /// </summary>
    public struct Tick
    {
        public string Mode { get; set; }
        public UInt32 InstrumentToken { get; set; }
        public bool Tradable { get; set; }
        public decimal LastPrice { get; set; }
        public UInt32 LastQuantity { get; set; }
        public decimal AveragePrice { get; set; }
        public UInt32 Volume { get; set; }
        public UInt32 BuyQuantity { get; set; }
        public UInt32 SellQuantity { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public DepthItem[] Bids { get; set; }
        public DepthItem[] Offers { get; set; }

        // KiteConnect 3 Fields

        public DateTime? LastTradeTime { get; set; }
        public UInt32 OI { get; set; }
        public UInt32 OIDayHigh { get; set; }
        public UInt32 OIDayLow { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Market depth item structure
    /// </summary>
    public struct DepthItem
    {
        public DepthItem(JObject data)
        {
            Quantity = Convert.ToUInt32(data["quantity"], CultureInfo.InvariantCulture);
            Price = (decimal)data["price"];
            Orders = Convert.ToUInt32(data["orders"], CultureInfo.InvariantCulture);
        }

        public UInt32 Quantity { get; set; }
        public decimal Price { get; set; }
        public UInt32 Orders { get; set; }
    }

    /// <summary>
    /// Historical structure
    /// </summary>
    public struct Historical
    {
        public Historical(dynamic data)
        {
            TimeStamp = DateTime.Parse(data[0].ToString("MM/dd/yyyy HH:mm:sszzz"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal); 
            Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
            High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
            Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
            Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
            Volume = Convert.ToUInt32(data[5], CultureInfo.InvariantCulture);
            OI = data.Count > 6 ? Convert.ToUInt32(data[6], CultureInfo.InvariantCulture) : Convert.ToUInt32(0, CultureInfo.InvariantCulture);
        }

        public DateTime TimeStamp { get; }
        public decimal Open { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Close { get; }
        public UInt32 Volume { get; }
        public UInt32 OI { get; }
    }

    /// <summary>
    /// Holding structure
    /// </summary>
    public struct Holding
    {
        public Holding(JObject data)
        {
            try
            {
                Product = data["product"].ToString();
                Exchange = data["exchange"].ToString();
                Price = (decimal)data["price"];
                LastPrice = (decimal)data["last_price"];
                CollateralQuantity = Convert.ToInt32(data["collateral_quantity"], CultureInfo.InvariantCulture);
                PNL = (decimal)data["pnl"];
                ClosePrice = (decimal)data["close_price"];
                AveragePrice = (decimal)data["average_price"];
                TradingSymbol = data["tradingsymbol"].ToString();
                CollateralType = data["collateral_type"].ToString();
                T1Quantity = Convert.ToInt32(data["t1_quantity"], CultureInfo.InvariantCulture);
                InstrumentToken = Convert.ToUInt32(data["instrument_token"], CultureInfo.InvariantCulture);
                ISIN = data["isin"].ToString();
                RealisedQuantity = Convert.ToInt32(data["realised_quantity"], CultureInfo.InvariantCulture);
                Quantity = Convert.ToInt32(data["quantity"], CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public string Product { get; set; }
        public string Exchange { get; set; }
        public decimal Price { get; set; }
        public decimal LastPrice { get; set; }
        public int CollateralQuantity { get; set; }
        public decimal PNL { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal AveragePrice { get; set; }
        public string TradingSymbol { get; set; }
        public string CollateralType { get; set; }
        public int T1Quantity { get; set; }
        public UInt32 InstrumentToken { get; set; }
        public string ISIN { get; set; }
        public int RealisedQuantity { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Available margin structure
    /// </summary>
    public struct AvailableMargin
    {
        public AvailableMargin(JObject data)
        {
            try
            {
                AdHocMargin = (decimal)data["adhoc_margin"];
                Cash = (decimal)data["cash"];
                Collateral = (decimal)data["collateral"];
                IntradayPayin = (decimal)data["intraday_payin"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public decimal AdHocMargin { get; set; }
        public decimal Cash { get; set; }
        public decimal Collateral { get; set; }
        public decimal IntradayPayin { get; set; }
    }

    /// <summary>
    /// Utilised margin structure
    /// </summary>
    public struct UtilisedMargin
    {
        public UtilisedMargin(JObject data)
        {
            try
            {
                Debits = (decimal)data["debits"];
                Exposure = (decimal)data["exposure"];
                M2MRealised = (decimal)data["m2m_realised"];
                M2MUnrealised = (decimal)data["m2m_unrealised"];
                OptionPremium = (decimal)data["option_premium"];
                Payout = (decimal)data["payout"];
                Span = (decimal)data["span"];
                HoldingSales = (decimal)data["holding_sales"];
                Turnover = (decimal)data["turnover"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public decimal Debits { get; set; }
        public decimal Exposure { get; set; }
        public decimal M2MRealised { get; set; }
        public decimal M2MUnrealised { get; set; }
        public decimal OptionPremium { get; set; }
        public decimal Payout { get; set; }
        public decimal Span { get; set; }
        public decimal HoldingSales { get; set; }
        public decimal Turnover { get; set; }

    }

    /// <summary>
    /// UserMargin structure
    /// </summary>
    public struct UserMargin
    {
        public UserMargin(JObject data)
        {
            try
            {
                Enabled = (bool)data["enabled"];
                Net = (decimal)data["net"];
                Available = JsonConvert.DeserializeObject<AvailableMargin>(data["available"].ToString());
                Utilised = JsonConvert.DeserializeObject<UtilisedMargin>(data["utilised"].ToString());
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public bool Enabled { get; set; }
        public decimal Net { get; set; }
        public AvailableMargin Available { get; set; }
        public UtilisedMargin Utilised { get; set; }
    }

    /// <summary>
    /// User margins response structure
    /// </summary>
    public struct UserMarginsResponse
    {
        public UserMarginsResponse(JObject data)
        {
            try
            {
                Equity = JsonConvert.DeserializeObject<UserMargin>(data["equity"].ToString());
                Commodity = JsonConvert.DeserializeObject<UserMargin>(data["commodity"].ToString());
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }
        public UserMargin Equity { get; set; }
        public UserMargin Commodity { get; set; }
    }

    /// <summary>
    /// UserMargin structure
    /// </summary>
    public struct InstrumentMargin
    {
        public InstrumentMargin(Dictionary<string, dynamic> data)
        {
            try
            {
                Margin = data["margin"];
                COLower = data["co_lower"];
                MISMultiplier = data["mis_multiplier"];
                Tradingsymbol = data["tradingsymbol"];
                COUpper = data["co_upper"];
                NRMLMargin = data["nrml_margin"];
                MISMargin = data["mis_margin"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public string Tradingsymbol { get; set; }
        public decimal Margin { get; set; }
        public decimal COLower { get; set; }
        public decimal COUpper { get; set; }
        public decimal MISMultiplier { get; set; }
        public decimal MISMargin { get; set; }
        public decimal NRMLMargin { get; set; }
    }
    /// <summary>
    /// Position structure
    /// </summary>
    public struct Position
    {
        public Position(JObject data)
        {
            try
            {
                Product = data["product"].ToString();
                OvernightQuantity = Convert.ToInt32(data["overnight_quantity"],CultureInfo.InvariantCulture);
                Exchange = data["exchange"].ToString();
                SellValue = Convert.ToInt32(data["sell_value"], CultureInfo.InvariantCulture);
                BuyM2M = Convert.ToInt32(data["buy_m2m"], CultureInfo.InvariantCulture);
                LastPrice = (decimal)data["last_price"];
                TradingSymbol = data["tradingsymbol"].ToString();
                Realised = (decimal)data["realised"];
                PNL = (decimal)data["pnl"];
                Multiplier = Convert.ToInt32(data["multiplier"], CultureInfo.InvariantCulture);
                SellQuantity = Convert.ToInt32(data["sell_quantity"], CultureInfo.InvariantCulture);
                SellM2M = (decimal)data["sell_m2m"];
                BuyValue = (decimal)data["buy_value"];
                BuyQuantity = Convert.ToInt32(data["buy_quantity"], CultureInfo.InvariantCulture);
                AveragePrice = (decimal)data["average_price"];
                Unrealised = (decimal)data["unrealised"];
                Value = (decimal)data["value"];
                BuyPrice = (decimal)data["buy_price"];
                SellPrice = (decimal)data["sell_price"];
                M2M = (decimal)data["m2m"];
                InstrumentToken = Convert.ToUInt32(data["instrument_token"],CultureInfo.InvariantCulture);
                ClosePrice = (decimal)data["close_price"];
                Quantity = Convert.ToInt32(data["quantity"], CultureInfo.InvariantCulture);
                DayBuyQuantity = Convert.ToInt32(data["day_buy_quantity"], CultureInfo.InvariantCulture);
                DayBuyValue = (decimal)data["day_buy_value"];
                DayBuyPrice = (decimal)data["day_buy_price"];
                DaySellQuantity = Convert.ToInt32(data["day_sell_quantity"],CultureInfo.InvariantCulture);
                DaySellValue = (decimal)data["day_sell_value"];
                DaySellPrice = (decimal)data["day_sell_price"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public string Product { get; }
        public int OvernightQuantity { get; }
        public string Exchange { get; }
        public decimal SellValue { get; }
        public decimal BuyM2M { get; }
        public decimal LastPrice { get; }
        public string TradingSymbol { get; }
        public decimal Realised { get; }
        public decimal PNL { get; }
        public decimal Multiplier { get; }
        public int SellQuantity { get; }
        public decimal SellM2M { get; }
        public decimal BuyValue { get; }
        public int BuyQuantity { get; }
        public decimal AveragePrice { get; }
        public decimal Unrealised { get; }
        public decimal Value { get; }
        public decimal BuyPrice { get; }
        public decimal SellPrice { get; }
        public decimal M2M { get; }
        public UInt32 InstrumentToken { get; }
        public decimal ClosePrice { get; }
        public int Quantity { get; }
        public int DayBuyQuantity { get; }
        public decimal DayBuyPrice { get; }
        public decimal DayBuyValue { get; }
        public int DaySellQuantity { get; }
        public decimal DaySellPrice { get; }
        public decimal DaySellValue { get; }
    }

    /// <summary>
    /// Position response structure
    /// </summary>
    public struct PositionResponse
    {
        public PositionResponse(JObject data)
        {
            Day = new List<Position>();
            Net = new List<Position>();

            foreach (JObject item in data["day"])
                Day.Add(new Position(item));
            foreach (JObject item in data["net"])
                Net.Add(new Position(item));
        }

        public List<Position> Day { get; }
        public List<Position> Net { get; }
    }

    /// <summary>
    /// Order structure
    /// </summary>
    public struct Order
    {
        public Order(JToken data)
        {
            try
            {
                AveragePrice = (decimal)data["average_price"];
                CancelledQuantity = (int)data["cancelled_quantity"];
                DisclosedQuantity = (int)data["disclosed_quantity"];
                Exchange = (string)data["exchange"];
                ExchangeOrderId = (string)data["exchange_order_id"];
                ExchangeTimestamp = Utils.StringToDate((string)data["exchange_timestamp"]);
                FilledQuantity = (int)data["filled_quantity"];
                InstrumentToken = Convert.ToUInt32((uint)data["instrument_token"]);
                OrderId = (string)data["order_id"];
                OrderTimestamp = Utils.StringToDate((string)data["order_timestamp"]);
                OrderType = (string)data["order_type"];
                ParentOrderId = (string)data["parent_order_id"];
                PendingQuantity = (int)data["pending_quantity"];
                PlacedBy = (string)data["placed_by"];
                Price = (decimal)data["price"];
                Product = (string)data["product"];
                Quantity = (int)data["quantity"];
                Status = (string)data["status"];
                StatusMessage = (string)data["status_message"];
                Tag = (string)data["tag"];
                Tradingsymbol = (string)data["tradingsymbol"];
                TransactionType = (string)data["transaction_type"];
                TriggerPrice = (decimal)data["trigger_price"];
                Validity = (string)data["validity"];
                Variety = (string)data["variety"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public decimal AveragePrice { get; set; }
        public int CancelledQuantity { get; set; }
        public int DisclosedQuantity { get; set; }
        public string Exchange { get; set; }
        public string ExchangeOrderId { get; set; }
        public DateTime? ExchangeTimestamp { get; set; }
        public int FilledQuantity { get; set; }
        public uint InstrumentToken { get; set; }
        public string OrderId { get; set; }
        public DateTime? OrderTimestamp { get; set; }
        public string OrderType { get; set; }
        public string ParentOrderId { get; set; }
        public int PendingQuantity { get; set; }
        public string PlacedBy { get; set; }
        public decimal Price { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public string Tag { get; set; }
        public string Tradingsymbol { get; set; }
        public string TransactionType { get; set; }
        public decimal TriggerPrice { get; set; }
        public string Validity { get; set; }
        public string Variety { get; set; }
    }

    public struct ChannelSubscription
    {
        public string ChannelId { get; set; }
        public string a { get; set; }
        public uint[] v { get; set; }
    }

    public struct ChannelUnsubscription
    {
        public string ChannelId { get; set; }
        public string a { get; set; }
        public uint[] v { get; set; }
    }

    /// <summary>
    /// GTTOrder structure
    /// </summary>
    public struct GTT
    {
        public GTT(Dictionary<string, dynamic> data)
        {
            try
            {
                Id = data["id"];
                Condition = new GTTCondition(data["condition"]);
                TriggerType = data["type"];

                Orders = new List<GTTOrder>();
                foreach (Dictionary<string, dynamic> item in data["orders"])
                    Orders.Add(new GTTOrder(item));

                Status = data["status"];
                CreatedAt = Utils.StringToDate(data["created_at"]);
                UpdatedAt = Utils.StringToDate(data["updated_at"]);
                ExpiresAt = Utils.StringToDate(data["expires_at"]);
                Meta = new GTTMeta(data["meta"]);
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public int Id { get; set; }
        public GTTCondition? Condition { get; set; }
        public string TriggerType { get; set; }
        public List<GTTOrder> Orders { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public GTTMeta? Meta { get; set; }
    }

    /// <summary>
    /// GTTMeta structure
    /// </summary>
    public struct GTTMeta
    {
        public GTTMeta(Dictionary<string, dynamic> data)
        {
            try
            {
                RejectionReason = data != null && data.ContainsKey("rejection_reason") ? data["rejection_reason"] : "";
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public string RejectionReason { get; set; }
    }

    /// <summary>
    /// GTTCondition structure
    /// </summary>
    public struct GTTCondition
    {
        public GTTCondition(Dictionary<string, dynamic> data)
        {
            try
            {
                InstrumentToken = data["instrument_token"];
                Exchange = data["exchange"];
                TradingSymbol = data["tradingsymbol"];
                TriggerValues = Utils.ToDecimalList(data["trigger_values"] as ArrayList);
                LastPrice = data["last_price"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public int InstrumentToken { get; set; }
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public List<decimal> TriggerValues { get; set; }
        public decimal LastPrice { get; set; }
    }

    /// <summary>
    /// GTTOrder structure
    /// </summary>
    public struct GTTOrder
    {
        public GTTOrder(Dictionary<string, dynamic> data)
        {
            try
            {
                TransactionType = data["transaction_type"];
                Product = data["product"];
                OrderType = data["order_type"];
                Quantity = data["quantity"];
                Price = data["price"];
                Result = data["result"] == null ? null : new Nullable<GTTResult>(new GTTResult(data["result"]));
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public string TransactionType { get; set; }
        public string Product { get; set; }
        public string OrderType { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public GTTResult? Result { get; set; }
    }

    /// <summary>
    /// GTTResult structure
    /// </summary>
    public struct GTTResult
    {
        public GTTResult(Dictionary<string, dynamic> data)
        {
            try
            {
                OrderResult = data["order_result"] == null ? null : new Nullable<GTTOrderResult>(new GTTOrderResult(data["order_result"]));
                Timestamp = data["timestamp"];
                TriggeredAtPrice = data["triggered_at"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public GTTOrderResult? OrderResult { get; set; }
        public string Timestamp { get; set; }
        public decimal TriggeredAtPrice { get; set; }
    }

    /// <summary>
    /// GTTOrderResult structure
    /// </summary>
    public struct GTTOrderResult
    {
        public GTTOrderResult(Dictionary<string, dynamic> data)
        {
            try
            {
                OrderId = data["order_id"];
                RejectionReason = data["rejection_reason"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }

        public string OrderId { get; set; }
        public string RejectionReason { get; set; }
    }

    /// <summary>
    /// GTTParams structure
    /// </summary>
    public struct GTTParams
    {
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public int InstrumentToken { get; set; }
        public string TriggerType { get; set; }
        public decimal LastPrice { get; set; }
        public List<GTTOrderParams> Orders { get; set; }
        public List<decimal> TriggerPrices { get; set; }
    }

    /// <summary>
    /// GTTOrderParams structure
    /// </summary>
    public struct GTTOrderParams
    {
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        // Order type (LIMIT, SL, SL-M, MARKET)
        public string OrderType { get; set; }
        // Product code (NRML, MIS, CNC)
        public string Product { get; set; }
        // Transaction type (BUY, SELL)
        public string TransactionType { get; set; }
    }

    /// <summary>
    /// Instrument structure
    /// </summary>
    public struct Instrument
    {
        public Instrument(Dictionary<string, dynamic> data)
        {
            try
            {
                InstrumentToken = Convert.ToUInt32(data["instrument_token"]);
                ExchangeToken = Convert.ToUInt32(data["exchange_token"]);
                TradingSymbol = data["tradingsymbol"];
                Name = data["name"];
                LastPrice = Convert.ToDecimal(data["last_price"]);
                TickSize = Convert.ToDecimal(data["tick_size"]);
                Expiry = Utils.StringToDate(data["expiry"]);
                InstrumentType = data["instrument_type"];
                Segment = data["segment"];
                Exchange = data["exchange"];

                if (data["strike"].Contains("e"))
                    Strike = decimal.Parse(data["strike"], NumberStyles.Float);
                else
                    Strike = Convert.ToDecimal(data["strike"]);

                LotSize = Convert.ToUInt32(data["lot_size"]);
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public uint InstrumentToken { get; set; }
        public uint ExchangeToken { get; set; }
        public string TradingSymbol { get; set; }
        public string Name { get; set; }
        public decimal LastPrice { get; set; }
        public decimal TickSize { get; set; }
        public DateTime? Expiry { get; set; }
        public string InstrumentType { get; set; }
        public string Segment { get; set; }
        public string Exchange { get; set; }
        public decimal Strike { get; set; }
        public uint LotSize { get; set; }
    }

    /// <summary>
    /// Instrument structure
    /// </summary>
    public class CsvInstrument
    {
        [Name("instrument_token")]
        public uint InstrumentToken { get; set; }
        [Name("exchange_token")]
        public uint ExchangeToken { get; set; }
        [Name("tradingsymbol")]
        public string TradingSymbol { get; set; }
        [Name("name")]
        public string Name { get; set; }
        [Name("last_price")]
        public decimal LastPrice { get; set; }
        [Name("tick_size")]
        public decimal TickSize { get; set; }
        [Name("expiry")]
        public DateTime? Expiry { get; set; }
        [Name("instrument_type")]
        public string InstrumentType { get; set; }
        [Name("segment")]
        public string Segment { get; set; }
        [Name("exchange")]
        public string Exchange { get; set; }
        [Name("strike")]
        public decimal Strike { get; set; }
        [Name("lot_size")]
        public uint LotSize { get; set; }
    }

    /// <summary>
    /// Trade structure
    /// </summary>
    public struct Trade
    {
        public Trade(Dictionary<string, dynamic> data)
        {
            try
            {
                TradeId = data["trade_id"];
                OrderId = data["order_id"];
                ExchangeOrderId = data["exchange_order_id"];
                Tradingsymbol = data["tradingsymbol"];
                Exchange = data["exchange"];
                InstrumentToken = Convert.ToUInt32(data["instrument_token"]);
                TransactionType = data["transaction_type"];
                Product = data["product"];
                AveragePrice = data["average_price"];
                Quantity = data["quantity"];
                FillTimestamp = Utils.StringToDate(data["fill_timestamp"]);
                ExchangeTimestamp = Utils.StringToDate(data["exchange_timestamp"]);
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public string TradeId { get; }
        public string OrderId { get; }
        public string ExchangeOrderId { get; }
        public string Tradingsymbol { get; }
        public string Exchange { get; }
        public UInt32 InstrumentToken { get; }
        public string TransactionType { get; }
        public string Product { get; }
        public decimal AveragePrice { get; }
        public int Quantity { get; }
        public DateTime? FillTimestamp { get; }
        public DateTime? ExchangeTimestamp { get; }
    }

    /// <summary>
    /// Trigger range structure
    /// </summary>
    public struct TrigerRange
    {
        public TrigerRange(Dictionary<string, dynamic> data)
        {
            try
            {
                InstrumentToken = Convert.ToUInt32(data["instrument_token"]);
                Lower = data["lower"];
                Upper = data["upper"];
                Percentage = data["percentage"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }
        public UInt32 InstrumentToken { get; }
        public decimal Lower { get; }
        public decimal Upper { get; }
        public decimal Percentage { get; }
    }

    /// <summary>
    /// User structure
    /// </summary>
    public struct User
    {
        public User(dynamic data)
        {
            try
            {
                APIKey = data["data"]["api_key"];
                //Products = (string[])data["data"]["products"].ToArray(typeof(string));
                Products = data["data"]["products"].ToObject<string[]>();
                UserName = data["data"]["user_name"];
                UserShortName = data["data"]["user_shortname"];
                AvatarURL = data["data"]["avatar_url"];
                Broker = data["data"]["broker"];
                AccessToken = data["data"]["access_token"];
                PublicToken = data["data"]["public_token"];
                RefreshToken = data["data"]["refresh_token"];
                UserType = data["data"]["user_type"];
                UserId = data["data"]["user_id"];
                LoginTime = Utils.StringToDate(data["data"]["login_time"].ToString());
                Exchanges = data["data"]["exchanges"].ToObject<string[]>();
                OrderTypes = data["data"]["order_types"].ToObject<string[]>();
                Email = data["data"]["email"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public string APIKey { get; }
        public string[] Products { get; }
        public string UserName { get; }
        public string UserShortName { get; }
        public string AvatarURL { get; }
        public string Broker { get; }
        public string AccessToken { get; }
        public string PublicToken { get; }
        public string RefreshToken { get; }
        public string UserType { get; }
        public string UserId { get; }
        public DateTime? LoginTime { get; }
        public string[] Exchanges { get; }
        public string[] OrderTypes { get; }
        public string Email { get; }
    }

    public struct TokenSet
    {
        public TokenSet(Dictionary<string, dynamic> data)
        {
            try
            {
                UserId = data["data"]["user_id"];
                AccessToken = data["data"]["access_token"];
                RefreshToken = data["data"]["refresh_token"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }
        }
        public string UserId { get; }
        public string AccessToken { get; }
        public string RefreshToken { get; }
    }

    /// <summary>
    /// User structure
    /// </summary>
    public struct Profile
    {
        public Profile(Dictionary<string, dynamic> data)
        {
            try
            {
                Products = (string[])data["data"]["products"].ToArray(typeof(string));
                UserName = data["data"]["user_name"];
                UserShortName = data["data"]["user_shortname"];
                AvatarURL = data["data"]["avatar_url"];
                Broker = data["data"]["broker"];
                UserType = data["data"]["user_type"];
                Exchanges = (string[])data["data"]["exchanges"].ToArray(typeof(string));
                OrderTypes = (string[])data["data"]["order_types"].ToArray(typeof(string));
                Email = data["data"]["email"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }


        public string[] Products { get; }
        public string UserName { get; }
        public string UserShortName { get; }
        public string AvatarURL { get; }
        public string Broker { get; }
        public string UserType { get; }
        public string[] Exchanges { get; }
        public string[] OrderTypes { get; }
        public string Email { get; }
    }

    /// <summary>
    /// Quote structure
    /// </summary>
    public struct Quote
    {
        public Quote(JObject data)
        {
            try
            {
                InstrumentToken = Convert.ToUInt32(data["instrument_token"], CultureInfo.InvariantCulture);
                Timestamp = Utils.StringToDate(data["timestamp"].ToString());
                LastPrice = (decimal)data["last_price"];

                Change = (decimal)data["net_change"];

                Open = (decimal)data["ohlc"]["open"];
                Close = (decimal)data["ohlc"]["close"];
                Low = (decimal)data["ohlc"]["low"];
                High = (decimal)data["ohlc"]["high"];

                if (data["last_quantity"] != null)
                {
                    // Non index quote
                    LastQuantity = Convert.ToUInt32(data["last_quantity"], CultureInfo.InvariantCulture);
                    LastTradeTime = Utils.StringToDate(data["last_trade_time"].ToString());
                    AveragePrice = (decimal)data["average_price"];
                    Volume = Convert.ToUInt32(data["volume"], CultureInfo.InvariantCulture);

                    BuyQuantity = Convert.ToUInt32(data["buy_quantity"], CultureInfo.InvariantCulture);
                    SellQuantity = Convert.ToUInt32(data["sell_quantity"], CultureInfo.InvariantCulture);

                    OI = Convert.ToUInt32(data["oi"], CultureInfo.InvariantCulture);

                    OIDayHigh = Convert.ToUInt32(data["oi_day_high"], CultureInfo.InvariantCulture);
                    OIDayLow = Convert.ToUInt32(data["oi_day_low"], CultureInfo.InvariantCulture);

                    LowerCircuitLimit = (decimal)data["lower_circuit_limit"];
                    UpperCircuitLimit = (decimal)data["upper_circuit_limit"];

                    Bids = new List<DepthItem>();
                    Offers = new List<DepthItem>();

                    if (data["depth"]["buy"] != null)
                    {
                        foreach (JObject bid in data["depth"]["buy"])
                            Bids.Add(new DepthItem(bid));
                    }

                    if (data["depth"]["sell"] != null)
                    {
                        foreach (JObject offer in data["depth"]["sell"])
                            Offers.Add(new DepthItem(offer));
                    }
                }
                else
                {
                    // Index quote
                    LastQuantity = 0;
                    LastTradeTime = null;
                    AveragePrice = 0;
                    Volume = 0;

                    BuyQuantity = 0;
                    SellQuantity = 0;

                    OI = 0;

                    OIDayHigh = 0;
                    OIDayLow = 0;

                    LowerCircuitLimit = 0;
                    UpperCircuitLimit = 0;

                    Bids = new List<DepthItem>();
                    Offers = new List<DepthItem>();
                }
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }

        public UInt32 InstrumentToken { get; set; }
        public decimal LastPrice { get; set; }
        public UInt32 LastQuantity { get; set; }
        public decimal AveragePrice { get; set; }
        public UInt32 Volume { get; set; }
        public UInt32 BuyQuantity { get; set; }
        public UInt32 SellQuantity { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public List<DepthItem> Bids { get; set; }
        public List<DepthItem> Offers { get; set; }

        // KiteConnect 3 Fields

        public DateTime? LastTradeTime { get; set; }
        public UInt32 OI { get; set; }
        public UInt32 OIDayHigh { get; set; }
        public UInt32 OIDayLow { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// OHLC Quote structure
    /// </summary>
    public struct OHLC
    {
        public OHLC(Dictionary<string, dynamic> data)
        {
            try
            {
                InstrumentToken = Convert.ToUInt32(data["instrument_token"]);
                LastPrice = data["last_price"];

                Open = data["ohlc"]["open"];
                Close = data["ohlc"]["close"];
                Low = data["ohlc"]["low"];
                High = data["ohlc"]["high"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }
        public UInt32 InstrumentToken { get; set; }
        public decimal LastPrice { get; }
        public decimal Open { get; }
        public decimal Close { get; }
        public decimal High { get; }
        public decimal Low { get; }
    }

    /// <summary>
    /// LTP Quote structure
    /// </summary>
    public struct LTP
    {
        public LTP(Dictionary<string, dynamic> data)
        {
            try
            {
                InstrumentToken = Convert.ToUInt32(data["instrument_token"]);
                LastPrice = data["last_price"];
            }
            catch (Exception e)
            {
                throw new DataException("Unable to parse data. " + Utils.JsonSerialize(data), HttpStatusCode.OK, e);
            }

        }
        public UInt32 InstrumentToken { get; set; }
        public decimal LastPrice { get; }
    }
#pragma warning restore 1591
}
