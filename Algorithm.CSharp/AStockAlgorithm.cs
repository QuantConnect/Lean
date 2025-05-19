using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;

namespace QuantConnect.Algorithm.CSharp
{
    public class AStockAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        public  DateTime ParseShanghaiTime(string dateString) => 
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
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
            // 设置基准股票
            var benchmarkSymbol = AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
            SetBenchmark(benchmarkSymbol);
            // 添加股票数据
            _symbol = AddData<Api5MinCustomData>("sz.000001",Resolution.Daily, TimeZones.Utc).Symbol;
        }
        public override void OnData(Slice data)
        {
            // 假设我们要输出当前数据的时间和收盘价
            if (data.ContainsKey(_symbol))
            {
                var currentData = data[_symbol];
                var time = ParseShanghaiTime(currentData.Date);
                var closePrice = currentData.Price;

                Log($"时间: {time}, 收盘价: {closePrice}");
            }
        }
    }
}