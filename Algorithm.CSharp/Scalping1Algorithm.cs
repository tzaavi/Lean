using System;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    // http://investazor.com/2013/08/21/simple-forex-scalping-strategy-system/
    public class Scalping1Algorithm : QCAlgorithm
    {
        private BollingerBands bb;
        private RelativeStrengthIndex rsi;
        private TradeBar lastBar;

        public override void Initialize()
        {
            SetStartDate(2015, 08, 19);
            SetEndDate(2015, 08, 29);
            

            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick, "oanda", true, 0, false);

            bb = BB("EURUSD", 50, 2, MovingAverageType.Simple, Resolution.Minute);
            rsi = RSI("EURUSD", 14, MovingAverageType.Exponential, Resolution.Minute);

            var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += OnMinute;
            SubscriptionManager.AddConsolidator("EURUSD", consolidator);

            Securities["EURUSD"].TransactionModel = new ConstantFeeTransactionModel(0);
        }

        public void OnMinute(Object o, TradeBar bar)
        {

            if (Portfolio.Invested)
            {
                if (ShouldClosePosition())
                {
                    Liquidate();
                }
            }
            else
            {
                if (ChekcLongOpportunity(bar))
                {
                    //Order("EURUSD", 10000);
                }

                if (ChekcShortOpportunity(bar))
                {
                    Order("EURUSD", -10000);
                }
            }



            Plot("Price", "BB_mid", bb.MiddleBand);
            Plot("Price", "BB_upper", bb.UpperBand);
            Plot("Price", "BB_lower", bb.LowerBand);
            Plot("Price", "close", bar.Close);
            Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
            PlotIndicator("RSI", rsi);
            lastBar = bar;
        }

        public bool ChekcLongOpportunity(TradeBar bar)
        {
            if (lastBar == null)
                return false;

            return lastBar.Close < bb.LowerBand && bar.Close > bb.LowerBand;
        }

        public bool ChekcShortOpportunity(TradeBar bar)
        {
            if (lastBar == null)
                return false;

            //return lastBar.Close > bb.UpperBand && bar.Close < bb.UpperBand && IsBearishShootingStar(bar);
            return rsi.Current > 70 && IsBearishShootingStar(bar);
        }

        public bool ShouldClosePosition()
        {
            var holding = Portfolio["EURUSD"];

            var pips = (holding.Price - holding.AveragePrice)*10000;

            return pips > 5 || pips < -5;
        }

        //http://www.onlinetradingconcepts.com/TechnicalAnalysis/Candlesticks/ShootingStar.html
        public bool IsBearishShootingStar(TradeBar bar)
        {
            var body = Math.Abs(bar.Open - bar.Close);
            var HL = Math.Abs(bar.High - bar.Low);
            decimal upperShadow;
            decimal lowerShadow;

            if (HL == 0)
            {
                return false;
            }

            if (bar.Close > bar.Open)
            {
                upperShadow = Math.Abs(bar.High - bar.Close);
                lowerShadow = Math.Abs(bar.Open - bar.Low);
            }
            else
            {
                upperShadow = Math.Abs(bar.High - bar.Open);
                lowerShadow = Math.Abs(bar.Close - bar.Low);
            }

            var bodyRatio = body/HL;
            var upperRatio = upperShadow/HL;
            var lowerRatio = lowerShadow/HL;

            return lowerRatio < 0.1m && bodyRatio < (upperRatio / 2);

        }

        public bool IsBullishShootingStar()
        {
            return false;
        }

        public void OnData(Ticks data)
        {
            
        }
    }
}