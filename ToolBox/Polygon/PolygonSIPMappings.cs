using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.Polygon
{
    // Provides information for SIP sale conditions according to the Polygon Data Vendor
    // https://polygon.io/glossary/us/stocks/conditions-indicators
    //
    public static class PolygonSIPTradeMappings
    {
        // Trade Conditions
        public static string TradeConditionDescription(int modiferNumber)
        {
            return tradeMappings[modiferNumber, 1];
        }
        public static string TradeSIPMapping(int modiferNumber)
        {
            return tradeMappings[modiferNumber, 2];
        }
        public static bool TradeUpdateHighLow(int modiferNumber)
        {
            if (tradeMappings[modiferNumber, 3] == "TRUE") { return true; } else { return false; }
        }

        //Consolidated Processing Guidelines
        public static bool TradeConsolidatedProcessingUpdateLast(int modiferNumber)
        {
            if (tradeMappings[modiferNumber, 4] == "TRUE") { return true; } else { return false; }
        }
        public static bool TradeConsolidatedProcessingUpdateHighLow(int modiferNumber)
        {
            if (tradeMappings[modiferNumber, 5] == "TRUE") { return true; } else { return false; }
        }

        //Market Center Processing Guidelines
        public static bool TradeMarketProcessingUpdateLast(int modiferNumber)
        {
            if (tradeMappings[modiferNumber, 6] == "TRUE") { return true; } else { return false; }
        }
        public static bool TradeMarketProcessingUpdateVolume(int modiferNumber)
        {
            if (tradeMappings[modiferNumber, 7] == "TRUE") { return true; } else { return false; }
        }

        // Array index mapped to provide access to each item very quickly
        // {"Modifier", "Condition", "SIP Mapping", "Update High/Low", "Update Last", "Update High/Low", "Update Last", "Update Volume"},
        private static string[,] tradeMappings = new string[60, 8]
        {
            {"0", "Regular Sale", "@", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"1", "Acquisition", "A", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"2", "Average Price Trade", "W", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"3", "Automatic Execution", "N/A", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"4", "Bunched Trade", "B", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"5", "Bunched Sold Trade", "G", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"},
            {"6", "CAP Election", "", "", "", "", "", ""},
            {"7", "Cash Sale", "C", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"8", "Closing Prints", "6", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"9", "Cross Trade", "X", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"10", "Derivatively Priced", "4", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"},
            {"11", "Distribution", "D", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"12", "Form T", "T", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"13", "Extended Trading Hours (Sold Out of Sequence)", "U", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"14", "Intermarket Sweep", "F", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"15", "Market Center Official Close", "M", "FALSE", "FALSE", "TRUE", "TRUE", "FALSE"},
            {"16", "Market Center Official Open3", "Q", "FALSE", "FALSE", "TRUE", "FALSE", "FALSE"},
            {"17", "Market Center Opening Trade3", "", "", "", "", "", ""},
            {"18", "Market Center Reopening Trade3", "", "", "", "", "", ""},
            {"19", "Market Center Closing Trade3", "", "", "", "", "", ""},
            {"20", "Next Day", "N", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"21", "Price Variation Trade", "H", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"22", "Prior Reference Price", "P", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"},
            {"23", "Rule 155 Trade (AMEX)", "K", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"24", "Rule 127 NYSE", "", "", "", "", "", ""},
            {"25", "Opening Prints", "O", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"26", "Opened", "", "", "", "", "", ""},
            {"27", "Stopped Stock (Regular Trade)", "1", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"28", "Re-Opening Prints", "5", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"29", "Seller", "R", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"},
            {"30", "Sold Last", "L", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"31", "Placeholder", "", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE"},
            {"32", "Sold Out", "", "", "", "", "", ""},
            {"33", "Sold (out of Sequence)", "Z", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"},
            {"34", "Split Trade", "S", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"35", "Stock Option", "", "", "", "", "", ""},
            {"36", "Yellow Flag Regular Trade", "Y", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"},
            {"37", "Odd Lot Trade", "I", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"38", "Corrected Consolidated Close (per listing market)", "9", "TRUE", "TRUE", "FALSE", "FALSE", "FALSE"},
            {"39", "UnkFalsewn", "", "", "", "", "", ""},
            {"40", "Held", "", "", "", "", "", ""},
            {"41", "Trade Thru Exempt", "", "", "", "", "", ""},
            {"42", "FalsenEligible", "", "", "", "", "", ""},
            {"43", "FalsenEligible Extended", "", "", "", "", "", ""},
            {"44", "Cancelled", "", "", "", "", "", ""},
            {"45", "Recovery", "", "", "", "", "", ""},
            {"46", "Correction", "", "", "", "", "", ""},
            {"47", "As of", "", "", "", "", "", ""},
            {"48", "As of Correction", "", "", "", "", "", ""},
            {"49", "As of Cancel", "", "", "", "", "", ""},
            {"50", "OOB", "", "", "", "", "", ""},
            {"51", "Summary", "", "", "", "", "", ""},
            {"52", "Contingent Trade", "V", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"53", "Qualified Contingent Trade (\"QCT\")", "7", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE"},
            {"54", "Errored", "", "", "", "", "", ""},
            {"55", "OPENING_REOPENING_TRADE_DETAIL", "", "", "", "", "", ""},
            {"56", "Placeholder", "E", "", "", "", "", ""},
            {"57", "Placeholder", "", "", "", "", "", ""},
            {"58", "Placeholder", "", "", "", "", "", ""},
            {"59", "Placeholder for 611 exempt", "8", "", "", "", "", ""}
        };





        // TODO:  FIX-> Make these a case/switch or concurrent dictionary ... not thread safe dictionary !  problem


        //// Trade Corrections
        //private static Dictionary<string, string> tradeCorrectionsNYSE = new Dictionary<string, string>()
        //{
        //    {"0", "Regular trade which was not corrected, changed or signified as cancel or error."},
        //    {"1", "Original trade which was late corrected (This record contains the original time - HHMM and the corrected data for the trade)."},
        //    {"7", "Original trade which was later marked as erroneous"},
        //    {"8", "Original trade which was later cancelled"},
        //    {"10", "Cancel record (This record follows '08' records)"},
        //    {"11", "Error record (This record follows '07' records)"},
        //    {"12", "Correction record (This record follows'01' records and contains the correction time and the original \"incorrect\" data). The final correction will be published."}
        //};




    }

    public static class PolygonQuoteMappings
    {

        public static bool isSuspiciousQuote(int condition)
        {
            if (condition == 0)   // TODO:  Fix this to more precise and intelligent
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //// Quote Conditions
        //private static Dictionary<string, string> quoteMapping = new Dictionary<string, string>()
        //{
        //        // {"Modifier", "Condition"},
        //        {"-1", "Invalid"},
        //        {"0", "Regular"},
        //        {"1", "RegularTwoSidedOpen"},
        //        {"2", "RegularOneSidedOpen"},
        //        {"3", "SlowAsk"},
        //        {"4", "SlowBid"},
        //        {"5", "SLowBidAsk"},
        //        {"6", "SlowDueLRPBid"},
        //        {"7", "SlowDueLRPAsk"},
        //        {"8", "SlowDueNYSELRP"},
        //        {"9", "SlowDueSetSlowListBidAsk"},
        //        {"10", "ManualAskAutomatedBid"},
        //        {"11", "ManualBidAutomatedAsk"},
        //        {"12", "ManualBidAndAsk"},
        //        {"13", "Opening"},
        //        {"14", "Closing"},
        //        {"15", "Closed"},
        //        {"16", "Resume"},
        //        {"17", "FastTrading"},
        //        {"18", "TradingRangeIndication"},
        //        {"19", "MarketMakerQuotesClosed"},
        //        {"20", "NonFirm"},
        //        {"21", "NewsDissemination"},
        //        {"22", "OrderInflux"},
        //        {"23", "OrderImbalance"},
        //        {"23", "DueToRelatedSecurityNewsDissemination"},
        //        {"25", "DueToRelatedSecurityNewsPending"},
        //        {"26", "AdditionalInformation"},
        //        {"27", "NewsPending"},
        //        {"28", "AdditionalInformationDueToRelatedSecurity"},
        //        {"29", "DueToRelatedSecurity"},
        //        {"30", "InViewOfCommon"},
        //        {"31", "EquipmentChangeover"},
        //        {"32", "NoOpenNoResponse"},
        //        {"33", "SubPennyTrading"},
        //        {"34", "AutomatedBidNoOfferNoBid"},
        //        {"35", "LULDPriceBand"},
        //        {"36", "MarketWideCircuitBreakerLevel1"},
        //        {"37", "MarketWideCircuitBreakerLevel2"},
        //        {"38", "MarketWideCircuitBreakerLevel3"},
        //        {"39", "RepublishedLULDPriceBand"},
        //        {"40", "OnDemandAuction"},
        //        {"41", "CashOnlySettlement"},
        //        {"42", "NextDaySettlement"},
        //        {"43", "LULDTradingPause"},
        //        {"71", "SLowDuelRPBidAsk"},
        //        {"80", "Cancel"},
        //        {"81", "Corrected_Price"},
        //        {"82", "SIPGenerated"},
        //        {"83", "Unknown"},
        //        {"84", "Crossed_Market"},
        //        {"85", "Locked_Market"},
        //        {"86", "Depth_On_Offer_Side"},
        //        {"87", "Depth_On_Bid_Side"},
        //        {"88", "Depth_On_Bid_And_Offer"},
        //        {"89", "Pre_Opening_Indication"},
        //        {"90", "Syndicate_Bid"},
        //        {"91", "Pre_Syndicate_Bid"},
        //        {"92", "Penalty_Bid"}
        //};

    }
}
