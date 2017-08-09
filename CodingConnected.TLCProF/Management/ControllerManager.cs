using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CodingConnected.TLCProF.Helpers;
using CodingConnected.TLCProF.Models;
using NLog;

namespace CodingConnected.TLCProF.Management
{
    public class ControllerManager
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private List<ManagerBase> _managers;
        private List<FunctionalityContainer> _functionalities;
        private bool _processing;
        private double _missedTime;

        #endregion // Fields

        #region Properties

        public ControllerModel Controller { get; private set; }
        public List<TimerModel> Timers { get; private set; }

        #endregion // Properties

        #region Public Methods

        public void InsertFunctionality(Action action, ControllerFunctionalityEnum functionality, int order)
        {
            var container = _functionalities.FirstOrDefault(x => x.Functionality == functionality);
            if(container == null)
            {
                throw new NotImplementedException("No container found for functionality " + functionality);
            }
            if (container.Actions.ContainsKey(order))
            {
                throw new NotImplementedException("Container with functionality " + functionality + "already has functionality at index " + order);
            }
            container.AddAction(action, order);
        }

        public void ExecuteStep(double timeAmount)
        {
            if (_processing)
            {
                _missedTime += timeAmount;
                _logger.Warn("Missed a step");
                return;
            }

            _processing = true;

            // Time
            var time = Math.Truncate(timeAmount + _missedTime);
            _missedTime = (timeAmount + _missedTime) - time;
            Controller.Clock.Update((int)time);
            foreach (var t in Timers)
            {
                t.Step((int)time);
            }

            foreach(var sg in Controller.SignalGroups)
            {
                sg.ClearStateRequests();
            }

            // Functionality
            foreach(var f in _functionalities)
            {
                f.ExecuteActions();
            }
            
            _processing = false;

        }

        #endregion // Public Methods

        #region Private Methods

        private void Initialize(ControllerModel controller)
        {
            Controller = controller;

            // Collect elements
            Timers = GetAllTimers(Controller);
            ControllerUtilities.SetAllReferences(Controller);

            // Build list of functionality containers
            _functionalities = new List<FunctionalityContainer>();
            foreach(ControllerFunctionalityEnum f in Enum.GetValues(typeof(ControllerFunctionalityEnum)))
            {
                _functionalities.Add(new FunctionalityContainer(f));
            }

            // Find all functionality
            _managers = new List<ManagerBase>();
            var tlcprof = typeof(ControllerManager).Assembly;
            foreach (var type in tlcprof.GetTypes())
            {
                var attr = (ControllerManagerAttribute)Attribute.GetCustomAttribute(type, typeof(ControllerManagerAttribute));
                if (attr == null) continue;
                try
                {
                    var v = Activator.CreateInstance(type, this, controller) as ManagerBase;
                    _managers.Add(v);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error creating manager of type " + type + ".\n\nException: \n" + e);
                }
            }

            // Subscribe controller to relevant events of its members
            foreach (var sg in controller.SignalGroups)
            {
                sg.StateChanged += controller.OnSignalGroupStateChanged;
                sg.CurrentWaitingTime.Ended += controller.OnMaximumWaitingTimeExceeded;
                sg.CurrentWaitingTime.SetMaximum(controller.Data.MaximumWaitingTime, TimerTypeEnum.Seconds);
            }
        }

        private List<TimerModel> GetAllTimers(object obj)
        {
            var l = new List<TimerModel>();
            if (obj == null) return l;

            // Object as IOElement
            var tm = obj as TimerModel;
            if (tm != null)
            {
                l.Add(tm);
            }

            var objType = obj.GetType();
            var properties = objType.GetProperties();
            foreach (var property in properties)
            {
                if (property.CustomAttributes.Any(x => x.AttributeType == typeof(IgnoreDataMemberAttribute)) &&
                    property.CustomAttributes.All(x => x.AttributeType != typeof(TimerAttribute)))
                {
                    continue;
                }

                if (property.PropertyType.IsValueType) continue;

                var propValue = property.GetValue(obj);
                if (propValue is IList elems)
                {
                    foreach (var item in elems)
                    {
                        // Property as IOElement
                        tm = item as TimerModel;
                        if (tm != null)
                        {
                            l.Add(tm);
                        }
                        else
                        {
                            foreach (var i in GetAllTimers(item))
                            {
                                l.Add(i);
                            }
                        }
                    }
                }
                else
                {
                    // Property as IOElement
                    tm = propValue as TimerModel;
                    if (tm != null)
                    {
                        l.Add(tm);
                    }
                    else
                    {
                        foreach (var i in GetAllTimers(propValue))
                        {
                            l.Add(i);
                        }
                    }
                }
            }
            return l;
        }

        #endregion // Private Methods

        #region Constructor

        public ControllerManager(ControllerModel controller)
        {
            if (!IntegrityChecker.IsInterGreenMatrixOK(controller))
            {
                throw new NotImplementedException();
            }

            Initialize(controller);
        }
        #endregion // Constructor
    }
}
