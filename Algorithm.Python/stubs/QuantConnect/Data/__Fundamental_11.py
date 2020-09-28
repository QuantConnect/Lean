from .__Fundamental_12 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class ChangeinFinancialAssetsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the financial assets.
    
    ChangeinFinancialAssetsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinFinancialAssetsCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinFinancialLiabilitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the financial liabilities.
    
    ChangeinFinancialLiabilitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinFinancialLiabilitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInFundsWithheldCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change during the reporting period associated with funds withheld.
    
    ChangeInFundsWithheldCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInFundsWithheldCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInIncomeTaxPayableCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the income tax payables.
    
    ChangeInIncomeTaxPayableCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInIncomeTaxPayableCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInsuranceContractAssetsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the contract assets.
    
    ChangeinInsuranceContractAssetsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInsuranceContractAssetsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInsuranceContractLiabilitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the insurance contract liabilities.
    
    ChangeinInsuranceContractLiabilitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInsuranceContractLiabilitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInsuranceFundsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the insurance funds.
    
    ChangeinInsuranceFundsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInsuranceFundsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInsuranceLiabilitiesNetofReinsuranceIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income/Expense due to changes between periods in insurance liabilities.
    
    ChangeinInsuranceLiabilitiesNetofReinsuranceIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInsuranceLiabilitiesNetofReinsuranceIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInInterestPayableCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the interest payable. Interest payable means carrying value as of the balance sheet
                date of interest payable on all forms of debt.
    
    ChangeInInterestPayableCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInInterestPayableCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInInventoryCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the Inventories. Inventories represent merchandise bought for resale and supplies and
                raw materials purchased for use in revenue producing operations.
    
    ChangeInInventoryCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInInventoryCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInvestmentContractIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income/Expense due to changes between periods in Investment Contracts.
    
    ChangeinInvestmentContractIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInvestmentContractIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeinInvestmentContractLiabilitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of the investment contract liabilities.
    
    ChangeinInvestmentContractLiabilitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeinInvestmentContractLiabilitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ChangeInLoansCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change that a lender gives money or property to a borrower and the borrower agrees to return the property or repay the
                borrowed money, along with interest, at a predetermined date in the future.
    
    ChangeInLoansCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ChangeInLoansCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
