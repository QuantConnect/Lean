from .__Fundamental_70 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class TrustFeesbyCommissionsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Bank manages funds on behalf of its customers through the operation of various trust accounts. Any fees earned through managing
                those funds are called trust fees, which are recognized when earned. This item is typically available for bank industry.
    
    TrustFeesbyCommissionsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TrustFeesbyCommissionsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnallocatedSurplusBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of surplus from insurance contracts which has not been allocated at the balance sheet date. This is represented as a
                liability to policyholders, as it pertains to cumulative income arising from the with-profits business.
    
    UnallocatedSurplusBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnallocatedSurplusBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnbilledReceivablesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenues that are not currently billed from the customer under the terms of the contract.  This item is usually only available for
                utility industry.
    
    UnbilledReceivablesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnbilledReceivablesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnderwritingExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Also known as Policy Acquisition Costs; and reported by insurance companies.  The cost incurred by an insurer when deciding
                whether to accept or decline a risk; may include meetings with the insureds or brokers, actuarial review of loss history, or physical
                inspections of exposures. Also, expenses deducted from insurance company revenues (including incurred losses and acquisition
                costs) to determine underwriting profit.
    
    UnderwritingExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnderwritingExpensesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnearnedIncomeBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income received but not yet earned, it represents the unearned amount that is netted against the total loan.
    
    UnearnedIncomeBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnearnedIncomeBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnearnedPremiumsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount of premiums written on insurance contracts that have not been earned as of the balance sheet date.
    
    UnearnedPremiumsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnearnedPremiumsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnpaidLossAndLossReserveBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Liability amount that reflects claims that are expected based upon statistical projections, but which have not been reported to the
                insurer.
    
    UnpaidLossAndLossReserveBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnpaidLossAndLossReserveBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnrealizedGainLossBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A profit or loss that results from holding onto an asset rather than cashing it in and officially taking the profit or loss.
    
    UnrealizedGainLossBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnrealizedGainLossBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnrealizedGainLossOnInvestmentSecuritiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The increases (decreases) in the market value of unsold securities whose gains (losses) were included in earnings.
    
    UnrealizedGainLossOnInvestmentSecuritiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnrealizedGainLossOnInvestmentSecuritiesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class UnrealizedGainsLossesOnDerivativesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The gross gains and losses on derivatives. This item is usually only available for insurance industry.
    
    UnrealizedGainsLossesOnDerivativesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.UnrealizedGainsLossesOnDerivativesCashFlowStatement:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ValuationRatios(System.object):
    """
    Definition of the ValuationRatios class
    
    ValuationRatios()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.ValuationRatios) -> None:
        pass

    ActualForwardDividend: float

    ActualTrailingDividend: float

    BookValuePerShare: float

    BookValueYield: float

    BuyBackYield: float

    CAPERatio: float

    CashReturn: float

    CFOPerShare: float

    CFYield: float

    DivYield5Year: float

    EarningYield: float

    EVtoEBIT: float

    EVToEBIT3YrAvg: float

    EVToEBIT3YrAvgChange: float

    EVToEBITDA: float

    EVToEBITDA10YearGrowth: float

    EVToEBITDA1YearGrowth: float

    EVToEBITDA3YearGrowth: float

    EVToEBITDA3YrAvg: float

    EVToEBITDA3YrAvgChange: float

    EVToEBITDA5YearGrowth: float

    EVtoFCF: float

    EVToFCF10YearGrowth: float

    EVToFCF1YearGrowth: float

    EVToFCF3YearGrowth: float

    EVToFCF3YrAvg: float

    EVToFCF3YrAvgChange: float

    EVToFCF5YearGrowth: float

    EVToForwardEBIT: float

    EVToForwardEBITDA: float

    EVToForwardRevenue: float

    EVtoPreTaxIncome: float

    EVtoRevenue: float

    EVToRevenue10YearGrowth: float

    EVToRevenue1YearGrowth: float

    EVToRevenue3YearGrowth: float

    EVToRevenue3YrAvg: float

    EVToRevenue3YrAvgChange: float

    EVToRevenue5YearGrowth: float

    EVtoTotalAssets: float

    EVToTotalAssets10YearGrowth: float

    EVToTotalAssets1YearGrowth: float

    EVToTotalAssets3YearGrowth: float

    EVToTotalAssets3YrAvg: float

    EVToTotalAssets3YrAvgChange: float

    EVToTotalAssets5YearGrowth: float

    ExpectedDividendGrowthRate: float

    FCFPerShare: float

    FCFRatio: float

    FCFYield: float

    FFOPerShare: float

    FirstYearEstimatedEPSGrowth: float

    ForwardCalculationStyle: str

    ForwardDividend: float

    ForwardDividendYield: float

    ForwardEarningYield: float

    ForwardPERatio: float

    ForwardROA: float

    ForwardROE: float

    NormalizedPEGatio: float

    NormalizedPERatio: float

    PayoutRatio: float

    PBRatio: float

    PBRatio10YearGrowth: float

    PBRatio1YearGrowth: float

    PBRatio3YearGrowth: float

    PBRatio3YrAvg: float

    PBRatio3YrAvgChange: float

    PBRatio5YearGrowth: float

    PCashRatio3YrAvg: float

    PCFRatio: float

    PEGPayback: float

    PEGRatio: float

    PERatio: float

    PERatio10YearAverage: float

    PERatio10YearGrowth: float

    PERatio10YearHigh: float

    PERatio10YearLow: float

    PERatio1YearAverage: float

    PERatio1YearGrowth: float

    PERatio1YearHigh: float

    PERatio1YearLow: float

    PERatio3YearGrowth: float

    PERatio3YrAvg: float

    PERatio3YrAvgChange: float

    PERatio5YearAverage: float

    PERatio5YearGrowth: float

    PERatio5YearHigh: float

    PERatio5YearLow: float

    PFCFRatio10YearGrowth: float

    PFCFRatio1YearGrowth: float

    PFCFRatio3YearGrowth: float

    PFCFRatio3YrAvg: float

    PFCFRatio3YrAvgChange: float

    PFCFRatio5YearGrowth: float

    PriceChange1M: float

    PricetoCashRatio: float

    PricetoEBITDA: float

    PSRatio: float

    PSRatio10YearGrowth: float

    PSRatio1YearGrowth: float

    PSRatio3YearGrowth: float

    PSRatio3YrAvg: float

    PSRatio3YrAvgChange: float

    PSRatio5YearGrowth: float

    RatioPE5YearAverage: float

    SalesPerShare: float

    SalesYield: float

    SecondYearEstimatedEPSGrowth: float

    SustainableGrowthRate: float

    TangibleBookValuePerShare: float

    TangibleBVPerShare3YrAvg: float

    TangibleBVPerShare5YrAvg: float

    TotalAssetPerShare: float

    TotalYield: float

    TrailingCalculationStyle: str

    TrailingDividendYield: float

    TwoYearsForwardEarningYield: float

    TwoYearsForwardPERatio: float

    TwoYrsEVToForwardEBIT: float

    TwoYrsEVToForwardEBITDA: float

    WorkingCapitalPerShare: float

    WorkingCapitalPerShare3YrAvg: float

    WorkingCapitalPerShare5YrAvg: float
