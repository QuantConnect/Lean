from .__Fundamental_48 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class NotesReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An amount representing an agreement for an unconditional promise by the maker to pay the entity (holder) a definite sum of money
                at a future date(s) within one year of the balance sheet date or the normal operating cycle, whichever is longer. Such amount may
                include accrued interest receivable in accordance with the terms of the note. The note also may contain provisions including a
                discount or premium, payable on demand, secured, or unsecured, interest bearing or non-interest bearing, among a myriad of other
                features and characteristics.
    
    NotesReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NotesReceivableBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OccupancyAndEquipmentIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Includes total expenses of occupancy and equipment. This item is usually only available for bank industry.
    
    OccupancyAndEquipmentIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OccupancyAndEquipmentIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingCashFlowCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net cash from (used in) all of the entity's operating activities, including those of discontinued operations, of the reporting entity.
                Operating activities include all transactions and events that are not defined as investing or financing activities. Operating activities
                generally involve producing and delivering goods and providing services. Cash flows from operating activities are generally the cash
                effects of transactions and other events that enter into the determination of net income.
    
    OperatingCashFlowCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingCashFlowCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingExpenseAsReportedIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Operating expense as reported by the company, may be the same or not the same as Morningstar's standardized definition.
    
    OperatingExpenseAsReportedIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingExpenseAsReportedIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Operating expenses are primary recurring costs associated with central operations (other than cost of goods sold) that are incurred
                in order to generate sales.
    
    OperatingExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingExpenseIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingGainsLossesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The gain or loss from the entity's ongoing operations.
    
    OperatingGainsLossesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingGainsLossesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income from normal business operations after deducting cost of revenue and operating expenses. It does not include income from
                any investing activities.
    
    OperatingIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingLeaseAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A contract that allows for the use of an asset, but does not convey rights of ownership of the asset. An operating lease is not
                capitalized; it is accounted for as a rental expense in what is known as "off balance sheet financing." For the lessor, the asset being
                leased is accounted for as an asset and is depreciated as such.
    
    OperatingLeaseAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingLeaseAssetsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperatingRevenueIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sales and income that the company makes from its business operations. This applies only to non-bank and insurance companies.
                For Utility template companies, this is the sum of revenue from electric, gas, transportation and other operating revenue.
                For Transportation template companies, this is the sum of revenue-passenger, revenue-cargo, and other operating revenue.
    
    OperatingRevenueIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperatingRevenueIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperationAndMaintenanceIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of operation and maintenance expenses, which is the one important operating expense for the utility
                industry. It includes any costs related to production and maintenance cost of the property during the revenue generation process.
                This item is usually only available for mining and utility industries.
    
    OperationAndMaintenanceIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperationAndMaintenanceIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperationIncomeGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's operating income on a percentage basis. Morningstar calculates the growth percentage based on the
                underlying operating income data reported in the Income Statement within the company filings or reports.
    
    OperationIncomeGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperationIncomeGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
