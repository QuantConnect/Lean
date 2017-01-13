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
using System.Globalization;
using System.IO;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// Data Source used to get Accern news data
    /// </summary>
    public class Accern : BaseData
    {
        /// <summary>
        /// Unique Identifier for the articles
        /// </summary>
        public string ArticleId;

        /// <summary>
        /// A story is a unique company and event pair. Each story that is detected gets a unique ID and several articles can talk about the same story, hence, sharing the same story ID
        /// </summary>
        public string StoryId;

        /// <summary>
        /// Name of the company mentioned In the article
        /// </summary>
        public string Name;

        /// <summary>
        /// Unique global ID of the company, derived from Bloomberg Open Symbology
        /// </summary>
        public string GlobalId;

        /// <summary>
        /// Entity level ID of the company, derived from Bloomberg Open Symbology
        /// </summary>
        public string EntityId;

        /// <summary>
        /// Determines if the company is publicly traded or private
        /// </summary>
        public string Type;

        /// <summary>
        /// Identifies the exchange that the company is traded on
        /// </summary>
        public string Exchange;

        /// <summary>
        /// The sector of the company
        /// </summary>
        public string Sector;

        /// <summary>
        /// The industry of the company
        /// </summary>
        public string Industry;

        /// <summary>
        /// Headquarter country location of the company
        /// </summary>
        public string Country;

        /// <summary>
        /// Headquarter region location of the company
        /// </summary>
        public string Region;

        /// <summary>
        /// Indices that track the company
        /// </summary>
        public string Index;

        /// <summary>
        /// Top three competitors associated with the company
        /// </summary>
        public string Competitors;

        /// <summary>
        ///  Name of 2nd the company if any
        /// </summary>
        public string Name2;

        /// <summary>
        /// Ticker name of 2nd company if any
        /// </summary>
        public string Symbol2;

        /// <summary>
        /// Global Id of 2nd company if any
        /// </summary>
        public string GlobalId2;

        /// <summary>
        /// Entity id of 2nd company if any
        /// </summary>
        public string EntityId2;

        /// <summary>
        /// Type of 2nd company if any
        /// </summary>
        public string Type2;

        /// <summary>
        /// Exchange of 2nd company if any
        /// </summary>
        public string Exchange2;

        /// <summary>
        /// Sector of 2nd company if any
        /// </summary>
        public string Sector2;

        /// <summary>
        /// The industry of the 2nd company if any
        /// </summary>
        public string Industry2;

        /// <summary>
        /// Headquarter country location of the 2nd company if any
        /// </summary>
        public string Country2;

        /// <summary>
        /// Headquarter region location of the 2nd company if any
        /// </summary>
        public string Region2;

        /// <summary>
        /// Indices that track the 2nd company if any
        /// </summary>
        public string Index2;

        /// <summary>
        ///  Top three competitors associated with the 2nd company if any
        /// </summary>
        public string Competitors2;

        /// <summary>
        /// Group name formed by compressing 1,000+ events into 16 Event Groups
        /// </summary>
        public string GroupName;

        /// <summary>
        /// A subsection of an event group
        /// </summary>
        public string GroupType;

        /// <summary>
        /// Group name of 2nd event if any
        /// </summary>
        public string GroupName2;

        /// <summary>
        /// Group Type of 2nd event if any
        /// </summary>
        public string GroupType2;

        /// <summary>
        /// An aggregated sentiment score for articles in a specific story
        /// </summary>
        public decimal StorySentiment;

        /// <summary>
        /// Accern recognizes how a story saturates on the web.
        /// It currently look at the volume of articles related to a story (Story_Volume), along with the web traffic of each article’s source (Source_Traffic) and aggregated traffic (Story_Traffic) to gauge a specific saturation level ranging from low, medium, or high
        /// </summary>
        public string StorySaturation;

        /// <summary>
        ///Story Volume gives the number of articles published 
        /// </summary>
        public int StoryVolume;

        /// <summary>
        /// Determines if the article is the first to mention a specific story (company + event pair) within a two-week period
        /// </summary>
        public bool FirstMention;

        /// <summary>
        /// Classifies the sources as “blog,” “news,” and “tweet.” Depending on the source of the article, an appropriate “type” tag is given to the article
        /// </summary>
        public string ArticleType;

        /// <summary>
        /// A sentiment score given to each article based on how the author has written it
        /// </summary>
        public decimal ArticleSentiment;

        /// <summary>
        /// Evaluates the overall credibility of a source based on past stories it released
        /// </summary>
        public int OverallSourceRank;

        /// <summary>
        /// Evaluates the event-specific credibility of a source based on past stories it released
        /// </summary>
        public int EventSourceRank;

        /// <summary>
        /// Source Rank of 2nd event if any
        /// </summary>
        public int EventSourceRank2;

        /// <summary>
        /// Evaluates the overall credibility of the author based on past stories it released
        /// </summary>
        public int OverallAuthorRank;

        /// <summary>
        /// Evaluates the event-specific credibility of the author based on past stories it released
        /// </summary>
        public int EventAuthorRank;

        /// <summary>
        /// Author rank of the 2nd event if any
        /// </summary>
        public int EventAuthorRank2;

        /// <summary>
        /// Percentage possibility that the event will generally lead to a 1% or more "same day" change in the stock price of a company
        /// </summary>
        public decimal OverallEventImpactScore;

        /// <summary>
        /// Percentage possibility that the story will lead to a 1% or more "same day" change in the stock prices of the company
        /// </summary>
        public int EventEntityImpactScore;

        /// <summary>
        /// Impact Score of the 2nd event story if any
        /// </summary>
        public int EventEntityImpactScore2;

        /// <summary>
        /// Provides a summary of the event group if any available eg. Corporate Action, Innovation, etc.
        /// </summary>
        public string EventSummaryGroup;

        /// <summary>
        /// Gives a general theme of the event if any available eg. Crime, Project, Management, etc.
        /// </summary>
        public string EventSummaryTheme;

        /// <summary>
        /// Gives the main topic of the event if any available eg. Corporation, Government, etc.
        /// </summary>
        public string EventSummaryTopic;

        /// <summary>
        /// Gives what the action was for the summary eg, Increased, Decreased, Charity, Stop, Control, etc.
        /// </summary>
        public string EventSummaryAction;

        /// <summary>
        /// Tells us the sub-theme of the event
        /// </summary>
        public string EventSummarySubTheme;

        /// <summary>
        /// Tells us about the acting parties of the event eg. Government, Industry, Company, Production, etc.
        /// </summary>
        public string EventSummaryActingParty;

        /// <summary>
        /// Direct URLs to the articles which were captured.
        /// </summary>
        public string ArticleUrl;

        /// <summary>
        /// Return the URL external source for the data: QuantConnect will download it an read it line by line automatically:
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLive)
        {
            var address = Path.Combine(Globals.DataFolder, "news", "accern", config.Symbol.Value.ToLower(), date.ToString("yyyyMM") + ".zip");
            return new SubscriptionDataSource(address, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Convert each line of the file above into an object.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLive)
        {
            var accernData = new Accern();
            
            var data = line.Split(',');

            accernData.ArticleId = data[0];
            accernData.StoryId = data[1];
            accernData.Time = DateTime.ParseExact(data[2], "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);
            accernData.Name = data[3];
            accernData.Symbol = Symbol.Create(data[4], SecurityType.Base, QuantConnect.Market.USA);
            accernData.GlobalId = data[5];
            accernData.EntityId = data[6];
            accernData.Type = data[7];
            accernData.Exchange = data[8];
            accernData.Sector = data[9];
            accernData.Industry = data[10];
            accernData.Country = data[11];
            accernData.Region = data[12];
            accernData.Index = data[13];
            accernData.Competitors = data[14];
            accernData.Name2 = data[15];
            accernData.Symbol2 = data[16];
            accernData.GlobalId2 = data[17];
            accernData.EntityId2 = data[18];
            accernData.Type2 = data[19];
            accernData.Exchange2 = data[20];
            accernData.Sector2 = data[21];
            accernData.Industry2 = data[22];
            accernData.Country2 = data[23];
            accernData.Region2 = data[24];
            accernData.Index2 = data[25];
            accernData.Competitors2 = data[26];
            accernData.GroupName = data[27];
            accernData.GroupType = data[28];
            accernData.GroupName2 = data[29];
            accernData.GroupType2 = data[30];
            if(data[31] != "") accernData.StorySentiment = Convert.ToDecimal(data[31]);
            accernData.StorySaturation = data[32];
            if (data[33] != "") accernData.StoryVolume = Convert.ToInt32(data[33]);
            if (data[34] != "") accernData.FirstMention = Convert.ToBoolean(data[34]);
            accernData.ArticleType = data[35];
            if (data[36] != "") accernData.ArticleSentiment = Convert.ToDecimal(data[36]);
            if (data[37] != "") accernData.OverallSourceRank = Convert.ToInt32(data[37]);
            if (data[38] != "") accernData.EventSourceRank = Convert.ToInt32(data[38]);
            if (data[39] != "") accernData.EventSourceRank2 = Convert.ToInt32(data[39]);
            if (data[40] != "") accernData.OverallAuthorRank = Convert.ToInt32(data[40]);
            if (data[41] != "") accernData.EventAuthorRank = Convert.ToInt32(data[41]);
            if (data[42] != "") accernData.EventAuthorRank2 = Convert.ToInt32(data[42]);
            if (data[43] != "") accernData.OverallEventImpactScore = Convert.ToDecimal(data[43]);
            if (data[44] != "") accernData.EventEntityImpactScore = Convert.ToInt32(data[44]);
            if (data[45] != "") accernData.EventEntityImpactScore2 = Convert.ToInt32(data[45]);
            accernData.EventSummaryGroup = data[46];
            accernData.EventSummaryTheme = data[47];
            accernData.EventSummaryTopic = data[48];
            accernData.EventSummaryAction = data[49];
            accernData.EventSummarySubTheme = data[50];
            accernData.EventSummaryActingParty = data[51];
            accernData.ArticleUrl = data[52];

            return accernData;
        }
        
    }
}
