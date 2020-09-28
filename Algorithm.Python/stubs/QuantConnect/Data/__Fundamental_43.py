from .__Fundamental_44 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class NetLongTermDebtIssuanceCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of long term debt. Long term debt includes notes payable, bonds payable, mortgage
                loans, convertible debt, subordinated debt and other types of long term debt.
    
    NetLongTermDebtIssuanceCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetLongTermDebtIssuanceCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetMargin(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of net income to revenue. Morningstar calculates the ratio by using the underlying data reported in the company
                filings or reports:   Net Income / Revenue.
    
    NetMargin(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetMargin:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetNonOperatingInterestIncomeExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net-Non Operating interest income or expenses caused by financing activities.
    
    NetNonOperatingInterestIncomeExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetNonOperatingInterestIncomeExpenseIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetOccupancyExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Occupancy expense may include items, such as depreciation of facilities and equipment, lease expenses, property taxes and
                property and casualty insurance expense. This item is usually only available for bank industry.
    
    NetOccupancyExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetOccupancyExpenseIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetOtherFinancingChargesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Miscellaneous charges incurred due to Financing activities.
    
    NetOtherFinancingChargesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetOtherFinancingChargesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetOtherInvestingChangesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Miscellaneous charges incurred due to Investing activities.
    
    NetOtherInvestingChangesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetOtherInvestingChangesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetOutwardLoansCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Adjustments due to net loans to/from outsiders in the Investing Cash Flow section.
    
    NetOutwardLoansCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetOutwardLoansCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetPolicyholderBenefitsAndClaimsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net provision in current period for future policy benefits, claims, and claims settlement expenses incurred in the claims
                settlement process before the effects of reinsurance arrangements. The value is net of the effects of contracts assumed and
                ceded.
    
    NetPolicyholderBenefitsAndClaimsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetPolicyholderBenefitsAndClaimsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetPPEBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Tangible assets that are held by an entity for use in the production or supply of goods and services, for rental to others, or for
                administrative purposes and that are expected to provide economic benefit for more than one year; net of accumulated
                depreciation.
    
    NetPPEBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetPPEBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetPPEPurchaseAndSaleCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net change between Purchases/Sales of PPE.
    
    NetPPEPurchaseAndSaleCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetPPEPurchaseAndSaleCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetPreferredStockIssuanceCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increase or decrease between periods of preferred stock.
    
    NetPreferredStockIssuanceCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetPreferredStockIssuanceCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NetPremiumsWrittenIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net premiums written are gross premiums written less ceded premiums. This item is usually only available for insurance industry.
    
    NetPremiumsWrittenIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NetPremiumsWrittenIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
