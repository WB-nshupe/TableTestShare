using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using TableTest.Utilities;
using TableTest.Utilities.ExtensionMethods;

namespace TableTest.LoopTools
{
    public class LoopSchedule
    {
        private static readonly List<string> HEADERS = new List<string>
        {
            "ZONE",
            "MANIFOLD",
            "LOOP",
            "LENGTH"
        };

        private const string Title = "LOOP SCHEDULE";
        private const string LayerName = "B_Schedules";


        public static void Add()
        {
            Active.Database.BlockTableId.Get<BlockTable>((bt, tr) =>
            {
                LoopDataCollection loopData = LoopDataCollection.Get(tr);
                if (loopData == null) return;

                if (!(Active.Database.CurrentSpaceId == bt[BlockTableRecord.PaperSpace]))
                {
                    Active.WriteMessage("\n You must be in paperspace to insert the Loop Schedule.");
                    return;
                }

                if(!Active.Editor.GetUserPoint("\nEnter table insertion point: ", id: out Point3d insertPoint)) return;

                Table tb = BuildTable(Title, HEADERS, insertPoint, loopData);
                if (tb == null) return;

                bt[BlockTableRecord.PaperSpace].Get<BlockTableRecord>(btr =>
                {
                    btr.AppendEntity(tb);
                    tr.AddNewlyCreatedDBObject(tb, true);


                }, tr: tr, mode: OpenMode.ForWrite);

            });
        }

        private static Table BuildTable(string title, List<string> headers, Point3d insertPoint, LoopDataCollection tableData)
        {
            /*
                 *Table gets created with default 1 row and 1 column
                 * so when inserting default rows and columns, remove old row and column
                 * which get pushed to the last index
                 */

            Table tb = new Table();

            try
            {
                tb.Position = insertPoint;

                tb.TableStyle = Active.Database.Tablestyle;

                tb.SetSize(tableData.Count + 3, 1);

                tb.SetRowHeight(.5);
                tb.SetColumnWidth(1.5);
                tb.Cells[0, 0].SetValue(title, ParseOption.ParseOptionNone);
                tb.Cells[0, 0].TextHeight = .22;

                //Set the width of the columns
                tb.InsertColumns(0, 1.00, 1);
                tb.InsertColumns(0, 1.625, 1);
                tb.InsertColumns(0, 1.5, 1);

                //for each Header
                int irowcount = 1;
                int icolcount = 0;
                foreach (String header in HEADERS)
                {
                    tb.Cells[irowcount, icolcount].SetValue(header, ParseOption.ParseOptionNone);
                    tb.Cells[irowcount, icolcount].TextHeight = 0.20;
                    icolcount++;
                }

                //Set header row height
                tb.Rows[irowcount].Height = .50;

                //for each Data row
                irowcount = 2;
                foreach (LoopMarkerData loop in tableData)
                {

                    tb.Cells[irowcount, 0].SetValue(loop.ZoneId, ParseOption.ParseOptionNone);
                    tb.Cells[irowcount, 0].TextHeight = 0.20;

                    tb.Cells[irowcount, 1].SetValue(loop.Manifold, ParseOption.ParseOptionNone);
                    tb.Cells[irowcount, 1].TextHeight = 0.20;

                    tb.Cells[irowcount, 2].SetValue(loop.Loop, ParseOption.ParseOptionNone);
                    tb.Cells[irowcount, 2].TextHeight = 0.20;

                    tb.Cells[irowcount, 3].TextString = loop.LengthTotal;
                    tb.Cells[irowcount, 3].TextHeight = 0.20;

                    // Set row height for data
                    tb.Rows[irowcount].Height = .375;

                    //next row
                    irowcount++;
                }

                //Add Totals
                tb.Cells[tb.Rows.Count - 1, 1].SetValue("Total", ParseOption.ParseOptionNone);
                tb.Cells[tb.Rows.Count - 1, 1].TextHeight = 0.25;

                tb.Cells[tb.Rows.Count - 1, 3].Contents.Add();
                //tb.Cells[tb.Rows.Count - 1, 3].Contents[0].Formula = "=Sum(D3:D" + (tb.Rows.Count - 1) + ")";
                tb.Cells[tb.Rows.Count - 1, 3].Contents[0].Formula = $"%<\\AcExpr (Sum(D3:D{(tb.Rows.Count)}))>%";
                //Generate the layout
                tb.GenerateLayout();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                return null;
            }
            return tb;
        }


    }
}
