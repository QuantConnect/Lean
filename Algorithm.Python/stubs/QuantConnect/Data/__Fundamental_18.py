from .__Fundamental_19 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class DebtDueInYear1BalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Debt due under 1 year according to the debt maturity schedule reported by the company.
    
    DebtDueInYear1BalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtDueInYear1BalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebtDueInYear2BalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Debt due under 2 years according to the debt maturity schedule reported by the company.
    
    DebtDueInYear2BalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtDueInYear2BalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebtDueInYear5BalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Debt due within 5 year if the company provide maturity schedule in range e.g. 1-5 years, 2-5 years. Debt due under 5 years
                according to the debt maturity schedule reported by the company. If a range is reported by the company, the value will be collected
                under the maximum number of years (eg. 1-5 years, 3-5 years or 5 years will all be collected under this data point.)
    
    DebtDueInYear5BalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtDueInYear5BalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebtSecuritiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Debt securities held as investments.
    
    DebtSecuritiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtSecuritiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebtSecuritiesinIssueBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Any debt financial instrument issued instead of cash loan.
    
    DebtSecuritiesinIssueBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtSecuritiesinIssueBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebttoAssets(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is a leverage ratio used to determine how much debt (a sum of long term and current portion of debt) a company has on its
                balance sheet relative to total assets. This ratio examines the percent of the company that is financed by debt.
    
    DebttoAssets(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebttoAssets:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DebtTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total aggregate of all written promises and/or agreements to repay a stated amount of borrowed funds at a specified date in
                the future; in a Non-Differentiated Balance Sheet.
    
    DebtTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DebtTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DecreaseinInterestBearingDepositsinBankCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change on interest-bearing deposits in other financial institutions for relatively short periods of time including, for example,
                certificates of deposits.
    
    DecreaseinInterestBearingDepositsinBankCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DecreaseinInterestBearingDepositsinBankCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DeferredAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An amount owed to a firm that is not expected to be received by the firm within one year from the date of the balance sheet.
    
    DeferredAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DeferredAssetsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DeferredCostsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An expenditure not recognized as a cost of operation of the period in which incurred, but carried forward to be written off in future
                periods.
    
    DeferredCostsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DeferredCostsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DeferredIncomeTaxCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The component of income tax expense for the period representing the net change in the entities deferred tax assets and liabilities
                pertaining to continuing operations.
    
    DeferredIncomeTaxCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DeferredIncomeTaxCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DeferredIncomeTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Collections of cash or other assets related to revenue producing activity for which revenue has not yet been recognized on a Non-
                Differentiated Balance Sheet.
    
    DeferredIncomeTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DeferredIncomeTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class DeferredPolicyAcquisitionCostsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net amount of deferred policy acquisition costs capitalized on contracts remaining in force as of the balance sheet date.
    
    DeferredPolicyAcquisitionCostsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.DeferredPolicyAcquisitionCostsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
