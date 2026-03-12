using CodingConnected.TLCProF.Helpers;
using CodingConnected.TLCProF.Hosting;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Simulation;
using NLog;
using System.Text;

Logger logger = LogManager.GetCurrentClassLogger();
ControllerModel controllerModel;
ControllerManager controllerManager;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#region Command line args

var cmdArgs = Environment.GetCommandLineArgs();
if (cmdArgs.Any(x => x.StartsWith("-help")))
{
    Console.WriteLine("TLC-PROF HEADLESS CONTROLLER >>> HELP !");
    Console.WriteLine("Command line args:");
    Console.WriteLine("  -xml=\"tlc1.xml\" > set xml configuration");
    Console.WriteLine("  -streaming=8001 > set port for streaming (override xml setting)");
    return;
}


var streamingPort = -1;
var xmlFile = "123456_tlcprof.xml";
if (cmdArgs.Any(x => x.StartsWith("-streaming=")))
{
    streamingPort = int.Parse(cmdArgs.First(x => x.StartsWith("-streaming=")).Replace("-streaming=", ""));
}
if (cmdArgs.Any(x => x.StartsWith("-xml=")))
{
    xmlFile = cmdArgs.First(x => x.StartsWith("-xml=")).Replace("-xml=", "");
}

#endregion // Command line args

#region Loading data

logger.Info("BOOTING UP TLC-PROF");

// Read controller application data from XML
var ser = new TLCPROFSerializer();
var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xmlFile);
if (File.Exists(filename))
{
    try
    {
        controllerModel = ser.DeserializeController(filename);
        controllerManager = new ControllerManager(controllerModel);
    }
    catch (Exception e)
    {
        logger.Error(e, "Failed to load controller application from file {0}", filename);
        throw;
    }
}
else
{
    logger.Error("Failed to load controller application; could not find file {0}", filename);
    throw new FileNotFoundException(filename);
}

if (streamingPort > 0) controllerModel.Data.StreamingVlogPort = streamingPort;

#endregion // Loading data

logger.Info($"Loaded controller data from \"{xmlFile}\"");
logger.Info("Controller name: " + (controllerModel.Data.Name ?? "UNDEFINED"));
logger.Info("VLOG filebased is: " + (controllerModel.Data.EnableFileLogging ? "ON" : "OFF"));
logger.Info("VLOG streaming is: " + (controllerModel.Data.EnableStreamingLogging ? "ON" : "OFF"));
logger.Info("VLOG streaming port: " + controllerModel.Data.StreamingVlogPort);

// Run
var sim = new SimpleControllerSim(controllerModel, 43);
sim.SimulationInit(controllerModel.Clock.CurrentTime);
var host = new SimpleControllerHost(controllerManager, sim, 100, 100, stepDelay: true, realTime: true);

host.VlogLogger.MessageBroadcast += VlogLoggerMessageBroadcast;
host.StartController();
host.VlogLogger.InitVLOG(controllerModel);

int k = 0;
var streaming = true;
while (k != 'q') 
{
    var key = Console.ReadKey();
    Console.WriteLine();
    k = key.KeyChar;
    if (k == 's')
    {
        if (streaming) host.VlogLogger.MessageBroadcast -= VlogLoggerMessageBroadcast;
        else host.VlogLogger.MessageBroadcast += VlogLoggerMessageBroadcast;
        streaming = !streaming;
        logger.Info($"Streaming to console is: {(streaming ? "ON" : "OFF")}");
    }
    else if (k == 'q')
    {
        logger.Info("STOPPING TLC-PROF");
    }
    else if (k != '\r' && k != '\n')
    {
        logger.Warn($"Unknown command: {k}");
    }
}

return;

void VlogLoggerMessageBroadcast(object? sender, string e)
{
    Console.Write(e);
}
