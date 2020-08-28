from .__Fundamental_69 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class TradeandOtherPayablesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of all non-current payables and accrued expenses.
    
    TradeandOtherPayablesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradeandOtherPayablesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradeAndOtherReceivablesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts due from customers or clients, more than one year from the balance sheet date, for goods or services that have been
                delivered or sold in the normal course of business, or other receivables.
    
    TradeAndOtherReceivablesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradeAndOtherReceivablesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingandFinancialLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total carrying amount of total trading, financial liabilities and debt in a non-differentiated balance sheet.
    
    TradingandFinancialLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingandFinancialLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingAndOtherReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This will serve as the "parent" value to AccountsReceivable (DataId 23001) and OtherReceivables (DataId 23342) for all company
                financials reported in the IFRS GAAP.
    
    TradingAndOtherReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingAndOtherReceivableBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Trading account assets are bought and held principally for the purpose of selling them in the near term (thus held for only a short
                period of time). Unrealized holding gains and losses for trading securities are included in earnings.
    
    TradingAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingAssetsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingGainLossIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A broker-dealer or other financial entity may buy and sell securities exclusively for its own account, sometimes referred to as
                proprietary trading. The profit or loss is measured by the difference between the acquisition cost and the selling price or current
                market or fair value. The net gain or loss, includes both realized and unrealized, from trading cash instruments, equities and
                derivative contracts (including commodity contracts) that has been recognized during the accounting period for the broker dealer or
                other financial entity's own account. This item is typically available for bank industry.
    
    TradingGainLossIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingGainLossIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amount of liabilities as of the balance sheet date that pertain to principal and customer trading transactions, or which
                may be incurred with the objective of generating a profit from short-term fluctuations in price as part of an entity's market-making,
                hedging and proprietary trading. Examples include short positions in securities, derivatives and commodities, obligations under
                repurchase agreements, and securities borrowed arrangements.
    
    TradingLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TradingSecuritiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total of financial instruments that are bought and held principally for the purpose of selling them in the near term (thus held for
                only a short period of time) or for debt and equity securities formerly categorized as available-for-sale or held-to-maturity which the
                company held as of the date it opted to account for such securities at fair value.
    
    TradingSecuritiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TradingSecuritiesBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TreasuryBillsandOtherEligibleBillsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Investments backed by the central government, it usually carries less risk than other investments.
    
    TreasuryBillsandOtherEligibleBillsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TreasuryBillsandOtherEligibleBillsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TreasurySharesNumberBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Number of Treasury Shares.
    
    TreasurySharesNumberBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TreasurySharesNumberBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TreasuryStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The portion of shares that a company keeps in their own treasury. Treasury stock may have come from a repurchase or buyback
                from shareholders; or it may have never been issued to the public in the first place. These shares don't pay dividends, have no
                voting rights, and are not included in shares outstanding calculations.
    
    TreasuryStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TreasuryStockBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
