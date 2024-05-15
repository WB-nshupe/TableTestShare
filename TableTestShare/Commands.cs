using Autodesk.AutoCAD.Runtime;

namespace TableTest.Commands
{
    public class Commands
    {

        #region Schedules

        [CommandMethod("AddTable")]
        public void AddTable()
        {
            LoopTools.LoopSchedule.Add();
        }

        #endregion


    }
}
