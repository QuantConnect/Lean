from .__Fundamental_23 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class DividendReceivedCFOCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend received on investment, in the Operating Cash Flow section.
    
    DividendReceivedCFOCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendReceivedCFOCashFlowStatement:
        pass

    SixMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendsPaidDirectCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend paid to the investors, for the direct cash flow.
    
    DividendsPaidDirectCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendsPaidDirectCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendsPayableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of the carrying values of dividends declared but unpaid on equity securities issued and outstanding (also includes dividends
                collected on behalf of another owner of securities that are being held by entity) by the entity.
    
    DividendsPayableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendsPayableBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendsReceivedCFICashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend received on investment, in the Investing Cash Flow section.
    
    DividendsReceivedCFICashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendsReceivedCFICashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DividendsReceivedDirectCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend received on the investment, for the direct cash flow.
    
    DividendsReceivedDirectCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DividendsReceivedDirectCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DPSGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's dividends per share (DPS) on a percentage basis. Morningstar calculates the annualized growth
                percentage based on the underlying DPS from its dividend database.  Morningstar collects its DPS from company filings and
                reports, as well as from third party sources.
    
    DPSGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DPSGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DueFromRelatedPartiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    For an unclassified balance sheet, carrying amount as of the balance sheet date of obligations due all related parties.
    
    DueFromRelatedPartiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DueFromRelatedPartiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DuefromRelatedPartiesCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owed to the company from a non-arm's length entity, due within the company's current operating cycle.
    
    DuefromRelatedPartiesCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DuefromRelatedPartiesCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DuefromRelatedPartiesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owed to the company from a non-arm's length entity, due after the company's current operating cycle.
    
    DuefromRelatedPartiesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DuefromRelatedPartiesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DuetoRelatedPartiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owed by the company to a non-arm's length entity.
    
    DuetoRelatedPartiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DuetoRelatedPartiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DuetoRelatedPartiesCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owed by the company to a non-arm's length entity that has to be repaid within the company's current operating cycle.
    
    DuetoRelatedPartiesCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DuetoRelatedPartiesCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DuetoRelatedPartiesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owed by the company to a non-arm's length entity that has to be repaid after the company's current operating cycle.
    
    DuetoRelatedPartiesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DuetoRelatedPartiesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EarningRatios(System.object):
    """
    Definition of the EarningRatios class
    
    EarningRatios()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.EarningRatios) -> None:
        pass

    BookValuePerShareGrowth: QuantConnect.Data.Fundamental.BookValuePerShareGrowth

    DilutedContEPSGrowth: QuantConnect.Data.Fundamental.DilutedContEPSGrowth

    DilutedEPSGrowth: QuantConnect.Data.Fundamental.DilutedEPSGrowth

    DPSGrowth: QuantConnect.Data.Fundamental.DPSGrowth

    EquityPerShareGrowth: QuantConnect.Data.Fundamental.EquityPerShareGrowth

    FCFPerShareGrowth: QuantConnect.Data.Fundamental.FCFPerShareGrowth

    NormalizedBasicEPSGrowth: QuantConnect.Data.Fundamental.NormalizedBasicEPSGrowth

    NormalizedDilutedEPSGrowth: QuantConnect.Data.Fundamental.NormalizedDilutedEPSGrowth

    RegressionGrowthofDividends5Years: QuantConnect.Data.Fundamental.RegressionGrowthofDividends5Years
