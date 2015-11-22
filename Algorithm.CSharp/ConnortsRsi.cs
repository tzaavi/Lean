using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class ConnortsRsi : QCAlgorithm
    {
        private RelativeStrengthIndex rsi;
        private SimpleMovingAverage maPrice;
        private SimpleMovingAverage maTrend;

        private const string Symbol = "EURUSD";

        [IntParameter(2, 2, 1)]
        public int RsiSize = 2;

        public int MaTrendSize = 200;

        public int MaPriceSize = 5;

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2015, 11, 21);
            SetCash(10000);
            AddSecurity(SecurityType.Forex, Symbol, Resolution.Minute, "oanda", true, 0, false);

            // custom fee model
            Securities[Symbol].TransactionModel = new ConstantFeeTransactionModel(0);

            // init indicators
            rsi = new RelativeStrengthIndex(RsiSize);
            maTrend = new SimpleMovingAverage(MaTrendSize);
            maPrice = new SimpleMovingAverage(MaPriceSize);

            // consolidate
            var consolidatorLong = new TradeBarConsolidator(TimeSpan.FromMinutes(60));
            consolidatorLong.DataConsolidated += OnLongTimePertiodData;
            SubscriptionManager.AddConsolidator(Symbol, consolidatorLong);
        }

        void OnLongTimePertiodData(object o, TradeBar bar)
        {
            // update indicators
            rsi.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Close));
            maPrice.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Close));
            maTrend.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Close));

            // cheack if we have every thing to run strategy
            if (rsi.IsReady && maPrice.IsReady && maTrend.IsReady)
            {
                if (!Portfolio.Invested)
                {
                    // check long
                    if (rsi < 10 &&
                        bar.Close > maTrend &&
                        bar.Close < maPrice)
                    {
                        Order(Symbol, 10000);
                    }


                    // check short
                    if (rsi > 90 &&
                        bar.Close < maTrend &&
                        bar.Close > maPrice)
                    {
                        Order(Symbol, -10000);
                    }
                }


                if (Portfolio.Invested)
                {
                    var holding = Portfolio[Symbol];

                    if (holding.IsLong && bar.Close > maPrice)
                    {
                        Liquidate();
                    }

                    if (holding.IsShort && bar.Close < maPrice)
                    {
                        Liquidate();
                    }
                }

            }

            // plots
            Plot("Price", "Close", bar.Close);
            Plot("Price", "Open", bar.Open);
            Plot("Price", "Low", bar.Low);
            Plot("Price", "High", bar.High);
            Plot("Price", "maTrend", maTrend.Current);
            Plot("Price", "maPrice", maPrice.Current);
        }
    }
}
