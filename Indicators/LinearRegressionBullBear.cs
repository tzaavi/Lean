using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    // source https://www.tradingview.com/script/4X2icV1L-RS-NM-Improved-Linear-Regression-Bull-and-Bear-Power-v01/

    // this code uses the Linear Regression Bull and Bear Power indicator created by RicardoSantos
    // and adds a signal line 
    // Use : if signal line is changes color, you have your signal, green = buy, red = sell
    // Advice : best used with a zero lag indicator like ZeroLagEMA_LB from LazyBear
    // if price is above ZLEMA and signal = green => buy, price below ZLEMA and signal = red => sell
    public class LinearRegressionBullBear : WindowIndicator<IndicatorDataPoint>
    {
        private Maximum max;
        private Minimum min;

        public LinearRegressionBullBear(string name, int period) : base(name, period)
        {
        }

        public LinearRegressionBullBear(int period) :
            this(string.Format("BBP_NM ({0})", period), period)
        {
            max = new Maximum(period);
            min = new Minimum(period);
        }


        private decimal ExpLR(decimal height, decimal length)
        {
            return height + (height/length);
        }

        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            var h_value = window.Max();
            var l_value = window.Min();

            max.Update(input);
            min.Update(input);

            var h_bar = max.Current;
            var l_bar = min.Current;

            var bear = 0 - ExpLR(h_value - input.Value, h_bar);
            var bull = 0 + ExpLR(input.Value - l_value, l_bar);
            var direction = bull*2 + bear*2;

            return direction;
        }

        public bool IsBull
        {
            get { return Current > 0; }
        }

        public bool IsBear
        {
            get { return Current < 0; }
        }
    }
}