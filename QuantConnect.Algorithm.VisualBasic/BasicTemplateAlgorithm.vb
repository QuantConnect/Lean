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
