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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Logging;
using QuantConnect.Util;
using Formatting = Newtonsoft.Json.Formatting;

namespace QuantConnect.ToolBox.SECDataDownloader
{
    public class SECDataConverter
    {
        /// <summary>
        /// Raw data source path
        /// </summary>
        public string RawSource;
        
        /// <summary>
        /// Destination of formatted data
        /// </summary>
        public string Destination;

        /// <summary>
        /// Max file size in bytes we're willing to parse. Default is 50Mb
        /// </summary>
        public int MaxFileSize = 50000000;

        /// <summary>
        /// Assets keyed by CIK used to resolve underlying ticker 
        /// </summary>
        public readonly Dictionary<string, List<string>> CikTicker = new Dictionary<string, List<string>>();

        /// <summary>
        /// Keyed by CIK, keyed by accession number, contains the publication date for a report
        /// </summary>
        public ConcurrentDictionary<string, Dictionary<string, DateTime>> PublicationDates = new ConcurrentDictionary<string, Dictionary<string, DateTime>>();

        /// <summary>
        /// Keyed by ticker (CIK if ticker not found); contains SEC report(s) that we pass to <see cref="WriteReport"/>
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentDictionary<DateTime, List<ISECReport>>> Reports = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, List<ISECReport>>>();

        /// <summary>
        /// List of known equities taken from the daily data folder
        /// </summary>
        public List<string> KnownEquities;

        public SECDataConverter(string rawSource, string destination, string knownEquityFolder)
        {
            RawSource = rawSource;
            Destination = destination;
            
            var data = File.ReadAllText(Path.Combine(RawSource, "cik-ticker-mappings.txt")).Trim();
            
            // Process data into dictionary of CIK -> List{T} of tickers
            data.Split('\n')
                .Select(x => x.Split('\t'))
                .ToList()
                .ForEach(
                    tickerCik =>
                    {
                        // tickerCik[0] = symbol, tickerCik[1] = CIK
                        var cikFormatted = tickerCik[1].PadLeft(10, '0');

                        List<string> symbol;
                        if (!CikTicker.TryGetValue(cikFormatted, out symbol))
                        {
                            symbol = new List<string>();
                            CikTicker[cikFormatted] = symbol;
                        }

                        symbol.Add(tickerCik[0]);
                    }
                );
            
            KnownEquities = Directory.GetFiles(knownEquityFolder)
                .Select(x => Path.GetFileNameWithoutExtension(x).ToLower())
                .ToList();
        }

