using Autodesk.AutoCAD.DatabaseServices;
using System.Runtime.CompilerServices;
using System;

namespace TableTest.Utilities.ExtensionMethods
{
    public static partial class ObjectIdMethods
    {
        public static void Get<T>(this ObjectId id,
            Action<T> action,
            Transaction tr = null,
            OpenMode mode = OpenMode.ForRead,
            [CallerMemberName] string callerName = "") where T : DBObject
        {
            if (id == ObjectId.Null) return;
            if (tr == null)
            {
                using (tr = id.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        Get<T>(id,
                            action: (o, transaction) => action(o),
                            tranHolder: tr,
                            mode: mode,
                            callerName: callerName);

                        tr.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Active.WriteMessage($"\nError in {nameof(Get)} check {nameof(callerName)} : {ex.Message}");
                        tr.Abort();
                    }
                }
            }
            else
            {
                Get<T>(id,
                    action: (o, transaction) => action(o),
                    tranHolder: tr,
                    mode: mode,
                    callerName: callerName);
            }

            
        }

        public static void Get<T>(this ObjectId id,
            Action<T, Transaction> action,
            Transaction tranHolder = null,
            OpenMode mode = OpenMode.ForRead,
            [CallerMemberName] string callerName = "") where T : DBObject
        {
            if (id == ObjectId.Null) return;
            if (tranHolder == null)
            {
                using (tranHolder = id.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        using (T item = tranHolder.GetObject(id, mode, false, true) as T)
                        {
                            if (item == null) return;
                            action(item, tranHolder);
                        }

                        tranHolder.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Active.WriteMessage($"\nError in {nameof(Get)} check {callerName} : {ex.Message}");
                        tranHolder.Abort();
                    }
                }
            }
            else
            {
                using (T item = tranHolder.GetObject(id, mode, false, true) as T)
                {
                    if (item == null) return;
                    action(item, tranHolder);
                }
            }
        }
    }
}