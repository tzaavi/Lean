using System;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    // https://www.tradingview.com/chart/EURUSD/ATbi3N4l-NM-Bull-and-Bear-Power-trade-signal-thanks-to-RS/

    public class Scalping3Algorithm : QCAlgorithm
    {
        private ZeroLagExponentialMovingAverage zlema;
        private ParabolicStopAndReverse sar;
        private decimal stopLoss;
        private int tradeCount = 0;
        private LinearRegressionBullBear BBP_NM;

        private DateTime lastOrderTime;

        public override void Initialize()
        {
            SetStartDate(2015, 06, 1);
            SetEndDate(2015, 06, 30);


            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "dukascopy", true, 0, false);

            sar = PSAR("EURUSD", resolution: Resolution.Minute);

            BBP_NM = new LinearRegressionBullBear(10);
            RegisterIndicator("EURUSD", BBP_NM, Resolution.Minute, x => x.Value);

            zlema = new ZeroLagExponentialMovingAverage(34);
            RegisterIndicator("EURUSD", zlema, Resolution.Minute, x => x.Value);



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
            Plot("Price", "ZLEMA", zlema.Current);
            Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
            Plot("Holding", "Holding", Portfolio["EURUSD"].Quantity);

            Plot("BBP_NM", BBP_NM.Current);
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
            if (bar.Close > zlema &&
                bar.Close > sar &&
                BBP_NM.IsBull)
            {
                stopLoss = sar - 0.0001m;
                Order("EURUSD", 10000);
                lastOrderTime = bar.Time;
            }
        }

        public void HandleShortOpportunity(TradeBar bar)
        {
            if (bar.Close < zlema &&
                bar.Close < sar &&
                BBP_NM.IsBear)
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