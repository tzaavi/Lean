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

            public override void Initialize()
            {
                SetStartDate(2014, 05, 01);  //Set Start Date
                SetEndDate(2014, 05, 15);    //Set End Date
                SetCash(100000);             //Set Strategy Cash

                AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Hour);
                AddSecurity(SecurityType.Forex, "NZDUSD", Resolution.Hour);

                fast = EMA("EURUSD", 15, Resolution.Hour);
                slow = EMA("NZDUSD", 30, Resolution.Hour);
            }

            /// <summary>
            /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            /// </summary>
            /// <param name="data">TradeBars IDictionary object with your stock data</param>
            public void OnData(TradeBars data)
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


                var holdingNZDUSD = Portfolio["NZDUSD"];
                bool investedNZDUSD = false;

                if ((holdingNZDUSD.Quantity == 0 || holdingNZDUSD.Quantity < 0) && !investedNZDUSD)
                {
                    Order("NZDUSD", Math.Abs(holdingNZDUSD.Quantity) + 100);
                    investedNZDUSD = true;
                    Debug("--Purchased Stock");
                }

                if ((holdingNZDUSD.Quantity == 0 || holdingNZDUSD.Quantity > 0) && !investedNZDUSD)
                {
                    Order("NZDUSD", -(holdingNZDUSD.Quantity + 100));
                    investedNZDUSD = true;
                    Debug("--Purchased Short");
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
                PlotIndicator("NZDUSD_EMA", slow);
            }
        }
    }
}