from .__Fundamental_64 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class ShortTermDebtPaymentsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash outflow for a borrowing having initial term of repayment within one year or the normal operating cycle, if longer.
    
    ShortTermDebtPaymentsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ShortTermDebtPaymentsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ShortTermInvestmentsAvailableForSaleBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current assets section of a company's balance sheet that contains the investments that a company holds with the purpose for
                trading.
    
    ShortTermInvestmentsAvailableForSaleBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ShortTermInvestmentsAvailableForSaleBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ShortTermInvestmentsHeldToMaturityBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current assets section of a company's balance sheet that contains the investments that a company has made that will expire
                at a fixed date within one year.
    
    ShortTermInvestmentsHeldToMaturityBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ShortTermInvestmentsHeldToMaturityBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ShortTermInvestmentsTradingBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current assets section of a company's balance sheet that contains the investments that a company can trade at any moment.
    
    ShortTermInvestmentsTradingBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ShortTermInvestmentsTradingBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SocialSecurityCostsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Benefits paid to the employees in respect of their work.
    
    SocialSecurityCostsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SocialSecurityCostsIncomeStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SolvencyRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Measure of whether a company's cash flow is sufficient to meet its short-term and long-term debt requirements. The lower this
                ratio is, the greater the probability that the company will be in financial distress. Net Income + Depreciation, Depletion and
                Amortization/ average of annual Total Liabilities over the most recent two periods.
    
    SolvencyRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SolvencyRatio:
        pass

    OneYear: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SpecialIncomeChargesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Earnings or losses attributable to occurrences or actions by the firm that is either infrequent or unusual.
    
    SpecialIncomeChargesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SpecialIncomeChargesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class StaffCostsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total staff cost which is paid to the employees that is not part of Selling, General, and Administration expense.
    
    StaffCostsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.StaffCostsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class StockBasedCompensationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Value of stock issued during the period as a result of any share-based compensation plan other than an employee stock ownership
                plan (ESOP).
    
    StockBasedCompensationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.StockBasedCompensationCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class StockBasedCompensationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cost to the company for granting stock options to reward employees.
    
    StockBasedCompensationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.StockBasedCompensationIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class StockholdersEquityBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The residual interest in the assets of the enterprise that remains after deducting its liabilities. Equity is increased by owners'
                investments and by comprehensive income, and it is reduced by distributions to the owners.
    
    StockholdersEquityBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.StockholdersEquityBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class StockholdersEquityGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the stockholder's equity on a percentage basis. Morningstar calculates the growth percentage based on the residual
                interest in the assets of the enterprise that remains after deducting its liabilities reported in the Balance Sheet within the company
                filings or reports.
    
    StockholdersEquityGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.StockholdersEquityGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
