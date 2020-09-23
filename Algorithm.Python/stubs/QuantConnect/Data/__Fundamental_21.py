from .__Fundamental_22 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class DilutedContinuousOperations(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS from Continuing Operations is the earnings from continuing operations divided by the common shares outstanding
                adjusted for the assumed conversion of all potentially dilutive securities.  Securities having a dilutive effect may include convertible
                debentures, warrants, options, and convertible preferred stock.
    
    DilutedContinuousOperations(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedContinuousOperations:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedDiscontinuousOperations(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS from Discontinued Operations is the earnings from discontinued operations divided by the common shares outstanding
                adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible
                debentures, warrants, options, and convertible preferred stock. This only includes gain or loss from discontinued operations.
    
    DilutedDiscontinuousOperations(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedDiscontinuousOperations:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS is the bottom line net income divided by the common shares outstanding adjusted for the assumed conversion of all
                potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and
                convertible preferred stock. This value will be derived when not reported for the fourth quarter and will be less than or equal to
                Basic EPS.
    
    DilutedEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedEPS:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedEPSGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's diluted earnings per share (EPS) on a percentage basis. Morningstar calculates the annualized growth
                percentage based on the underlying diluted EPS reported in the Income Statement within the company filings or reports.
    
    DilutedEPSGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedEPSGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedEPSOtherGainsLosses(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The earnings from gains and losses (in the reporting period) divided by the common shares outstanding adjusted for the assumed
                conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants,
                options, convertible preferred stock, etc.
    
    DilutedEPSOtherGainsLosses(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedEPSOtherGainsLosses:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedExtraordinary(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS from Extraordinary Gain/Losses is the gain or loss from extraordinary items divided by the common shares outstanding
                adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible
                debentures, warrants, options, and convertible preferred stock.
    
    DilutedExtraordinary(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedExtraordinary:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DilutedNIAvailtoComStockholdersIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net income to calculate Diluted EPS, accounting for adjustments assuming that all the convertible instruments are being converted
                to Common Equity.
    
    DilutedNIAvailtoComStockholdersIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DilutedNIAvailtoComStockholdersIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendCoverageRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Reflects a firm's capacity to pay a dividend, and is defined as Earnings Per Share / Dividend Per Share
    
    DividendCoverageRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendCoverageRatio:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividends earned from equity investment securities. This item is usually only available for bank industry.
    
    DividendIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendPaidCFOCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend paid to the investors, in the Operating Cash Flow section.
    
    DividendPaidCFOCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendPaidCFOCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendPerShare(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of dividend that a stockholder will receive for each share of stock held. It can be calculated by taking the total amount
                of dividends paid and dividing it by the total shares outstanding. Dividend per share = total dividend payment/total number of
                outstanding shares
    
    DividendPerShare(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendPerShare:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
