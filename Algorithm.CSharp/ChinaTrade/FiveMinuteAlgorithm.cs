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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Orders;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Risk;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade
{
    public class FiveMinuteAlgorithm : QCAlgorithm
    {
        private ISignalGenerator _signalGenerator;
        private IRiskManager _riskManager;
        private IOrderExecutor _orderExecutor;
        private Dictionary<Symbol, FiveMinAnalysis> _macdAnalysis = new Dictionary<Symbol, FiveMinAnalysis>();

        public override void Initialize()
        {
            SetStartDate(2024, 1, 1);
            SetEndDate(2025, 12, 31);
            // 设置基准货币为人民币
            SetAccountCurrency("CNY"); 
            // 初始化CNY现金账户（假设初始金额为10万）
            SetCash("CNY", 10000000); 
            SetTimeZone(TimeZones.Utc);
            // 设置手续费模型
            SetBrokerageModel(new AStockBrokerageModel());
            // 设置benchmark
            var benchmarkSymbol = AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
            SetBenchmark(benchmarkSymbol);
            // 初始化数据
            InitializeData();
            // 初始化模块
            _signalGenerator = new FiveMinSignalGenerator(_macdAnalysis);
            _riskManager = new RiskManager(this);
            _orderExecutor = new OrderExecutor(this);
        }

        private void InitializeData()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetStringAsync("http://43.142.139.247/api/dayapi/date/2025-05-09").Result;
                var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(response);
                int size = 20; // 每份的个数，可根据需要调整
                int part = 0; // 当前分片的索引
                // 按照Industry是否为"未知"分成两部分
                var gupiao = jsonData.Where(x => x.Industry.ToString() != "未知").ToList();
                var zhishu = jsonData.Where(x => x.Industry.ToString() == "未知").ToList(); // 这是指数
                // 仅对Industry为"未知"的部分进行分页
                var partItems = gupiao.Skip(part * size).Take(size).ToList();
                var singlePartItems = jsonData.Where(x => x.Name.ToString() == "陕西煤业").ToList();

                foreach (var item in partItems)
                {
                    var code = item.Code.ToString();
                    // 日线收盘价指标
                    var daysymbol = AddData<ApiDayCustomData>(code, Resolution.Daily, TimeZones.Utc).Symbol;
                    var daymacd = MACD(daysymbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
                    var daycloseIdentity = Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Close));

                    // 5min指标
                    var symbol = AddData<Api5MinCustomData>(code, Resolution.Minute, TimeZones.Utc).Symbol;
                    var macd = MACD(symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Minute);
                    var closeIdentity = Identity(symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Close));

                    var macdAnalysis = new FiveMinAnalysis(macd, closeIdentity, item.Name.ToString(), item.Industry.ToString(),daymacd,daycloseIdentity);

                    _macdAnalysis.Add(symbol, macdAnalysis);
                    if (LiveMode)
                    {
                        // 预热MACD和收盘价指标
                        WarmUpIndicators(symbol, macd, closeIdentity, item.Name.ToString(), item.Industry.ToString(),daysymbol, daymacd, daycloseIdentity);
                    }
                }
            }
        }

        private void WarmUpIndicators(Symbol symbol,MovingAverageConvergenceDivergence macd, IndicatorBase<Indicators.IndicatorDataPoint> closeIdentity,string name,string industry,
        Symbol daysymbol,MovingAverageConvergenceDivergence daymacd, IndicatorBase<Indicators.IndicatorDataPoint> daycloseIdentity)
        {
            // 计算MACD所需最小数据量(26周期+9信号线)
            var requiredBars = 12600 + 9;
            var history = History<Api5MinCustomData>(symbol, requiredBars * 2, Resolution.Minute);
            if (history == null || !history.Any())
            {
                Debug("无法获取历史数据用于预热");
                return;
            }
            Debug($"获取到 {history.Count()} 条历史数据用于预热");
            foreach (var bar in history.OrderBy(x => x.Time))
            {
                macd.Update(bar.Time, bar.Close);
                if (bar is Api5MinCustomData customData)
                {
                    closeIdentity.Update(bar.EndTime, customData.Close);
                }
            }
            Debug($"预热完成 - MACD.IsReady: {macd.IsReady}, CloseIdentity.IsReady: {closeIdentity.IsReady}");

            // day的指标
            var dayhistory = History<ApiDayCustomData>(daysymbol, requiredBars * 2, Resolution.Daily);
            if (dayhistory == null ||!dayhistory.Any())
            {
                Debug("无法获取历史数据用于预热");
                return;
            }
            Debug($"获取到 {dayhistory.Count()} 条历史数据用于预热");
            foreach (var bar in dayhistory.OrderBy(x => x.Time))
            {   
                daymacd.Update(bar.Time, bar.Close);
                if (bar is ApiDayCustomData customData)
                {
                    daycloseIdentity.Update(bar.EndTime, customData.Close);
                }
            }
            // 只在循环外创建一次实例
            _macdAnalysis[symbol] = new FiveMinAnalysis(macd, closeIdentity,name,industry,daymacd,daycloseIdentity);
            Debug($"预热完成 - daymacd.IsReady: {daymacd.IsReady}, daycloseIdentity.IsReady: {daycloseIdentity.IsReady}");
        }

        public override void OnData(Slice data)
        {
            // 生成交易信号
            var signals = _signalGenerator.GenerateSignals(data);
            // 检查风险
            var risks = _riskManager.CheckRisks(Portfolio);
            // 执行订单
            _orderExecutor.ExecuteSignals(signals, risks).Wait();
        }
    }
}