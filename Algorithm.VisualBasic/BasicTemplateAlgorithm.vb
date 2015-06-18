' QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
' Lean Algorithmic Trading Engine v2.0. Copyright 2015 QuantConnect Corporation.
' 
' Licensed under the Apache License, Version 2.0 (the "License"); 
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at http:'www.apache.org/licenses/LICENSE-2.0
' 
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.

'BasicTemplate Algorithm Class in Visual Basic
Imports QuantConnect.Data.Market

Public Class BasicTemplateAlgorithm
    Inherits QCAlgorithm

    ' Initialize your algorithm
    Public Overrides Sub Initialize()
        SetCash(100000)
        SetStartDate(2013, 10, 7)
        SetEndDate(2013, 10, 11)
        AddSecurity(SecurityType.Equity, "SPY", Resolution.Second)
    End Sub

    ' Handle TradeBar Data Eventss
    Public Sub OnData(data As TradeBars)
        If Not Portfolio.Invested Then
            SetHoldings("SPY", 1)
        End If
    End Sub

End Class
