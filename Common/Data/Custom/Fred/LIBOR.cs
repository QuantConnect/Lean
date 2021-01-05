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

namespace QuantConnect.Data.Custom.Fred
{
    public partial class Fred
    {
        public static class LIBOR
        {
            ///<summary>
            /// Spot Next London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHFONTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SpotNextBasedOnSwissFranc = "CHFONTD156N";

            ///<summary>
            /// Spot Next London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPYONTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SpotNextBasedOnJapaneseYen = "JPYONTD156N";

            ///<summary>
            /// 6-Month London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY6MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SixMonthBasedOnJapaneseYen = "JPY6MTD156N";

            ///<summary>
            /// 3-Month London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY3MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string ThreeMonthBasedOnJapaneseYen = "JPY3MTD156N";

            ///<summary>
            /// 6-Month London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD6MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SixMonthBasedOnUSD = "USD6MTD156N";

            ///<summary>
            /// 1-Month London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY1MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneMonthBasedOnJapaneseYen = "JPY1MTD156N";

            ///<summary>
            /// 12-Month London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY12MD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwelveMonthBasedOnJapaneseYen = "JPY12MD156N";

            ///<summary>
            /// 12-Month London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP12MD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwelveMonthBasedOnBritishPound = "GBP12MD156N";

            ///<summary>
            /// 1-Month London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP1MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneMonthBasedOnBritishPound = "GBP1MTD156N";

            ///<summary>
            /// 1-Week London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP1WKD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneWeekBasedOnBritishPound = "GBP1WKD156N";

            ///<summary>
            /// 2-Month London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP2MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwoMonthBasedOnBritishPound = "GBP2MTD156N";

            ///<summary>
            /// 3-Month London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP3MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string ThreeMonthBasedOnBritishPound = "GBP3MTD156N";

            ///<summary>
            /// 1-Week London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY1WKD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneWeekBasedOnJapaneseYen = "JPY1WKD156N";

            ///<summary>
            /// 2-Month London Interbank Offered Rate (LIBOR), based on Japanese Yen (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPY2MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwoMonthBasedOnJapaneseYen = "JPY2MTD156N";

            ///<summary>
            /// 6-Month London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF6MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SixMonthBasedOnSwissFranc = "CHF6MTD156N";

            ///<summary>
            /// 3-Month London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF3MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string ThreeMonthBasedOnSwissFranc = "CHF3MTD156N";

            ///<summary>
            /// 1-Month London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD1MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneMonthBasedOnUSD = "USD1MTD156N";

            ///<summary>
            /// 12-Month London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF12MD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwelveMonthBasedOnSwissFranc = "CHF12MD156N";

            ///<summary>
            /// 12-Month London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD12MD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwelveMonthBasedOnUSD = "USD12MD156N";

            ///<summary>
            /// 1-Month London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF1MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneMonthBasedOnSwissFranc = "CHF1MTD156N";

            ///<summary>
            /// 1-Week London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF1WKD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneWeekBasedOnSwissFranc = "CHF1WKD156N";

            ///<summary>
            /// 2-Month London Interbank Offered Rate (LIBOR), based on Swiss Franc (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHF2MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwoMonthBasedOnSwissFranc = "CHF2MTD156N";

            ///<summary>
            /// 12-Month London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR12MD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwelveMonthBasedOnEuro = "EUR12MD156N";

            ///<summary>
            /// 6-Month London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBP6MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SixMonthBasedOnBritishPound = "GBP6MTD156N";

            ///<summary>
            /// 1-Month London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR1MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneMonthBasedOnEuro = "EUR1MTD156N";

            ///<summary>
            /// 2-Month London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR2MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwoMonthBasedOnEuro = "EUR2MTD156N";

            ///<summary>
            /// 3-Month London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR3MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string ThreeMonthBasedOnEuro = "EUR3MTD156N";

            ///<summary>
            /// 6-Month London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR6MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string SixMonthBasedOnEuro = "EUR6MTD156N";

            ///<summary>
            /// Overnight London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EURONTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OvernightBasedOnEuro = "EURONTD156N";

            ///<summary>
            /// 1-Week London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD1WKD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneWeekBasedOnUSD = "USD1WKD156N";

            ///<summary>
            /// 2-Month London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD2MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string TwoMonthBasedOnUSD = "USD2MTD156N";

            ///<summary>
            /// 3-Month London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USD3MTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string ThreeMonthBasedOnUSD = "USD3MTD156N";

            ///<summary>
            /// Overnight London Interbank Offered Rate (LIBOR), based on U.S. Dollar (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USDONTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OvernightBasedOnUSD = "USDONTD156N";

            ///<summary>
            /// 1-Week London Interbank Offered Rate (LIBOR), based on Euro (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EUR1WKD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OneWeekBasedOnEuro = "EUR1WKD156N";

            ///<summary>
            /// Overnight London Interbank Offered Rate (LIBOR), based on British Pound (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBPONTD156N
            /// The data series is lagged by one week due to an agreement with the source.
            /// London Interbank Offered Rate is the average interest rate at which leading banks borrow funds of a sizeable amount from other banks in the London market. Libor is the most widely used "benchmark" or reference rate for short term interest rates
            /// In consideration for ICE Benchmark Administration Limited ("IBA") coordinating and the Libor Contributor Banks supplying the data from which ICE LIBOR is compiled, the subscriber acknowledges and agrees that, to the fullest extent permitted by law, none of the IBA or the LIBOR Contributor Banks:
            /// (1) accept any responsibility or liability for the frequency of provision and accuracy of the ICE LIBOR rate or any use made of the ICE LIBOR rate by the subscriber, whether or not arising from the negligence of any of IBA or the LIBOR Contributor Banks; or
            /// (2) shall be liable for any loss of business or profits nor any direct, indirect or consequential loss or damage resulting from any such irregularity, inaccuracy or use of the Information.
            /// Copyright, 2016, ICE Benchmark Administration.
            /// </remarks>
            public static string OvernightBasedOnBritishPound = "GBPONTD156N";
        }
    }
}