        /// <summary>
        /// Converts the data from raw format (*.nz.tar.gz) to json files consumable by LEAN
        /// </summary>
        /// <param name="startDate">Starting date to start process files</param>
        /// <param name="endDate">Ending date to stop processing files</param>
        public void Process(DateTime startDate, DateTime endDate)
        {
            Parallel.ForEach(
                Directory.GetFiles(RawSource, "*.nc.tar.gz", SearchOption.AllDirectories).ToList(),
                rawFile =>
                {
                    // GetFileNameWithoutExtension only strips the first extension from the name.
                    var fileDate = Path.GetFileName(rawFile).Split('.')[0];
                    var extractDataPath = Path.Combine(RawSource, fileDate);

                    DateTime currentDate;
                    if (!DateTime.TryParseExact(fileDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out currentDate))
                    {
                        throw new Exception($"Unable to parse date from file {rawFile}. Filename we attempted to parse: {fileDate}");
                    }
                    
                    // Only process files within start and end bounds
                    if (currentDate < startDate || currentDate > endDate)
                    {
                        return;
                    }

                    using (var data = File.OpenRead(rawFile))
                    {
                        using (var archive = TarArchive.CreateInputTarArchive(new GZipInputStream(data)))
                        {
                            Directory.CreateDirectory(extractDataPath);
                            archive.ExtractContents(extractDataPath);

                            Log.Trace($"SECDataConverter.Process(): Extracted SEC data to path {extractDataPath}");
                        }
                    }

                    // For the meantime, let's only process .nc files, and deal with correction files later.
                    Parallel.ForEach(
                        Directory.GetFiles(extractDataPath, "*.nc", SearchOption.AllDirectories),
                        rawReportFilePath =>
                        {
                            // Avoid processing files greater than MaxFileSize megabytes
                            if (MaxFileSize < new FileInfo(rawReportFilePath).Length)
                            {
                                Log.Trace($"SECDataConverter.Process(): File {rawReportFilePath} is too large to process. Continuing...");
                                return;
                            }

                            var factory = new SECReportFactory();
                            var xmlText = new StringBuilder();

                            // We need to escape any nested XML to ensure our deserialization happens smoothly
                            var parsingText = false;

                            foreach (var line in File.ReadLines(rawReportFilePath))
                            {
                                var newTextLine = line;
                                var currentTagName = GetTagNameFromLine(newTextLine);

                                // This tag is present rarely in SEC reports, but is unclosed without value when encountered.
                                // Verified by searching with ripgrep for "CONFIRMING-COPY"
                                if (currentTagName == "CONFIRMING-COPY")
                                {
                                    return;
                                }

                                // Don't encode the closing tag
                                if (currentTagName == "/TEXT")
                                {
                                    parsingText = false;
                                }

                                // To ensure that we can serialize/deserialize data with hours, minutes, seconds
                                if (currentTagName == "FILING-DATE" || currentTagName == "PERIOD" ||
                                    currentTagName == "DATE-OF-FILING-CHANGE" || currentTagName == "DATE-CHANGED")
                                {
                                    newTextLine = $"{newTextLine.TrimEnd()} 00:00:00";
                                }

                                // Encode all contents inside tags to prevent errors in XML parsing.
                                // The json deserializer will convert these values back to their original form
                                if (!parsingText && HasValue(newTextLine))
                                {
                                    newTextLine =
                                        $"<{currentTagName}>{SecurityElement.Escape(GetTagValueFromLine(newTextLine))}</{currentTagName}>";
                                }
                                // Escape all contents inside TEXT tags
                                else if (parsingText)
                                {
                                    newTextLine = SecurityElement.Escape(newTextLine);
                                }

                                // Don't encode the opening tag
                                if (currentTagName == "TEXT")
                                {
                                    parsingText = true;
                                }

                                xmlText.AppendLine(newTextLine);
                            }

                            ISECReport report;
                            try
                            {
                                report = factory.CreateSECReport(xmlText.ToString());
                            }
                            catch (DataException e)
                            {
                                Log.Trace($"SECDataConverter.Process(): {e.Message}");
                                return;
                            }
                            catch (XmlException e)
                            {
                                Log.Error(e, $"SECDataConverter.Process(): Failed to parse XML from file path: {rawReportFilePath}");
                                return;
                            }
                            
                            // First filer listed in SEC report is usually the company listed on stock exchanges
                            var companyCik = report.Report.Filers.First().CompanyData.Cik;

                            // Some companies can operate under two tickers, but have the same CIK.
                            List<string> tickers;
                            if (!CikTicker.TryGetValue(companyCik, out tickers))
                            {
                                tickers = new List<string>();
                            }

                            try
                            {
                                // There can potentially not be an index file present for the given CIK
                                GetPublicationDate(report, companyCik);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e);
                            }

                            // Default to company CIK if no known ticker is found.
                            // We will write a file for every ticker that exists under the CIK to ensure consistency
                            foreach (var ticker in tickers.Where(KnownEquities.Contains).DefaultIfEmpty(companyCik))
                            {
                                var tickerReports = Reports.GetOrAdd(
                                    ticker,
                                    _ => new ConcurrentDictionary<DateTime, List<ISECReport>>()
                                );
                                var reports = tickerReports.GetOrAdd(
                                    report.Report.FilingDate.Date,
                                    _ => new List<ISECReport>()
                                );

                                reports.Add(report);
                            }
                        }
                    );

                    Parallel.ForEach(Reports.Keys, ticker =>
                        {
                            List<ISECReport> reports;
                            if (!Reports[ticker].TryRemove(currentDate, out reports))
                            {
                                return;
                            }

                            WriteReport(reports, ticker);
                        }
                    );

                    // This will clean up after ourselves without having to pay
                    // the expense of deleting every single file inside the raw_data folder
                    Directory.Delete(extractDataPath, true);
                }
            );
        }

