using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Parabolic SAR Indicator 
    /// Base on TA-Lib implementation
    /// </summary>
    public class ParabolicStopAndReversal : TradeBarIndicator
    {
        private bool _isLong;
        private TradeBar _previousBar;
        private decimal _sar;
        private decimal _ep;
        private bool _isReady;
        private decimal _af = 0.02m;
        private decimal _afInit = 0.02m;
        private decimal _maxAf = 0.2m;
        private decimal _afIncrement = 0.02m;
        private decimal _outputSar;


        /// <summary>
        /// Create new Parabolic SAR
        /// </summary>
        /// <param name="start">Start value for acceleration factor</param>
        /// <param name="increment">Acceleration factor increment</param>
        /// <param name="max">Max value for acceleration factor</param>
        public ParabolicStopAndReversal(decimal start = 0.02m, decimal increment = 0.02m, decimal max = 0.2m) 
            : base(string.Format("SAR({0},{1},{2})", start, increment, max))
        {
        }

        public override bool IsReady
        {
            get { return _isReady; }
        }

        private void Init(TradeBar currentBar)
        {
            // init position
            _isLong = currentBar.Close >= _previousBar.Close;


            // init sar and Extreme price
            if (_isLong)
            {
                _ep = Math.Min(currentBar.High, _previousBar.High);
                _sar = _previousBar.Low;
            }
            else
            {
                _ep = Math.Min(currentBar.Low, _previousBar.Low);
                _sar = _previousBar.High;
            }
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (_previousBar == null)
            {
                _previousBar = input;
                return 0;
            }

            if (Samples == 2)
            {
                Init(input);
                _isReady = false;
            }

            if (_isLong)
            {
                HandleLongPosition(input);
            }
            else
            {
                HandleShortPosition(input);
            }

            _previousBar = input;

            return _outputSar;
        }

        private void HandleLongPosition(TradeBar currentBar)
        {
            // Switch to short if the low penetrates the SAR value.
            if (currentBar.Low <= _sar)
            {
                // Switch and Overide the SAR with the ep
                _isLong = false;
                _sar = _ep;

                // Make sure the overide SAR is within yesterday's and today's range.
                if (_sar < _previousBar.High)
                    _sar = _previousBar.High;
                if (_sar < currentBar.High)
                    _sar = currentBar.High;

                // Output the overide SAR 
                _outputSar = _sar;

                // Adjust af and ep
                _af = _afInit;
                _ep = currentBar.Low;

                // Calculate the new SAR
                _sar = _sar + _af*(_ep - _sar);

                // Make sure the new SAR is within yesterday's and today's range.
                if (_sar < _previousBar.High)
                    _sar = _previousBar.High;
                if (_sar < currentBar.High)
                    _sar = currentBar.High;

            }

            // No switch
            else
            {
                // Output the SAR (was calculated in the previous iteration) 
                _outputSar = _sar;

                // Adjust af and ep.
                if (currentBar.High > _ep)
                {
                    _ep = currentBar.High;
                    _af += _afIncrement;
                    if (_af > _maxAf)
                        _af = _maxAf;
                }

                // Calculate the new SAR
                _sar = _sar + _af * (_ep - _sar);

                // Make sure the new SAR is within yesterday's and today's range.
                if (_sar > _previousBar.Low)
                    _sar = _previousBar.Low;
                if (_sar > currentBar.Low)
                    _sar = currentBar.Low;
            }
        }


        private void HandleShortPosition(TradeBar currentBar)
        {
            // Switch to long if the high penetrates the SAR value.
            if (currentBar.High >= _sar)
            {
                // Switch and Overide the SAR with the ep
                _isLong = true;
                _sar = _ep;

                // Make sure the overide SAR is within yesterday's and today's range.
                if (_sar > _previousBar.Low)
                    _sar = _previousBar.Low;
                if (_sar > currentBar.Low)
                    _sar = currentBar.Low;

                // Output the overide SAR 
                _outputSar = _sar;

                // Adjust af and ep
                _af = _afInit;
                _ep = currentBar.High;

                // Calculate the new SAR
                _sar = _sar + _af * (_ep - _sar);

                // Make sure the new SAR is within yesterday's and today's range.
                if (_sar > _previousBar.Low)
                    _sar = _previousBar.Low;
                if (_sar > currentBar.Low)
                    _sar = currentBar.Low;
            }

            //No switch
            else
            {
                // Output the SAR (was calculated in the previous iteration)
                _outputSar = _sar;

                // Adjust af and ep.
                if (currentBar.Low < _ep)
                {
                    _ep = currentBar.Low;
                    _af += _afIncrement;
                    if (_af > _maxAf)
                        _af = _maxAf;
                }

                // Calculate the new SAR
                _sar = _sar + _af * (_ep - _sar);

                // Make sure the new SAR is within yesterday's and today's range.
                if (_sar < _previousBar.High)
                    _sar = _previousBar.High;
                if (_sar < currentBar.High)
                    _sar = currentBar.High;
            }
        }
    }
}