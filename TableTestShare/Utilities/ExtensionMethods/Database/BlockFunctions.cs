using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WarmBoardTools.Utilities;
using WarmBoardTools.Utilities.ExtensionMethods;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using CADMasters.AutoCAD.Objects;
using DocumentFormat.OpenXml.Spreadsheet;

namespace WarmBoardTools
{
    public partial class DatabaseExtensionMethods
    {
        private const string OldBlockResourceFile = "C:\\Program Files\\Autodesk\\ApplicationPlugins\\WarmBoardTools.bundle\\Resources\\BlockResourceFile.dwg";

        public static ObjectId Insert(
            this Database database,
            string blockName,
            Point3d insPoint,
            Transaction transaction = null,
            string layerName = "0",
            string filePath = null,
            double scale = 1,
            double rotation = 0,
            ObjectId modelOrPaper = new ObjectId())
        {
            ObjectId objectIdToReturn = ObjectId.Null;

            using (Active.Document.LockDocument())
            {
                if(transaction == null)
                    database.OpenTransaction(tr =>
                    {
                        objectIdToReturn = theAction(tr);
                    });
                else
                    objectIdToReturn = theAction(transaction);
            }

            return objectIdToReturn;

            ObjectId theAction(Transaction tr)
            {
                ObjectId id = FindOrLoadBlock();

                if (id.IsNull) return ObjectId.Null;

                if (!database.LayerExists(layerName))
                {
                    if (ResourceFolder.LayerConverter.TryGetConversion(layerName, out string conversion)) layerName = conversion;
                    else layerName = "0";
                }

                using (BlockTableRecord btr = (BlockTableRecord)tr.GetObject(modelOrPaper, OpenMode.ForWrite))
                using (BlockReference br = new BlockReference((Point3d)insPoint, id))// Insert the block into the drawing.
                {
                    br.SetDatabaseDefaults();
                    br.ScaleFactors = new Scale3d(scale, scale, scale);
                    br.Layer = layerName;
                    br.Rotation = rotation * System.Math.PI / 180;
                    btr.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    using (BlockTableRecord BlkTblRec = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord)
                    {
                        if (BlkTblRec.HasAttributeDefinitions)
                        {
                            foreach (ObjectId objId in BlkTblRec)
                            {
                                AttributeDefinition AttDef = tr.GetObject(objId, OpenMode.ForRead) as AttributeDefinition;
                                if (AttDef != null)
                                {
                                    AttributeReference AttRef = new AttributeReference();
                                    AttRef.SetAttributeFromBlock(AttDef, br.BlockTransform);
                                    br.AttributeCollection.AppendAttribute(AttRef);
                                    tr.AddNewlyCreatedDBObject(AttRef, true);
                                }
                            }
                        }
                    }

                    return br.Id;
                }
            }

            ObjectId FindOrLoadBlock()
            {
                ObjectId id = ObjectId.Null;

                database.StartTransaction(tran =>
                {
                    database.BlockTableId.Get<BlockTable>(bt =>
                    {
                        if (modelOrPaper == ObjectId.Null) modelOrPaper = bt[BlockTableRecord.ModelSpace];
                        if(!bt.Has(blockName)) 
                            database.LoadBlockFromDwg(filepath: filePath, blocks: blockName);

                        if(bt.Has(blockName)) id = bt[blockName];

                    }, tr: tran);

                });
                return id;
            }
        }






