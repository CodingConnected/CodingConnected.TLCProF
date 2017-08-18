using System;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Simulation
{
    public class SimpleDetectorSim
    {
        #region Fields

        private readonly DetectorModel _model;
        private readonly Random _random;
        private int _intensityLow;
        private int _intensityHigh;
        private DateTime NextChange;
        private DateTime NextIntensityChange;

        #endregion // Fields

        #region Properties


        #endregion // Properties

        #region Public Methods

        public void UpdateSimulationState(DateTime currentTime)
        {
            if (currentTime >= NextIntensityChange)
            {
                _intensityLow = GetRandomNumber(300, 2, 4);
                _intensityHigh = GetRandomNumber(_intensityLow * 2, _intensityLow + 2, 3);
            }
            if (currentTime >= NextChange)
            {
                _model.Presence = !_model.Presence;
                NextChange = currentTime.AddSeconds(_random.Next(_intensityLow, _intensityHigh));
            }
        }

        private int GetRandomNumber(int max, int min, double probabilityPower = 2)
        {
            var randomDouble = _random.NextDouble();

            var result = Math.Floor(min + (max + 1 - min) * (Math.Pow(randomDouble, probabilityPower)));
            return (int)result;
        }

        #endregion // Public Methods

        #region Constructor

        public SimpleDetectorSim(DetectorModel model, DateTime startDate, Random random)
        {
            _model = model;
            _random = random;
            NextIntensityChange = startDate.AddHours(_random.Next(3, 6));
            _intensityLow = GetRandomNumber(300, 2, 4);
            _intensityHigh = GetRandomNumber(_intensityLow * 2, _intensityLow + 2, 3);
            NextChange = startDate.AddSeconds(_random.Next(_intensityLow, _intensityHigh));
        }

        #endregion // Constructor
    }
}