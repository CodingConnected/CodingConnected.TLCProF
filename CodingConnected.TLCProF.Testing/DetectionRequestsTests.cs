using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodingConnected.TLCProF.Hosting;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Simulation;
using NUnit.Framework;

namespace CodingConnected.TLCProF.Testing
{
	[TestFixture]
    public class DetectionRequestsTests
    {
		[Test]
	    public void DetectorWithOccupiedTimerZero_PresenceSetToTrue_SetsRequestOnNextStep()
	    {
		    var c = ControllerProvider.GetsimpleController1();
		    var m = new ControllerManager(c);
		    var sg02 = c.SignalGroups.First(x => x.Name == "02");
		    var d021 = sg02.Detectors.First(x => x.Name == "021");

			m.ExecuteStep(5000);
			d021.Presence = true;
			m.ExecuteStep(1);

			Assert.IsTrue(sg02.HasGreenRequest);
	    }

	    [Test]
	    public void DetectorWithOccupiedTimerNonZero_PresenceSetToTrue_SetsRequestOnOccupiedTimerEnd()
	    {
		    var c = ControllerProvider.GetsimpleController1();
		    var m = new ControllerManager(c);
		    var sg02 = c.SignalGroups.First(x => x.Name == "02");
		    var d021 = sg02.Detectors.First(x => x.Name == "021");
			d021.OccupiedTimer.SetMaximum(3, TimerTypeEnum.Seconds);

		    m.ExecuteStep(5000);
		    d021.Presence = true;
		    m.ExecuteStep(3000);

		    Assert.IsTrue(sg02.HasGreenRequest);
	    }

	    [Test]
	    public void DetectorWithOccupiedTimerNonZero_PresenceSetToTrue_NoRequestBeforeOccupiedTimerEnd()
	    {
		    var c = ControllerProvider.GetsimpleController1();
		    var m = new ControllerManager(c);
		    var sg02 = c.SignalGroups.First(x => x.Name == "02");
		    var d021 = sg02.Detectors.First(x => x.Name == "021");
		    d021.OccupiedTimer.SetMaximum(3, TimerTypeEnum.Seconds);

		    m.ExecuteStep(5000);
		    d021.Presence = true;
		    m.ExecuteStep(2999);

		    Assert.IsFalse(sg02.HasGreenRequest);
	    }

	    [Test]
	    public void DetectorRequestRedNonGuaranteed_PresenceSetToTrue_NoRequestBeforeRedGuaranteedDone()
	    {
		    var c = ControllerProvider.GetsimpleController1();
		    var m = new ControllerManager(c);
		    var sg02 = c.SignalGroups.First(x => x.Name == "02");
		    var d021 = sg02.Detectors.First(x => x.Name == "021");

		    m.ExecuteStep(100);
		    d021.Presence = true;
		    m.ExecuteStep(1800);

		    Assert.IsFalse(sg02.HasGreenRequest);
	    }

	    [Test]
	    public void DetectorRequestRedNonGuaranteed_PresenceSetToTrue_RequestAfterRedGuaranteedDone()
	    {
		    var c = ControllerProvider.GetsimpleController1();
		    var m = new ControllerManager(c);
		    var sg02 = c.SignalGroups.First(x => x.Name == "02");
		    var d021 = sg02.Detectors.First(x => x.Name == "021");

		    m.ExecuteStep(100);
		    d021.Presence = true;
		    m.ExecuteStep(2000);

		    Assert.IsTrue(sg02.HasGreenRequest);
	    }
	}
}