        /// <summary>
        /// This will load a DBObject from one of the block resource files to the database calling the method.
        /// </summary>
        /// <typeparam name="T">Type of the object to load, must be derived from DBObject</typeparam>
        /// <param name="destDb">Database calling the method, where the copied item will go</param>
        /// <param name="predicate">The condition each item of type T will be compared against to determine if it should be copied</param>
        /// <param name="breakCondition">The condition to stop going through block resource files and exit.</param>
        /// <param name="filepath">null will look through all the block resource file drawings</param>
        /// <param name="btr">default is model space for the destDb</param>
        /// <param name="callerName">Used to give helpful error codes</param>
        /// <returns>True is break condition was met ie if the item was found and copied.</returns>
        public static bool LoadFromDwg<T>(this Database destDb,
            Predicate<T> predicate,
            Func<Database, ObjectIdCollection, bool> breakCondition,
            string filepath = null,
            ObjectId btr = default,
            [CallerMemberName] string callerName = "") where T : DBObject
        {
            if (btr == ObjectId.Null) btr = ModelSpaceId();
            if (btr == ObjectId.Null) return false;

            bool breakFlag = false;

            foreach (Database sourceDb in GetBlockResourceDatabase(filepath))
            {
                if(sourceDb == null) continue;
                if (breakFlag)
                {
                    sourceDb.Dispose();
                    break;
                }

                ObjectIdCollection idsToClone = new ObjectIdCollection();
                
                sourceDb.StartTransaction(tr =>
                {
                    sourceDb.InModelSpace((modelBtr, tran) =>
                    {
                        modelBtr.ForEach<T>(obj =>
                        {
                            if (predicate(obj)) idsToClone.Add(obj.ObjectId);

                        }, tr: tr);

                    }, tran: tr, callerName: callerName);

                });
                
                


                IdMapping mapping = new IdMapping();

                sourceDb.WblockCloneObjects(idsToClone,
                    btr,
                    mapping,
                DuplicateRecordCloning.Replace,
                false);

                Active.WriteMessage($"\nCopied {idsToClone.Count} entities from {sourceDb.Filename} to the current drawing");

                if (breakCondition(sourceDb, idsToClone)) breakFlag = true;
                
                sourceDb.Dispose();
                idsToClone = new ObjectIdCollection();
                
            }

            return breakFlag;

            ObjectId ModelSpaceId()
            {
                ObjectId modelId = ObjectId.Null;

                destDb.BlockTableId.Get<BlockTable>(table =>
                {
                    modelId = table[BlockTableRecord.ModelSpace];
                });

                return modelId;
            }
        }


        public static bool LoadBlockFromDwg(this Database destDb, string filepath = null, params string[] blocks)
        {
            // Create a variable to store the list of block identifiers
            ObjectIdCollection blockIds = new ObjectIdCollection();
            List<string> blockList = new List<string>(blocks);
            bool loadAll = blocks.Length < 1;

            Transaction sourceTran; BlockTable sourceBt;

            foreach (Database sourceDb in GetBlockResourceDatabase(filepath))
            {
                if (sourceDb == null) continue;

                using (sourceTran = sourceDb.TransactionManager.StartOpenCloseTransaction())
                using (sourceBt = sourceTran.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false, true) as BlockTable)
                {
                    try
                    {
                        if (blockList.Count == 0)
                        {
                            //load all blocks
                            foreach (ObjectId id in sourceBt)
                            {
                                using (BlockTableRecord btr =
                                       sourceTran.GetObject(id, OpenMode.ForRead, false, true) as BlockTableRecord)
                                {
                                    if (!btr.IsAnonymous && !btr.IsLayout) blockIds.Add(id);
                                }
                            }
                        }
                        else
                        {
                            for (int i = blockList.Count - 1; i >= 0; i--)
                            {
                                string block = blockList[i];
                                if (!sourceBt.Has(block)) continue;
                                blockIds.Add(sourceBt[block]);
                                blockList.Remove(block);
                            }
                        }

                        sourceTran.Commit();
                    }
                    catch
                    {
                        sourceTran.Abort();
                    }
                }


                IdMapping mapping = new IdMapping();

                sourceDb.WblockCloneObjects(blockIds,
                    destDb.BlockTableId,
                    mapping,
                    DuplicateRecordCloning.Replace,
                    false);

                Active.WriteMessage($"\nCopied {blockIds.Count} block definitions from {sourceDb.Filename} to the current drawing");

                sourceDb.Dispose();
                blockIds = new ObjectIdCollection();

                if (!loadAll && blockList.Count == 0) break;

            }

            return blockList.Count == 0;
        }

        /// <summary>
        /// Need to make sure to run Dispose on returned Databases.
        /// </summary>
        /// <param name="filepath">for picking specific BlockResourceFile. If null will return all from available BRF's</param>
        /// <returns></returns>
        private static IEnumerable<Database> GetBlockResourceDatabase(string filepath)
        {
            if (filepath == null)
            {
                if (!ResourceFolder.BlocksFolderExists)
                {
                    Active.WriteMessage($"Error in {nameof(GetBlockResourceDatabase)}: Resource folder does not exist.");

                    if (File.Exists(OldBlockResourceFile))
                    {
                        Database sourceDb = new Database(false, true);
                        sourceDb.ReadDwgFile(OldBlockResourceFile, FileShare.Read, true, "");
                        yield return sourceDb;
                    }
                    else
                    {
                        yield break;
                    }
                }
                foreach (string path in ResourceFolder.BlockResourceFilesList)
                {
                    if (!File.Exists(path)) continue;

                    Database sourceDb = new Database(false, true);
                    sourceDb.ReadDwgFile(path, FileShare.Read, true, "");

                    yield return sourceDb;
                }
            }
            else
            {
                if (!File.Exists(filepath)) yield break;

                Database sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(filepath, FileShare.Read, true, "");

                yield return sourceDb;
            }

            yield break;
        }
    }
}