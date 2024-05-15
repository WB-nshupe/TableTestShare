using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace TableTest.Utilities.ExtensionMethods
{
    public static class EditorMethods
    {
        static string names;

        public static PromptSelectionResult GetSelectionBlock(this Editor ed, params string[] blockNames)
        {
            names = string.Join(",", blockNames);
            var filter = new SelectionFilter(
                new[] {
                    new TypedValue(0, "INSERT")//,
                    //new TypedValue(2, "`*U*," + names)
                });
            ed.SelectionAdded += OnSelectionAdded;
            var result = ed.GetSelection(filter);
            ed.SelectionAdded -= OnSelectionAdded;
            return result;
        }

        static void OnSelectionAdded(object sender, SelectionAddedEventArgs e)
        {
            var ids = e.AddedObjects.GetObjectIds();
            if (ids.Length == 0) return;
            using (var tr = ids[0].Database.TransactionManager.StartTransaction())
            {
                for (int i = ids.Length-1; i >= 0; i--)
                {
                    ids[i].Get<BlockReference>(br =>
                    {
                        br.DynamicBlockTableRecord.Get<BlockTableRecord>(btr =>
                        {
                            if (!Autodesk.AutoCAD.Internal.Utils.WcMatchEx(btr.Name, names, true))
                                e.Remove(i);


                        }, tr);
                    }, tr);
                    
                }
                tr.Commit();
            }
        }

        #region Retrievers

        public static bool GetUserPoint(this Editor ed, string prompt, out Point3d id, bool allowNone = false, Point3d basePoint = new Point3d())
        {
            id = Point3d.Origin;

            PromptPointOptions opts = new PromptPointOptions(prompt)
            {
                AllowNone = allowNone
            };

            if (basePoint != Point3d.Origin)
            {
                opts.BasePoint = basePoint;
                opts.UseBasePoint = true;
                opts.UseDashedLine = true;
            }

            PromptPointResult ppr = ed.GetPoint(opts);

            if (ppr.Status != PromptStatus.OK) return false;

            id = ppr.Value;

            return true;
        }

        #endregion

    }
}
