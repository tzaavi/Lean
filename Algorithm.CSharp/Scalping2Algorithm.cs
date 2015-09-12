using System;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    // http://www.dolphintrader.com/forex-scalping-strategy-parabolic-sar-advanced-macd-v3-indicator/
    public class Scalping2Algorithm : QCAlgorithm
    {
        private ExponentialMovingAverage ema;
        private ParabolicStopAndReversal sar;


        public override void Initialize()
        {
            SetStartDate(2015, 09, 08);
            SetEndDate(2015, 09, 08);
            

            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "oanda", true, 0, false);

            ema = EMA("EURUSD", 125);
            sar = new ParabolicStopAndReversal();
            RegisterIndicator("EURUSD", sar, Resolution.Minute);

            var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += OnMinute;
            SubscriptionManager.AddConsolidator("EURUSD", consolidator);

            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);
        }

        public void OnMinute(Object o, TradeBar bar)
        {


            Plot("Price", "close", bar.Close);
            //Plot("Price", "EMA-125", ema.Current);
            Plot("Price", "SAR", sar.Current);
            Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
            PlotIndicator("EMA", ema);
        }

        public bool ChekcLongOpportunity(TradeBar bar)
        {
            return false;
        }

        public void OnData(Ticks data)
        {
            
        }
    }
}