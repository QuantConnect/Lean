from .__Fundamental_53 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class OtherReceivablesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other non-current receivables not otherwise classified.
    
    OtherReceivablesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherReceivablesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherReservesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other reserves owned by the company that cannot be identified by other specific items in the Reserves section.
    
    OtherReservesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherReservesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherShortTermInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of short term investments, which will be expired within one year that are not specifically classified as
                Available-for-Sale, Held-to-Maturity,  nor Trading investments.
    
    OtherShortTermInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherShortTermInvestmentsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherSpecialChargesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All other special charges that are not otherwise classified
    
    OtherSpecialChargesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherSpecialChargesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherStaffCostsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other costs in incurred in lieu of the employees that cannot be identified by other specific items in the Staff Costs section.
    
    OtherStaffCostsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherStaffCostsIncomeStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherTaxesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Any taxes that are not part of income taxes. This item is usually not available for bank and insurance industries.
    
    OtherTaxesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherTaxesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherunderPreferredStockDividendIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Dividend paid to the preferred shareholders before the common stock shareholders.
    
    OtherunderPreferredStockDividendIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherunderPreferredStockDividendIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherUnderwritingExpensesPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid out for underwriting expenses, such as the acquisition of new and renewal insurance contracts, in operating cash flow,
                using the direct method. This item is usually only available for insurance industry
    
    OtherUnderwritingExpensesPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherUnderwritingExpensesPaidCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PayablesAndAccruedExpensesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This balance sheet account includes all current payables and accrued expenses.
    
    PayablesAndAccruedExpensesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PayablesAndAccruedExpensesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PayablesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The sum of all payables owed and expected to be paid within one year or one operating cycle, including accounts payables, taxes
                payable, dividends payable and all other current payables.
    
    PayablesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PayablesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PaymentForLoansCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payment from a bank or insurance company to the lender who lends money or property based on the agreement, along with
                interest, at a predetermined date in the future.
    
    PaymentForLoansCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PaymentForLoansCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PaymentsonBehalfofEmployeesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid in a form of salaries or other benefits to employees of the company, in the direct cash flow.
    
    PaymentsonBehalfofEmployeesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PaymentsonBehalfofEmployeesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PaymentstoSuppliersforGoodsandServicesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid to suppliers when purchasing goods or services by the company, in the direct cash flow.
    
    PaymentstoSuppliersforGoodsandServicesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PaymentstoSuppliersforGoodsandServicesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
