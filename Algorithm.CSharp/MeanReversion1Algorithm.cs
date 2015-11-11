using System;
using System.Collections.Generic;
using NodaTime;
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

        private RollingWindow<TradeBar> lastBars = new RollingWindow<TradeBar>(3);
        private decimal lastShortClose;

        [IntParameter(90, 110, 5)]
        public int SlowTrendSize = 100;

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2014, 2, 1);
            

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

            // setup charts
            AddChart(new Chart("Price", ChartType.Overlay));
        }

        public void OnShortTimePertiodData(Object o, TradeBar bar)
        {
            // open trade
            if (!Portfolio.Invested)
            {
                if (isLongSetup && bar.Close > lastBars[0].Low && lastShortClose < lastBars[0].Low)
                {
                    stop = bar.Close - 0.001m;
                    target = bar.Close + 0.002m;
                    Order("EURUSD", 10000);
                }

                if (isShortSetup && bar.Close < lastBars[0].High && lastShortClose > lastBars[0].High)
                {
                    stop = bar.Close + 0.001m;
                    target = bar.Close - 0.002m;
                    Order("EURUSD", -10000);
                }
            }


            //update stops
            if (Portfolio.Invested)
            {
                var holding = Portfolio["EURUSD"];

                if (holding.IsLong && (bar.Close - 0.001m) > stop)
                {
                    stop = bar.Close - 0.001m;
                }

                if (holding.IsShort && (bar.Close + 0.001m) < stop)
                {
                    stop = bar.Close + 0.001m;
                }
            }


            // close trade
            if (Portfolio.Invested)
            {
                var holding = Portfolio["EURUSD"];

                if (holding.IsLong && (bar.Close < stop))
                {
                    Liquidate();
                }

                if (holding.IsShort && (bar.Close > stop))
                {
                    Liquidate();
                }
            }

            lastShortClose = bar.Close;

        }

        public void OnLongTimePertiodData(Object o, TradeBar bar)
        {
            // update indicators
            emaTrendFast.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            emaTrendSlow.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            atr.Update(bar);
            
            // cheack if we have every thing to run strategy
            if (emaTrendSlow > 0 && lastEmaTrendSlow > 0 && lastBars.IsReady)
            {
                // check long setup
                isLongSetup =
                    bar.Close > lastEmaTrendSlow &&
                    bar.Close < lastEmaTrendFast &&
                    bar.Low < lastBars[0].Low &&
                    lastBars[0].Low < lastBars[1].Low &&
                    lastBars[1].Low < lastBars[2].Low &&

                    bar.Close < lastBars[0].Close &&
                    lastBars[0].Close < lastBars[1].Close &&
                    lastBars[1].Close < lastBars[2].Close;


                // check short setup
                isShortSetup =
                    bar.Close < lastEmaTrendSlow &&
                    bar.Close > lastEmaTrendFast &&
                    bar.High > lastBars[0].High &&
                    lastBars[0].High > lastBars[1].High &&
                    lastBars[1].High > lastBars[2].High &&

                    bar.Close > lastBars[0].Close &&
                    lastBars[0].Close > lastBars[1].Close &&
                    lastBars[1].Close > lastBars[2].Close;

            }


            // save values for later use
            lastBars.Add(bar);
            lastAtr = atr.Current;
            lastEmaTrendSlow = emaTrendSlow.Current;
            lastEmaTrendFast = emaTrendFast.Current;

            // plots
            Plot("Price", "Close", bar.Close);
            Plot("Price", "Open", bar.Open);
            Plot("Price", "Low", bar.Low);
            Plot("Price", "High", bar.High);
            Plot("Price", "emaFastTrend", emaTrendFast.Current);
            Plot("Price", "emaSlowTrend", emaTrendSlow.Current);
            if (stop > 0)
                Plot("Price", "stop", stop);
        }
    }
}