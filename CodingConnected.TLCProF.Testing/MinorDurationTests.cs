using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Simulation;
using CodingConnected.TLCProF.Testing;
using NUnit.Framework;

namespace CodingConnected.TLCProF.Testing2
{
	[TestFixture]
	class MinorDurationTests
	{
		[Test]
		public void DuurtestController1_4Controllers1HourPerController1_NoMaxWaitingTimeExceeded()
		{
			var fault = false;
			var dts = new List<Thread>(4);
		    for (var i = 0; i < 4; ++i)
		    {
			    var i1 = i;
			    dts.Add(new Thread(() =>
			    {
				    var c = ControllerProvider.GetsimpleController1();
				    var m = new ControllerManager(c);
				    var s = new SimpleControllerSim(c, 42 + i1);
				    c.MaximumWaitingTimeExceeded += (o, e) => fault = true;
				    s.SimulationInit(c.Clock.CurrentTime);
				    for (long i2 = 0; i2 < 36000; ++i2)
				    {
					    s.SimulationStep(100);
					    m.ExecuteStep(100);
				    }
			    }));
		    }
		
			dts.ForEach(x => x.Start());
			dts.ForEach(x => x.Join());
			
			Assert.IsFalse(fault);
		}

		[Test]
		public void DuurtestController1_4Controllers1HourPerController2_NoMaxWaitingTimeExceeded()
		{
			var fault = false;
			var dts = new List<Thread>(4);
			for (var i = 0; i < 4; ++i)
			{
				var i1 = i;
				dts.Add(new Thread(() =>
				{
					var c = ControllerProvider.GetsimpleController1();
					var m = new ControllerManager(c);
					var s = new SimpleControllerSim(c, 42 + 4 + i1);
					c.MaximumWaitingTimeExceeded += (o, e) => fault = true;
					s.SimulationInit(c.Clock.CurrentTime);
					for (long i2 = 0; i2 < 36000; ++i2)
					{
						s.SimulationStep(100);
						m.ExecuteStep(100);
					}
				}));
			}

			dts.ForEach(x => x.Start());
			dts.ForEach(x => x.Join());

			Assert.IsFalse(fault);
		}
	}
}
