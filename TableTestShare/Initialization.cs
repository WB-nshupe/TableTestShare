using Autodesk.AutoCAD.Runtime;
using TableTest.Utilities;
using TableTest.Commands;

//GUID = 75E2584B-8D04-439f-9AA7-E831580AC329

[assembly: ExtensionApplication(typeof(TableTest.TableTestInitializer))]
[assembly: CommandClass(typeof(Commands))]

namespace TableTest
{
    public class TableTestInitializer : IExtensionApplication
    { 
        

        public void Initialize()
        {
            Active.WriteMessage("\nInitialized Test.");

            

        }
        
        public void Terminate()
        {

        }

    }

}

