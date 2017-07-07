using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    public class SwitchModel
    {
        #region Properties

        [ModelName]
        public string Name { get; set; }

        // Settings
        public bool State { get; set; }

        #endregion // Properties

        #region Constructor

        public SwitchModel()
        {

        }

        #endregion // Constructor
    }
}
