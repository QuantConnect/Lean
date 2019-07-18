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

using System.IO;
using NUnit.Framework;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.ToolBox.SECDataDownloader;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class SECReportTests
    {
        private const string _xmlForm8k = "<SUBMISSION><ACCESSION-NUMBER>0000106455-19-000002</ACCESSION-NUMBER><DATE-OF-FILING-CHANGE>00010101 00:00:00</DATE-OF-FILING-CHANGE><DOCUMENT><element><DESCRIPTION>8-K</DESCRIPTION><FILENAME>f8k_01022019i701.htm</FILENAME><SEQUENCE>1</SEQUENCE><TYPE>8-K</TYPE></element></DOCUMENT><FILER><element><BUSINESS-ADDRESS><element><CITY>ENGLEWOOD</CITY><PHONE>303-922-6463</PHONE><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></BUSINESS-ADDRESS><COMPANY-DATA><ASSIGNED-SIC>1221</ASSIGNED-SIC><CIK>0000106455</CIK><CONFORMED-NAME>WESTMORELAND COAL Co</CONFORMED-NAME><FISCAL-YEAR-END>1231</FISCAL-YEAR-END><IRS-NUMBER>231128670</IRS-NUMBER><STATE-OF-INCORPORATION>DE</STATE-OF-INCORPORATION></COMPANY-DATA><FILING-VALUES><ACT>34</ACT><FILE-NUMBER>001-11155</FILE-NUMBER><FILM-NUMBER>19500087</FILM-NUMBER><FORM-TYPE>8-K</FORM-TYPE></FILING-VALUES><FORMER-COMPANY><element><DATE-CHANGED>19920703 00:00:00</DATE-CHANGED><FORMER-CONFORMED-NAME>WESTMORELAND COAL CO</FORMER-CONFORMED-NAME></element></FORMER-COMPANY><MAIL-ADDRESS><element><CITY>ENGLEWOOD</CITY><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></MAIL-ADDRESS></element></FILER><FILING-DATE>20190102 07:56:32</FILING-DATE><ITEMS>7.01</ITEMS><PERIOD>20190102 00:00:00</PERIOD><PUBLIC-DOCUMENT-COUNT>9</PUBLIC-DOCUMENT-COUNT><TYPE>8-K</TYPE></SUBMISSION>";
        private const string _xmlForm10k = "<SUBMISSION><ACCESSION-NUMBER>0000106455-19-000002</ACCESSION-NUMBER><DATE-OF-FILING-CHANGE>00010101 00:00:00</DATE-OF-FILING-CHANGE><DOCUMENT><element><DESCRIPTION>10-K</DESCRIPTION><FILENAME>f8k_01022019i701.htm</FILENAME><SEQUENCE>1</SEQUENCE><TYPE>10-K</TYPE></element></DOCUMENT><FILER><element><BUSINESS-ADDRESS><element><CITY>ENGLEWOOD</CITY><PHONE>303-922-6463</PHONE><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></BUSINESS-ADDRESS><COMPANY-DATA><ASSIGNED-SIC>1221</ASSIGNED-SIC><CIK>0000106455</CIK><CONFORMED-NAME>WESTMORELAND COAL Co</CONFORMED-NAME><FISCAL-YEAR-END>1231</FISCAL-YEAR-END><IRS-NUMBER>231128670</IRS-NUMBER><STATE-OF-INCORPORATION>DE</STATE-OF-INCORPORATION></COMPANY-DATA><FILING-VALUES><ACT>34</ACT><FILE-NUMBER>001-11155</FILE-NUMBER><FILM-NUMBER>19500087</FILM-NUMBER><FORM-TYPE>10-K</FORM-TYPE></FILING-VALUES><FORMER-COMPANY><element><DATE-CHANGED>19920703 00:00:00</DATE-CHANGED><FORMER-CONFORMED-NAME>WESTMORELAND COAL CO</FORMER-CONFORMED-NAME></element></FORMER-COMPANY><MAIL-ADDRESS><element><CITY>ENGLEWOOD</CITY><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></MAIL-ADDRESS></element></FILER><FILING-DATE>20190102 07:56:32</FILING-DATE><ITEMS>7.01</ITEMS><PERIOD>20190102 00:00:00</PERIOD><PUBLIC-DOCUMENT-COUNT>9</PUBLIC-DOCUMENT-COUNT><TYPE>10-K</TYPE></SUBMISSION>";
        private const string _xmlForm10q = "<SUBMISSION><ACCESSION-NUMBER>0000106455-19-000002</ACCESSION-NUMBER><DATE-OF-FILING-CHANGE>00010101 00:00:00</DATE-OF-FILING-CHANGE><DOCUMENT><element><DESCRIPTION>10-Q</DESCRIPTION><FILENAME>f8k_01022019i701.htm</FILENAME><SEQUENCE>1</SEQUENCE><TYPE>10-Q</TYPE></element></DOCUMENT><FILER><element><BUSINESS-ADDRESS><element><CITY>ENGLEWOOD</CITY><PHONE>303-922-6463</PHONE><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></BUSINESS-ADDRESS><COMPANY-DATA><ASSIGNED-SIC>1221</ASSIGNED-SIC><CIK>0000106455</CIK><CONFORMED-NAME>WESTMORELAND COAL Co</CONFORMED-NAME><FISCAL-YEAR-END>1231</FISCAL-YEAR-END><IRS-NUMBER>231128670</IRS-NUMBER><STATE-OF-INCORPORATION>DE</STATE-OF-INCORPORATION></COMPANY-DATA><FILING-VALUES><ACT>34</ACT><FILE-NUMBER>001-11155</FILE-NUMBER><FILM-NUMBER>19500087</FILM-NUMBER><FORM-TYPE>10-Q</FORM-TYPE></FILING-VALUES><FORMER-COMPANY><element><DATE-CHANGED>19920703 00:00:00</DATE-CHANGED><FORMER-CONFORMED-NAME>WESTMORELAND COAL CO</FORMER-CONFORMED-NAME></element></FORMER-COMPANY><MAIL-ADDRESS><element><CITY>ENGLEWOOD</CITY><STATE>CO</STATE><STREET1>9540 SOUTH MAROON CIRCLE</STREET1><STREET2>SUITE 300</STREET2><ZIP>80112</ZIP></element></MAIL-ADDRESS></element></FILER><FILING-DATE>20190102 07:56:32</FILING-DATE><ITEMS>7.01</ITEMS><PERIOD>20190102 00:00:00</PERIOD><PUBLIC-DOCUMENT-COUNT>9</PUBLIC-DOCUMENT-COUNT><TYPE>10-Q</TYPE></SUBMISSION>";

        [Test]
        public void SECReportFactory_CreatesProperReportType()
        {
            var factory = new SECReportFactory();
            var report8k = factory.CreateSECReport(_xmlForm8k) as SECReport8K;
            var report10k = factory.CreateSECReport(_xmlForm10k) as SECReport10K;
            var report10q = factory.CreateSECReport(_xmlForm10q) as SECReport10Q;

            Assert.NotNull(report8k);
            Assert.NotNull(report10k);
            Assert.NotNull(report10q);
        }

        [TestCase("<ACCESSION-NUMBER>0000106455-19-000002", true)]
        [TestCase("<DATE-OF-FILING-CHANGE>00010101 00:00:00", true)]
        [TestCase("<TEXT>", false)]
        public void SECDataConverter_LineHasValue(string line, bool expected)
        {
            Assert.AreEqual(expected, SECDataConverter.HasValue(line));
        }

        [TestCase("<ACCESSION-NUMBER>0000106455-19-000002", "0000106455-19-000002")]
        [TestCase("<DATE-OF-FILING-CHANGE>00010101 00:00:00", "00010101 00:00:00")]
        [TestCase("<TEXT>", "")]
        public void SECDataConverter_GetsTagValueFromInput(string line, string expected)
        {
            var factory = new SECReportFactory();
            
            Assert.AreEqual(expected, SECDataConverter.GetTagValueFromLine(line));
        }

        [TestCase("<ACCESSION-NUMBER>0000106455-19-000002", "ACCESSION-NUMBER")]
        [TestCase("<DATE-OF-FILING-CHANGE>00010101 00:00:00", "DATE-OF-FILING-CHANGE")]
        [TestCase("<TEXT>", "TEXT")]
        public void SECDataConverter_ShouldReturnInternalTagName(string line, string expected)
        {
            var factory = new SECReportFactory();

            Assert.AreEqual(expected, SECDataConverter.GetTagNameFromLine(line));
        }
        
        [Test]
        public void SECDataConvert_ShouldParseSingleOrMultipleFilingValuesFields()
        {
            var single = File.ReadAllText(Path.Combine("TestData", "sec_report_raw_single.xml"));
            var multiple = File.ReadAllText(Path.Combine("TestData", "sec_report_raw_multiple.xml"));
            var factory = new SECReportFactory();

            Assert.DoesNotThrow(() => factory.CreateSECReport(single));
            Assert.DoesNotThrow(() => factory.CreateSECReport(multiple));
        }
    }
}
