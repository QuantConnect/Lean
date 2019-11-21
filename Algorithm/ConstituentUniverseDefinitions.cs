/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for defining constituent universes based on the Morningstar
    /// asset classification <see cref="AssetClassification"/> https://www.morningstar.com/
    /// </summary>
    public class ConstituentUniverseDefinitions
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Universe which selects companies whose revenues and earnings have both been growing significantly faster than
        /// the general economy.
        /// </summary>
        public Universe AggressiveGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-AggressiveGrowth", SecurityType.Equity, Market.USA), "constituents-universe-AggressiveGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that are growing respectably faster than the general economy, and often pay a
        /// steady dividend. They tend to be mature and solidly profitable businesses.
        /// </summary>
        public Universe ClassicGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-ClassicGrowth", SecurityType.Equity, Market.USA), "constituents-universe-ClassicGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies in the cyclicals and durables sectors, except those in the three types below.
        /// The profits of cyclicals tend to rise and fall with the general economy.
        /// </summary>
        public Universe Cyclicals(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Cyclicals", SecurityType.Equity, Market.USA), "constituents-universe-Cyclicals"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that have had consistently declining cash flows and earnings over the past
        /// three years, and/or very high debt.
        /// </summary>
        public Universe Distressed(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Distressed", SecurityType.Equity, Market.USA), "constituents-universe-Distressed"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that deal in assets such as oil, metals, and real estate, which tend to do
        /// well in inflationary environments.
        /// </summary>
        public Universe HardAsset(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-HardAsset", SecurityType.Equity, Market.USA), "constituents-universe-HardAsset"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that have dividend yields at least twice the average for large-cap stocks.
        /// They tend to be mature, slow-growing companies.
        /// </summary>
        public Universe HighYield(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-HighYield", SecurityType.Equity, Market.USA), "constituents-universe-HighYield"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that have shown slow revenue and earnings growth (typically less than the rate
        /// of GDP growth) over at least three years.
        /// </summary>
        public Universe SlowGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-SlowGrowth", SecurityType.Equity, Market.USA), "constituents-universe-SlowGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Universe which selects companies that have shown strong revenue growth but slower or spotty earnings growth.
        /// Very small or young companies also tend to fall into this class.
        /// </summary>
        public Universe SpeculativeGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-SpeculativeGrowth", SecurityType.Equity, Market.USA), "constituents-universe-SpeculativeGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe LargeValue(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-LargeValue", SecurityType.Equity, Market.USA), "constituents-universe-LargeValue"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe LargeCore(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-LargeCore", SecurityType.Equity, Market.USA), "constituents-universe-LargeCore"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe LargeGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-LargeGrowth", SecurityType.Equity, Market.USA), "constituents-universe-LargeGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe MidValue(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MidValue", SecurityType.Equity, Market.USA), "constituents-universe-MidValue"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe MidCore(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MidCore", SecurityType.Equity, Market.USA), "constituents-universe-MidCore"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe MidGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MidGrowth", SecurityType.Equity, Market.USA), "constituents-universe-MidGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe SmallValue(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-SmallValue", SecurityType.Equity, Market.USA), "constituents-universe-SmallValue"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe SmallCore(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-SmallCore", SecurityType.Equity, Market.USA), "constituents-universe-SmallCore"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Classifies securities according to market capitalization and growth and value factor
        /// </summary>
        /// <remarks>Please refer to http://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
        public Universe SmallGrowth(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-SmallGrowth", SecurityType.Equity, Market.USA), "constituents-universe-SmallGrowth"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Agriculture industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Agriculture(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Agriculture", SecurityType.Equity, Market.USA), "constituents-universe-Agriculture"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar BuildingMaterials industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe BuildingMaterials(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-BuildingMaterials", SecurityType.Equity, Market.USA), "constituents-universe-BuildingMaterials"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Chemicals industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Chemicals(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Chemicals", SecurityType.Equity, Market.USA), "constituents-universe-Chemicals"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar ForestProducts industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe ForestProducts(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-ForestProducts", SecurityType.Equity, Market.USA), "constituents-universe-ForestProducts"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar MetalsAndMining industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe MetalsAndMining(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MetalsAndMining", SecurityType.Equity, Market.USA), "constituents-universe-MetalsAndMining"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Steel industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Steel(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Steel", SecurityType.Equity, Market.USA), "constituents-universe-Steel"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar VehiclesAndParts industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe VehiclesAndParts(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-VehiclesAndParts", SecurityType.Equity, Market.USA), "constituents-universe-VehiclesAndParts"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar FixturesAndAppliances industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe FixturesAndAppliances(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-FixturesAndAppliances", SecurityType.Equity, Market.USA), "constituents-universe-FixturesAndAppliances"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar HomebuildingAndConstruction industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe HomebuildingAndConstruction(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-HomebuildingAndConstruction", SecurityType.Equity, Market.USA), "constituents-universe-HomebuildingAndConstruction"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar ManufacturingApparelAndAccessories industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe ManufacturingApparelAndAccessories(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-ManufacturingApparelAndAccessories", SecurityType.Equity, Market.USA), "constituents-universe-ManufacturingApparelAndAccessories"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar PackagingAndContainers industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe PackagingAndContainers(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-PackagingAndContainers", SecurityType.Equity, Market.USA), "constituents-universe-PackagingAndContainers"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar PersonalServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe PersonalServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-PersonalServices", SecurityType.Equity, Market.USA), "constituents-universe-PersonalServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Restaurants industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Restaurants(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Restaurants", SecurityType.Equity, Market.USA), "constituents-universe-Restaurants"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar RetailCyclical industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe RetailCyclical(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-RetailCyclical", SecurityType.Equity, Market.USA), "constituents-universe-RetailCyclical"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar TravelAndLeisure industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe TravelAndLeisure(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-TravelAndLeisure", SecurityType.Equity, Market.USA), "constituents-universe-TravelAndLeisure"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar AssetManagement industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe AssetManagement(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-AssetManagement", SecurityType.Equity, Market.USA), "constituents-universe-AssetManagement"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Banks industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Banks(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Banks", SecurityType.Equity, Market.USA), "constituents-universe-Banks"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar CapitalMarkets industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe CapitalMarkets(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-CapitalMarkets", SecurityType.Equity, Market.USA), "constituents-universe-CapitalMarkets"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Insurance industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Insurance(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Insurance", SecurityType.Equity, Market.USA), "constituents-universe-Insurance"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar DiversifiedFinancialServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe DiversifiedFinancialServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-DiversifiedFinancialServices", SecurityType.Equity, Market.USA), "constituents-universe-DiversifiedFinancialServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar CreditServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe CreditServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-CreditServices", SecurityType.Equity, Market.USA), "constituents-universe-CreditServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar RealEstate industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe RealEstate(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-RealEstate", SecurityType.Equity, Market.USA), "constituents-universe-RealEstate"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar REITs industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe REITs(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-REITs", SecurityType.Equity, Market.USA), "constituents-universe-REITs"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar BeveragesAlcoholic industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe BeveragesAlcoholic(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-BeveragesAlcoholic", SecurityType.Equity, Market.USA), "constituents-universe-BeveragesAlcoholic"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar BeveragesNonAlcoholic industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe BeveragesNonAlcoholic(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-BeveragesNonAlcoholic", SecurityType.Equity, Market.USA), "constituents-universe-BeveragesNonAlcoholic"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar ConsumerPackagedGoods industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe ConsumerPackagedGoods(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-ConsumerPackagedGoods", SecurityType.Equity, Market.USA), "constituents-universe-ConsumerPackagedGoods"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Education industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Education(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Education", SecurityType.Equity, Market.USA), "constituents-universe-Education"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar RetailDefensive industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe RetailDefensive(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-RetailDefensive", SecurityType.Equity, Market.USA), "constituents-universe-RetailDefensive"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar TobaccoProducts industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe TobaccoProducts(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-TobaccoProducts", SecurityType.Equity, Market.USA), "constituents-universe-TobaccoProducts"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Biotechnology industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Biotechnology(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Biotechnology", SecurityType.Equity, Market.USA), "constituents-universe-Biotechnology"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar DrugManufacturers industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe DrugManufacturers(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-DrugManufacturers", SecurityType.Equity, Market.USA), "constituents-universe-DrugManufacturers"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar HealthcarePlans industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe HealthcarePlans(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-HealthcarePlans", SecurityType.Equity, Market.USA), "constituents-universe-HealthcarePlans"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar HealthcareProvidersAndServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe HealthcareProvidersAndServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-HealthcareProvidersAndServices", SecurityType.Equity, Market.USA), "constituents-universe-HealthcareProvidersAndServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar MedicalDevicesAndInstruments industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe MedicalDevicesAndInstruments(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MedicalDevicesAndInstruments", SecurityType.Equity, Market.USA), "constituents-universe-MedicalDevicesAndInstruments"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar MedicalDiagnosticsAndResearch industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe MedicalDiagnosticsAndResearch(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MedicalDiagnosticsAndResearch", SecurityType.Equity, Market.USA), "constituents-universe-MedicalDiagnosticsAndResearch"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar MedicalDistribution industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe MedicalDistribution(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MedicalDistribution", SecurityType.Equity, Market.USA), "constituents-universe-MedicalDistribution"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar UtilitiesIndependentPowerProducers industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe UtilitiesIndependentPowerProducers(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-UtilitiesIndependentPowerProducers", SecurityType.Equity, Market.USA), "constituents-universe-UtilitiesIndependentPowerProducers"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar UtilitiesRegulated industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe UtilitiesRegulated(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-UtilitiesRegulated", SecurityType.Equity, Market.USA), "constituents-universe-UtilitiesRegulated"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar TelecommunicationServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe TelecommunicationServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-TelecommunicationServices", SecurityType.Equity, Market.USA), "constituents-universe-TelecommunicationServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar MediaDiversified industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe MediaDiversified(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-MediaDiversified", SecurityType.Equity, Market.USA), "constituents-universe-MediaDiversified"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar InteractiveMedia industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe InteractiveMedia(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-InteractiveMedia", SecurityType.Equity, Market.USA), "constituents-universe-InteractiveMedia"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar OilAndGas industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe OilAndGas(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-OilAndGas", SecurityType.Equity, Market.USA), "constituents-universe-OilAndGas"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar OtherEnergySources industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe OtherEnergySources(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-OtherEnergySources", SecurityType.Equity, Market.USA), "constituents-universe-OtherEnergySources"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar AerospaceAndDefense industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe AerospaceAndDefense(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-AerospaceAndDefense", SecurityType.Equity, Market.USA), "constituents-universe-AerospaceAndDefense"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar BusinessServices industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe BusinessServices(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-BusinessServices", SecurityType.Equity, Market.USA), "constituents-universe-BusinessServices"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Conglomerates industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Conglomerates(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Conglomerates", SecurityType.Equity, Market.USA), "constituents-universe-Conglomerates"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Construction industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Construction(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Construction", SecurityType.Equity, Market.USA), "constituents-universe-Construction"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar FarmAndHeavyConstructionMachinery industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe FarmAndHeavyConstructionMachinery(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-FarmAndHeavyConstructionMachinery", SecurityType.Equity, Market.USA), "constituents-universe-FarmAndHeavyConstructionMachinery"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar IndustrialDistribution industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe IndustrialDistribution(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-IndustrialDistribution", SecurityType.Equity, Market.USA), "constituents-universe-IndustrialDistribution"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar IndustrialProducts industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe IndustrialProducts(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-IndustrialProducts", SecurityType.Equity, Market.USA), "constituents-universe-IndustrialProducts"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Transportation industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Transportation(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Transportation", SecurityType.Equity, Market.USA), "constituents-universe-Transportation"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar WasteManagement industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe WasteManagement(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-WasteManagement", SecurityType.Equity, Market.USA), "constituents-universe-WasteManagement"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Software industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Software(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Software", SecurityType.Equity, Market.USA), "constituents-universe-Software"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Hardware industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Hardware(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Hardware", SecurityType.Equity, Market.USA), "constituents-universe-Hardware"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Morningstar Semiconductors industry group <see cref="MorningstarIndustryGroupCode"/>
        /// </summary>
        public Universe Semiconductors(UniverseSettings universeSettings = null)
        {
            return new ConstituentsUniverse(
                new Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-Semiconductors", SecurityType.Equity, Market.USA), "constituents-universe-Semiconductors"),
                universeSettings ?? _algorithm.UniverseSettings);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstituentUniverseDefinitions"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for obtaining the default <see cref="UniverseSettings"/></param>
        public ConstituentUniverseDefinitions(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }
    }
}
