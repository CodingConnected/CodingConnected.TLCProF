using CodingConnected.TLCProF.Helpers;
using CodingConnected.TLCProF.Hosting;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Simulation;
using NLog;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Logger _logger = LogManager.GetCurrentClassLogger();

_logger.Info("BOOTING UP TLC-PROF");

var cmdArgs = Environment.GetCommandLineArgs();
var ftpPort = 8021;
if (cmdArgs.Length > 1)
{
    ftpPort = int.Parse(cmdArgs[1]);
}

var ftp = new SharpFtpServer.FtpServer(System.Net.IPAddress.Any, ftpPort);
ftp.Start();

ControllerModel controllerapplication;
ControllerManager controllermanager;

// Read controller application data from XML
var ser = new TLCPROFSerializer();
var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "123456_tlcprof.xml");
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

_logger.Info("Loaded controller data from \"123456_tlcprof.xml\"");
_logger.Info("Controller name: " + (controllerapplication.Data.Name ?? "UNDEFINED"));
_logger.Info("VLOG filebased is: " + (controllerapplication.Data.EnableFileLogging ? "ON" : "OFF"));
_logger.Info("VLOG streaming is: " + (controllerapplication.Data.EnableStreamingLogging ? "ON" : "OFF"));
_logger.Info("VLOG streaming port: " + controllerapplication.Data.StreamingVlogPort);

// Run
var sim = new SimpleControllerSim(controllerapplication, 43);
sim.SimulationInit(controllerapplication.Clock.CurrentTime);
var host = new SimpleControllerHost(controllermanager, sim, 100, 100, true, true);

void VlogLogger_MessageBroadcast(object? sender, string e)
{
    Console.Write(e);
};

host.VlogLogger.MessageBroadcast += VlogLogger_MessageBroadcast;
host.StartController();
host.VlogLogger.InitVLOG(controllerapplication);

int k = 0;
var streaming = true;
while (k != 'q') 
{
    var key = Console.ReadKey();
    Console.WriteLine();
    k = key.KeyChar;
    if (k == 's')
    {
        if (streaming) host.VlogLogger.MessageBroadcast -= VlogLogger_MessageBroadcast;
        else host.VlogLogger.MessageBroadcast += VlogLogger_MessageBroadcast;
        streaming = !streaming;
        _logger.Info($"Streaming to console is: {(streaming ? "ON" : "OFF")}");
    }
    else if (k == 'q')
    {
        _logger.Info($"STOPPING TLC-PROF");
    }
    else if (k != '\r' && k != '\n')
    {
        _logger.Warn($"Unknown command: {k}");
    }
}
