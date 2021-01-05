# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Custom.SEC import *

### <summary>
### Demonstration algorithm showing how to use and access SEC data
### </summary>
### <meta name="tag" content="fundamental" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="SEC" />
class SECReportDataAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2019, 1, 31)
        self.SetCash(100000)

        self.ticker = "AAPL"
        self.symbol = self.AddData(SECReport10Q, self.ticker, Resolution.Daily).Symbol
        self.AddData(SECReport8K, self.ticker, Resolution.Daily)

    def OnData(self, slice):
        # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        data = slice[self.ticker]
        report = data.Report

        self.Log(f"Form Type {report.FormType}")
        self.Log(f"Filing Date: {str(report.FilingDate)}")

        for filer in report.Filers:
            self.Log(f"Filing company name: {filer.CompanyData.ConformedName}")
            self.Log(f"Filing company CIK: {filer.CompanyData.Cik}")
            self.Log(f"Filing company EIN: {filer.CompanyData.IrsNumber}")

            for formerCompany in filer.FormerCompanies:
                self.Log(f"Former company name of {filer.CompanyData.ConformedName}: {formerCompany.FormerConformedName}")
                self.Log(f"Date of company name change: {str(formerCompany.Changed)}")


        # SEC documents can come in multiple documents.
        # For multi-document reports, sometimes the document contents after the first document
        # are files that have a binary format, such as JPG and PDF files
        for document in report.Documents:
            self.Log(f"Filename: {document.Filename}")
            self.Log(f"Document description: {document.Description}")

            # Print sample of contents contained within the document
            self.Log(document.Text[:100])
            self.Log("=================")


