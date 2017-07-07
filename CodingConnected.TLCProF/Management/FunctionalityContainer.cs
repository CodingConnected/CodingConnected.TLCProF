using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Management
{
    public class FunctionalityContainer
    {
        #region Fields

        private SortedDictionary<int, Action> _Actions;
        public SortedDictionary<int, Action> Actions
        {
            get
            {
                if (_Actions == null)
                {
                    _Actions = new SortedDictionary<int, Action>();
                }
                return _Actions;
            }
        }

        #endregion // Fields

        #region Properties

        public ControllerFunctionalityEnum Functionality { get; private set; }

        #endregion // Properties

        #region Public Methods

        public void AddAction(Action action, int order)
        {
            if(Actions.ContainsKey(order))
            {
                throw new InvalidOperationException($"Cannot add action; functionality container for {Functionality.ToString()} already has an action at index {order}.");
            }
            else
            {
                Actions.Add(order, action);
            }
        }

        public void ExecuteActions()
        {
            foreach(var a in Actions)
            {
                a.Value.Invoke();
            }
        }

        #endregion // Public Methods

        #region Constructor

        public FunctionalityContainer(ControllerFunctionalityEnum functionality)
        {
            Functionality = functionality;
        }

        #endregion // Constructor
    }
}
