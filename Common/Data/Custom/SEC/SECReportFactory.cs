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

using Newtonsoft.Json;
using System.Data;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace QuantConnect.Data.Custom.SEC
{
    public class SECReportFactory
    {
        /// <summary>
        /// Factory method creates SEC report by deserializing XML formatted SEC data to <see cref="SECReportSubmission"/> 
        /// </summary>
        /// <param name="xmlText">XML text containing SEC data</param>
        public ISECReport CreateSECReport(string xmlText)
        {
            var secReportXml = new XmlDocument();
            secReportXml.LoadXml(xmlText);

            var json = JsonConvert.SerializeXmlNode(secReportXml, Formatting.None, true);
            var secReportDocument = JsonConvert.DeserializeObject<SECReportSubmission>(json, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var formType = secReportDocument.FormType;

            switch (formType)
            {
                case "8-K":
                    return new SECReport8K(secReportDocument);
                case "10-K":
                    return new SECReport10K(secReportDocument);
                case "10-Q":
                    return new SECReport10Q(secReportDocument);
                default:
                    throw new DataException($"SEC form type {formType} is not supported at this time");
            }
        }
    }
}
