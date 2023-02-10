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
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)

        spy = self.AddEquity("SPY").Symbol

        spyCusip = spy.CUSIP
        spyCompositeFigi = spy.CompositeFIGI
        spySedol = spy.SEDOL
        spyIsin = spy.ISIN

        self.CheckSymbolRepresentation(spyCusip, "CUSIP")
        self.CheckSymbolRepresentation(spyCompositeFigi, "Composite FIGI")
        self.CheckSymbolRepresentation(spySedol, "SEDOL")
        self.CheckSymbolRepresentation(spyIsin, "ISIN")

        # Check Symbol API vs QCAlgorithm API
        self.CheckAPIsSymbolRepresentations(spyCusip, self.CUSIP(spy), "CUSIP");
        self.CheckAPIsSymbolRepresentations(spyCompositeFigi, self.CompositeFIGI(spy), "Composite FIGI");
        self.CheckAPIsSymbolRepresentations(spySedol, self.SEDOL(spy), "SEDOL");
        self.CheckAPIsSymbolRepresentations(spyIsin, self.ISIN(spy), "ISIN");

        self.Log(f"\nSPY CUSIP: {spyCusip}"
                 f"\nSPY Composite FIGI: {spyCompositeFigi}"
                 f"\nSPY SEDOL: {spySedol}"
                 f"\nSPY ISIN: {spyIsin}")

    def CheckSymbolRepresentation(symbol: str, standard: str) -> None:
        if not symbol:
            raise Exception(f"{standard} symbol representation is null or empty")

    def CheckAPIsSymbolRepresentations(symbolApiSymbol: str, algorithmApiSymbol: str, standard: str) -> None:
        if symbolApiSymbol != algorithmApiSymbol:
            raise Exception(f"Symbol API {standard} symbol representation ({symbolApiSymbol}) does not match "
                            f"QCAlgorithm API {standard} symbol representation ({algorithmApiSymbol})")
