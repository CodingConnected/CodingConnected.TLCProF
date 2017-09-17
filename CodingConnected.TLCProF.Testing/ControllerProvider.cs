using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Testing
{
	public static class ControllerProvider
	{
		public static ControllerModel GetsimpleController1()
		{
			// Signalgroups
			var sg02 = new SignalGroupModel("02", 40, 50, 250, 30, 20, 30, 60);
			sg02.Detectors.Add(new DetectorModel("021", DetectorRequestTypeEnum.RedNonGuaranteed, DetectorExtendingTypeEnum.HeadMax, 0, 0, 10, 360));
			sg02.Detectors.Add(new DetectorModel("022", DetectorRequestTypeEnum.Red, DetectorExtendingTypeEnum.Measure, 0, 0, 10, 360));
			sg02.Detectors.Add(new DetectorModel("023", DetectorRequestTypeEnum.None, DetectorExtendingTypeEnum.Measure, 0, 0, 10, 360));

			var sg05 = new SignalGroupModel("05", 40, 50, 250, 30, 20, 30, 60);
			sg05.Detectors.Add(new DetectorModel("051", DetectorRequestTypeEnum.RedNonGuaranteed, DetectorExtendingTypeEnum.HeadMax, 0, 0, 10, 360));
			sg05.Detectors.Add(new DetectorModel("052", DetectorRequestTypeEnum.Red, DetectorExtendingTypeEnum.Measure, 0, 0, 10, 360));

			var sg12 = new SignalGroupModel("12", 40, 50, 250, 30, 20, 30, 60);
			sg12.Detectors.Add(new DetectorModel("121", DetectorRequestTypeEnum.RedNonGuaranteed, DetectorExtendingTypeEnum.HeadMax, 0, 0, 10, 360));
			sg12.Detectors.Add(new DetectorModel("122", DetectorRequestTypeEnum.RedNonGuaranteed, DetectorExtendingTypeEnum.None, 0, 0, 10, 360));

			var c = new ControllerModel();
			c.Data.MaximumWaitingTime = 240;
			c.SignalGroups.Add(sg02);
			c.SignalGroups.Add(sg05);
			c.SignalGroups.Add(sg12);

			sg02.InterGreenTimes.Add(new InterGreenTimeModel("02", "05", 45));
			sg02.InterGreenTimes.Add(new InterGreenTimeModel("02", "12", 35));
			sg05.InterGreenTimes.Add(new InterGreenTimeModel("05", "02", 55));
			sg05.InterGreenTimes.Add(new InterGreenTimeModel("05", "12", 25));
			sg12.InterGreenTimes.Add(new InterGreenTimeModel("12", "02", 15));
			sg12.InterGreenTimes.Add(new InterGreenTimeModel("12", "05", 65));

			var b1 = new BlockModel("B1");
			b1.AddSignalGroup("02");

			var b2 = new BlockModel("B2");
			b2.AddSignalGroup("05");

			var b3 = new BlockModel("B3");
			b3.AddSignalGroup("12");

			c.BlockStructure.WaitingBlockName = "B1";
			c.BlockStructure.Blocks.Add(b1);
			c.BlockStructure.Blocks.Add(b2);
			c.BlockStructure.Blocks.Add(b3);

			return c;
		}
	}
}