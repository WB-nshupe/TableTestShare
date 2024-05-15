using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using TableTest.Utilities;

namespace TableTest
{
    public partial class DatabaseExtensionMethods
    {
        public static void UnlockTurnOnThawAllLayers(this Database database)
        {
            using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
            using (LayerTable layerTable = tr.GetObject(database.LayerTableId, OpenMode.ForRead, false, true) as LayerTable)
            {
                try
                {
                    if (layerTable == null) return;
                    foreach (ObjectId id in layerTable)
                    {
                        using (LayerTableRecord ltr =
                               tr.GetObject(id, OpenMode.ForWrite, false, true) as LayerTableRecord)
                        {
                            if (ltr.IsOff) ltr.IsOff = false;
                            if (ltr.IsLocked) ltr.IsLocked = false;
                            if (ltr.IsFrozen) ltr.IsFrozen = false;
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(UnlockTurnOnThawAllLayers)}: {ex.Message}");
                }
            }
        }
        public static Dictionary<string, Color> GetLayerColors(this Database database)
        {
            var dict = new Dictionary<string, Color>();

            using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
            using (LayerTable layerTable = tr.GetObject(database.LayerTableId, OpenMode.ForRead, false, true) as LayerTable)
            {
                try
                {
                    if (layerTable == null) return new Dictionary<string, Color>();
                    foreach (ObjectId id in layerTable)
                    {
                        using (LayerTableRecord ltr =
                               tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            dict.Add(ltr.Name, ltr.Color);
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(GetLayerColors)}: {ex.Message}");
                }
            }

            return dict;
        }
        public static List<string> GetLayerNames(this Database database)
        {
            var dict = new List<string>();

            using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
            using (LayerTable layerTable = tr.GetObject(database.LayerTableId, OpenMode.ForRead, false, true) as LayerTable)
            {
                try
                {
                    if (layerTable == null) return new List<string>();
                    foreach (ObjectId id in layerTable)
                    {
                        using (LayerTableRecord ltr =
                               tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            dict.Add(ltr.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(GetLayerColors)}: {ex.Message}");
                }
            }

            return dict;
        }
        public static bool LayerExists(this Database database, string layerName)
        {
            using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
            using (LayerTable layerTable = tr.GetObject(database.LayerTableId, OpenMode.ForRead, false, true) as LayerTable)
            {
                try
                {
                    if (layerTable == null) return false;
                    foreach (ObjectId id in layerTable)
                    {
                        using (LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            if (ltr.Name.Equals(layerName)) return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError occured in {nameof(LayerExists)}: {ex.Message}");
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the layer existed or was successfully created.
        /// </summary>
        /// <param name="layername">Name or layer to find or create</param>
        /// <param name="layerTableRecordId">ObjectId for found or created layer</param>
        /// <param name="color">Default is 7</param>
        /// <param name="lWeight">Default is LineWeight000</param>
        /// <returns></returns>
        public static bool TryGetOrCreateLayer(
            this Database db,
            string layername,
            out ObjectId layerTableRecordId,
            short color = 7,
            LineWeight lWeight = LineWeight.LineWeight000)
        {
            using (DocumentLock dimlock = Active.Document.LockDocument())
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                try
                {
                    using (LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)
                    {
                        if (lt.Has(layername))
                        {
                            layerTableRecordId = lt[layername];
                            return true;
                        }

                        lt.UpgradeOpen();
                        LayerTableRecord ltr = new LayerTableRecord
                        {
                            Name = layername,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, color),
                            LineWeight = lWeight
                        };
                        layerTableRecordId = lt.Add(ltr);

                        tr.AddNewlyCreatedDBObject(ltr, true);
                        tr.Commit();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage("\nProblem occured because " + ex.Message);
                    layerTableRecordId = ObjectId.Null;
                    return false;
                }
            }
        }

        public static bool TryGetLayer(
            this Database db,
            string layername,
            out ObjectId layerTableRecordId)
        {
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                try
                {
                    using (LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)
                    {
                        if (lt.Has(layername))
                        {
                            layerTableRecordId = lt[layername];
                            return true;
                        }
                        layerTableRecordId = ObjectId.Null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    tr.Abort();
                    Active.WriteMessage("\nProblem occured because " + ex.Message);
                    layerTableRecordId = ObjectId.Null;
                    return false;
                }
            }
        }
    }
}