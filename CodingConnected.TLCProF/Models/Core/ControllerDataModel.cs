using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "ControllerData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ControllerDataModel
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        #endregion // Properties
    }
}