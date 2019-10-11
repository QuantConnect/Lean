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
*/

namespace QuantConnect.Data.Custom.USEnergy
{
    /// <summary>
    /// United States Energy Information Adminstration
    /// </summary>
    public static class USEnergy
    {
        /// <summary>
        /// Weekly U.S. Field Production of Crude Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string FieldProductionOfCrudeOil = "WCRFPUS2";

        /// <summary>
        /// Weekly U.S. Refiner Net Input of Crude Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerNetInputOfCrudeOil = "WCRRIUS2";

        /// <summary>
        /// Weekly U.S. Gross Inputs into Refineries  (Thousand Barrels per Day)
        /// </summary>
        public const string GrossInputsIntoRefineries = "WGIRIUS2";

        /// <summary>
        /// Weekly U. S. Operable Crude Oil Distillation Capacity   (Thousand Barrels per Calendar Day)
        /// </summary>
        public const string OperableCrudeOilDistillationCapacityPerCalendarDay = "WOCLEUS2";

        /// <summary>
        /// Weekly U.S. Percent Utilization of Refinery Operable Capacity (Percent)
        /// </summary>
        public const string PercentUtilizationOfRefineryOperableCapacityPercent = "WPULEUS3";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfGasolineBlendingComponents = "WBCRI_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Motor Gasoline Blending Components, RBOB  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfMotorGasolineBlendingComponentsRbob = "W_EPOBGRR_YIR_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Conventional CBOB Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfConventionalCbobGasolineBlendingComponents = "WO6RI_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Conventional GTAB Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfConventionalGtabGasolineBlendingComponents = "WO7RI_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Conventional Other Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfConventionalOtherGasolineBlendingComponents = "WO9RI_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Input of Fuel Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetInputOfFuelEthanol = "W_EPOOXE_YIR_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Adjusted Net Production of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderAdjustedNetProductionOfFinishedMotorGasoline = "WGFRPUS2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfFinishedMotorGasoline = "W_EPM0F_YPR_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Reformulated Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfReformulatedMotorGasoline = "WGRRPUS2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Finished Reformulated Motor Gasoline with Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfFinishedReformulatedMotorGasolineWithEthanol = "WG1TP_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Other Finished Reformulated Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfOtherFinishedReformulatedMotorGasoline = "W_EPM0RO_YPT_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Conventional Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfConventionalMotorGasoline = "WG4TP_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline with Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineWithEthanol = "WG5TP_NUS_2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline, Ed 55 and Lower  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineEd55AndLower = "W_EPM0CAL55_YPT_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline, Greater than Ed 55  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineGreaterThanEd55 = "W_EPM0CAG55_YPT_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Other Finished Conventional Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfOtherFinishedConventionalMotorGasoline = "WG6TP_NUS_2";

        /// <summary>
        /// Weekly U.S. Supply Adjustment of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string SupplyAdjustmentOfFinishedMotorGasoline = "W_EPM0F_VUA_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfKeroseneTypeJetFuel = "WKJRPUS2";

        /// <summary>
        /// Weekly U.S.  Refiner and Blender Net Production of Commercial Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfCommercialKeroseneTypeJetFuel = "WKCRPUS2";

        /// <summary>
        /// Weekly U.S.  Refiner and Blender Net Production of Military Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfMilitaryKeroseneTypeJetFuel = "WKMRPUS2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Distillate Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfDistillateFuelOil = "WDIRPUS2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Distillate Fuel Oil, 0 to 15 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfDistillateFuelOil0To15PpmSulfur = "WD0TP_NUS_2";

        /// <summary>
        /// Weekly U.S. Production of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductionOfDistillateFuelOilGreaterThan15To500PpmSulfur = "WD1TP_NUS_2";

        /// <summary>
        /// Weekly U.S.  Refiner and Blender Net Production of Distillate Fuel Oil Greater than 500 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfDistillateFuelOilGreaterThan500PpmSulfur = "WDGRPUS2";

        /// <summary>
        /// Weekly U.S. Refiner and Blender Net Production of Residual Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerAndBlenderNetProductionOfResidualFuelOil = "WRERPUS2";

