using System;
using System.IO;
using CodingConnected.TLCProF.BmpUI;
using CodingConnected.TLCProF.Helpers;
using CodingConnected.TLCProF.Hosting;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Simulation;
using NLog;

namespace TLC_PROF_BmpUI_testAppl
{
    internal static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            ControllerModel controllerapplication;
            ControllerManager controllermanager;

            // Read controller application data from XML
            var ser = new TLCPROFSerializer();
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tint1_tlcprof.xml");
            if (File.Exists(filename))
            {
                try
                {
                    controllerapplication = ser.DeserializeController(filename);
                    controllermanager = new ControllerManager(controllerapplication);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to load controller application from file {0}", filename);
                    throw;
                }
            }
            else
            {
                _logger.Error("Failed to load controller application; could not find file {0}", filename);
                throw new FileNotFoundException(filename);
            }

            // Initialize GUI
            var mainGui = new BmpUIMain(controllerapplication, "tint1.png");
            mainGui.Initialize();

            // Run
            var sim = new SimpleControllerSim(controllerapplication, 43);
            var host = new SimpleControllerHost(controllermanager, null, 100, 100, true, false);
            mainGui.SimulationChanged += (o, e) =>
            {
                if (e)
                {
                    sim.SimulationInit(controllerapplication.Clock.CurrentTime);
                    host.Simulator = sim;
                }
                else
                {
                    host.Simulator = null;
                }
            };
            mainGui.SpeedChanged += (o, e) =>
            {
                switch (e)
                {
                    case 1:
                        host.StepDelaySize = 100;
                        host.StepSize = 100;
                        host.StepDelay = true;
                        break;
                    case 2:
                        host.StepDelaySize = 50;
                        host.StepSize = 100;
                        host.StepDelay = true;
                        break;
                    case 3:
                        host.StepDelaySize = 10;
                        host.StepSize = 100;
                        host.StepDelay = true;
                        break;
                    case 4:
                        host.StepDelaySize = 5;
                        host.StepSize = 100;
                        host.StepDelay = true;
                        break;
                    case 5:
                        host.StepSize = 100;
                        host.StepDelay = false;
                        break;
                }
            };
            mainGui.HaltedChanged += (o, e) =>
            {
                host.HaltController(e);
            };
            mainGui.StepRequested += (o, e) =>
            {
                if (!host.Running)
                {
                    host.TakeSingleStep();
                }
            };
            mainGui.Closed += (sender, eventArgs) =>
            {
                host.StopController();
            };
            host.StepTaken += (sender, eventArgs) =>
            {
                mainGui.ControllerInfo = controllerapplication.BlockStructure.CurrentBlock.Name;
            };
            host.StartController();
            mainGui.StartUI();
        }
    }
}
