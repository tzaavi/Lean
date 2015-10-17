using System;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class TrendFollowingAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage emaTrendFast;
        private ExponentialMovingAverage emaTrendSlow;
        private ExponentialMovingAverage emaEntry;
        private AverageTrueRange atr;

        private decimal stop;
        private TradeBar lastLongPeriodBar;
        private decimal lastLongPeriodAtr;
        private decimal trendStrength;
        private bool isStrongBull;
        private bool isStrongBear;

        public override void Initialize()
        {
            SetStartDate(2015, 09, 1);
            SetEndDate(2015, 09, 30);

            // securities
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute, "oanda", true, 0, false);
            
            // custom fee model
            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);

            // indicators
            emaTrendFast = new ExponentialMovingAverage(10);
            emaTrendSlow = new ExponentialMovingAverage(50);
            emaEntry = new ExponentialMovingAverage(10);
            atr = new AverageTrueRange(7);

            // consolidate
            var consolidatorLong = new TradeBarConsolidator(TimeSpan.FromHours(4));
            consolidatorLong.DataConsolidated += OnLongTimePertiodData;
            SubscriptionManager.AddConsolidator("EURUSD", consolidatorLong);

            var consolidatorShort = new TradeBarConsolidator(TimeSpan.FromMinutes(20));
            consolidatorShort.DataConsolidated += OnShortTimePertiodData;
            SubscriptionManager.AddConsolidator("EURUSD", consolidatorShort);
        }

        public void OnShortTimePertiodData(Object o, TradeBar bar)
        {
            // no point to continue if indicators not resy
            if (!emaTrendSlow.IsReady)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                // short signal
                if (emaTrendFast < emaTrendSlow && lastLongPeriodBar.Close > emaTrendFast && bar.Close < emaTrendFast)
                {
                    stop = lastLongPeriodBar.Close + 0.0001m;
                    Order("EURUSD", -10000);
                }

                // long signal
                if (emaTrendFast > emaTrendSlow && lastLongPeriodBar.Close < emaTrendFast && bar.Close > emaTrendFast)
                {
                    stop = lastLongPeriodBar.Close - 0.0001m;
                    Order("EURUSD", 10000);
                }
            }

            if (Portfolio.Invested)
            {
                var holding = Portfolio["EURUSD"];

                if (holding.IsLong)
                {
                    HanldeOpenLongPoistoin(bar);
                }

                if (holding.IsShort)
                {
                    HanldeOpenShortPoistoin(bar);
                }
            }
        }

        public void OnLongTimePertiodData(Object o, TradeBar bar)
        {
            // update indicators
            emaEntry.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            emaTrendFast.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            emaTrendSlow.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Value));
            atr.Update(bar);

            // no point to continue if indicators not resy
            if (!emaTrendSlow.IsReady)
            {
                lastLongPeriodBar = bar;
                lastLongPeriodAtr = atr.Current;
                return;
            }
                

            // calc trend
            //trendStrength = emaTrendFast - emaTrendSlow;
            //isStrongBull = bar.Close > emaTrendFast && emaTrendFast > emaTrendSlow;
            //isStrongBear = bar.Close < emaTrendFast && emaTrendFast < emaTrendSlow;
            

            // check for investment opportunity
            //if (!Portfolio.Invested)
            //    HandleLongOpportunity(bar);

            //if (!Portfolio.Invested)
            //    HandleShortOpportunity(bar);

         
            // plots
            Plot("close", bar.Close);
            Plot("trendStrength", trendStrength);
            Plot("emaFastTrend", emaTrendFast.Current);
            Plot("emaSlowTrend", emaTrendSlow.Current);
            if(stop > 0)
                Plot("stop", stop);

            // save values for later use
            lastLongPeriodBar = bar;
            lastLongPeriodAtr = atr.Current;
        }

        private void HandleLongOpportunity(TradeBar bar)
        {
            if (isStrongBull)
            {
                stop = bar.Low - 0.0001m;
                Order("EURUSD", 10000);
            }
        }

        private void HanldeOpenLongPoistoin(TradeBar bar)
        {
            // update stop
            var newStop = lastLongPeriodBar.Close - 2 * lastLongPeriodAtr;
            if (newStop > stop)
                stop = newStop;

            // close position
            if (Portfolio.Invested && bar.Close < stop)
            {
                stop = 0;
                Liquidate();
            }
        }

        private void HandleShortOpportunity(TradeBar bar)
        {
            if (isStrongBear)
            {
                stop = bar.High + 0.0001m;
                Order("EURUSD", -10000);
            }
        }

        private void HanldeOpenShortPoistoin(TradeBar bar)
        {
            // update stop
            var newStop = lastLongPeriodBar.Close + 2 * lastLongPeriodAtr;
            if (newStop < stop)
                stop = newStop;

            // close position
            if (Portfolio.Invested && bar.Close > stop)
            {
                stop = 0;
                Liquidate();
            }
        }
    }
}