# GSD Unit Tests

## Unit Test Projects

### GSD.UnitTests

* Targets .NET Core
* Contains all unit tests that are .NET Standard compliant

### GSD.UnitTests.Windows

* Targets .NET Framework
* Contains all unit tests that depend on .NET Framework assemblies
* Has links (in the `NetCore` folder) to all of the unit tests in GSD.UnitTests 

GSD.UnitTests.Windows links in all of the tests from GSD.UnitTests to ensure that they pass on both the .NET Core and .Net Framework platforms.

## Running Unit Tests

**Option 1: `Scripts\RunUnitTests.bat`**

`RunUnitTests.bat` will run both GSD.UnitTests and GSD.UnitTests.Windows

**Option 2: Run individual projects from Visual Studio**

GSD.UnitTests and GSD.UnitTests.Windows can both be run from Visual Studio.  Simply set either as the StartUp project and run them from the IDE.

## Adding New Tests

### GSD.UnitTests or GSD.UnitTests.Windows?

Whenever possible new unit tests should be added to GSD.UnitTests. If the new tests are for a .NET Framework assembly (e.g. `GSD.Platform.Windows`) 
then they will need to be added to GSD.UnitTests.Windows.

### Adding New Test Files

When adding new test files, keep the following in mind:

* New test files that are added to GSD.UnitTests will not appear in the `NetCore` folder of GSD.UnitTests.Windows until the GSD solution is reloaded.
* New test files that are meant to be run on both .NET platforms should be added to the **GSD.UnitTests** project.
