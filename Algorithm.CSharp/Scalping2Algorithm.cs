using System;
using System.Linq;
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
        private ParabolicStopAndReverse sar;
        private MovingAverageConvergenceDivergence macd;
        private decimal stopLoss;
        private int tradeCount = 0;

        private DateTime lastOrderTime;

        public override void Initialize()
        {
            SetStartDate(2015, 08, 2);
            SetEndDate(2015, 08, 31);


            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "dukascopy", true, 0, false);

            ema = EMA("EURUSD", 125, Resolution.Minute);
            macd = MACD("EURUSD", 12, 26, 9, resolution: Resolution.Minute);
            sar = PSAR("EURUSD", resolution: Resolution.Minute);


            var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += OnMinute;
            SubscriptionManager.AddConsolidator("EURUSD", consolidator);

            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);
        }

        public void OnMinute(Object o, TradeBar bar)
        {
            if (!Portfolio.Invested)
                HandleLongOpportunity(bar);    
            

            if (!Portfolio.Invested)
                HandleShortOpportunity(bar);

            if (sar.IsReady)
                Plot("Price", "SAR", sar.Current);

            Plot("Price", "close", bar.Close);
            Plot("Price", "EMA", ema.Current);
            Plot("MACD", "Fast", macd.Fast);
            Plot("MACD", "Slow", macd.Slow);
            Plot("MACD", "Signal", macd.Signal);
            Plot("MACD", "Current", macd.Current);
            Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
            Plot("Holding", "Holding", Portfolio["EURUSD"].Quantity);
        }

        public void OnData(Ticks data)
        {
            var tick = data["EURUSD"].LastOrDefault();

            if (Portfolio.Invested && tick != null)
            {
                HanldeOpenPoistoin(tick);
            }

        }

        public void HandleLongOpportunity(TradeBar bar)
        {
            if (bar.Close > ema &&
                bar.Close > sar &&
                macd > 0.0002m)
            {
                stopLoss = sar - 0.0001m;
                Order("EURUSD", 10000);
                lastOrderTime = bar.Time;
            }
        }

        public void HandleShortOpportunity(TradeBar bar)
        {
            if (bar.Close < ema &&
                bar.Close < sar &&
                macd < -0.0002m)
            {
                stopLoss = sar + 0.0001m;
                Order("EURUSD", -10000);
                lastOrderTime = bar.Time;
            }
        }

        public void HanldeOpenPoistoin(Tick tick)
        {
            var holding = Portfolio["EURUSD"];

            var pips = (holding.Price - holding.AveragePrice) * 10000;

            if ((tick.Time - lastOrderTime).TotalHours > 10 && pips > 0)
            {
                Liquidate();
            }

            if ((tick.Time - lastOrderTime).TotalHours > 20)
            {
                Liquidate();
            }


            if (holding.IsLong && (pips > 20 || tick.Price < stopLoss))
            {
                Liquidate();
            }

            if (holding.IsShort && (pips > 20 || tick.Price > stopLoss))
            {
                Liquidate();
            }
        }
    }
}