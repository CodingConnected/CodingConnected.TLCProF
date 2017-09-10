using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml;
using CodingConnected.TLCProF.Models;
using TLCGen.Models.Enumerations;
using DetectorTypeEnum = TLCGen.Models.Enumerations.DetectorTypeEnum;

namespace CodingConnected.TLCProF.TLCGenGen
{
    public static class TLCProFCodeGenerator
    {
        public static void GenerateXml(TLCGen.Models.ControllerModel model, string pathname)
        {
            try
            {
                var newmodel = new ControllerModel();
                newmodel.Data.Name = model.Data.Naam;
                newmodel.Data.MaximumWaitingTime = model.Data.Fasebewaking;

                foreach (var sg in model.Fasen)
                {
                    var nsg = new SignalGroupModel(sg.Naam, sg.TGG, sg.TFG, 250, sg.TGL, sg.TRG, sg.TRG, sg.Kopmax);
                    nsg.ExtendGreenFree = sg.Meeverlengen == NooitAltijdAanUitEnum.Altijd ||
                                          sg.Meeverlengen == NooitAltijdAanUitEnum.SchAan;
                    nsg.WaitGreen = sg.Wachtgroen == NooitAltijdAanUitEnum.Altijd ||
                                    sg.Wachtgroen == NooitAltijdAanUitEnum.SchAan;
                    nsg.FixedRequest = (sg.VasteAanvraag == NooitAltijdAanUitEnum.Altijd ||
                                       sg.VasteAanvraag == NooitAltijdAanUitEnum.SchAan) ? FixedRequestTypeEnum.Red : FixedRequestTypeEnum.None;
                    var coor = sg.BitmapCoordinaten.FirstOrDefault();
                    if (coor != null)
                    {
                        nsg.Coordinates = new System.Drawing.Point(coor.X, coor.Y);
                    }
                    foreach (var d in sg.Detectoren)
                    {
                        var nd = new DetectorModel(d.Naam, ConvertRequestType(d.Aanvraag),
                            ConvertExtendingType(d.Verlengen),
                            d.TDB ?? 0, d.TDH ?? 0, d.TBG ?? 0, d.TDH ?? 0)
                        {
                            Type = ConvertDetectorType(d.Type)
                        };
                        var dcoor = d.BitmapCoordinaten.FirstOrDefault();
                        if (dcoor != null)
                        {
                            nd.Coordinates = new System.Drawing.Point(dcoor.X, dcoor.Y);
                        }
                        nsg.Detectors.Add(nd);
                    }
                    newmodel.SignalGroups.Add(nsg);
                }
                foreach (var c in model.InterSignaalGroep.Conflicten)
                {
                    var sgf = newmodel.SignalGroups.First(x => x.Name == c.FaseVan);
                    sgf.InterGreenTimes.Add(new InterGreenTimeModel(sgf.Name, c.FaseNaar, c.Waarde));
                }
                foreach (var ml in model.ModuleMolen.Modules)
                {
                    var nml = new BlockModel(ml.Naam);
                    foreach (var mlsg in ml.Fasen)
                    {
                        nml.AddSignalGroup(mlsg.FaseCyclus);
                    }
                    newmodel.BlockStructure.Blocks.Add(nml);
                }
                foreach (var msg in model.ModuleMolen.FasenModuleData)
                {
                    var newmsg = newmodel.BlockStructure.Blocks.SelectMany(x => x.SignalGroups).First(x => x.SignalGroupName == msg.FaseCyclus);
                    if (newmsg != null)
                    {
                        newmsg.BlocksAheadAllowed = msg.ModulenVooruit;
                        newmsg.AlternativeSpace = msg.AlternatieveRuimte * 100;
                    }
                }
                newmodel.BlockStructure.WaitingBlockName = model.ModuleMolen.WachtModule;

                var filename = Path.Combine(pathname, model.Data.Naam + "_tlcprof.xml");

                var xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineHandling = NewLineHandling.Entitize
                };
                var ser = new DataContractSerializer(typeof(ControllerModel), new DataContractSerializerSettings()
                {
                    SerializeReadOnlyTypes = true
                });
                using (var fs = new FileStream(filename, FileMode.Create))
                using (var xmlWriter = XmlWriter.Create(fs, xmlWriterSettings))
                {
                    ser.WriteObject(xmlWriter, newmodel);
                    xmlWriter.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "TLCProFCodeGenerator: Error occured");
            }
        }

        private static DetectorRequestTypeEnum ConvertRequestType(
            DetectorAanvraagTypeEnum t)
        {
            switch (t)
            {
                case DetectorAanvraagTypeEnum.Geen:
                    return DetectorRequestTypeEnum.None;
                case DetectorAanvraagTypeEnum.Uit:
                    return DetectorRequestTypeEnum.None;
                case DetectorAanvraagTypeEnum.RnietTRG:
                    return DetectorRequestTypeEnum.RedNonGuaranteed;
                case DetectorAanvraagTypeEnum.Rood:
                    return DetectorRequestTypeEnum.Red;
                case DetectorAanvraagTypeEnum.RoodGeel:
                    return DetectorRequestTypeEnum.Amber;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        private static DetectorExtendingTypeEnum ConvertExtendingType(
            DetectorVerlengenTypeEnum t)
        {
            switch (t)
            {
                case DetectorVerlengenTypeEnum.Geen:
                    return DetectorExtendingTypeEnum.None;
                case DetectorVerlengenTypeEnum.Uit:
                    return DetectorExtendingTypeEnum.None;
                case DetectorVerlengenTypeEnum.Kopmax:
                    return DetectorExtendingTypeEnum.HeadMax;
                case DetectorVerlengenTypeEnum.MK1:
                    return DetectorExtendingTypeEnum.Measure;
                case DetectorVerlengenTypeEnum.MK2:
                    return DetectorExtendingTypeEnum.Measure;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        private static CodingConnected.TLCProF.Models.DetectorTypeEnum ConvertDetectorType(TLCGen.Models.Enumerations.DetectorTypeEnum t)
        {
            switch (t)
            {
                case DetectorTypeEnum.Kop:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Head;
                case DetectorTypeEnum.Lang:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Long;
                case DetectorTypeEnum.Verweg:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Away;
                case DetectorTypeEnum.File:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Jam;
                case DetectorTypeEnum.Knop:
                case DetectorTypeEnum.KnopBinnen:
                case DetectorTypeEnum.KnopBuiten:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Button;
                case DetectorTypeEnum.VecomIngang:
                case DetectorTypeEnum.OpticomIngang:
                case DetectorTypeEnum.Overig:
                case DetectorTypeEnum.Radar:
                    return CodingConnected.TLCProF.Models.DetectorTypeEnum.Other;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }
    }
}