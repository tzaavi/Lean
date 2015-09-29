using System;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    //todo: - dicide when to open position on tick not bar
    //      - take in account spred 
    //      - find out the best diff value to open position
    //      - make the take profit dynamic (calcualted value)
    public class Scalping4Algorithm : QCAlgorithm
    {
        private ExponentialMovingAverage emaLongTrend;
        private ExponentialMovingAverage emaShortTrend;
        private ExponentialMovingAverage emaPrice;
        private ParabolicStopAndReverse sar;
        private Slope slope;
        private decimal stopLoss;
        private DateTime lastOrderTime;

        public override void Initialize()
        {
            SetStartDate(2015, 06, 1);
            SetEndDate(2015, 06, 30);


            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "dukascopy", true, 0, false);

            emaLongTrend = EMA("EURUSD", 200, Resolution.Minute);
            emaShortTrend = EMA("EURUSD", 30, Resolution.Minute);
            emaPrice = EMA("EURUSD", 10, Resolution.Minute);
            sar = PSAR("EURUSD", resolution: Resolution.Minute);

            slope = new Slope(10, 10);
            RegisterIndicator("EURUSD", slope, Resolution.Minute, x => x.Value);


            var consolidator = new TickConsolidator(TimeSpan.FromMinutes(5));
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
                Plot("SAR", sar.Current);

            if(slope.IsReady)
                Plot("slope", slope.Current);

            Plot("Price", "close", bar.Close);
            Plot("Price", "emaLongTrend", emaLongTrend.Current);
            Plot("Price", "emaShortTrend", emaShortTrend.Current);
            Plot("Price", "emaPrice", emaPrice.Current);
            Plot("Balance", Portfolio.TotalPortfolioValue);
            Plot("Holding", Portfolio["EURUSD"].Quantity);
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
            if (bar.Close > sar &&
                emaPrice > emaShortTrend &&
                slope * 10000 > 3m)
            {
                stopLoss = sar - 0.0001m;
                Order("EURUSD", 10000);
                lastOrderTime = bar.Time;
            }
        }

        public void HandleShortOpportunity(TradeBar bar)
        {
            if (bar.Close < sar &&
                emaPrice < emaShortTrend &&
                slope * 10000 < -3m)
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

            if (holding.IsLong && (pips > 20 || tick.Price < stopLoss || (emaPrice < emaShortTrend && pips > 0)))
            {
                Liquidate();
            }

            if (holding.IsShort && (pips > 20 || tick.Price > stopLoss || (emaPrice > emaShortTrend && pips > 0)))
            {
                Liquidate();
            }
        }
    }
}