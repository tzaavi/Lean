using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    //https://www.tradingview.com/script/LTqZz3l9-Indicator-Zero-Lag-EMA-a-simple-trading-strategy/
    public class ZeroLagExponentialMovingAverage : Indicator
    {
        private ExponentialMovingAverage ema;
        private ExponentialMovingAverage emaOfEma;

        public ZeroLagExponentialMovingAverage(string name, int period) 
            : base(name)
        {
            ema = new ExponentialMovingAverage(period);
            emaOfEma = new ExponentialMovingAverage(period);
        }

        public ZeroLagExponentialMovingAverage(int period)
            : this("ZL_EMA" + period, period)
        {
        }

        public override bool IsReady
        {
            get { return ema.IsReady && emaOfEma.IsReady; }
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            ema.Update(input);
            emaOfEma.Update(ema.Current);

            var diff = ema - emaOfEma;

            return ema + diff;
        }
    }
}