        /// <summary>
        /// Weekly U.S. Refiner, Blender, and Gas Plant Net Production of Propane and Propylene  (Thousand Barrels per Day)
        /// </summary>
        public const string RefinerBlenderAndGasPlantNetProductionOfPropaneAndPropylene = "WPRTP_NUS_2";

        /// <summary>
        /// Weekly U.S. Oxygenate Plant Production of Fuel Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string OxygenatePlantProductionOfFuelEthanol = "W_EPOOXE_YOP_NUS_MBBLD";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Crude Oil and Petroleum Products  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfCrudeOilAndPetroleumProducts = "WTTSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks excluding SPR of Crude Oil and Petroleum Products  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksExcludingSprOfCrudeOilAndPetroleumProducts = "WTESTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Crude Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfCrudeOil = "WCRSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks excluding SPR of Crude Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksExcludingSprOfCrudeOil = "WCESTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks excluding SPR and including Lease Stock of Crude Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksExcludingSprAndIncludingLeaseStockOfCrudeOil = "W_EPC0_SAX_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Crude Oil Stocks in Transit (on Ships) from Alaska  (Thousand Barrels)
        /// </summary>
        public const string CrudeOilStocksInTransitOnShipsFromAlaska = "W_EPC0_SKA_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Crude Oil in SPR  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfCrudeOilInSpr = "WCSSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Total Gasoline  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfTotalGasoline = "WGTSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Finished Motor Gasoline  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfFinishedMotorGasoline = "WGFSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Reformulated Motor Gasoline  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfReformulatedMotorGasoline = "WGRSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Reformulated Motor Gasoline with Fuel ALcohol  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfReformulatedMotorGasolineWithFuelAlcohol = "WG1ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Reformulated Motor Gasoline, Non-Oxygentated  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfReformulatedMotorGasolineNonOxygentated = "WG3ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional Motor Gasoline  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalMotorGasoline = "WG4ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional Motor Gasoline with Fuel Ethanol  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalMotorGasolineWithFuelEthanol = "WG5ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional Motor Gasoline, Ed55 and Lower  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalMotorGasolineEd55AndLower = "W_EPM0CAL55_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional Motor Gasoline, Greater than Ed55  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalMotorGasolineGreaterThanEd55 = "W_EPM0CAG55_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Other Conventional Motor Gasoline  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfOtherConventionalMotorGasoline = "WG6ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfGasolineBlendingComponents = "WBCSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Motor Gasoline Blending Components, RBOB  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfMotorGasolineBlendingComponentsRbob = "W_EPOBGRR_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Reformulated RBOB with Ether Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfReformulatedRbobWithEtherGasolineBlendingComponents = "WO4ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Reformulated RBOB with Alcohol Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfReformulatedRbobWithAlcoholGasolineBlendingComponents = "WO3ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional CBOB Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalCbobGasolineBlendingComponents = "WO6ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional GTAB Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalGtabGasolineBlendingComponents = "WO7ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Conventional Other Gasoline Blending Components  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfConventionalOtherGasolineBlendingComponents = "WO9ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Fuel Ethanol  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfFuelEthanol = "W_EPOOXE_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Kerosene-Type Jet Fuel  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfKeroseneTypeJetFuel = "WKJSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Distillate Fuel Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfDistillateFuelOil = "WDISTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Distillate Fuel Oil, 0 to 15 ppm Sulfur  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfDistillateFuelOil0To15PpmSulfur = "WD0ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfDistillateFuelOilGreaterThan15To500PpmSulfur = "WD1ST_NUS_1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Distillate Fuel Oil, Greater Than 500 ppm Sulfur  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfDistillateFuelOilGreaterThan500PpmSulfur = "WDGSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Residual Fuel Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfResidualFuelOil = "WRESTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Propane and Propylene  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfPropaneAndPropylene = "WPRSTUS1";

