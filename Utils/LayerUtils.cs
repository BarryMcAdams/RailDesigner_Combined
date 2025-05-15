// Utils/LayerUtils.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices; // << Ensure this is present
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.IO;

namespace RailDesigner1.Utils
{
    public static class LayerUtils
    {
        // Returns layer name e.g. L-RAIL-POST
        public static string GetOrCreateLayer(Database db, string componentTypeString, short colorIndex = 7, string linetypeName = "Continuous")
        {
            if (db == null || string.IsNullOrWhiteSpace(componentTypeString))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\nError: Invalid database or component type string passed to GetOrCreateLayer.");
                return Application.DocumentManager.MdiActiveDocument?.Database?.Clayer.ToString() ?? "0";
            }

            string layerName = $"L-RAIL-{componentTypeString.ToUpperInvariant()}";
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = null;
                try
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    if (!lt.Has(layerName))
                    {
                        lt.UpgradeOpen();
                        LayerTableRecord ltr = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                        };

                        ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead); // Can be Read initially
                        if (ltt.Has(linetypeName))
                        {
                            ltr.LinetypeObjectId = ltt[linetypeName];
                        }
                        else
                        {
                            if (!linetypeName.Equals("Continuous", System.StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    // --- Reverted to db.LoadLineTypeFile ---
                                    // Ensure 'db' is the Database object passed into the method
                                    db.LoadLineTypeFile(linetypeName, "acad.lin");

                                    // Re-check the table *after* loading
                                    if (ltt.Has(linetypeName)) ltr.LinetypeObjectId = ltt[linetypeName];
                                    else
                                    {
                                        ed?.WriteMessage($"\nWarning: Linetype '{linetypeName}' not found in acad.lin or failed to load. Using Continuous.");
                                        if (ltt.Has("Continuous")) ltr.LinetypeObjectId = ltt["Continuous"]; else ltr.LinetypeObjectId = db.Celtype;
                                    }
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception acEx)
                                {
                                    ed?.WriteMessage($"\nWarning: Could not load linetype '{linetypeName}' (AutoCAD Error: {acEx.ErrorStatus}): {acEx.Message}. Using Continuous.");
                                    if (ltt.Has("Continuous")) ltr.LinetypeObjectId = ltt["Continuous"]; else ltr.LinetypeObjectId = db.Celtype;
                                }
                                catch (System.Exception ex)
                                {
                                    ed?.WriteMessage($"\nWarning: Could not load linetype '{linetypeName}' (System Error): {ex.Message}. Using Continuous.");
                                    if (ltt.Has("Continuous")) ltr.LinetypeObjectId = ltt["Continuous"]; else ltr.LinetypeObjectId = db.Celtype;
                                }
                            }
                            else // Requested "Continuous"
                            {
                                if (ltt.Has("Continuous"))
                                {
                                    ltr.LinetypeObjectId = ltt["Continuous"];
                                }
                                else
                                {
                                    ed?.WriteMessage($"\nCritical Error: Standard linetype 'Continuous' not found in the linetype table.");
                                    ltr.LinetypeObjectId = db.Celtype;
                                }
                            }
                        }

                        lt.Add(ltr);
                        tr.AddNewlyCreatedDBObject(ltr, true);
                        ed?.WriteMessage($"\nLayer '{layerName}' created.");
                    }
                    tr.Commit();
                    return layerName;
                }
                catch (System.Exception ex)
                {
                    ErrorLogger.LogError($"Error creating/getting layer '{layerName}': {ex.Message}", ex);
                    ed?.WriteMessage($"\nError in GetOrCreateLayer for '{layerName}': {ex.Message}");
                    return db.Clayer.ToString();
                }
            }
        }

        // Returns ObjectId of the layer
        public static ObjectId GetOrCreateLayerId(Transaction tr, Database db, string layerName, short colorIndex = 7, string linetypeName = "Continuous")
        {
            if (tr == null || db == null || string.IsNullOrWhiteSpace(layerName))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\nError: Invalid transaction, database or layerName passed to GetOrCreateLayerId.");
                return ObjectId.Null;
            }

            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            LinetypeTable ltt = null;

            if (lt.Has(layerName))
            {
                return lt[layerName];
            }
            else
            {
                try
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };

                    ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead); // Read initially
                    if (ltt.Has(linetypeName))
                    {
                        ltr.LinetypeObjectId = ltt[linetypeName];
                    }
                    else
                    {
                        if (!linetypeName.Equals("Continuous", System.StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                // --- Reverted to db.LoadLinetypeFile ---
                                db.LoadLineTypeFile(linetypeName, "acad.lin");

                                // Re-check table after loading
                                if (ltt.Has(linetypeName)) ltr.LinetypeObjectId = ltt[linetypeName];
                                else
                                {
                                    Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning: Linetype '{linetypeName}' not found after load attempt in GetOrCreateLayerId. Using Continuous.");
                                    // Fallback handled below
                                }
                            }
                            catch (System.Exception loadEx)
                            {
                                ErrorLogger.LogError($"Failed to load linetype '{linetypeName}' in GetOrCreateLayerId.", loadEx);
                                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning: Failed to load linetype '{linetypeName}' in GetOrCreateLayerId: {loadEx.Message}.");
                                // Fallback handled below
                            }
                        }

                        if (ltr.LinetypeObjectId.IsNull)
                        {
                            if (ltt.Has("Continuous")) ltr.LinetypeObjectId = ltt["Continuous"];
                            else ltr.LinetypeObjectId = db.Celtype;
                        }
                    }

                    ObjectId layerId = lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                    return layerId;
                }
                catch (System.Exception ex)
                {
                    ErrorLogger.LogError($"Error creating layer '{layerName}' in GetOrCreateLayerId: {ex.Message}", ex);
                    Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nError creating layer '{layerName}' in GetOrCreateLayerId: {ex.Message}");
                    return ObjectId.Null;
                }
            }
        }
    }
}