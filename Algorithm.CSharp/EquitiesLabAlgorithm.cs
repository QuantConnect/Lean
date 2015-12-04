using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class EquitiesLabAlgorithm : QCAlgorithm
    {
        public const string EquitiesLabKey = @"45e047dd182cd3846e4dcd5e2e7ce40e772e8302e76fd5f9790b116072bcc672";
        public const string EquitiesLabScreener = @"2792ba7b29506180544f6d8b53409fb703ab251659e9138bdf3f2b83f7b969b5";
        public const string EquitiesLabUrlFormat = @"https://www.equitieslab.com/play/prod/RestControl/get?key={0}&screener={1}&date={2}&metadata=false";

        private DateTime tradedToday;
        private EquitiesLabResponse _todaysResponse;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2015, 01, 05);
            SetEndDate(2015, 01, 09);

            SetCash(1000*1000);


            AddUniverse("equities-lab-universe", date =>
            {
                using (var client = new WebClient())
                {
                    //2014-12-30
                    var file = client.DownloadString(String.Format(EquitiesLabUrlFormat, EquitiesLabKey, EquitiesLabScreener, date.ToString("yyyy-MM-dd")));
                    var response = JsonConvert.DeserializeObject<EquitiesLabResponse>(file);
                    _todaysResponse = new EquitiesLabResponse();
                    _todaysResponse.Securities = response.Securities.Where(x => ValidSymbols.Contains(x.Ticker)).ToList();
                    return _todaysResponse.Securities.Select(x => x.Ticker);
                }
            });

            // cancell all orders at EOD
            Schedule.Event("Cancel Open Orders").EveryDay().At(TimeSpan.FromHours(16)).Run(() =>
            {
                foreach (var ticket in Transactions.GetOrderTickets(x => x.Status.IsOpen()))
                {
                    ticket.Cancel();
                }
            });
        }

        public void OnData(TradeBars slice)
        {
            if (tradedToday.Date != Time.Date)
            {
                // leave a small buffer of cash
                var targetPercentage = 1m/(_todaysResponse.Securities.Count + 1);

                foreach (var target in _todaysResponse.Securities.Where(x => ValidSymbols.Contains(x.Ticker)))
                {
                    // rebalance portfolio to equal weights
                    SetHoldings(target.Ticker, targetPercentage);

                    // set stop loss / profit orders
                    var existingHoldings = Securities[target.Ticker].Holdings.Quantity;
                    StopMarketOrder(target.Ticker, -existingHoldings, target.StopLoss);
                    LimitOrder(target.Ticker, -existingHoldings, target.StopGain);
                }

                tradedToday = Time.Date;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                // if we receive a fill cancel the other outstanding order
                Transactions.CancelOpenOrders(orderEvent.Symbol);
            }
        }

        class EquitiesLabResponse
        {
            [JsonProperty(PropertyName = "securities")]
            public List<EquitiesLabTarget> Securities = new List<EquitiesLabTarget>();
        }

        class EquitiesLabTarget
        {
            [JsonProperty(PropertyName = "ticker")]
            public string Ticker;
            [JsonProperty(PropertyName = "stop_loss")]
            public decimal StopLoss;
            [JsonProperty(PropertyName = "stop_gain")]
            public decimal StopGain;
        }

        public static readonly HashSet<string> ValidSymbols = new HashSet<string>
        {
            {"NP"},
            {"PKX"},
            {"RYN"},
            {"HCLP"},
            {"OLN"},
            {"MLM"},
            {"SCOK"},
            {"NEU"},
            {"SSLT"},
            {"MON"},
            {"HSC"},
            {"TX"},
            {"CLW"},
            {"ROCK"},
            {"FNV"},
            {"WOR"},
            {"HMY"},
            {"TAHO"},
            {"AAON"},
            {"STLD"},
            {"MAS"},
            {"TS"},
            {"GLT"},
            {"PGEM"},
            {"CHMT"},
            {"NRP"},
            {"BBL"},
            {"EMN"},
            {"SYT"},
            {"SNMX"},
            {"ARLP"},
            {"USCR"},
            {"DD"},
            {"DOW"},
            {"NOR"},
            {"DEL"},
            {"KOP"},
            {"AA"},
            {"OCIP"},
            {"FMSA"},
            {"USLM"},
            {"SCL"},
            {"GPRE"},
            {"SHW"},
            {"PPP"},
            {"JHX"},
            {"UFPI"},
            {"OMG"},
            {"GOLD"},
            {"X"},
            {"SLCA"},
            {"RGLD"},
            {"WY"},
            {"TREC"},
            {"TMST"},
            {"RPM"},
            {"CSTM"},
            {"CYT"},
            {"TNH"},
            {"AUY"},
            {"GSM"},
            {"WLKP"},
            {"AVD"},
            {"SCHN"},
            {"KALU"},
            {"USG"},
            {"GGB"},
            {"FMC"},
            {"HBM"},
            {"VHI"},
            {"ZEUS"},
            {"FBR"},
            {"MOS"},
            {"RIO"},
            {"GRA"},
            {"BIOA"},
            {"PAH"},
            {"IPHS"},
            {"GG"},
            {"AWI"},
            {"HUN"},
            {"BTU"},
            {"IIIN"},
            {"IPI"},
            {"PENX"},
            {"POT"},
            {"MTRN"},
            {"CENX"},
            {"APD"},
            {"MT"},
            {"ASPN"},
            {"RS"},
            {"GFF"},
            {"EXP"},
            {"BVN"},
            {"CF"},
            {"DDC"},
            {"SWC"},
            {"CCJ"},
            {"CDE"},
            {"RIOM"},
            {"LYB"},
            {"BCC"},
            {"RTK"},
            {"AG"},
            {"RNO"},
            {"FSM"},
            {"CHNR"},
            {"SHLM"},
            {"LXU"},
            {"UFS"},
            {"SCCO"},
            {"SZYM"},
            {"ZINC"},
            {"UAN"},
            {"FELP"},
            {"IBP"},
            {"TREX"},
            {"ABX"},
            {"VMC"},
            {"WLB"},
            {"RYAM"},
            {"AMRS"},
            {"OMN"},
            {"IOSP"},
            {"SXCP"},
            {"ECL"},
            {"BAK"},
            {"SLW"},
            {"TC"},
            {"AKS"},
            {"OC"},
            {"SQM"},
            {"CE"},
            {"PVG"},
            {"GFI"},
            {"SSRI"},
            {"TROX"},
            {"PX"},
            {"ASH"},
            {"YZC"},
            {"SA"},
            {"HL"},
            {"AXTA"},
            {"BECN"},
            {"MUX"},
            {"WDFC"},
            {"CBT"},
            {"SBGL"},
            {"LPX"},
            {"NTIC"},
            {"CGA"},
            {"OEC"},
            {"IFF"},
            {"VALE"},
            {"CLF"},
            {"CRH"},
            {"CMC"},
            {"DRD"},
            {"HW"},
            {"NCS"},
            {"PGTI"},
            {"NUE"},
            {"CMP"},
            {"MIL"},
            {"MDM"},
            {"CLD"},
            {"SID"},
            {"PPG"},
            {"DOOR"},
            {"AHGP"},
            {"BHP"},
            {"AGI"},
            {"PAAS"},
            {"NX"},
            {"NTK"},
            {"MTX"},
            {"KS"},
            {"ACH"},
            {"FCX"},
            {"SWM"},
            {"PCL"},
            {"SMG"},
            {"ALB"},
            {"SXC"},
            {"ODC"},
            {"MERC"},
            {"CSTE"},
            {"KGC"},
            {"NEM"},
            {"AXLL"},
            {"KRA"},
            {"AGU"},
            {"WPP"},
            {"CBPX"},
            {"AEM"},
            {"HNRG"},
            {"BXC"},
            {"RNF"},
            {"SIAL"},
            {"APOG"},
            {"USAP"},
            {"FUL"},
            {"MBII"},
            {"FF"},
            {"STCK"},
            {"FOE"},
            {"SXT"},
            {"BCPC"},
            {"VAL"},
            {"OCIR"},
            {"SYNL"},
            {"POL"},
            {"TGEN"},
            {"KWR"},
            {"ARG"},
            {"MEOH"},
            {"ZEP"},
            {"NWPX"},
            {"EXK"},
            {"PATK"},
            {"WLK"},
            {"ACI"},
            {"TRQ"},
            {"KMG"},
            {"HWKN"},
            {"RFP"},
            {"KRO"},
            {"MDU"},
            {"HNH"},
            {"BLDR"}
        };
    }
}
