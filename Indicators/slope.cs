namespace QuantConnect.Indicators
{
    public class Slope : Indicator
    {
        private ExponentialMovingAverage _ema;
        private readonly RollingWindow<decimal> _window;

        public Slope(string name, int regressionPerid, int smoothPeriod) : base(name)
        {
            _ema = new ExponentialMovingAverage(smoothPeriod);
            _window = new RollingWindow<decimal>(regressionPerid);
        }

        public Slope(int regressionPerid, int smoothPeriod)
            : this(string.Format("Slope({0}, {1}", regressionPerid, smoothPeriod), regressionPerid, smoothPeriod)
        {
            
        }
            
        public override bool IsReady
        {
            get { return _window.IsReady; }
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _ema.Update(input);
            if(_ema.IsReady)
                _window.Add(_ema);

            foreach (var y in _window)
            {
                var x = y;
            }

            return IsReady ? GetLinregSlope() : 0;
        }

        private decimal GetLinregSlope()
        {
            decimal sxy = 0;
            decimal sx = 0;
            decimal sy = 0;
            decimal sx2 = 0;
            decimal n = _window.Size;

            var i = n;
            foreach (var y in _window)
            {
                decimal x = --i;
                sxy += x * y;
                sx += x;
                sy += y;
                sx2 += x * x;
            }

            return ((n * sxy) - (sx * sy)) / ((n * sx2) - (sx * sx));
        }

        public override void Reset()
        {
            base.Reset();
            _ema.Reset();
            _window.Reset();
        }
    }
}