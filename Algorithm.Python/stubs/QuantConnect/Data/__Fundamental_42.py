from .__Fundamental_43 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class NetIncomeFromTaxLossCarryforwardIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Occurs if a company has had a net loss from operations on a previous year that can be carried forward to reduce net income for tax
                purposes.
    
    NetIncomeFromTaxLossCarryforwardIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeFromTaxLossCarryforwardIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's net income on a percentage basis. Morningstar calculates the growth percentage based on the
                underlying net income data reported in the Income Statement within the company filings or reports.
    
    NetIncomeGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeIncludingNoncontrollingInterestsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net income of the group after the adjustment of all expenses and benefit.
    
    NetIncomeIncludingNoncontrollingInterestsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeIncludingNoncontrollingInterestsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Includes all the operations (continuing and discontinued) and all the other income or charges (extraordinary, accounting changes,
                tax loss carry forward, and other gains and losses).
    
    NetIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIncomePerEmployee(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of Net Income to Employees. Morningstar calculates the ratio by using the underlying data reported in the
                company filings or reports:     Net Income / Employee Number.
    
    NetIncomePerEmployee(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIncomePerEmployee:
        pass

    OneYear: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIntangiblesPurchaseAndSaleCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change between Purchases/Sales of Intangibles.
    
    NetIntangiblesPurchaseAndSaleCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIntangiblesPurchaseAndSaleCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetInterestIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total interest income minus total interest expense. It represents the difference between interest and dividends earned on interest-
                bearing assets and interest paid to depositors and other creditors.
    
    NetInterestIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetInterestIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetInvestmentIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total of interest, dividends, and other earnings derived from the insurance company's invested assets minus the expenses
                associated with these investments. Excluded from this income are capital gains or losses as the result of the sale of assets, as well
                as any unrealized capital gains or losses.
    
    NetInvestmentIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetInvestmentIncomeIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetInvestmentPropertiesPurchaseAndSaleCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net increase or decrease in cash due to purchases or sales of investment properties during the accounting PeriodAsByte.
    
    NetInvestmentPropertiesPurchaseAndSaleCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetInvestmentPropertiesPurchaseAndSaleCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetInvestmentPurchaseAndSaleCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change between Purchases/Sales of Investments.
    
    NetInvestmentPurchaseAndSaleCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetInvestmentPurchaseAndSaleCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetIssuancePaymentsOfDebtCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of debt.
    
    NetIssuancePaymentsOfDebtCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetIssuancePaymentsOfDebtCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetLoanBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the value of all loans after deduction of the appropriate allowances for loan and lease losses.
    
    NetLoanBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetLoanBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
