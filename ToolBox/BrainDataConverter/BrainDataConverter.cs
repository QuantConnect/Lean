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
using QuantConnect.Data;
using QuantConnect.Data.Custom.BrainData;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.BrainDataConverter
{
    public class BrainDataConverter : SingleLineDataConverter
    {
        /// <summary>
        /// Creates an instance of the object. Note: construct your <see cref="DirectoryInfo"/> instance
        /// to point at the `braindata` folder, but don't specify the sentiment or stock ranking folders
        /// </summary>
        /// <param name="sourceDirectory">Directory where we load raw data from</param>
        /// <param name="destinationDirectory">The data's final destination directory</param>
        public BrainDataConverter(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, string market = Market.USA)
            : base(sourceDirectory, destinationDirectory, market)
        {
        }

        /// <summary>
        /// Converts the data by date
        /// </summary>
        /// <param name="date">Date to convert data from</param>
        /// <returns>Boolean value indicating success status</returns>
        public override bool Convert(DateTime date)
        {
            var success = true;

            var sentimentSevenSourceFile = new FileInfo(Path.Combine(SourceDirectory.FullName, "sentiment", $"sent_us_ndays_7_{date:yyyyMMdd}.csv"));
            var sentimentSevenFinalDirectory = new DirectoryInfo(Path.Combine(DestinationDirectory.FullName, "sentiment_us_weekly"));
            var sentimentThirtySourceFile = new FileInfo(Path.Combine(SourceDirectory.FullName, "sentiment", $"sent_us_ndays_30_{date:yyyyMMdd}.csv"));
            var sentimentThirtyFinalDirectory = new DirectoryInfo(Path.Combine(DestinationDirectory.FullName, "sentiment_us_monthly"));

            var rankingFiveSourceFile = new FileInfo(Path.Combine(SourceDirectory.FullName, "rankings", $"ml_alpha_5_days_{date:yyyyMMdd}.csv"));
            var rankingFiveFinalDirectory = new DirectoryInfo(Path.Combine(DestinationDirectory.FullName, "rankings_five_day"));
            var rankingTenSourceFile = new FileInfo(Path.Combine(SourceDirectory.FullName, "rankings", $"ml_alpha_10_days_{date:yyyyMMdd}.csv"));
            var rankingTenFinalDirectory = new DirectoryInfo(Path.Combine(DestinationDirectory.FullName, "rankings_ten_day"));

            // Create the directories so that we don't get an error if we try to move a file to a non-existent directory
            sentimentSevenFinalDirectory.Create();
            sentimentThirtyFinalDirectory.Create();
            rankingFiveFinalDirectory.Create();
            rankingTenFinalDirectory.Create();

            // Multiple try...catch so that we can attempt to write all files before exiting
            try
            {
                using (var sentimentStream = sentimentSevenSourceFile.OpenRead())
                {
                    WriteToFile(
                        Process<BrainDataSentimentWeekly>(date, sentimentStream),
                        sentimentSevenFinalDirectory
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"BrainDataConverter.Convert(): Failed to process seven day sentiment data: {sentimentSevenSourceFile.FullName}");
                success = false;
            }
            try
            {
                using (var sentimentStream = sentimentThirtySourceFile.OpenRead())
                {
                    WriteToFile(
                        Process<BrainDataSentimentMonthly>(date, sentimentStream),
                        sentimentThirtyFinalDirectory
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"BrainDataConverter.Convert(): Failed to process thirty day sentiment data: {sentimentThirtySourceFile.FullName}");
                success = false;
            }

            try
            {
                using (var rankingStream = rankingFiveSourceFile.OpenRead())
                {
                    WriteToFile(
                        Process<BrainDataRankingsFiveDay>(date, rankingStream),
                        rankingFiveFinalDirectory
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"BrainDataConverter.Convert(): Failed to process five day ranking data: {rankingFiveSourceFile.FullName}");
                success = false;
            }
            try
            {
                using (var rankingStream = rankingTenSourceFile.OpenRead())
                {
                    WriteToFile(
                        Process<BrainDataRankingsTenDay>(date, rankingStream),
                        rankingTenFinalDirectory
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"BrainDataConverter.Convert(): Failed to process ten day ranking data: {rankingTenSourceFile.FullName}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Skips the header row of raw Brain Data data
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override bool ShouldSkipLine<T>(string line)
        {
            return line.StartsWith("DATE");
        }

        /// <summary>
        /// Converts formatted data to an instance of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">BaseData derived type</typeparam>
        /// <param name="ticker">Ticker of the data we're processing</param>
        /// <param name="line">Line of formatted data. This should come from the Data Folder</param>
        /// <returns>Instance of T</returns>
        public override T GetDataInstance<T>(string ticker, string line)
        {
            var mockSubscription = new SubscriptionDataConfig(
                typeof(T),
                Symbol.Create(ticker, SecurityType.Base, Market.USA),
                Resolution.Daily,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var instance = new T();
            return (T)instance.Reader(mockSubscription, line, DateTime.MinValue, false);
        }

        /// <summary>
        /// Converts raw data to instance of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">BaseData derived type</typeparam>
        /// <param name="line">Line of unformatted/raw data</param>
        /// <returns>Instance of T</returns>
        public override T GetDataInstanceFromRaw<T>(string line)
        {
            var csv = line.ToCsv();

            if (typeof(T) == typeof(BrainDataSentimentWeekly))
            {
                return (T)(BaseData)new BrainDataSentimentWeekly
                {
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Symbol = Symbol.Create(csv[1], SecurityType.Base, Market.USA),
                    Sector = csv[2],
                    SentimentScore = System.Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture)
                };
            }
            if (typeof(T) == typeof(BrainDataSentimentMonthly))
            {
                return (T)(BaseData)new BrainDataSentimentMonthly
                {
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Symbol = Symbol.Create(csv[1], SecurityType.Base, Market.USA),
                    Sector = csv[2],
                    SentimentScore = System.Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture)
                };
            }
            if (typeof(T) == typeof(BrainDataRankingsFiveDay))
            {
                return (T)(BaseData)new BrainDataRankingsFiveDay
                {
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Symbol = Symbol.Create(csv[2], SecurityType.Base, Market.USA),
                    RankingScore = System.Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture)
                };
            }
            if (typeof(T) == typeof(BrainDataRankingsTenDay))
            {
                return (T)(BaseData)new BrainDataRankingsTenDay
                {
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Symbol = Symbol.Create(csv[2], SecurityType.Base, Market.USA),
                    RankingScore = System.Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture)
                };
            }

            throw new NotImplementedException($"Type \"{typeof(T).Name}\" is not supported");
        }

        /// <summary>
        /// Serializes the instance to a string
        /// </summary>
        /// <typeparam name="T">Type of instance to serialize to string</typeparam>
        /// <param name="instance">Instance object to serialize to string</param>
        /// <returns>serialized data (as string)</returns>
        public override string Serialize<T>(T instance)
        {
            if (typeof(T) == typeof(BrainDataSentimentWeekly))
            {
                var castInstance = (BrainDataSentimentWeekly)(BaseData)instance;
                return $"{castInstance.Time:yyyyMMdd HH:mm},{castInstance.Sector},{castInstance.SentimentScore}";
            }
            if (typeof(T) == typeof(BrainDataSentimentMonthly))
            {
                var castInstance = (BrainDataSentimentMonthly)(BaseData)instance;
                return $"{castInstance.Time:yyyyMMdd HH:mm},{castInstance.Sector},{castInstance.SentimentScore}";
            }
            if (typeof(T) == typeof(BrainDataRankingsFiveDay))
            {
                var castInstance = (BrainDataRankingsFiveDay)(BaseData)instance;
                return $"{castInstance.Time:yyyyMMdd HH:mm},{castInstance.RankingScore}";
            }
            if (typeof(T) == typeof(BrainDataRankingsTenDay))
            {
                var castInstance = (BrainDataRankingsTenDay)(BaseData)instance;
                return $"{castInstance.Time:yyyyMMdd HH:mm},{castInstance.RankingScore}";
            }

            throw new NotImplementedException($"Type \"{typeof(T).Name}\" is not supported");
        }
    }
}
