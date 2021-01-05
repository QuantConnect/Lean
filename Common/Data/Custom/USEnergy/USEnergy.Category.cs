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
    public partial class USEnergy
    {
        /// <summary>
        /// Petroleum
        /// </summary>
        public static class Petroleum
        {
            /// <summary>
            /// United States
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// U.S. Refiner and Blender Adjusted Net Production of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderAdjustedNetProductionOfFinishedMotorGasoline = "PET.WGFRPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Finished Motor Gasoline in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfFinishedMotorGasoline = "PET.WGFSTUS1.W";

                /// <summary>
                /// U.S. Product Supplied of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfFinishedMotorGasoline = "PET.WGFUPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Crude Oil in SPR in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfCrudeOilInSpr = "PET.WCSSTUS1.W";

                /// <summary>
                /// U.S.  Refiner and Blender Net Production of Distillate Fuel Oil Greater than 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfDistillateFuelOilGreaterThan500PpmSulfur = "PET.WDGRPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Distillate Fuel Oil, Greater Than 500 ppm Sulfur in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfDistillateFuelOilGreaterThan500PpmSulfur = "PET.WDGSTUS1.W";

                /// <summary>
                /// U.S. Exports of Total Distillate in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfTotalDistillate = "PET.WDIEXUS2.W";

                /// <summary>
                /// U.S. Imports of Distillate Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfDistillateFuelOil = "PET.WDIIMUS2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Distillate Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfDistillateFuelOil = "PET.WDIRPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Kerosene-Type Jet Fuel in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfKeroseneTypeJetFuel = "PET.WKJSTUS1.W";

                /// <summary>
                /// U.S. Product Supplied of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfKeroseneTypeJetFuel = "PET.WKJUPUS2.W";

                /// <summary>
                /// U.S. Imports of Total Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfTotalGasoline = "PET.WGTIMUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Total Gasoline in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfTotalGasoline = "PET.WGTSTUS1.W";

                /// <summary>
                /// U.S. Gross Inputs into Refineries in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyGrossInputsIntoRefineries = "PET.WGIRIUS2.W";

                /// <summary>
                /// U.S. Imports of Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfReformulatedMotorGasoline = "PET.WGRIMUS2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfReformulatedMotorGasoline = "PET.WGRRPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated Motor Gasoline in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedMotorGasoline = "PET.WGRSTUS1.W";

                /// <summary>
                /// U.S. Ending Stocks of Distillate Fuel Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfDistillateFuelOil = "PET.WDISTUS1.W";

                /// <summary>
                /// U.S. Product Supplied of Distillate Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfDistillateFuelOil = "PET.WDIUPUS2.W";

                /// <summary>
                /// U.S.  Refiner and Blender Net Production of Military Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfMilitaryKeroseneTypeJetFuel = "PET.WKMRPUS2.W";

                /// <summary>
                /// U. S. Operable Crude Oil Distillation Capacity in Thousand Barrels per Calendar Day (Mbbl/d)
                /// </summary>
                public const string WeeklyOperableCrudeOilDistillationCapacity = "PET.WOCLEUS2.W";

                /// <summary>
                /// U.S. Propylene Nonfuel Use Stocks at Bulk Terminals in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyPropyleneNonfuelUseStocksAtBulkTerminals = "PET.WPLSTUS1.W";

                /// <summary>
                /// U.S. Ending Stocks of Propane and Propylene in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfPropaneAndPropylene = "PET.WPRSTUS1.W";

                /// <summary>
                /// U.S. Percent Utilization of Refinery Operable Capacity in Percent (%)
                /// </summary>
                public const string WeeklyPercentUtilizationOfRefineryOperableCapacity = "PET.WPULEUS3.W";

                /// <summary>
                /// U.S. Exports of Residual Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfResidualFuelOil = "PET.WREEXUS2.W";

                /// <summary>
                /// U.S. Imports of Residual Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfResidualFuelOil = "PET.WREIMUS2.W";

                /// <summary>
                /// U.S.  Refiner and Blender Net Production of Commercial Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfCommercialKeroseneTypeJetFuel = "PET.WKCRPUS2.W";

                /// <summary>
                /// U.S. Exports of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfKeroseneTypeJetFuel = "PET.WKJEXUS2.W";

                /// <summary>
                /// U.S. Imports of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfKeroseneTypeJetFuel = "PET.WKJIMUS2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfKeroseneTypeJetFuel = "PET.WKJRPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks excluding SPR of Crude Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksExcludingSprOfCrudeOil = "PET.WCESTUS1.W";

                /// <summary>
                /// U.S. Exports of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfCrudeOil = "PET.WCREXUS2.W";

                /// <summary>
                /// U.S. Field Production of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyFieldProductionOfCrudeOil = "PET.WCRFPUS2.W";

                /// <summary>
                /// U.S. Imports of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfCrudeOil = "PET.WCRIMUS2.W";

                /// <summary>
                /// U.S. Net Imports of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyNetImportsOfCrudeOil = "PET.WCRNTUS2.W";

                /// <summary>
                /// U.S. Refiner Net Input of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetInputOfCrudeOil = "PET.WCRRIUS2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Residual Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfResidualFuelOil = "PET.WRERPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Residual Fuel Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfResidualFuelOil = "PET.WRESTUS1.W";

                /// <summary>
                /// U.S. Product Supplied of Residual Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfResidualFuelOil = "PET.WREUPUS2.W";

                /// <summary>
                /// U.S. Exports of Total Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfTotalPetroleumProducts = "PET.WRPEXUS2.W";

                /// <summary>
                /// U.S. Imports of Total Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfTotalPetroleumProducts = "PET.WRPIMUS2.W";

                /// <summary>
                /// U.S. Net Imports of Total Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyNetImportsOfTotalPetroleumProducts = "PET.WRPNTUS2.W";

                /// <summary>
                /// U.S. Product Supplied of Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfPetroleumProducts = "PET.WRPUPUS2.W";

                /// <summary>
                /// U.S. Ending Stocks excluding SPR of Crude Oil and Petroleum Products in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksExcludingSprOfCrudeOilAndPetroleumProducts = "PET.WTESTUS1.W";

                /// <summary>
                /// U.S. Exports of Crude Oil and Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfCrudeOilAndPetroleumProducts = "PET.WTTEXUS2.W";

                /// <summary>
                /// U.S. Imports of Crude Oil and Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfCrudeOilAndPetroleumProducts = "PET.WTTIMUS2.W";

                /// <summary>
                /// U.S. Net Imports of Crude Oil and Petroleum Products in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyNetImportsOfCrudeOilAndPetroleumProducts = "PET.WTTNTUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Crude Oil and Petroleum Products in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfCrudeOilAndPetroleumProducts = "PET.WTTSTUS1.W";

                /// <summary>
                /// U.S. Ending Stocks of Unfinished Oils in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfUnfinishedOils = "PET.WUOSTUS1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Other Finished Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfOtherFinishedConventionalMotorGasoline = "PET.WG6TP_NUS_2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Distillate Fuel Oil, 0 to 15 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfDistillateFuelOil0To15PpmSulfur = "PET.WD0TP_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.WD1ST_NUS_1.W";

                /// <summary>
                /// U.S. Production of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductionOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.WD1TP_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated Motor Gasoline with Fuel ALcohol in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedMotorGasolineWithFuelAlcohol = "PET.WG1ST_NUS_1.W";

                /// <summary>
                /// U.S. Ending Stocks of Crude Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfCrudeOil = "PET.WCRSTUS1.W";

                /// <summary>
                /// U.S. Crude Oil Imports by SPR in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyCrudeOilImportsBySpr = "PET.WCSIMUS2.W";

                /// <summary>
                /// U.S. Imports of Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfGasolineBlendingComponents = "PET.WBCIMUS2.W";

                /// <summary>
                /// U.S. Ending Stocks of Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfGasolineBlendingComponents = "PET.WBCSTUS1.W";

                /// <summary>
                /// U.S. Commercial Crude Oil Imports Excluding SPR in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyCommercialCrudeOilImportsExcludingSpr = "PET.WCEIMUS2.W";

                /// <summary>
                /// U.S. Refiner, Blender, and Gas Plant Net Production of Propane and Propylene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerBlenderAndGasPlantNetProductionOfPropaneAndPropylene = "PET.WPRTP_NUS_2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Finished Reformulated Motor Gasoline with Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfFinishedReformulatedMotorGasolineWithEthanol = "PET.WG1TP_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated Motor Gasoline, Non-Oxygentated in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedMotorGasolineNonOxygentated = "PET.WG3ST_NUS_1.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional Motor Gasoline in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalMotorGasoline = "PET.WG4ST_NUS_1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfConventionalMotorGasoline = "PET.WG4TP_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional Motor Gasoline with Fuel Ethanol in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalMotorGasolineWithFuelEthanol = "PET.WG5ST_NUS_1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline with Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineWithEthanol = "PET.WG5TP_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Other Conventional Motor Gasoline in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfOtherConventionalMotorGasoline = "PET.WG6ST_NUS_1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Conventional CBOB Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfConventionalCbobGasolineBlendingComponents = "PET.WO6RI_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional CBOB Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalCbobGasolineBlendingComponents = "PET.WO6ST_NUS_1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Conventional GTAB Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfConventionalGtabGasolineBlendingComponents = "PET.WO7RI_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional GTAB Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalGtabGasolineBlendingComponents = "PET.WO7ST_NUS_1.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Conventional Other Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfConventionalOtherGasolineBlendingComponents = "PET.WO9RI_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional Other Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalOtherGasolineBlendingComponents = "PET.WO9ST_NUS_1.W";

                /// <summary>
                /// U.S. No. 2 Heating Oil Wholesale/Resale Price in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyNo2HeatingOilWholesaleResalePrice = "PET.W_EPD2F_PWR_NUS_DPG.W";

                /// <summary>
                /// U.S. Crude Oil Stocks in Transit (on Ships) from Alaska in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyCrudeOilStocksInTransitOnShipsFromAlaska = "PET.W_EPC0_SKA_NUS_MBBL.W";

                /// <summary>
                /// U.S. Days of Supply of Crude Oil excluding SPR in Number of Days (Days)
                /// </summary>
                public const string WeeklyDaysOfSupplyOfCrudeOilExcludingSpr = "PET.W_EPC0_VSD_NUS_DAYS.W";

                /// <summary>
                /// U.S. Days of Supply of Total Distillate in Number of Days (Days)
                /// </summary>
                public const string WeeklyDaysOfSupplyOfTotalDistillate = "PET.W_EPD0_VSD_NUS_DAYS.W";

                /// <summary>
                /// U.S. Weekly No. 2 Heating Oil Residential Price in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyWeeklyNo2HeatingOilResidentialPrice = "PET.W_EPD2F_PRS_NUS_DPG.W";

                /// <summary>
                /// U.S. Product Supplied of Propane and Propylene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfPropaneAndPropylene = "PET.WPRUP_NUS_2.W";

                /// <summary>
                /// U.S. Product Supplied of Other Oils in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyProductSuppliedOfOtherOils = "PET.WWOUP_NUS_2.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfGasolineBlendingComponents = "PET.WBCRI_NUS_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Distillate Fuel Oil, 0 to 15 ppm Sulfur in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfDistillateFuelOil0To15PpmSulfur = "PET.WD0ST_NUS_1.W";

                /// <summary>
                /// U.S. Days of Supply of Kerosene-Type Jet Fuel in Number of Days (Days)
                /// </summary>
                public const string WeeklyDaysOfSupplyOfKeroseneTypeJetFuel = "PET.W_EPJK_VSD_NUS_DAYS.W";

                /// <summary>
                /// U.S. Days of Supply of Total Gasoline in Number of Days (Days)
                /// </summary>
                public const string WeeklyDaysOfSupplyOfTotalGasoline = "PET.W_EPM0_VSD_NUS_DAYS.W";

                /// <summary>
                /// U.S. Ending Stocks of Asphalt and Road Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfAsphaltAndRoadOil = "PET.W_EPPA_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Ending Stocks of Kerosene in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfKerosene = "PET.W_EPPK_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Supply Adjustment of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklySupplyAdjustmentOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.W_EPDM10_VUA_NUS_2.W";

                /// <summary>
                /// U.S. Imports of Conventional Motor Gasoline with Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfConventionalMotorGasolineWithFuelEthanol = "PET.WG5IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Other Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfOtherConventionalMotorGasoline = "PET.WG6IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Distillate Fuel Oil, 0 to 15 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfDistillateFuelOil0To15PpmSulfur = "PET.WD0IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.WD1IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Distillate Fuel Oil, Greater than 500 to 2000 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfDistillateFuelOilGreaterThan500To2000PpmSulfur = "PET.WD2IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Propane and Propylene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfPropaneAndPropylene = "PET.WPRIM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Conventional GTAB Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfConventionalGtabGasolineBlendingComponents = "PET.WO7IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Distillate Fuel Oil, Greater than 2000 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfDistillateFuelOilGreaterThan2000PpmSulfur = "PET.WD3IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Reformulated Motor Gasoline with Fuel ALcohol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfReformulatedMotorGasolineWithFuelAlcohol = "PET.WG1IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfConventionalMotorGasoline = "PET.WG4IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Conventional Other Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfConventionalOtherGasolineBlendingComponents = "PET.WO9IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Conventional CBOB Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfConventionalCbobGasolineBlendingComponents = "PET.WO6IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Blender Net Production of Kerosene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfKerosene = "PET.W_EPPK_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Kerosene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfKerosene = "PET.W_EPPK_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of Other Oils (Excluding Fuel Ethanol) in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfOtherOilsExcludingFuelEthanol = "PET.W_EPPO6_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Refiner Net Production of Residual Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfResidualFuelOil = "PET.W_EPPR_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfReformulatedMotorGasoline = "PET.W_EPM0R_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfReformulatedMotorGasoline = "PET.W_EPM0R_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of Fuel Ethanol in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfFuelEthanol = "PET.W_EPOOXE_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Blender Net Production of Distillate Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfDistillateFuelOil = "PET.W_EPD0_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Distillate Fuel Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfDistillateFuelOil = "PET.W_EPD0_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfKeroseneTypeJetFuel = "PET.W_EPJK_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Kerosene-Type Jet Fuel in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfKeroseneTypeJetFuel = "PET.W_EPJK_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Propane Residential Price in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyPropaneResidentialPrice = "PET.W_EPLLPA_PRS_NUS_DPG.W";

                /// <summary>
                /// U.S. Propane Wholesale/Resale Price in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyPropaneWholesaleResalePrice = "PET.W_EPLLPA_PWR_NUS_DPG.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Motor Gasoline Blending Components, RBOB in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfMotorGasolineBlendingComponentsRbob = "PET.W_EPOBGRR_YIR_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of NGPLs/LRGs (Excluding Propane/Propylene) in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfNgplsLrgsExcludingPropanePropylene = "PET.W_EPL0XP_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Days of Supply of Propane/Propylene in Number of Days (Days)
                /// </summary>
                public const string WeeklyDaysOfSupplyOfPropanePropylene = "PET.W_EPLLPZ_VSD_NUS_DAYS.W";

                /// <summary>
                /// U.S. Blender Net Production of Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfConventionalMotorGasoline = "PET.W_EPM0C_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfConventionalMotorGasoline = "PET.W_EPM0C_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Supply Adjustment of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklySupplyAdjustmentOfFinishedMotorGasoline = "PET.W_EPM0F_VUA_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfFinishedMotorGasoline = "PET.W_EPM0F_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfFinishedMotorGasoline = "PET.W_EPM0F_YPR_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfFinishedMotorGasoline = "PET.W_EPM0F_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Distillate Fuel Oil, Greater Than 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfDistillateFuelOilGreaterThan500PpmSulfur = "PET.W_EPD00H_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Distillate Fuel Oil, Greater Than 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfDistillateFuelOilGreaterThan500PpmSulfur = "PET.W_EPD00H_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.W_EPDM10_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Distillate Fuel Oil, Greater than 15 to 500 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfDistillateFuelOilGreaterThan15To500PpmSulfur = "PET.W_EPDM10_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Distillate Fuel Oil, 0 to 15 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfDistillateFuelOil0To15PpmSulfur = "PET.W_EPDXL0_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Distillate Fuel Oil, 0 to 15 ppm Sulfur in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfDistillateFuelOil0To15PpmSulfur = "PET.W_EPDXL0_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Conventional Motor Gasoline with Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfConventionalMotorGasolineWithFuelEthanol = "PET.W_EPM0CA_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Conventional Motor Gasoline with Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfConventionalMotorGasolineWithFuelEthanol = "PET.W_EPM0CA_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Other Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfOtherConventionalMotorGasoline = "PET.W_EPM0CO_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Other Conventional Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfOtherConventionalMotorGasoline = "PET.W_EPM0CO_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Reformulated Motor Gasoline with Fuel ALcohol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfReformulatedMotorGasolineWithFuelAlcohol = "PET.W_EPM0RA_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Reformulated Motor Gasoline with Fuel ALcohol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfReformulatedMotorGasolineWithFuelAlcohol = "PET.W_EPM0RA_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Oxygenate Plant Production of Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyOxygenatePlantProductionOfFuelEthanol = "PET.W_EPOOXE_YOP_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Motor Gasoline, Finished, Conventional, Ed55 and Lower in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfMotorGasolineFinishedConventionalEd55AndLower = "PET.W_EPM0CAL55_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline, Ed 55 and Lower in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineEd55AndLower = "PET.W_EPM0CAL55_YPT_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Motor Gasoline, Finished, Conventional, Ed55 and Lower in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfMotorGasolineFinishedConventionalEd55AndLower = "PET.W_EPM0CAL55_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Exports of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfFinishedMotorGasoline = "PET.W_EPM0F_EEX_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Finished Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfFinishedMotorGasoline = "PET.W_EPM0F_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Other Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfOtherReformulatedMotorGasoline = "PET.W_EPM0RO_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Other Finished Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfOtherFinishedReformulatedMotorGasoline = "PET.W_EPM0RO_YPT_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Other Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfOtherReformulatedMotorGasoline = "PET.W_EPM0RO_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of Motor Gasoline Blending Components, RBOB in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfMotorGasolineBlendingComponentsRbob = "PET.W_EPOBGRR_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Input of Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetInputOfFuelEthanol = "PET.W_EPOOXE_YIR_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Motor Gasoline, Finished, Conventional, Greater than Ed55 in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfMotorGasolineFinishedConventionalGreaterThanEd55 = "PET.W_EPM0CAG55_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Motor Gasoline, Finished, Conventional, Ed55 and Lower in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfMotorGasolineFinishedConventionalEd55AndLower = "PET.W_EPM0CAL55_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Crude Oil Imports for SPR by Others in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyCrudeOilImportsForSprByOthers = "PET.W_EPC0_IMU_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional Motor Gasoline, Greater than Ed55 in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalMotorGasolineGreaterThanEd55 = "PET.W_EPM0CAG55_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Imports of Fuel Ethanol in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfFuelEthanol = "PET.W_EPOOXE_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Liquefied Petroleum Gasses Less Propane/Propylene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfLiquefiedPetroleumGassesLessPropanePropylene = "PET.W_EPL0XP_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Exports of Propane and Propylene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfPropaneAndPropylene = "PET.W_EPLLPZ_EEX_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Other Reformulated Motor Gasoline in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfOtherReformulatedMotorGasoline = "PET.W_EPM0RO_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Blender Net Production of Motor Gasoline, Finished, Conventional, Greater Than Ed55 in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyBlenderNetProductionOfMotorGasolineFinishedConventionalGreaterThanEd55 = "PET.W_EPM0CAG55_YPB_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner and Blender Net Production of Finished Conventional Motor Gasoline, Greater than Ed 55 in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerAndBlenderNetProductionOfFinishedConventionalMotorGasolineGreaterThanEd55 = "PET.W_EPM0CAG55_YPT_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Refiner Net Production of Finished Conventional Motor Gasoline, Greater than Ed 55 in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyRefinerNetProductionOfFinishedConventionalMotorGasolineGreaterThanEd55 = "PET.W_EPM0CAG55_YPY_NUS_MBBLD.W";

                /// <summary>
                /// U.S. Ending Stocks of Conventional Motor Gasoline, Ed55 and Lower in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfConventionalMotorGasolineEd55AndLower = "PET.W_EPM0CAL55_SAE_NUS_MBBL.W";

                /// <summary>
                /// U.S. Imports of Kerosene in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfKerosene = "PET.W_EPPK_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Exports of Other Oils in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyExportsOfOtherOils = "PET.W_EPPO4_EEX_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports of Other Oils (Excluding Fuel Ethanol) in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfOtherOilsExcludingFuelEthanol = "PET.W_EPPO6_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Imports from  All Countries of Motor Gasoline Blending Components, RBOB in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromAllCountriesOfMotorGasolineBlendingComponentsRbob = "PET.W_EPOBGRR_IM0_NUS-Z00_MBBLD.W";

                /// <summary>
                /// U.S. Regular All Formulations Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyRegularAllFormulationsRetailGasolinePrices = "PET.EMM_EPMR_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Midgrade All Formulations Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyMidgradeAllFormulationsRetailGasolinePrices = "PET.EMM_EPMM_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Premium All Formulations Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyPremiumAllFormulationsRetailGasolinePrices = "PET.EMM_EPMP_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. All Grades All Formulations Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyAllGradesAllFormulationsRetailGasolinePrices = "PET.EMM_EPM0_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. All Grades Reformulated Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyAllGradesReformulatedRetailGasolinePrices = "PET.EMM_EPM0R_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Midgrade Reformulated Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyMidgradeReformulatedRetailGasolinePrices = "PET.EMM_EPMMR_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Premium Reformulated Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyPremiumReformulatedRetailGasolinePrices = "PET.EMM_EPMPR_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Regular Conventional Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyRegularConventionalRetailGasolinePrices = "PET.EMM_EPMRU_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Regular Reformulated Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyRegularReformulatedRetailGasolinePrices = "PET.EMM_EPMRR_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. No 2 Diesel Retail Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyNo2DieselRetailPrices = "PET.EMD_EPD2D_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Premium Conventional Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyPremiumConventionalRetailGasolinePrices = "PET.EMM_EPMPU_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Midgrade Conventional Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyMidgradeConventionalRetailGasolinePrices = "PET.EMM_EPMMU_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. All Grades Conventional Retail Gasoline Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyAllGradesConventionalRetailGasolinePrices = "PET.EMM_EPM0U_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. No 2 Diesel Ultra Low Sulfur (0-15 ppm) Retail Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyNo2DieselUltraLowSulfur015PpmRetailPrices = "PET.EMD_EPD2DXL0_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Ending Stocks excluding SPR and including Lease Stock of Crude Oil in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksExcludingSprAndIncludingLeaseStockOfCrudeOil = "PET.W_EPC0_SAX_NUS_MBBL.W";

                /// <summary>
                /// U.S. No 2 Diesel Low Sulfur (15-500 ppm) Retail Prices in Dollars per Gallon ($/gal)
                /// </summary>
                public const string WeeklyNo2DieselLowSulfur15500PpmRetailPrices = "PET.EMD_EPD2DM10_PTE_NUS_DPG.W";

                /// <summary>
                /// U.S. Imports of Reformulated RBOB with Alcohol Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfReformulatedRbobWithAlcoholGasolineBlendingComponents = "PET.WO3IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Imports of Reformulated RBOB with Ether Gasoline Blending Components in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsOfReformulatedRbobWithEtherGasolineBlendingComponents = "PET.WO4IM_NUS-Z00_2.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated GTAB Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedGtabGasolineBlendingComponents = "PET.WO2ST_NUS_1.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated RBOB with Alcohol Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedRbobWithAlcoholGasolineBlendingComponents = "PET.WO3ST_NUS_1.W";

                /// <summary>
                /// U.S. Ending Stocks of Reformulated RBOB with Ether Gasoline Blending Components in Thousand Barrels (Mbbl)
                /// </summary>
                public const string WeeklyEndingStocksOfReformulatedRbobWithEtherGasolineBlendingComponents = "PET.WO4ST_NUS_1.W";
            }

            /// <summary>
            /// Equatorial Guinea
            /// </summary>
            public static class EquatorialGuinea
            {
                /// <summary>
                /// U.S. Imports from Equatorial Guinea of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromEquatorialGuineaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NEK_MBBLD.W";
            }

            /// <summary>
            /// Iraq
            /// </summary>
            public static class Iraq
            {
                /// <summary>
                /// U.S. Imports from Iraq of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromIraqOfCrudeOil = "PET.W_EPC0_IM0_NUS-NIZ_MBBLD.W";
            }

            /// <summary>
            /// Kuwait
            /// </summary>
            public static class Kuwait
            {
                /// <summary>
                /// U.S. Imports from Kuwait of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromKuwaitOfCrudeOil = "PET.W_EPC0_IM0_NUS-NKU_MBBLD.W";
            }

            /// <summary>
            /// Mexico
            /// </summary>
            public static class Mexico
            {
                /// <summary>
                /// U.S. Imports from Mexico of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromMexicoOfCrudeOil = "PET.W_EPC0_IM0_NUS-NMX_MBBLD.W";
            }

            /// <summary>
            /// Nigeria
            /// </summary>
            public static class Nigeria
            {
                /// <summary>
                /// U.S. Imports from Nigeria of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromNigeriaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NNI_MBBLD.W";
            }

            /// <summary>
            /// Norway
            /// </summary>
            public static class Norway
            {
                /// <summary>
                /// U.S. Imports from Norway of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromNorwayOfCrudeOil = "PET.W_EPC0_IM0_NUS-NNO_MBBLD.W";
            }

            /// <summary>
            /// Russia
            /// </summary>
            public static class Russia
            {
                /// <summary>
                /// U.S. Imports from Russia of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromRussiaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NRS_MBBLD.W";
            }

            /// <summary>
            /// Saudi Arabia
            /// </summary>
            public static class SaudiArabia
            {
                /// <summary>
                /// U.S. Imports from Saudi Arabia of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromSaudiArabiaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NSA_MBBLD.W";
            }

            /// <summary>
            /// United Kingdom
            /// </summary>
            public static class UnitedKingdom
            {
                /// <summary>
                /// U.S. Imports from United Kingdom of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromUnitedKingdomOfCrudeOil = "PET.W_EPC0_IM0_NUS-NUK_MBBLD.W";
            }

            /// <summary>
            /// Venezuela
            /// </summary>
            public static class Venezuela
            {
                /// <summary>
                /// U.S. Imports from Venezuela of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromVenezuelaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NVE_MBBLD.W";
            }

            /// <summary>
            /// Algeria
            /// </summary>
            public static class Algeria
            {
                /// <summary>
                /// U.S. Imports from Algeria of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromAlgeriaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NAG_MBBLD.W";
            }

            /// <summary>
            /// Angola
            /// </summary>
            public static class Angola
            {
                /// <summary>
                /// U.S. Imports from Angola of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromAngolaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NAO_MBBLD.W";
            }

            /// <summary>
            /// Brazil
            /// </summary>
            public static class Brazil
            {
                /// <summary>
                /// U.S. Imports from Brazil of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromBrazilOfCrudeOil = "PET.W_EPC0_IM0_NUS-NBR_MBBLD.W";
            }

            /// <summary>
            /// Canada
            /// </summary>
            public static class Canada
            {
                /// <summary>
                /// U.S. Imports from Canada of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromCanadaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NCA_MBBLD.W";
            }

            /// <summary>
            /// Congo
            /// </summary>
            public static class Congo
            {
                /// <summary>
                /// U.S. Imports from Congo (Brazzaville) of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromCongoBrazzavilleOfCrudeOil = "PET.W_EPC0_IM0_NUS-NCF_MBBLD.W";
            }

            /// <summary>
            /// Colombia
            /// </summary>
            public static class Colombia
            {
                /// <summary>
                /// U.S. Imports from Colombia of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromColombiaOfCrudeOil = "PET.W_EPC0_IM0_NUS-NCO_MBBLD.W";
            }

            /// <summary>
            /// Ecuador
            /// </summary>
            public static class Ecuador
            {
                /// <summary>
                /// U.S. Imports from Ecuador of Crude Oil in Thousand Barrels per Day (Mbbl/d)
                /// </summary>
                public const string WeeklyImportsFromEcuadorOfCrudeOil = "PET.W_EPC0_IM0_NUS-NEC_MBBLD.W";
            }
        }
    }
}
