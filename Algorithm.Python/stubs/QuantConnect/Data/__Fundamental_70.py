import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime



class WagesandSalariesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is the portion under Staff Costs that represents salary paid to the employees in respect of their work.
    
    WagesandSalariesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WagesandSalariesIncomeStatement:
        pass

    SixMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class WaterProductionBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount for a facility and plant that provides water which might include wells, reservoirs, pumping stations, and control
                facilities; and waste water systems which includes the waste treatment and disposal facility and equipment. This item is usually
                only available for utility industry.
    
    WaterProductionBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WaterProductionBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class WorkingCapitalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Current Assets minus Current Liabilities.  This item is usually not available for bank and insurance industries.
    
    WorkingCapitalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WorkingCapitalBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class WorkingCapitalTurnoverRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total revenue / working capital (current assets minus current liabilities)
    
    WorkingCapitalTurnoverRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WorkingCapitalTurnoverRatio:
        pass

    OneYear: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class WorkInProcessBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Work, or goods, in the process of being fabricated or manufactured but not yet completed as finished goods. This item is usually
                available for manufacturing and mining industries.
    
    WorkInProcessBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WorkInProcessBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class WriteOffIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A reduction in the value of an asset or earnings by the amount of an expense or loss.
    
    WriteOffIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.WriteOffIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
