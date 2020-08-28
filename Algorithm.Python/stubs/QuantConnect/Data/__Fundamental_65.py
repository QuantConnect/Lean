from .__Fundamental_66 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class TaxRateForCalcsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Tax rate used for Morningstar calculations.
    
    TaxRateForCalcsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxRateForCalcsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalAdjustmentsforNonCashItemsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of all adjustments back from net income but without real cash outflow or inflow.
    
    TotalAdjustmentsforNonCashItemsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalAdjustmentsforNonCashItemsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of probable future economic benefits obtained or controlled by a particular enterprise as a result of past
                transactions or events.
    
    TotalAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalAssetsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalAssetsGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the total assets on a percentage basis. Morningstar calculates the growth percentage based on the total assets
                reported in the Balance Sheet within the company filings or reports.
    
    TotalAssetsGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalAssetsGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalCapitalizationBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Stockholder's Equity plus Long Term Debt.
    
    TotalCapitalizationBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalCapitalizationBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDebtBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All borrowings incurred by the company including debt and capital lease obligations.
    
    TotalDebtBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDebtBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDebtEquityRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of Total Debt to Common Equity. Morningstar calculates the ratio by using the underlying data reported in the
                Balance Sheet within the company filings or reports: (Current Debt And Current Capital Lease Obligation + Long-Term Debt And
                Long-Term Capital Lease Obligation / Common Equity.   [Note: Common Equity = Total Shareholder's Equity - Preferred Stock]
    
    TotalDebtEquityRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDebtEquityRatio:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDebtEquityRatioGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's total debt to equity ratio on a percentage basis. Morningstar calculates the growth percentage based
                on the total debt divided by the shareholder's equity reported in the Balance Sheet within the company filings or reports.
    
    TotalDebtEquityRatioGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDebtEquityRatioGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDebtInMaturityScheduleBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total Debt in Maturity Schedule is the sum of Debt details above.
    
    TotalDebtInMaturityScheduleBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDebtInMaturityScheduleBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDeferredCreditsAndOtherNonCurrentLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue received by a firm but not yet reported as income.  This item is usually only available for utility industry.
    
    TotalDeferredCreditsAndOtherNonCurrentLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDeferredCreditsAndOtherNonCurrentLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDepositsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A liability account which represents the total amount of funds deposited.
    
    TotalDepositsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDepositsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDividendPaymentofEquitySharesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total amount paid in dividends to equity securities investors.
    
    TotalDividendPaymentofEquitySharesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDividendPaymentofEquitySharesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalDividendPaymentofNonEquitySharesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total amount paid in dividends to Non-Equity securities investors.
    
    TotalDividendPaymentofNonEquitySharesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDividendPaymentofNonEquitySharesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
