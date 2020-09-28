from .__Fundamental_14 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class ChangeinTheGrossProvisionforUnearnedPremiumsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The change in the amount of the unearned premium reserves maintained by insurers.
    
    ChangeinTheGrossProvisionforUnearnedPremiumsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinTheGrossProvisionforUnearnedPremiumsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShareIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The change in the amount of unearned premium reserve to be covered by reinsurers.
    
    ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShareIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShareIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInTradingAccountSecuritiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change during the reporting period associated with trading account assets. Trading account assets are bought and held
                principally for the purpose of selling them in the near term (thus held for only a short period of time). Unrealized holding gains and
                losses for trading securities are included in earnings.
    
    ChangeInTradingAccountSecuritiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInTradingAccountSecuritiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInUnearnedPremiumsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The change during the period in the unearned portion of premiums written, excluding the portion amortized into income. This item is
                usually only available for insurance industry.
    
    ChangeInUnearnedPremiumsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInUnearnedPremiumsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInWorkingCapitalCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the working capital.  Working Capital is the amount left to the company to finance
                operations and expansion after current liabilities have been covered.
    
    ChangeInWorkingCapitalCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInWorkingCapitalCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangesInAccountReceivablesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the accounts receivables.
    
    ChangesInAccountReceivablesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangesInAccountReceivablesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangesInCashCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change between the beginning and ending balance of cash and cash equivalents.
    
    ChangesInCashCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangesInCashCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ClaimsandChangeinInsuranceLiabilitiesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income/Expense due to the insurer's changes in insurance liabilities.
    
    ClaimsandChangeinInsuranceLiabilitiesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClaimsandChangeinInsuranceLiabilitiesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ClaimsandPaidIncurredIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All reported claims arising out of incidents in that year are considered incurred grouped with claims paid out.
    
    ClaimsandPaidIncurredIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClaimsandPaidIncurredIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ClaimsOutstandingBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amounts owing to policy holders who have filed claims but have not yet been settled or paid.
    
    ClaimsOutstandingBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClaimsOutstandingBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ClaimsPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid out for claims by a insurance company during the period in operating cash flow, using the direct method. This item is
                usually only available for insurance industry
    
    ClaimsPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClaimsPaidCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ClassesofCashPaymentsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of total cash payment in the direct cash flow.
    
    ClassesofCashPaymentsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClassesofCashPaymentsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
