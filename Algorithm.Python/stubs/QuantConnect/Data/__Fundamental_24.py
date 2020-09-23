from .__Fundamental_25 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class EffectOfExchangeRateChangesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The effect of exchange rate changes on cash balances held in foreign currencies.
    
    EffectOfExchangeRateChangesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EffectOfExchangeRateChangesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ElectricUtilityPlantBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount for the electric plant related to the utility industry.
    
    ElectricUtilityPlantBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ElectricUtilityPlantBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EmployeeBenefitsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount as of the balance sheet date of the portion of the obligations recognized for the various benefits provided to former
                or inactive employees, their beneficiaries, and covered dependents after employment but before retirement.
    
    EmployeeBenefitsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EmployeeBenefitsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EndCashPositionCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash and cash equivalents balance at the end of the accounting period, as indicated on the Cash Flow statement. It is equal to
                the Beginning Cash and Equivalents, plus the Net Change in Cash and Equivalents.
    
    EndCashPositionCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EndCashPositionCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EquipmentIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Equipment expenses include depreciation, repairs, rentals, and service contract costs. This also includes equipment purchases
                which do not qualify for capitalization in accordance with the entity's accounting policy. This item may also include furniture
                expenses. This item is usually only available for bank industry.
    
    EquipmentIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EquipmentIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EquityAttributableToOwnersOfParentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """ EquityAttributableToOwnersOfParentBalanceSheet(store: IDictionary[str, Decimal]) """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EquityAttributableToOwnersOfParentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EquityInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This asset represents equity securities categorized neither as held-to-maturity nor trading.
    
    EquityInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EquityInvestmentsBalanceSheet:
        pass

    NineMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EquityPerShareGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's book value per share on a percentage basis. Morningstar calculates the annualized growth
                percentage based on the underlying equity and end of period shares outstanding reported in the company filings or reports.
    
    EquityPerShareGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EquityPerShareGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EquitySharesInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Investments in shares of a company representing ownership in that company.
    
    EquitySharesInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EquitySharesInvestmentsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ExcessTaxBenefitFromStockBasedCompensationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Reductions in the entity's income taxes that arise when compensation cost (from non-qualified share-based compensation)
                recognized on the entities tax return exceeds compensation cost from share-based compensation recognized in financial
                statements. This element reduces net cash provided by operating activities.
    
    ExcessTaxBenefitFromStockBasedCompensationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ExcessTaxBenefitFromStockBasedCompensationCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ExciseTaxesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Excise taxes are taxes paid when purchases are made on a specific good, such as gasoline. Excise taxes are often included in the
                price of the product. There are also excise taxes on activities, such as on wagering or on highway usage by trucks.
    
    ExciseTaxesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ExciseTaxesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ExpenseRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A measure of operating performance for Insurance companies, as it shows the relationship between the premiums earned and
                administrative expenses related to claims such as fees and commissions. A number of 1 or lower is preferred, as this means the
                premiums exceed the expenses. Calculated as: (Deferred Policy Acquisition Amortization Expense+Fees and Commission
                Expense+Other Underwriting Expenses+Selling, General and Administrative) / Net Premiums Earned
    
    ExpenseRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ExpenseRatio:
        pass

    OneYear: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
