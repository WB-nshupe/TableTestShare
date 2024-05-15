using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using TableTest.Utilities;
using TableTest.Utilities.ExtensionMethods;

namespace TableTest.LoopTools
{
    public class LoopDataCollection : List<LoopMarkerData>, ICloneable
    {
        public void SortData()
        {
            var sortedList = this.OrderBy(x => x.System, StringComparer.Ordinal)
                .ThenBy(x => ExtractNumber(x.Manifold))
                .ThenBy(x => x.Loop)
                .ToList();

            // Assuming you want to update the original list:
            this.Clear();
            this.AddRange(sortedList);

            int ExtractNumber(string input)
            {
                var match = Regex.Match(input, @"\d+");

                if (match.Success && int.TryParse(match.Value, out int num))
                    return num;

                return 0;
            }
        }
        public static LoopDataCollection Get(Transaction tr)
        {
            LoopDataCollection loopMarkerData = null;

            using (SpaceSwitcher switcher = new SpaceSwitcher())
            {
                var markerSelection = Active.Editor.GetSelectionBlock("Marker");
                loopMarkerData = FillLoopData(markerSelection, tr);
            }
            return loopMarkerData;
        }

        private static LoopDataCollection FillLoopData(PromptSelectionResult results, Transaction tr)
        {
            LoopDataCollection loopMarkerData = new LoopDataCollection();

            if (results.Status != PromptStatus.OK) return loopMarkerData;
            ObjectIdCollection markerIds = new ObjectIdCollection(results.Value.GetObjectIds());

            markerIds.ForEach<BlockReference>((marker, tran) =>
            {
                loopMarkerData.Add(new LoopMarkerData(marker, tran));

            }, tranHolder: tr);

            loopMarkerData.SortData();

            return loopMarkerData;
        }

        public object Clone()
        {
            LoopDataCollection data = new LoopDataCollection();
            this.ForEach(l => data.Add(l.Clone() as LoopMarkerData));
            return data;
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
            catch (Autodesk.AutoCAD.Runtime.Exception e)
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
