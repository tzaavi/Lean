﻿using System;
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

        private string symbol = "USDJPY";

        private RollingWindow<TradeBar> lastBars = new RollingWindow<TradeBar>(3);
        private decimal lastShortClose;

        //[IntParameter(95, 100, 5)]
        public int SlowTrendSize = 100;

        //[DecimalParameter(0.001, 0.003, 0.0005)]
        public decimal Target = 0.002m;

        //[DecimalParameter(0.1, 0.3, 0.02)]
        [DecimalParameter(0.16, 0.16, 0.01)]
        public decimal TralingStopPercent = 0.16m;

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2015, 11, 21);
            

            // securities
            AddSecurity(SecurityType.Forex, symbol, Resolution.Minute, "oanda", true, 0, false);
            
            // custom fee model
            Securities[symbol].TransactionModel = new ConstantFeeTransactionModel(0);

            // indicators
            emaTrendFast = new ExponentialMovingAverage(5);
            emaTrendSlow = new ExponentialMovingAverage(100);
            atr = new AverageTrueRange(10);

            // consolidate
            var consolidatorLong = new TradeBarConsolidator(TimeSpan.FromMinutes(60));
            consolidatorLong.DataConsolidated += OnLongTimePertiodData;
            SubscriptionManager.AddConsolidator(symbol, consolidatorLong);

            var consolidatorShort = new TradeBarConsolidator(TimeSpan.FromMinutes(1));
            consolidatorShort.DataConsolidated += OnShortTimePertiodData;
            SubscriptionManager.AddConsolidator(symbol, consolidatorShort);

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
                    stop = bar.Close - (TralingStopPercent / 100 * bar.Close);
                    target = bar.Close + Target;
                    Order(symbol, 10000);
                }

                if (isShortSetup && bar.Close < lastBars[0].High && lastShortClose > lastBars[0].High)
                {
                    stop = bar.Close + (TralingStopPercent / 100 * bar.Close);
                    target = bar.Close - Target;
                    Order(symbol, -10000);
                }
            }


            //update stops
            if (Portfolio.Invested)
            {
                var holding = Portfolio[symbol];

                if (holding.IsLong && (bar.Close - (TralingStopPercent / 100 * bar.Close)) > stop)
                {
                    stop = bar.Close - (TralingStopPercent / 100 * bar.Close);
                }

                if (holding.IsShort && (bar.Close + (TralingStopPercent / 100 * bar.Close)) < stop)
                {
                    stop = bar.Close + (TralingStopPercent / 100 * bar.Close);
                }
            }


            // close trade
            if (Portfolio.Invested)
            {
                var holding = Portfolio[symbol];

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