using System;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class MeanReversionAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage emaTrendFast;
        private ExponentialMovingAverage emaTrendSlow;
        private AverageTrueRange atr;

        private decimal stop;
        private decimal target;
        private decimal lastAtr;
        private decimal lastEmaTrendFast;
        private decimal lastEmaTrendSlow;
        private bool isLongSetup;
        private bool isShortSetup;

        private List<TradeBar> lastBars = new List<TradeBar>(); 

        public override void Initialize()
        {
            SetStartDate(2015, 01, 1);
            SetEndDate(2015, 09, 30);

            // securities
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute, "oanda", true, 0, false);
            
            // custom fee model
            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);

            // indicators
            emaTrendFast = new ExponentialMovingAverage(5);
            emaTrendSlow = new ExponentialMovingAverage(100);
            atr = new AverageTrueRange(10);

            // consolidate
            var consolidatorLong = new TradeBarConsolidator(TimeSpan.FromMinutes(60));
            consolidatorLong.DataConsolidated += OnLongTimePertiodData;
            SubscriptionManager.AddConsolidator("EURUSD", consolidatorLong);

            var consolidatorShort = new TradeBarConsolidator(TimeSpan.FromMinutes(1));
            consolidatorShort.DataConsolidated += OnShortTimePertiodData;
            SubscriptionManager.AddConsolidator("EURUSD", consolidatorShort);
        }

        public void OnShortTimePertiodData(Object o, TradeBar bar)
        {
            if (!Portfolio.Invested)
            {
                if (isLongSetup && bar.Close < lastBars[0].Close - 0.5m*lastAtr)
                {
                    stop = bar.Close - 0.0005m;
                    target = bar.Close + 0.002m;
                    Order("EURUSD", 10000);
                }

                if (isShortSetup && bar.Close > lastBars[0].Close + 0.5m * lastAtr)
                {
                    stop = bar.Close + 0.0005m;
                    target = bar.Close - 0.002m;
                    Order("EURUSD", -10000);
                }
            }


            if (Portfolio.Invested)
            {
                var holding = Portfolio["EURUSD"];

                if (holding.IsLong && (bar.Close < stop || bar.Close > target))
                {
                    Liquidate();
                }

                if (holding.IsShort && (bar.Close > stop || bar.Close < target))
                {
                    Liquidate();
                }
            }
            
        }

        public void OnLongTimePertiodData(Object o, TradeBar bar)
        {
            // update indicators
            emaTrendFast.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            emaTrendSlow.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            atr.Update(bar);
                

            // cheack if we have every thing to run strategy
            if (emaTrendSlow > 0 && lastEmaTrendSlow > 0)
            {
                // check long setup
                isLongSetup =
                    bar.Close > lastEmaTrendSlow &&
                    bar.Close < lastEmaTrendFast &&
                    bar.Low < lastBars[0].Low &&
                    lastBars[0].Low < lastBars[1].Low &&
                    lastBars[1].Low < lastBars[2].Low;


                // check short setup
                isShortSetup =
                    bar.Close < lastEmaTrendSlow &&
                    bar.Close > lastEmaTrendFast &&
                    bar.High > lastBars[0].High &&
                    lastBars[0].High > lastBars[1].High &&
                    lastBars[1].High > lastBars[2].High;

            }


            // save values for later use
            SaveBar(bar);
            lastAtr = atr.Current;
            lastEmaTrendSlow = emaTrendSlow.Current;
            lastEmaTrendFast = emaTrendFast.Current;

            // plots
            Plot("close", bar.Close);
            Plot("emaFastTrend", emaTrendFast.Current);
            Plot("emaSlowTrend", emaTrendSlow.Current);
            if (stop > 0)
                Plot("stop", stop);
        }

        private void SaveBar(TradeBar bar)
        {
            lastBars.Insert(0, bar);
            if(lastBars.Count > 3)
                lastBars.RemoveAt(lastBars.Count - 1);
        }
    }
}