        /// <summary>
        /// Weekly U.S. Propylene Nonfuel Use Stocks at Bulk Terminals  (Thousand Barrels)
        /// </summary>
        public const string PropyleneNonfuelUseStocksAtBulkTerminals = "WPLSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Other Oils (Excluding Fuel Ethanol)  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfOtherOilsExcludingFuelEthanol = "W_EPPO6_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Unfinished Oils  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfUnfinishedOils = "WUOSTUS1";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Kerosene  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfKerosene = "W_EPPK_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of Asphalt and Road Oil  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfAsphaltAndRoadOil = "W_EPPA_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Ending Stocks of NGPLs/LRGs (Excluding Propane/Propylene)  (Thousand Barrels)
        /// </summary>
        public const string EndingStocksOfNgplsLrgsExcludingPropanePropylene = "W_EPL0XP_SAE_NUS_MBBL";

        /// <summary>
        /// Weekly U.S. Days of Supply of Crude Oil excluding SPR  (Number of Days)
        /// </summary>
        public const string DaysOfSupplyOfCrudeOilExcludingSprNumberOfDays = "W_EPC0_VSD_NUS_DAYS";

        /// <summary>
        /// Weekly U.S. Days of Supply of Total Gasoline  (Number of Days)
        /// </summary>
        public const string DaysOfSupplyOfTotalGasolineNumberOfDays = "W_EPM0_VSD_NUS_DAYS";

        /// <summary>
        /// Weekly U.S. Days of Supply of Kerosene-Type Jet Fuel  (Number of Days)
        /// </summary>
        public const string DaysOfSupplyOfKeroseneTypeJetFuelNumberOfDays = "W_EPJK_VSD_NUS_DAYS";

        /// <summary>
        /// Weekly U.S. Days of Supply of Total Distillate  (Number of Days)
        /// </summary>
        public const string DaysOfSupplyOfTotalDistillateNumberOfDays = "W_EPD0_VSD_NUS_DAYS";

        /// <summary>
        /// Weekly U.S. Days of Supply of Propane/Propylene  (Number of Days)
        /// </summary>
        public const string DaysOfSupplyOfPropanePropyleneNumberOfDays = "W_EPLLPZ_VSD_NUS_DAYS";

        /// <summary>
        /// Weekly U.S. Imports of Crude Oil and Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfCrudeOilAndPetroleumProducts = "WTTIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Crude Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfCrudeOil = "WCRIMUS2";

        /// <summary>
        /// Weekly U.S. Commercial Crude Oil Imports Excluding SPR  (Thousand Barrels per Day)
        /// </summary>
        public const string CommercialCrudeOilImportsExcludingSpr = "WCEIMUS2";

        /// <summary>
        /// Weekly U.S. Crude Oil Imports by SPR  (Thousand Barrels per Day)
        /// </summary>
        public const string CrudeOilImportsBySpr = "WCSIMUS2";

        /// <summary>
        /// Weekly U.S. Crude Oil Imports for SPR by Others  (Thousand Barrels per Day)
        /// </summary>
        public const string CrudeOilImportsForSprByOthers = "W_EPC0_IMU_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Total Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfTotalPetroleumProducts = "WRPIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Total Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfTotalGasoline = "WGTIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfFinishedMotorGasoline = "W_EPM0F_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Reformulated Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfReformulatedMotorGasoline = "WGRIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Reformulated Motor Gasoline with Fuel ALcohol  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfReformulatedMotorGasolineWithFuelAlcohol = "WG1IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Other Reformulated Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfOtherReformulatedMotorGasoline = "W_EPM0RO_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Conventional Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfConventionalMotorGasoline = "WG4IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Conventional Motor Gasoline with Fuel Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfConventionalMotorGasolineWithFuelEthanol = "WG5IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Motor Gasoline, Finished, Conventional, Ed55 and Lower  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfMotorGasolineFinishedConventionalEd55AndLower = "W_EPM0CAL55_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Motor Gasoline, Finished, Conventional, Greater than Ed55  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfMotorGasolineFinishedConventionalGreaterThanEd55 = "W_EPM0CAG55_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Other Conventional Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfOtherConventionalMotorGasoline = "WG6IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfGasolineBlendingComponents = "WBCIMUS2";

