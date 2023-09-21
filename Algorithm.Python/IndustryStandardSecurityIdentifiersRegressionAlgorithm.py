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

from AlgorithmImports import *

### <summary>
### Algorithm illustrating how to get a security's industry-standard identifier from its `Symbol`
### </summary>
class IndustryStandardSecurityIdentifiersRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2014, 6, 5)
        self.SetEndDate(2014, 6, 5)

        equity = self.AddEquity("AAPL").Symbol

        cusip = equity.CUSIP
        compositeFigi = equity.CompositeFIGI
        sedol = equity.SEDOL
        isin = equity.ISIN
        cik = equity.CIK

        self.CheckSymbolRepresentation(cusip, "CUSIP")
        self.CheckSymbolRepresentation(compositeFigi, "Composite FIGI")
        self.CheckSymbolRepresentation(sedol, "SEDOL")
        self.CheckSymbolRepresentation(isin, "ISIN")
        self.CheckSymbolRepresentation(f"{cik}", "CIK")

        # Check Symbol API vs QCAlgorithm API
        self.CheckAPIsSymbolRepresentations(cusip, self.CUSIP(equity), "CUSIP")
        self.CheckAPIsSymbolRepresentations(compositeFigi, self.CompositeFIGI(equity), "Composite FIGI")
        self.CheckAPIsSymbolRepresentations(sedol, self.SEDOL(equity), "SEDOL")
        self.CheckAPIsSymbolRepresentations(isin, self.ISIN(equity), "ISIN")
        self.CheckAPIsSymbolRepresentations(f"{cik}", f"{self.CIK(equity)}", "CIK")

        self.Log(f"\nAAPL CUSIP: {cusip}"
                 f"\nAAPL Composite FIGI: {compositeFigi}"
                 f"\nAAPL SEDOL: {sedol}"
                 f"\nAAPL ISIN: {isin}"
                 f"\nAAPL CIK: {cik}")

    def CheckSymbolRepresentation(self, symbol: str, standard: str) -> None:
        if not symbol:
            raise Exception(f"{standard} symbol representation is null or empty")

    def CheckAPIsSymbolRepresentations(self, symbolApiSymbol: str, algorithmApiSymbol: str, standard: str) -> None:
        if symbolApiSymbol != algorithmApiSymbol:
            raise Exception(f"Symbol API {standard} symbol representation ({symbolApiSymbol}) does not match "
                            f"QCAlgorithm API {standard} symbol representation ({algorithmApiSymbol})")
