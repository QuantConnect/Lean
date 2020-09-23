from .__Fundamental_60 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class RentExpenseSupplementalIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The sum of all rent expenses incurred by the company for operating leases during the year, it is a supplemental value which would
                be reported outside consolidated statements or consolidated statement's footnotes.
    
    RentExpenseSupplementalIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RentExpenseSupplementalIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ReorganizationOtherCostsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A non-cash adjustment relating to restructuring costs.
    
    ReorganizationOtherCostsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ReorganizationOtherCostsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RepaymentinLeaseFinancingCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash outflow to repay lease financing during the PeriodAsByte.
    
    RepaymentinLeaseFinancingCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RepaymentinLeaseFinancingCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RepaymentOfDebtCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payments to Settle Long Term Debt plus Payments to Settle Short Term Debt.
    
    RepaymentOfDebtCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RepaymentOfDebtCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ReportedNormalizedBasicEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Normalized Basic EPS as reported by the company in the financial statements.
    
    ReportedNormalizedBasicEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ReportedNormalizedBasicEPS:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ReportedNormalizedDilutedEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Normalized Diluted EPS as reported by the company in the financial statements.
    
    ReportedNormalizedDilutedEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ReportedNormalizedDilutedEPS:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RepurchaseOfCapitalStockCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payments for Common Stock plus Payments for Preferred Stock.
    
    RepurchaseOfCapitalStockCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RepurchaseOfCapitalStockCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ResearchAndDevelopmentExpensesSupplementalIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of research and development expenses during the year. It is a supplemental value which would be reported
                outside consolidated statements.
    
    ResearchAndDevelopmentExpensesSupplementalIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ResearchAndDevelopmentExpensesSupplementalIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ResearchAndDevelopmentIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of research and development expenses during the year.
    
    ResearchAndDevelopmentIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ResearchAndDevelopmentIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RestrictedCashAndCashEquivalentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amounts of cash and cash equivalent items which are restricted as to withdrawal or usage. This item is available for
                bank and insurance industries.
    
    RestrictedCashAndCashEquivalentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestrictedCashAndCashEquivalentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RestrictedCashAndInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash and investments whose use in whole or in part is restricted for the long-term, generally by contractual agreements or
                regulatory requirements. This item is usually only available for bank industry.
    
    RestrictedCashAndInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestrictedCashAndInvestmentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RestrictedCashBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amounts of cash and cash equivalent items, which are restricted as to withdrawal or usage. Restrictions may include
                legally restricted deposits held as compensating balances against short-term borrowing arrangements, contracts entered into with
                others, or entity statements of intention with regard to particular deposits; however, time deposits and short-term certificates of
                deposit are not generally included in legally restricted deposits. Excludes compensating balance arrangements that are not
                agreements, which legally restrict the use of cash amounts shown on the balance sheet. For a classified balance sheet, represents
                the current portion only (the non-current portion has a separate concept); for an unclassified balance sheet represents the entire
                amount. This item is usually not available for bank and insurance industries.
    
    RestrictedCashBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestrictedCashBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
