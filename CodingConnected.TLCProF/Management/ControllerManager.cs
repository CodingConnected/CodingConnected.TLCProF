using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Helpers;
using CodingConnected.TLCProF.Models;
using NLog;

namespace CodingConnected.TLCProF.Management
{
    public class ControllerManager
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private List<ManagerBase> _Managers;
        private List<FunctionalityContainer> _Functionalities;
        private bool _Processing = false;
        private double missed_time = 0;

        #endregion // Fields

        #region Properties

        public ControllerModel Controller { get; private set; }
        public List<TimerModel> Timers { get; private set; }

        #endregion // Properties

        #region Public Methods

        public void InsertFunctionality(Action action, ControllerFunctionalityEnum functionality, int order)
        {
            var container = _Functionalities.Where(x => x.Functionality == functionality).FirstOrDefault();
            if(container == null)
            {
                throw new NotImplementedException("No container found for functionality " + functionality.ToString());
            }
            else if (container.Actions.ContainsKey(order))
            {
                throw new NotImplementedException("Container with functionality " + functionality.ToString() + "already has functionality at index " + order);
            }
            else
            {
                container.AddAction(action, order);
            }
        }

        public void ExecuteStep(double timeAmount)
        {
            if (_Processing)
            {
                missed_time += timeAmount;
                _logger.Warn("Missed a step");
                return;
            }

            _Processing = true;

            // Time
            var time = Math.Truncate(timeAmount + missed_time);
            missed_time = (timeAmount + missed_time) - time;
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
            foreach(var f in _Functionalities)
            {
                f.ExecuteActions();
            }
            
            _Processing = false;

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
            _Functionalities = new List<FunctionalityContainer>();
            foreach(ControllerFunctionalityEnum f in Enum.GetValues(typeof(ControllerFunctionalityEnum)))
            {
                _Functionalities.Add(new FunctionalityContainer(f));
            }

            // Find all functionality
            _Managers = new List<ManagerBase>();
            var tlcprof = typeof(ControllerManager).Assembly;
            foreach (var type in tlcprof.GetTypes())
            {
                var attr = (ControllerManagerAttribute)Attribute.GetCustomAttribute(type, typeof(ControllerManagerAttribute));
                if (attr == null) continue;
                try
                {
                    var v = Activator.CreateInstance(type, new object[] { this, controller }) as ManagerBase;
                    _Managers.Add(v);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error creating manager of type " + type.ToString() + ".\n\nException: \n" + e.ToString());
                }
            }

            // Subscribe controller to relevant events of its members
            foreach (var sg in controller.SignalGroups)
            {
                sg.StateChanged += controller.OnSignalGroupStateChanged;
            }
        }

        private List<TimerModel> GetAllTimers(object obj)
        {
            var l = new List<TimerModel>();
            if (obj == null) return l;

            // Object as IOElement
            TimerModel tm = obj as TimerModel;
            if (tm != null)
            {
                l.Add(tm);
            }

            Type objType = obj.GetType();
            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.CustomAttributes.Any(x => x.AttributeType == typeof(IgnoreDataMemberAttribute)))
                {
                    continue;
                }

                Type propType = property.PropertyType;
                if (!property.PropertyType.IsValueType)
                {
                    object propValue = property.GetValue(obj);
                    var elems = propValue as IList;
                    if (elems != null)
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
