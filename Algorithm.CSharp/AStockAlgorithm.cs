using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;
using static QuantConnect.Messages;
using QuantConnect.DataSource;
using System.Drawing;

namespace QuantConnect.Algorithm.CSharp
{
    public class AStockAlgorithm : QCAlgorithm
    {
        private Dictionary<Symbol, MacdAnalysis> _macdAnalysis = new Dictionary<Symbol, MacdAnalysis>();
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

            var benchmarkSymbol = AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
            SetBenchmark(benchmarkSymbol);
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

                foreach (var item in partItems)
                {
                    var code = item.Code.ToString();
                    var name = item.Name.ToString();
                    var industry = item.Industry.ToString();
                    var symbol = AddData<Api5MinCustomData>(code, Resolution.Minute, TimeZones.Utc).Symbol;
                    var macd = MACD(symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Minute);
                    var closeIdentity = Identity(symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Close));
                    var macdAnalysis = new MacdAnalysis(macd, closeIdentity, name, industry);
                    _macdAnalysis.Add(symbol, macdAnalysis); // 初始化字典中的每个Symbol的值为null
                    if (LiveMode)
                    {                 // 预热MACD和收盘价指标
                        WarmUpIndicators(symbol, macd, closeIdentity,name,industry);
                    }
                }
            }
        }
        private void WarmUpIndicators(Symbol symbol,MovingAverageConvergenceDivergence macd, IndicatorBase<Indicators.IndicatorDataPoint> closeIdentity,string name,string industry)
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
            // 只在循环外创建一次实例
            _macdAnalysis[symbol] = new MacdAnalysis(macd, closeIdentity,name,industry);
            Debug($"预热完成 - MACD.IsReady: {macd.IsReady}, CloseIdentity.IsReady: {closeIdentity.IsReady}");
        }
        public override void OnData(Slice data)
        {
            if (data == null) return;

            foreach (var symbol in _macdAnalysis.Keys)
            {
                if (!data.ContainsKey(symbol)) continue;

                var currentData = data[symbol];
                if (currentData == null) continue;

                try
                {
                    var time = ParseShanghaiTime(currentData.Date);
                    var closePrice = currentData.Close;

                    var macdAnalysis = _macdAnalysis[symbol];
                    if (macdAnalysis != null && macdAnalysis.Macd.IsReady && macdAnalysis.CloseIdentity.IsReady)
                    {
                        Log($"{macdAnalysis.Name},{macdAnalysis.Industry} 时间: {time}, 收盘价: {closePrice}, MACD: {macdAnalysis.Macd.Current.Value}, 收盘价: {macdAnalysis.CloseIdentity.Current.Value}, " +
                            $"{(macdAnalysis.IsGoldenCross ? "金叉" : "false")},  {(macdAnalysis.IsDeathCross ? "死叉" : "false")}, " +
                            $"{(macdAnalysis.IsBullishDivergence ? "底背离" : "false")}, {(macdAnalysis.IsBearishDivergence ? "顶背离" : "false")}, " +
                            $"{(macdAnalysis.IsReversal ? "反转" : "false")}, {(macdAnalysis.IsTrend ? "趋势" : "false")}, " +
                            $"K线收益率: {macdAnalysis.KLineReturn}, 20日收益率分位数: {macdAnalysis.TwentyDayReturnQuantile}");
                    }
                    else
                    {
                        Log($"时间: {time}, 收盘价: {closePrice}, MACD指标或收盘价指标数据尚未准备好");
                    }
                }
                catch (NullReferenceException ex)
                {
                    Log($"OnData方法中发生空引用异常: {ex.Message}");
                }
            }
        }

        public  DateTime ParseShanghaiTime(string dateString)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
            }
            catch (NullReferenceException ex)
            {
                Log($"解析上海时间时发生空引用异常: {ex.Message}");
                return DateTime.MinValue;
            }
        }
    }
}
