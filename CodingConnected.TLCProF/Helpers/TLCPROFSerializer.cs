using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Helpers
{
    public class TLCPROFSerializer
    {
        public void SerializeController(ControllerModel model, string filename)
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize,
                CheckCharacters = false
            };
            var ser = new DataContractSerializer(typeof(ControllerModel), new DataContractSerializerSettings()
            {
                SerializeReadOnlyTypes = true,
                RootName = XmlDictionaryString.Empty
            });
            using (var fs = new FileStream(filename, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fs, xmlWriterSettings))
            {
                ser.WriteObject(xmlWriter, model);
                xmlWriter.Close();
            }
        }

        public ControllerModel DeserializeController(string filename)
        {
            var xmlReaderSettings = new XmlReaderSettings
            {
                CheckCharacters = false
            };
            var d = new XmlDictionary();
            // We need to set rootname and rootnamespace explicitly for compatability with Mono
            var rootname = d.Add("Controller");
            var rootnamespace = d.Add("http://www.codingconnected.eu/TLC_PROF.Models");
            var ser = new DataContractSerializer(typeof(ControllerModel), new DataContractSerializerSettings()
            {
                SerializeReadOnlyTypes = true,
                RootName = rootname,
                RootNamespace = rootnamespace
            });
            ControllerModel model = null;
            using (var fs = new FileStream(filename, FileMode.Open))
            using (var xmlReader = XmlReader.Create(fs, xmlReaderSettings))
            {
                model = (ControllerModel)ser.ReadObject(xmlReader);
                xmlReader.Close();
            }
            return model;
        }
    }
}