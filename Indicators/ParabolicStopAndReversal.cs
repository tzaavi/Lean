﻿using System;
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

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return _isReady; }
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The trade bar input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            // On first iteration we can’t produce an SAR value so we save the current bar and return zero
            if (Samples == 1)
            {
                _previousBar = input;
                return 0;
            }

            // On second iteration we initiate the position the extreme point and the SAR
            if (Samples == 2)
            {
                Init(input);
                _isReady = true;
                _previousBar = input;
                return _sar;
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

        /// <summary>
        /// Initialize the indicator values 
        /// </summary>
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

        /// <summary>
        /// Calculate indicator value when the position is long
        /// </summary>
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

        /// <summary>
        /// Calculate indicator value when the position is short
        /// </summary>
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