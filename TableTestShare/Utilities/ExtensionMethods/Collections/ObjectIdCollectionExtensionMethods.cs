using System;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace TableTest.Utilities.ExtensionMethods
{
    public static class ObjectIdCollectionExtensionMethods
    {
        
        public static bool ForEach<T>(this ObjectIdCollection idCollection,
            Action<T> action,
            Transaction tr = null,
            OpenMode mode = OpenMode.ForRead,
            Predicate<T> earlyBreak = null,
            [CallerMemberName] string callerName = "") where T : DBObject
        {
            if(idCollection == null || idCollection.Count < 1) return default;
            if (tr == null)
            {
                using (tr = idCollection[0].Database.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        var answer = ForEach<T>(idCollection, (arg1, transaction) => action(arg1),
                            tranHolder: tr,
                            mode: mode,
                            earlyBreak: earlyBreak,
                            callerName: callerName);

                        tr.Commit();
                        return answer;

                    }
                    catch(Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Active.WriteMessage($"\nError in {nameof(ForEach)}, check {callerName} : {ex.Message}");
                        tr.Abort();
                    }
                }
            }
            else
            {
                return ForEach<T>(idCollection, (arg1, transaction) => action(arg1),
                    tranHolder: tr,
                    mode: mode,
                    earlyBreak: earlyBreak,
                    callerName: callerName);
            }


            return default;
        }

        public static bool ForEach<T>(this ObjectIdCollection idCollection,
            Action<T, Transaction> action,
            Transaction tranHolder = null,
            OpenMode mode = OpenMode.ForRead,
            Predicate<T> earlyBreak = null,
            [CallerMemberName] string callerName = "") where T : DBObject
        {
            if (idCollection == null || idCollection.Count < 1) return default;

            bool completedAll = false;
            bool breakFlag = false;

            if (tranHolder == null)
            {
                using (tranHolder = idCollection[0].Database.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {

                        foreach (ObjectId id in idCollection)
                        {
                            id.Get<T>(
                                action: o =>
                                {
                                    action(o, tranHolder);
                                    if (earlyBreak != null && earlyBreak(o)) breakFlag = true;

                                },
                                tr: tranHolder,
                                mode: mode,
                                callerName: callerName);

                            if (breakFlag) break;
                        }

                        completedAll = true;

                        tranHolder.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Active.WriteMessage($"\nError in {nameof(ForEach)} check {callerName} : {ex.Message}");
                        tranHolder.Abort();
                    }
                }
            }
            else
            {
                foreach (ObjectId id in idCollection)
                {
                    id.Get<T>(
                        action: o =>
                        {
                            action(o, tranHolder);
                            if (earlyBreak != null && earlyBreak(o)) breakFlag = true;

                        },
                        tr: tranHolder,
                        mode: mode,
                        callerName: callerName);

                    if (breakFlag) break;
                }

                completedAll = true;
            }

            return completedAll;

        }
    }
}