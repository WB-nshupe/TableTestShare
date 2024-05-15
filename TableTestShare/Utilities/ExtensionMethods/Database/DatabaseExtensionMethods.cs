using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using TableTest.Utilities;
using TableTest.Utilities.ExtensionMethods;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace TableTest
{
    public static partial class DatabaseExtensionMethods
    {

        public static void OpenTransaction(this Database database, Action<Transaction> action, [CallerMemberName] string callerName = "")
        {
            using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
            {
                try
                {
                    action(tr);
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(OpenTransaction)}: {ex.Message}.\nCheck {callerName}");
                }
            }
        }

        public static void StartTransaction(this Database database, Action<Transaction> action, [CallerMemberName] string callerName = "")
        {
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                try
                {
                    action(tr);
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(StartTransaction)}: {ex.Message}.\nCheck {callerName}");
                }
            }
        }


        public static void InModelSpace(this Database database,
            Action<BlockTableRecord, Transaction> action,
            Transaction tran = null,
            OpenMode mode = OpenMode.ForRead,
            [CallerMemberName] string callerName = "")
        {
            database.BlockTableId.Get<BlockTable>((bt, trans) =>
            {
                bt[BlockTableRecord.ModelSpace].Get<BlockTableRecord>(btr =>
                {
                    action(btr, trans);

                }, tr: trans, mode: mode, callerName: callerName);

            }, tranHolder: tran, mode: OpenMode.ForRead, callerName: callerName);
        }

        public static void ForEachInModelSpace<T>(this Database database,
            Action<T> action,
            Transaction tr,
            [CallerMemberName] string callerName = "") where T : Entity
        {
            database.InModelSpace((modelSpace, tran) =>
            {
                foreach (ObjectId id in modelSpace)
                {
                    if (!id.IsOfType<T>()) continue;

                    id.Get<T>(action, tr: tr);
                }

            }, tran: tr);
        }

    }

    public class SpaceSwitcher : IDisposable
    {
        private enum CurrentSpace
        {
            MODELSPACE = 0,
            PAPERSPACE = 1,
            VIEWPORT = 2
        }

        private CurrentSpace _current;
        private string _returnLayout = string.Empty;
        
        public SpaceSwitcher()
        {
            try
            {
                //Determine what space the user is in
                _current = GetCurrentSpace();

                switch (_current)
                {
                    case CurrentSpace.PAPERSPACE:
                        Active.Editor.WriteMessage("\nUsing Viewport...");
                        Active.Editor.SwitchToModelSpace();
                        break;
                }
            }
            catch (Exception e)
            {
                if (e.ErrorStatus == ErrorStatus.CannotChangeActiveViewport)
                {
                    LayoutManager lm = LayoutManager.Current;
                    _returnLayout = lm.CurrentLayout;
                    lm.CurrentLayout = "Model";
                    return;
                }
                
                Active.WriteMessage("\nCouldn't change space " + e.Message);
            }
        }

        public void Dispose()
        {
            var current = GetCurrentSpace();

            switch (current)
            {
                case CurrentSpace.VIEWPORT when _current.Equals(CurrentSpace.PAPERSPACE):
                    Active.Editor.WriteMessage("\nSwitching to paper...");
                    Active.Editor.SwitchToPaperSpace();
                    break;
                case CurrentSpace.MODELSPACE when !string.IsNullOrEmpty(_returnLayout):
                {
                    LayoutManager lm = LayoutManager.Current;
                    lm.CurrentLayout = _returnLayout;
                    break;
                }
            }
        }

        private static CurrentSpace GetCurrentSpace()
        {
            // Get the current values of CVPORT and TILEMODE
            object cvport = Application.GetSystemVariable("CVPORT");//C(urrent)V(iew)PORT
            object tilemode = Application.GetSystemVariable("TILEMODE");

            switch (Convert.ToInt16(tilemode))
            {
                case 1:
                    return CurrentSpace.MODELSPACE;//The model layout is active

                case 0 when Convert.ToInt16(cvport) > 1:
                    return CurrentSpace.VIEWPORT;//floating modelspace

                default:
                    return CurrentSpace.PAPERSPACE;//paperspace
            }
        }
    }
}
