from .__Fundamental_21 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class DepreciationAndAmortizationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current period expense charged against earnings on long-lived, physical assets used in the normal conduct of business and not
                intended for resale to allocate or recognize the cost of assets over their useful lives; or to record the reduction in book value of an
                intangible asset over the benefit period of such asset.
    
    DepreciationAndAmortizationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DepreciationAndAmortizationCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DepreciationAndAmortizationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The sum of depreciation and amortization expense in the Income Statement.
                Depreciation is the non-cash expense recognized on tangible assets used in the normal course of business, by allocating the cost of
                assets over their useful lives
                Amortization is the non-cash expense recognized on intangible assets over the benefit period of the asset.
    
    DepreciationAndAmortizationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DepreciationAndAmortizationIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DepreciationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An expense recorded to allocate a tangible asset's cost over its useful life. Since it is a non-cash expense, it increases free cash
                flow while decreasing reported earnings.
    
    DepreciationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DepreciationCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DepreciationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current period non-cash expense recognized on tangible assets used in the normal course of business, by allocating the cost of
                assets over their useful lives, in the Income Statement. Examples of tangible asset include buildings, production and equipment.
    
    DepreciationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DepreciationIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DepreciationSupplementalIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current period expense charged against earnings on tangible asset over its useful life. It is a supplemental value which would
                be reported outside consolidated statements.
    
    DepreciationSupplementalIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DepreciationSupplementalIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DerivativeAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fair values of assets resulting from contracts that meet the criteria of being accounted for as derivative instruments, net of the
                effects of master netting arrangements.
    
    DerivativeAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DerivativeAssetsBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DerivativeProductLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fair values of all liabilities resulting from contracts that meet the criteria of being accounted for as derivative instruments; and
                which are expected to be extinguished or otherwise disposed of after one year or beyond the normal operating cycle.
    
    DerivativeProductLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DerivativeProductLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedAccountingChange(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS from Cumulative Effect Accounting Changes is the earnings from accounting changes (in the reporting period) divided
                by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a
                dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock.
    
    DilutedAccountingChange(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedAccountingChange:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedAverageShares(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The shares outstanding used to calculate the diluted EPS, assuming the conversion of all convertible securities and the exercise of
                warrants or stock options. It is the weighted average diluted share outstanding through the whole accounting PeriodAsByte.  Note: If
                Diluted Average Shares are not presented by the firm in the Income Statement and Basic Average Shares are presented, Diluted
                Average Shares will equal Basic Average Shares.  However, if neither value is presented by the firm, Diluted Average Shares will be
                null.
    
    DilutedAverageShares(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedAverageShares:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedContEPSGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's diluted EPS from continuing operations on a percentage basis. Morningstar calculates the annualized
                growth percentage based on the underlying diluted EPS from continuing operations reported in the Income Statement within the
                company filings or reports.
    
    DilutedContEPSGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedContEPSGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
