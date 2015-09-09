using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    namespace QuantConnect.Algorithm.Examples
    {

        public class MyAlgorithm : QCAlgorithm
        {
            private ExponentialMovingAverage fast;
            private ExponentialMovingAverage slow;
            private MovingAverageConvergenceDivergence macd;
            private AverageDirectionalIndex adx;

            public override void Initialize()
            {
                SetStartDate(2015, 09, 02);  //Set Start Date
                SetEndDate(2015, 09, 02);    //Set End Date
                SetCash(100000);             //Set Strategy Cash

                AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute, "oanda", true, 0, false);
                AddSecurity(SecurityType.Forex, "EURJPY", Resolution.Minute, "oanda", true, 0, false);
                

                fast = EMA("EURUSD", 15, Resolution.Minute);
                slow = EMA("EURUSD", 30, Resolution.Minute);
                macd = MACD("EURUSD", 10, 20, 7, MovingAverageType.Exponential, Resolution.Minute);
                adx = ADX("EURUSD", 14, Resolution.Minute);
            }

            /// <summary>
            /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            /// </summary>
            /// <param name="data">TradeBars IDictionary object with your stock data</param>

            private int count = 0;
            public void OnData(TradeBars data)
            {
                count++;
                if (count%10 == 0)
                {
                    var holdingEURUSD = Portfolio["EURUSD"];
                    bool investedEURUSD = false;

                    if ((holdingEURUSD.Quantity == 0 || holdingEURUSD.Quantity < 0) && !investedEURUSD)
                    {
                        Order("EURUSD", Math.Abs(holdingEURUSD.Quantity) + 100);
                        investedEURUSD = true;
                        Debug("--Purchased Stock");
                    }

                    if ((holdingEURUSD.Quantity == 0 || holdingEURUSD.Quantity > 0) && !investedEURUSD)
                    {
                        Order("EURUSD", -(holdingEURUSD.Quantity + 100));
                        investedEURUSD = true;
                        Debug("--Purchased Short");
                    }


                    var holdingNZDUSD = Portfolio["EURJPY"];
                    bool investedNZDUSD = false;

                    if ((holdingNZDUSD.Quantity == 0 || holdingNZDUSD.Quantity < 0) && !investedNZDUSD)
                    {
                        Order("EURJPY", Math.Abs(holdingNZDUSD.Quantity) + 100);
                        investedNZDUSD = true;
                        Debug("--Purchased Stock");
                    }

                    if ((holdingNZDUSD.Quantity == 0 || holdingNZDUSD.Quantity > 0) && !investedNZDUSD)
                    {
                        Order("EURJPY", -(holdingNZDUSD.Quantity + 100));
                        investedNZDUSD = true;
                        Debug("--Purchased Short");
                    }
                }
                

               


                /*if (Portfolio.Invested)
                {
                    Order("EURUSD", -1000);
                    Order("NZDUSD", -1500);
                }

                if (!Portfolio.Invested)
                {
                    Order("EURUSD", 1000);
                    Order("NZDUSD", 1500);
                    Debug("Purchased Stock");
                }*/


                PlotIndicator("EURUSD_EMA", fast);
                PlotIndicator("EURUSD_EMA", slow);
                PlotIndicator("EURUSD_MACD", macd);
                PlotIndicator("ADX", adx);
                Plot("Balance", "Balance", Portfolio.TotalPortfolioValue);
            }
        }
    }
}