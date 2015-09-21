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
        private ParabolicStopAndReverse sar;
        private MovingAverageConvergenceDivergence macd;
        private decimal stopLoss;


        public override void Initialize()
        {
            SetStartDate(2015, 08, 19);
            SetEndDate(2015, 08, 23);
            

            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "oanda", true, 0, false);

            ema = EMA("EURUSD", 125);
            macd = MACD("EURUSD", 12, 26, 9);
            sar = PSAR("EURUSD", resolution: Resolution.Minute);

            PlotIndicator("EMA", ema);
            PlotIndicator("sar", sar);
            PlotIndicator("MACD", macd);

            var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += OnMinute;
            SubscriptionManager.AddConsolidator("EURUSD", consolidator);

            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);
        }

        public void OnMinute(Object o, TradeBar bar)
        {
            if (Portfolio.Invested)
            {
                HanldeOpenPoistoin(bar);
            }
            else
            {
                HandleLongOpportunity(bar);    
                HandleShortOpportunity(bar);
            }

            if (sar.IsReady)
                Plot("Price", "SAR", sar.Current);

            Plot("Price", "close", bar.Close);
            Plot("Price", "EMA", ema.Current);
            Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
        }

        public void HandleLongOpportunity(TradeBar bar)
        {
            if (bar.Close > ema &&
                bar.Close > sar &&
                macd > 0)
            {
                stopLoss = sar - 0.0001m;
                Order("EURUSD", 10000);
            }
        }

        public void HandleShortOpportunity(TradeBar bar)
        {
            if (bar.Close < ema &&
                bar.Close < sar &&
                macd < 0)
            {
                stopLoss = sar + 0.0001m;
                Order("EURUSD", -10000);
            }
        }

        public void HanldeOpenPoistoin(TradeBar bar)
        {
            var holding = Portfolio["EURUSD"];

            var pips = (holding.Price - holding.AveragePrice) * 10000;

            if (holding.IsLong && (pips > 20 || bar.Price < stopLoss))
            {
                Liquidate();
            }

            if (holding.IsShort && (pips > 20 || bar.Price > stopLoss))
            {
                Liquidate();
            }
        }

        public void OnData(Ticks data)
        {
            var tick = data["EURUSD"][0];
            var x1 = Convert.ToInt64(QuantConnect.Time.DateTimeToUnixTimeStamp(tick.Time.ToUniversalTime()));
            var x2 = Convert.ToInt64(QuantConnect.Time.DateTimeToUnixTimeStamp(tick.Time));
        }
    }
}