        /// <summary>
        /// Weekly U.S. Imports from  All Countries of Motor Gasoline Blending Components, RBOB  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsFromAllCountriesOfMotorGasolineBlendingComponentsRbob = "W_EPOBGRR_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Reformulated RBOB with Ether Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfReformulatedRbobWithEtherGasolineBlendingComponents = "WO4IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Reformulated RBOB with Alcohol Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfReformulatedRbobWithAlcoholGasolineBlendingComponents = "WO3IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Conventional CBOB Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfConventionalCbobGasolineBlendingComponents = "WO6IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Conventional GTAB Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfConventionalGtabGasolineBlendingComponents = "WO7IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Conventional Other Gasoline Blending Components  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfConventionalOtherGasolineBlendingComponents = "WO9IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Fuel Ethanol  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfFuelEthanol = "W_EPOOXE_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfKeroseneTypeJetFuel = "WKJIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Distillate Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfDistillateFuelOil = "WDIIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Distillate Fuel Oil, 0 to 15 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfDistillateFuelOil0To15PpmSulfur = "WD0IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfDistillateFuelOilGreaterThan15To500PpmSulfur = "WD1IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Distillate Fuel Oil, Greater than 500 to 2000 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfDistillateFuelOilGreaterThan500To2000PpmSulfur = "WD2IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Distillate Fuel Oil, Greater than 2000 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfDistillateFuelOilGreaterThan2000PpmSulfur = "WD3IM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Residual Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfResidualFuelOil = "WREIMUS2";

        /// <summary>
        /// Weekly U.S. Imports of Propane and Propylene  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfPropaneAndPropylene = "WPRIM_NUS-Z00_2";

        /// <summary>
        /// Weekly U.S. Imports of Other Oils (Excluding Fuel Ethanol)  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfOtherOilsExcludingFuelEthanol = "W_EPPO6_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Kerosene  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfKerosene = "W_EPPK_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Imports of Liquefied Petroleum Gasses Less Propane/Propylene  (Thousand Barrels per Day)
        /// </summary>
        public const string ImportsOfLiquefiedPetroleumGassesLessPropanePropylene = "W_EPL0XP_IM0_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Exports of Crude Oil and Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfCrudeOilAndPetroleumProducts = "WTTEXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Crude Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfCrudeOil = "WCREXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Total Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfTotalPetroleumProducts = "WRPEXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfFinishedMotorGasoline = "W_EPM0F_EEX_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Exports of Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfKeroseneTypeJetFuel = "WKJEXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Total Distillate  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfTotalDistillate = "WDIEXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Residual Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfResidualFuelOil = "WREEXUS2";

        /// <summary>
        /// Weekly U.S. Exports of Propane and Propylene  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfPropaneAndPropylene = "W_EPLLPZ_EEX_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Exports of Other Oils  (Thousand Barrels per Day)
        /// </summary>
        public const string ExportsOfOtherOils = "W_EPPO4_EEX_NUS-Z00_MBBLD";

        /// <summary>
        /// Weekly U.S. Net Imports of Crude Oil and Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string NetImportsOfCrudeOilAndPetroleumProducts = "WTTNTUS2";

        /// <summary>
        /// Weekly U.S. Net Imports of Crude Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string NetImportsOfCrudeOil = "WCRNTUS2";

        /// <summary>
        /// Weekly U.S. Net Imports of Total Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string NetImportsOfTotalPetroleumProducts = "WRPNTUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Petroleum Products  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfPetroleumProducts = "WRPUPUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Finished Motor Gasoline  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfFinishedMotorGasoline = "WGFUPUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Kerosene-Type Jet Fuel  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfKeroseneTypeJetFuel = "WKJUPUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Distillate Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfDistillateFuelOil = "WDIUPUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Residual Fuel Oil  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfResidualFuelOil = "WREUPUS2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Propane and Propylene  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfPropaneAndPropylene = "WPRUP_NUS_2";

        /// <summary>
        /// Weekly U.S. Product Supplied of Other Oils  (Thousand Barrels per Day)
        /// </summary>
        public const string ProductSuppliedOfOtherOils = "WWOUP_NUS_2";

        /// <summary>
        /// Weekly U.S. Supply Adjustment of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur  (Thousand Barrels per Day)
        /// </summary>
        public const string SupplyAdjustmentOfDistillateFuelOilGreaterThan15To500PpmSulfur = "W_EPDM10_VUA_NUS_2";
    }
}
