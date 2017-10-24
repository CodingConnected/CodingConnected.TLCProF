# TLCProF
TLCProF is a new, open source framework for programming vehicle dependent traffic light controller software. The abbreviation stands for: Traffic Light Controller Programming Framework. The project is currently in beta phase, and both its name and code base may undergo considerable changes in the near future.

> Please note: the project is currently in alfo state, and in active development. It's API and classes are likely to change occasianally until the project reaches a stable state.

The framework has been written in C# (minimum version: 6.0) using the .NET Framework (minimum version: 4.5). It is compatible with Mono (tested with version 3.2.8), and can thus be used under Linux.
> Note (10-09-2017): the current version of the UI has known issues when ran with Mono. A controller can however be ran on the command line without trouble.

Contents of the README:

- [Using the library](#Using the library)
	- [Programming a controller](#Programming a controller)
	- [Settings and state: the ControllerModel class](#)
		- [Manually coding a controller](#)
		- [Using XML to create a controller](#)
			- [Using TLCGen to generate an XML](#)
	- [Running the control process: the ControllerManager and SimpleControllerHost classes](#)
		- [Hosting the controller](#)
- [Building from source](#)
	- [Retrieve the sources](#)
	- [Restore dependencies: Paket](#)
	- [Building](#)

## Using the library
TLCProF consists of three parts:
- CodingConnected.TLCProF: contains the actual traffic light controller algorithms. It also provides some common functionality, such as a simple "controller host" that allows running a controller process in a loop, a command parser class and a xml deserialization class.
- CodingConnected.TLCProF.Sim: a relatively straightforward simulation module, allowing a semi-random simulation to be ran on a TLCProF controller.
- CodingConnected.TLCProF.BmpUI: provides a cross platform user interface, using a clickable bitmap. This is meant for testing purposes, to functionaly test and check a controller.

The example controller in Visual project "CodingConnected.TLCProF.ExampleController" provides a simple example that combines all three parts to create a controller with a user interface that allows testing its functionality.

Make sure to add references to the libraries that are used in a project.

### Programming a controller
Semantically, a controller consists of two parts: its settings and state on the one hand, its logic and algorithms on the other. In TLCProF, roughly the following design decisions have been made:
- The settings and state reside in a single object of type `ControllerModel`. This object and objects that are member of it do have logic, but that logic strictly (that is: as strictly as possible) partains to the object in question itself. For example, a `SignalGroupModel` exposes a method `HandleStateRequests()`, that causes it to determine its own new inner state based on its settings and requests it has received. This method is called by the `ControllerManager` (see below).
- Logic that operates on controller objects resides in manager classes, that inject their functinality into the main manager: an instance of `ControllerManager`. For example, a manager causes a request for green to be set on a signalgroup based on the state of its detection. this could be done inside the `SignalGroupModel` object, 

In practice this distinction is now always as strict as is described here. However, this design facilitates a modular setup, allowing easy addition without the need to change existing functionality. Also, a certain functionality can be programmed in a single place, which makes code changes trivial, while maintaining a good overview. Finally, unit testing (yet absent) of functionality is made possible.

### Settings and state: the `ControllerModel` class
To use the library to program a controller, an object of type `ControllerModel` should be created first. This object holds all relevant data and settings, and represents the state of the controlling process at a given moment in time. This can either be done manually, or by deserializing from XML. Below are two basic exmaples; please refer to the user manual and framework specification for more detailed descriptions of the various settings and functionalities TLCProF provides.

#### Manually coding a controller
First, instantiate a new controller model:
```[C#]
var controller = new ControllerModel();
```
Next, create some signalgroups and add them to the controller:
```[C#]
var sg02 = new SignalGroupModel("02", greengar: 40, greenfix: 50, greenex: 250, amber:
30, redgar: 10, redfix: 20, headmax: 100);
Controller.SignalGroups.Add(sg02);
```
This example uses named arguments to clarify the code, which of course is not mandatory. Note that not all settings for a signalgroup are set via its constructor. For example, the properties `FixedRequest` and `FixedRequestDelay` can be used to cause a signal group to always have a (delayed or direct) request. A typical signalgroup will have one or more detectors that cause a request for green:
```[C#]
sg02.Detectors.Add(new DetectorModel("021a", request:
DetectorRequestTypeEnum.RedNonGuaranteed, extend: DetectorExtendingTypeEnum.HeadMax,
occupied: 10, gap: 30, errorhi: 20, errorlo: 360));
```
Add intergreen times to the signalgroups to build the conflict matrix. Note that the resulting matrix should be symmetrivc, otherwise an exception will be raised.
```[C#]
sg02.InterGreenTimes.Add(new InterGreenTimeModel("02", "05", 30));
```
The current version of TLCProF provides a block structure as a mechanism to take runtime decisions about order of realistions of signalgroups. To build this structure:
```[C#]
var block1 = new BlockModel("B1");
block1.AddSignalGroup("02");
controller.BlockStructure.Blocks.Add(block1);
Controller.BlockStructure.WaitingBlockName = "B1";
```
The controller object now has a very basic, but complete and working setup.

#### Using XML to create a controller
Instead of coding a `ControllerModel` object by hand, it can be instantiated by deserializing from XML. The example controller uses this mecahnism. For example:
```[C#]
var ser = new TLCPROFSerializer();
var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tint1_tlcprof.xml");
controller = ser.DeserializeController(filename);
```
Of course, some exception handling should be in place here to check existence and integrity of the file.

> Note: for Mono compatibility, the `TLCPROFSerializer` class internally uses `DataContractSerializer`. This means the XML should match *exactly* with the C# classes. Even a very minor deviation will cause an exeption to be raised. Therefor, it is advised not to write an XML by hand, but instead use a generator (such as [this one](https://www.codingconnected.eu/software/tlcgen/)) to compose the XML.

##### Using TLCGen to generate an XML
To generate a `ControllerModel` XML file using the application TLCGen, do the following:
- Download TLCGen from [here](https://www.codingconnected.eu/software/tlcgen/)
- Build the project CodingConnected.TLCProF.TLCGenGen (see below for building from source)
- Copy the resulting file "CodingConnected.TLCProF.TLCGenGen.dll" to the Plugins folder in the folder where TLCGen.exe is located
- Copy file "CodingConnected.TLCProF.dll" to the folder where TLCGen.exe is located
- You can now select TLCProF as a generator from within TLCGen, and generate TLCProF XML files with the application

### Running the control process: the `ControllerManager` and `SimpleControllerHost` classes
The `ControllerModel` does nothing by itself, but instead is "managed" by a class that will cause state changes based on input state, and call relevant methods on objects inside the controller when a control step is taken. To instantiate the manager:
```[C#]
ControllerManager Manager = new ControllerManager(Controller);
```
The manager exposes a method `ExecuteStep()` that will "move" the controlling proces forward a giving amount of miliseconds. This method is typically called in a loop, for example by using an instance of `SimpleControllerHost` as is descibed next.

#### Hosting the controller
the controller object cannot "run itself", but instead needs to be hosted. TLCProF provides a relatively simple class to help with this:
```[C#]
var host = new SimpleControllerHost(controllermanager, null, 100, 100, true, false);
host.StartController();
var s = "";
while (s != "exit") { s = Console.ReadLine(); }
```
Please refer to the TLCProF specification for more details about this class and the meaning of it constructor arguments.

## Building from source
Because the framework uses C# 6.0 features, it can be built only with a compiler that supports those, such as Microsoft Visual Studio 2017. The project has been created using Visual 2017 Community Edition. Compiling with Mono may work, but is untested (an unsupported). However, a binary compiled with Visual Studio can be ran with Mono under Linux.

### Retrieve the sources
The sources can be downloaded as a zip, but preferably are downloaded via git from within Visual Studio, so staying up to date is easier. To do this, click menu Team > Manage connections. In the Manage Connections dropdown menu, select Connect to Project. Sign in with your Visual Studio account, and close the CodingConnected.TLCProF project. This will create a local clone of the repository, which can be easily kept up to date with remote code changes.

### Restore dependencies: Paket
The project uses [Paket](https://fsprojects.github.io/Paket/) as its package manager. To build from source, we first need to restore any dependencies.
- Use the menu Tools > Nuget Packet Manager > Packet Manager Console, to open the console.
- Type in the following command (without the quotes): ".paket\paket.bootstrapper.exe". This will download the latest version of Paket.
- Use paket to restore any dependencies: ".paket/paket.exe update". Reload the solution if a Visual Studio prompt appears.

### Building
Now that dependencies have been restored, we can build the solution.
- Rebuild the solution via menu Build > Rebuild Solution. The build should succeed without errors.
- To check that everything worked, run the example controller, by setting the project "CodingConnected.TLCProF.ExampleController" as Startup Project (in the context menu that appears after a right mouseclick on the project). Now use the menu Debug > Start Without Debugging to start the project (or use the shortcut key Ctrl-F5)