        /// <summary>
        /// Writes the report to disk, where it will be used by LEAN.
        /// If a ticker is not found, the company being reported
        /// will be stored with its CIK value as the ticker.
        ///
        /// Any existing duplicate files will be overwritten.
        /// </summary>
        /// <param name="reports">List of SEC Report objects</param>
        /// <param name="ticker">Symbol ticker</param>
        /// <param name="destination">Folder to write the reports to</param>
        public void WriteReport(List<ISECReport> reports, string ticker)
        {
            var report = reports.First();
            var reportPath = Path.Combine(Destination, ticker.ToLower(), $"{report.Report.FilingDate:yyyyMMdd}");
            var formTypeNormalized = report.Report.FormType.Replace("-", "");
            var reportFilePath = $"{reportPath}_{formTypeNormalized}";
            var reportFile = Path.Combine(reportFilePath, $"{formTypeNormalized}.json");

            Directory.CreateDirectory(reportFilePath);

            var reportSubmissions = reports.Select(r => r.Report);

            using (var writer = new StreamWriter(reportFile, false))
            {
                writer.Write(JsonConvert.SerializeObject(reportSubmissions, Formatting.None));
            }

            Compression.ZipDirectory(reportFilePath, $"{reportFilePath}.zip", false);
            Directory.Delete(reportFilePath, true);
        }

        /// <summary>
        /// Takes instance of <see cref="ISECReport"/> and gets publication date information for the given equity, then mutates the instance. 
        /// </summary>
        /// <param name="report">SEC report <see cref="BaseData"/> instance</param>
        /// <param name="companyCik">Company CIK to use to lookup filings for</param>
        /// <remarks>This method caches the results on a per company basis (by CIK), so subsequent lookups for the publication date of the same equity by CIK will be fast</remarks>
        public void GetPublicationDate(ISECReport report, string companyCik)
        {
            Dictionary<string, DateTime> companyPublicationDates;
            if (!PublicationDates.TryGetValue(companyCik, out companyPublicationDates))
            {
                PublicationDates.TryAdd(companyCik, GetReportPublicationTimes(companyCik));
                companyPublicationDates = PublicationDates[companyCik];
            }

            DateTime reportPublicationDate;
            if (companyPublicationDates.TryGetValue(report.Report.AccessionNumber.Replace("-", ""), out reportPublicationDate))
            {
                // Update the filing date to reflect SEC's publication date on their servers
                report.Report.MadeAvailableAt = reportPublicationDate;
            }
        }

        /// <summary>
        /// Gets company CIK values keyed by accession number
        /// </summary>
        /// <param name="cik">Company CIK</param>
        /// <returns><see cref="Dictionary{TKey,TValue}"/> keyed by accession number containing publication date of SEC reports</returns>
        private Dictionary<string, DateTime> GetReportPublicationTimes(string cik)
        {
            var indexFilePath = Path.Combine(RawSource, "indexes", $"{cik}.json");
            var indexFileExists = File.Exists(indexFilePath);

            if (!indexFileExists)
            {
                throw new Exception($"Submission not found for CIK: {cik}");
            }

            var index = JsonConvert.DeserializeObject<SECReportIndexFile>(File.ReadAllText(indexFilePath))
                .Directory;

            // Sometimes, SEC folders results are duplicated. We check for duplicates
            // before creating a dictionary to avoid a duplicate key error.
            return index.Items
                .Where(publication => publication.FileType == "folder.gif")
                .DistinctBy(publication => publication.Name)
                .ToDictionary(publication => publication.Name, publication => publication.LastModified);
        }

        /// <summary>
        /// Determines if the given line has a value associated with the tag
        /// </summary>
        /// <param name="line">Line of text from SEC report</param>
        /// <returns>Boolean indicating whether the line contains a value</returns>
        public static bool HasValue(string line)
        {
            var tagEnd = line.IndexOf(">", StringComparison.Ordinal);

            if (!line.StartsWith("<") || tagEnd == -1)
            {
                return false;
            }

            return line.Length > tagEnd + 1;
        }

        /// <summary>
        /// Gets the line's value (if there is one)
        /// </summary>
        /// <param name="line">Line of text from SEC report</param>
        /// <returns>Value associated with the tag</returns>
        public static string GetTagValueFromLine(string line)
        {
            return line.Substring(line.IndexOf(">", StringComparison.Ordinal) + 1);
        }

        /// <summary>
        /// Gets the tag name from a given line
        /// </summary>
        /// <param name="line">Line of text from SEC report</param>
        /// <returns>Tag name from the line</returns>
        public static string GetTagNameFromLine(string line)
        {
            var start = line.IndexOf("<", StringComparison.Ordinal) + 1;
            var length = line.IndexOf(">", StringComparison.Ordinal) - start;

            if (start == -1 || length <= 0)
            {
                return string.Empty;
            }

            return line.Substring(start, length);
        }
    }
}

