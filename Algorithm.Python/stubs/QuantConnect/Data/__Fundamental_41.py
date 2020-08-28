from .__Fundamental_42 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class NetForeignExchangeGainLossIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate foreign currency translation gain or loss (both realized and unrealized) included as part of revenue. This item is
                usually only available for insurance industry.
    
    NetForeignExchangeGainLossIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetForeignExchangeGainLossIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeCommonStockholdersIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net income minus the preferred dividends paid as presented in the Income Statement.
    
    NetIncomeCommonStockholdersIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeCommonStockholdersIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeContinuousOperationsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue less expenses and taxes from the entity's ongoing operations and before income (loss) from: Preferred Dividends;
                Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing Operation; Income from Tax
                Loss Carry forward; Other Gains/Losses.
    
    NetIncomeContinuousOperationsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeContinuousOperationsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeContinuousOperationsNetMinorityInterestIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue less expenses and taxes from the entity's ongoing operations net of minority interest and before income (loss) from:
                Preferred Dividends; Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing
                Operation; Income from Tax Loss Carry forward; Other Gains/Losses.
    
    NetIncomeContinuousOperationsNetMinorityInterestIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeContinuousOperationsNetMinorityInterestIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeContOpsGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's net income from continuing operations on a percentage basis. Morningstar calculates the growth
                percentage based on the underlying net income from continuing operations data reported in the Income Statement within the
                company filings or reports. This figure represents the rate of net income growth for parts of the business that will continue to
                generate revenue in the future.
    
    NetIncomeContOpsGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeContOpsGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeDiscontinuousOperationsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    To be classified as discontinued operations, if both of the following conditions are met:
                1: The operations and cash flow of the component have been or will be removed from the ongoing operations of the entity as a
                result of the disposal transaction, and
                2: The entity will have no significant continuing involvement in the operations of the component after the disposal transaction.
                The discontinued operation is reported net of tax.
                Gains/Loss on Disposal of Discontinued Operations: Any gains or loss recognized on disposal of discontinued operations,
                which is the difference between the carrying value of the division and its fair value less costs to sell.
                Provision for Gain/Loss on Disposal: The amount of current expense charged in order to prepare for the disposal of
                discontinued operations.
    
    NetIncomeDiscontinuousOperationsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeDiscontinuousOperationsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeExtraordinaryIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Gains (losses), whether arising from extinguishment of debt, prior period adjustments, or from other events or transactions, that are
                both unusual in nature and infrequent in occurrence thereby meeting the criteria for an event or transaction to be classified as an
                extraordinary item.
    
    NetIncomeExtraordinaryIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeExtraordinaryIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeFromContinuingAndDiscontinuedOperationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net Income from Continuing Operations and Discontinued Operations, added together.
    
    NetIncomeFromContinuingAndDiscontinuedOperationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeFromContinuingAndDiscontinuedOperationIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeFromContinuingOperationNetMinorityInterestIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue less expenses and taxes from the entity's ongoing operations net of minority interest and before income (loss) from:
                Preferred Dividends; Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing
                Operation; Income from Tax Loss Carry forward; Other Gains/Losses.
    
    NetIncomeFromContinuingOperationNetMinorityInterestIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeFromContinuingOperationNetMinorityInterestIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeFromContinuingOperationsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue less expenses and taxes from the entity's ongoing operations and before income (loss) from discontinued operations,
                extraordinary items, impact of changes in accounting principles, minority interest, and various other reconciling adjustments;
                represents the starting line for Operating Cash Flow.
    
    NetIncomeFromContinuingOperationsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeFromContinuingOperationsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
