from .__Fundamental_33 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime



class IncomeTaxPaidSupplementalDataCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of cash paid during the current period to foreign, federal state and local authorities as taxes on income.
    
    IncomeTaxPaidSupplementalDataCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncomeTaxPaidSupplementalDataCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncomeTaxPayableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A current liability account which reflects the amount of income taxes currently due to the federal, state, and local governments.
    
    IncomeTaxPayableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncomeTaxPayableBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncreaseDecreaseInDepositCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate net change during the reporting period in moneys given as security, collateral, or margin deposits.
    
    IncreaseDecreaseInDepositCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncreaseDecreaseInDepositCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncreaseDecreaseinLeaseFinancingCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Change in cash flow resulting from increase/decrease in lease financing.
    
    IncreaseDecreaseinLeaseFinancingCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncreaseDecreaseinLeaseFinancingCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncreaseDecreaseInNetUnearnedPremiumReservesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Premium might contain a portion of the amount that has been paid in advance for insurance that has not yet been provided, which
                is called unearned premium. If either party cancels the contract, the insurer must have the unearned premium ready to refund.
                Hence, the amount of premium reserve maintained by insurers is called unearned premium reserves, which is prepared for
                liquidation.  This item is usually only available for insurance industry.
    
    IncreaseDecreaseInNetUnearnedPremiumReservesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncreaseDecreaseInNetUnearnedPremiumReservesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncreaseinInterestBearingDepositsinBankCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Increase in interest-bearing deposits in bank.
    
    IncreaseinInterestBearingDepositsinBankCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncreaseinInterestBearingDepositsinBankCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class IncreaseinLeaseFinancingCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash inflow from increase in lease financing.
    
    IncreaseinLeaseFinancingCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.IncreaseinLeaseFinancingCashFlowStatement:
        pass

    SixMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InsuranceAndClaimsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Insurance and claims are the expenses in the period incurred with respect to protection provided by insurance entities against risks
                other than risks associated with production (which is allocated to cost of sales). This item is usually not available for insurance
                industries.
    
    InsuranceAndClaimsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InsuranceAndClaimsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InsuranceContractAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A contract under which one party (the insurer) accepts significant insurance risk from another party (the policyholder) by agreeing
                to compensate the policyholder if a specified uncertain future event (the insured event) adversely affects the policyholder. This
                includes Insurance Receivables and Premiums Receivables.
    
    InsuranceContractAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InsuranceContractAssetsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InsuranceContractLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Any type of insurance policy that protects an individual or business from the risk that they may be sued and held legally liable for
                something such as malpractice, injury or negligence. Liability insurance policies cover both legal costs and any legal payouts for
                which the insured would be responsible if found legally liable. Intentional damage and contractual liabilities are typically not covered
                in these types of policies.
    
    InsuranceContractLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InsuranceContractLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InsuranceFundsNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Liabilities related to insurance funds that are dissolved after one year.
    
    InsuranceFundsNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InsuranceFundsNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestandCommissionPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid for interest and commission in operating cash flow, using the direct method
    
    InterestandCommissionPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestandCommissionPaidCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
