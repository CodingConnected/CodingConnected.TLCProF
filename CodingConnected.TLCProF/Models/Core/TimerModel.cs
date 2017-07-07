﻿using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Timer", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class TimerModel
    {
        #region Properties

        [ModelName]
        [DataMember]
        public string Name { get; private set; }

        // State
        [IgnoreDataMember]
        public int Current { get; private set; }
        [IgnoreDataMember]
        public int Remaining { get; private set; }
        [IgnoreDataMember]
        public bool Running { get; private set; }

        // Settings
        [DataMember]
        public int Maximum { get; private set; }

        #endregion // Properties

        #region Events

        [field: NonSerialized]
        public event EventHandler Started;
        [field: NonSerialized]
        public event EventHandler Stopped;
        [field: NonSerialized]
        public event EventHandler Ended;
        [field: NonSerialized]
        public event EventHandler Continued;

        #endregion // Events

        #region Public Methods

        public void Start()
        {
            Current = 0;
            Remaining = Maximum;
            Running = true;
            Started?.Invoke(this, new EventArgs());
        }

        public void Stop()
        {
            Running = false;
            Stopped?.Invoke(this, new EventArgs());
        }

        public void Continue()
        {
            Running = true;
            Continued?.Invoke(this, new EventArgs());
        }

        public void Step(int miliseconds)
        {
            if (!Running) return;

            Current += miliseconds;
            Remaining -= miliseconds;

            if (Current < Maximum - miliseconds / 2) return;

            Running = false;
            Ended?.Invoke(this, new EventArgs());
        }

        public void SetMaximum(int maximum, TimerTypeEnum type)
        {
            switch(type)
            {
                case TimerTypeEnum.Minutes:
                    Maximum = maximum * 60000;
                    break;
                case TimerTypeEnum.Seconds:
                    Maximum = maximum * 1000;
                    break;
                case TimerTypeEnum.Tenths:
                    Maximum = maximum * 100;
                    break;
                case TimerTypeEnum.Hunderdths:
                    Maximum = maximum * 10;
                    break;
                case TimerTypeEnum.Thousands:
                    Maximum = maximum;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        #endregion // Public Methods

        #region Constructors

        public TimerModel(string name, int maximum, TimerTypeEnum type = TimerTypeEnum.Tenths)
        {
            Name = name;
            SetMaximum(maximum, type);
        }

        #endregion // Constructors
    }
}