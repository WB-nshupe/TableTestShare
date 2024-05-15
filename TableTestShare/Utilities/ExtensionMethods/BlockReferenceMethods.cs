using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace WarmBoardTools.Utilities.ExtensionMethods
{
    public static class BlockReferenceMethods
    {
        #region Getters

        //TODO might be able to condense the get/set functions for attributes that handle variable and constant att
        //if it can work on constant att, should work on variable ones.
        public static string GetAttributeValue(this BlockReference block, string attributeName, Transaction tr)
        {
            string returnValue = string.Empty;
            
            block.AttributeCollection.ForEach(att =>
            {
                if (!attributeName.Equals(att.Tag)) return;
                returnValue = att.TextString;

            }, tranHolder: tr);

            return returnValue;
        }

        public static Dictionary<string, string> GetAttributeValues(this BlockReference block, Transaction tr, List<string> attributeNames)
        {
            Dictionary<string, string> returnValue = new Dictionary<string, string>();

            block.AttributeCollection.ForEach(myAttRef =>
            {
                if (attributeNames.Contains(myAttRef.Tag))
                {
                    returnValue.Add(myAttRef.Tag, myAttRef.TextString);
                }
            }, tranHolder: tr);

            return returnValue;
        }

        public static Dictionary<string, string> GetConstantAttributeValues(this BlockReference block, Transaction tr, List<string> values)
        {
            Dictionary<string, string> returnDic = new Dictionary<string, string>();

            block.BlockTableRecord.Get<BlockTableRecord>(btr =>
            {
                if (!btr.HasAttributeDefinitions) return;

                btr.ForEach<AttributeDefinition>(attdef =>
                {
                    if (!values.Contains(attdef.Tag.ToUpper())) return;
                    try
                    {
                        returnDic.Add(attdef.Tag.ToUpper(), attdef.TextString);
                    }
                    catch
                    {
                    }


                }, tr: tr);

            }, tr: tr);

            return returnDic;
        }
        
        public static Point3d GetVisibleBlockPointPropertyValue(this BlockReference block, string propertyName)
        {
            double x = 0;
            double y = 0;

            foreach (DynamicBlockReferenceProperty prop in block.DynamicBlockReferencePropertyCollection)
            {
                if (!prop.VisibleInCurrentVisibilityState) continue;

                string propName = prop.PropertyName;
                if (propName.Contains(propertyName))
                {
                    switch (propName[propName.Length - 1])
                    {
                        case 'X':
                            double.TryParse(prop.Value.ToString(), out x);
                            break;
                        case 'Y':
                            double.TryParse(prop.Value.ToString(), out y);
                            break;
                    }
                }
            }

            if (x == 0 && y == 0) return Point3d.Origin;

            return new Point3d(x, y, 0);
        }

        /// <summary>
        /// All points are transformed to wcs
        /// </summary>
        /// <param name="block"></param>
        /// <param name="searchString"></param>
        /// <param name="returnValue"></param>
        public static void GetAllVisibleBlockPointPropertyValues(this BlockReference block, string searchString, SortedDictionary<string, Point3d> returnValue)
        {
            for (int i = 0; i < block.DynamicBlockReferencePropertyCollection.Count; i++)
            {
                DynamicBlockReferenceProperty prop = block.DynamicBlockReferencePropertyCollection[i];
                if (!prop.VisibleInCurrentVisibilityState) continue;

                DynamicBlockReferenceProperty prop2 = block.DynamicBlockReferencePropertyCollection[i + 1];
                if (!prop2.VisibleInCurrentVisibilityState) continue;

                if (!prop.PropertyName.Contains(searchString) || !prop2.PropertyName.Contains(searchString)) continue;


                var regex = new Regex($@"{searchString}(.*?) (X|Y)");
                string propName1 = regex.Replace(prop.PropertyName, "$1");
                string propName2 = regex.Replace(prop2.PropertyName, "$1");

                if (propName1 != propName2) continue;

                double.TryParse(prop.Value.ToString(), out double x);
                double.TryParse(prop2.Value.ToString(), out double y);

                Point3d point = new Point3d(x, y, 0).TransformBy(block.BlockTransform);

                if (returnValue.ContainsValue(point)) continue;
                if (returnValue.ContainsKey(propName1)) propName1 = propName1 + "a";

                returnValue.Add(propName1, point);
                i++;

            }
        }

        public static string GetBlockPropertyValue(this BlockReference block, string propertyName)
        {
            return !block.IsDynamicBlock ? null : (
                from DynamicBlockReferenceProperty prop
                    in block.DynamicBlockReferencePropertyCollection
                where prop.PropertyName.Equals(propertyName)
                select prop.Value.ToString()).FirstOrDefault();
        }

        public static string GetBlockParentName(this BlockReference block, Transaction tr)
        {
            if (block == null) return null;

            using (BlockTableRecord btr = tr.GetObject(block.OwnerId, OpenMode.ForRead, false, true) as BlockTableRecord)
            using (Layout layout = tr.GetObject(btr.LayoutId, OpenMode.ForRead, true) as Layout)
            {
                return layout != null ? layout.LayoutName : null; //owner is layout
            }
        }

        #endregion



        #region Setters

        public static void SetAttributeReferences(this BlockReference block, Dictionary<string, string> valuePairs, Transaction tr)
        {
            block.AttributeCollection.ForEach(myAttRef =>
            {
                if (!valuePairs.ContainsKey(myAttRef.Tag)) return;
                myAttRef.UpgradeOpen();
                myAttRef.TextString = valuePairs[myAttRef.Tag];

            }, tranHolder: tr);
        }

        public static void SetAttributeReference(this BlockReference block, string attributeName, string value, Transaction tr)
        {
            block.AttributeCollection.ForEach(myAttRef =>
            {
                if (!attributeName.Equals(myAttRef.Tag)) return;
                myAttRef.UpgradeOpen();
                myAttRef.TextString = value;

            }, tranHolder: tr);
        }

        public static bool SetDynamicBlockPropertyValue(this BlockReference block, string propertyName, object value)
        {
            if (!block.IsDynamicBlock) return false;

            foreach (DynamicBlockReferenceProperty prop in block.DynamicBlockReferencePropertyCollection)
            {
                if (!prop.PropertyName.Equals(propertyName)) continue;
                prop.Value = value;
                return true;
            }

            return false;
        }

        public static void SetPointProperty(this BlockReference block, string propertyName, Point2d pt)
        {
            try
            {
                if (!block.IsDynamicBlock) return;

                foreach (DynamicBlockReferenceProperty prop in block.DynamicBlockReferencePropertyCollection)
                {
                    string propName = prop.PropertyName.ToUpper();
                    if (propName.Contains(propertyName.ToUpper()) && prop.PropertyTypeCode.Equals(1))
                    {
                        prop.Value = propName[propName.Length - 1].Equals('X') ? pt.X : pt.Y;
                    }
                }
            }
            catch (System.Exception e)
            {
                Active.WriteMessage($"\nError in {nameof(SetPointProperty)}: {e.Message}\nCould not write {pt} to {propertyName}");
            }
        }

        public static void SetAttributeDefinition(this BlockReference block, string attName, string value)
        {
            using (Transaction tr = Active.Database.TransactionManager.StartOpenCloseTransaction())
            using(BlockTableRecord btr = tr.GetObject(block.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord)
            {
                try
                {
                    RXClass attDefClass = RXObject.GetClass(typeof(AttributeDefinition));

                    if (btr != null && !btr.HasAttributeDefinitions) return;

                    foreach (ObjectId arId in btr)
                    {
                        if (arId.ObjectClass != attDefClass) continue;

                        using (AttributeDefinition attributeDefinition = tr.GetObject(arId, OpenMode.ForRead, false, true) as AttributeDefinition)
                        {
                            if (attributeDefinition == null) continue;

                            if (!string.Equals(attributeDefinition.Tag, attName,
                                    StringComparison.CurrentCultureIgnoreCase)) continue;
                            try
                            {
                                attributeDefinition.UpgradeOpen();
                                attributeDefinition.TextString = value;
                                attributeDefinition.DowngradeOpen();
                            }
                            catch
                            {
                            }
                        }
                    }

                    tr.Commit();
                }
                catch (Exception e)
                {
                    tr.Abort();
                    Active.WriteMessage($"\nError in {nameof(SetAttributeDefinition)}: {e.Message}");
                }
            }
        }

        public static void SetAttributeDefinitions(this BlockReference block, Dictionary<string, string> valuePairs, Transaction tr)
        {
            block.BlockTableRecord.Get<BlockTableRecord>(btr =>
            {
                if(!btr.HasAttributeDefinitions) return;

                foreach (ObjectId id in btr)
                {
                    if(!id.IsOfType<AttributeDefinition>()) continue;
                    
                    id.Get<AttributeDefinition>(attDef =>
                    {

                        if (!valuePairs.ContainsKey(attDef.Tag.ToUpper())) return;
                        try
                        {
                            attDef.UpgradeOpen();
                            attDef.TextString = valuePairs[attDef.Tag.ToUpper()];
                        }
                        catch
                        {
                        }

                    }, tr: tr);
                }

            }, tr: tr);
        }

        #endregion


        public static bool IsInstanceOf(this BlockReference block, string blockName, Transaction tr, [CallerMemberName] string callerName = "")
        {
            bool answer = false;

            if (block == null) return false;

            block.DynamicBlockTableRecord.Get<BlockTableRecord>(btr =>
            {
                answer = Autodesk.AutoCAD.Internal.Utils.WcMatchEx(btr.Name, blockName, true);

            }, tr, callerName: callerName);

            return answer;
        }

        public static string GetName(this BlockReference block, Transaction tr)
        {
            ObjectId btrId = block.DynamicBlockTableRecord;
            using (BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord)
            {
                return btr.Name;
            }
        }
    }
}
