using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    // http://www.esignalcentral.com/support/futuresource/workstation/help/charts/studies/parab.htm
    /// <summary>
    ///     P = Position
    /// </summary>
    public class ParabolicStopAndReversal : TradeBarIndicator
    {
        private Position _position;
        private TradeBar _previousBar;
        private decimal _previousSar;
        private decimal _previousEP;
        private bool _isPositionChanged;
        private bool _isReady;
        private decimal _previousAF = 0.02m;


        public ParabolicStopAndReversal() : base("SAR")
        {
        }

        public override bool IsReady
        {
            get { return _isReady; }
        }

        private void Init(TradeBar currentBar)
        {
            
            // init position
            if (currentBar.Close < _previousBar.Close)
            {
                _position = Position.Short;
            }
            
            if (currentBar.Close >= _previousBar.Close)
            {
                _position = Position.Long;
            }

            
            // init sar
            if (_position == Position.Short)
            {
                _previousSar = _previousBar.High;
            }

            if (_position == Position.Long)
            {
                _previousSar = _previousBar.Low;
            }


            // init Extreme price
            if (_position == Position.Short)
            {
                _previousEP = Math.Min(currentBar.Low, _previousBar.Low);
            }

            if (_position == Position.Long)
            {
                _previousEP = Math.Min(currentBar.High, _previousBar.High);
            }

            _isReady = false;
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
                return 0;
            }
                

            // Determine current Extreme Price (EP)
            var currentEP = _position == Position.Short ? Math.Min(_previousEP, input.Low) : Math.Max(_previousEP, input.High);


            // Determine Acceleration Factor (AF)
            var currentAF = _previousAF;
            if (!_isPositionChanged && _previousEP != currentEP && currentAF < 0.2m)
                currentAF += 0.02m;

            // Calculate SAR
            var currentSAR = _previousSar + _previousAF*(_previousEP - _previousSar);


            // set "previous values" for next iteration
            _isPositionChanged = false;
            _previousEP = currentEP;
            _previousBar = input;
            _previousSar = currentSAR;
            _previousAF = currentAF;

            var nextPosition = _position;

            if (_position == Position.Long && currentSAR >= input.Low)
            {
                nextPosition = Position.Short;
                _isPositionChanged = true;
                _previousSar = _previousEP;
                _previousEP = input.Low;
                _previousAF = 0.02m;
            }

            if (_position == Position.Short && currentSAR <= input.High)
            {
                nextPosition = Position.Long;
                _isPositionChanged = true;
                _previousSar = _previousEP;
                _previousEP = input.High;
                _previousAF = 0.02m;
            }

            _position = nextPosition;


            return currentSAR;
        }




        private enum Position
        {
            Long,
            Short
        }

      
    }
}