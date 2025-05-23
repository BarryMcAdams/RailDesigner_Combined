// RailDesigner1.Placement.PostPlacementLogicWrapper.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() if used on IEnumerable from ModelSpace
using RailCreator; // Namespace for the original PostGenerator
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System; // Required for Guid

namespace RailDesigner1.Placement
{
    public class PlacementInfo 
    {
        public ComponentType Type { get; set; }
        public ObjectId BlockDefinitionId { get; set; }
        public Point3d Position { get; set; }
        public Vector3d Orientation { get; set; } // Normal vector representing direction
        public Dictionary<string, string> Attributes { get; set; } // For attribute overrides if needed
    }

    public class PostPlacementLogicWrapper
    {
        public List<PlacementInfo> CalculatePlacements(
            Polyline pathPolyline,
            RailingDesign designParams, /* RailDesigner1.RailingDesign */
            ObjectId postBlockDefId, /* ObjectId of the "POST" BlockTableRecord */
            Dictionary<string, string> postDefinitionAttributes, /* Dictionary<string, string> for the "POST" */
            Transaction trForRoo, /* Transaction needed by RailCreator.PostGenerator */
            Database dbForRoo /* Database needed by RailCreator.PostGenerator */
            )
        {
            var placements = new List<PlacementInfo>();
            if (pathPolyline == null || postBlockDefId == ObjectId.Null || postDefinitionAttributes == null) return placements;

            var rooDesign = new RailCreator.RailingDesign
            {
                RailHeight = designParams.RailHeight, 
                PostSpacing = designParams.PostSpacing, 
            };

            string postSizeString = postDefinitionAttributes.GetValueOrDefault("POSTSIZE", "2x2"); 
            if (postSizeString == "2x2" && postDefinitionAttributes.ContainsKey("WIDTH") && postDefinitionAttributes.ContainsKey("DEPTH"))
            {
                 string width = postDefinitionAttributes.GetValueOrDefault("WIDTH", "2");
                 string depth = postDefinitionAttributes.GetValueOrDefault("DEPTH", "2");
                 postSizeString = $"{width}x{depth}";
            }
            else if (postSizeString == "2x2" && postDefinitionAttributes.ContainsKey("WIDTH") && postDefinitionAttributes.ContainsKey("HEIGHT")) 
            {
                 string width = postDefinitionAttributes.GetValueOrDefault("WIDTH", "2");
                 string height = postDefinitionAttributes.GetValueOrDefault("HEIGHT", "2"); 
                 postSizeString = $"{width}x{height}";
            }
            rooDesign.PostSize = postSizeString;

            // --- Ghost Graphics Cleanup Logic ---
            ObjectId originalLayerId = dbForRoo.Clayer;
            string tempLayerName = "TEMP_POST_GEN_GHOSTS_" + Guid.NewGuid().ToString("N");
            ObjectId tempLayerId = ObjectId.Null;
            List<Point3d> postPoints = null; // Initialize to null

            try
            {
                // Create and set temporary layer as current
                LayerTable lt = (LayerTable)trForRoo.GetObject(dbForRoo.LayerTableId, OpenMode.ForWrite);
                if (lt.Has(tempLayerName))
                { 
                    // This case should be rare due to GUID, but handle defensively
                    tempLayerId = lt[tempLayerName];
                }
                else
                {
                    using (LayerTableRecord tempLayer = new LayerTableRecord())
                    {
                        tempLayer.Name = tempLayerName;
                        tempLayer.IsPlotted = false; // Ensure it's not plotted
                        tempLayerId = lt.Add(tempLayer);
                        trForRoo.AddNewlyCreatedDBObject(tempLayer, true);
                    }
                }
                dbForRoo.Clayer = tempLayerId;
                AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nSwitched to temp layer: {tempLayerName}");


                // Call the original RailCreator logic which draws "ghost" graphics
                BlockTableRecord spaceToDrawIn = (BlockTableRecord)trForRoo.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(dbForRoo), OpenMode.ForWrite);
                postPoints = RailCreator.PostGenerator.CalculatePostPositions(pathPolyline, rooDesign, spaceToDrawIn, trForRoo);
                AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nRailCreator.PostGenerator.CalculatePostPositions executed. Found {postPoints?.Count ?? 0} points.");


                // Erase ghost graphics drawn on the temporary layer
                List<ObjectId> ghostObjectIds = new List<ObjectId>();
                // Re-open ModelSpace for read to iterate, as spaceToDrawIn was for write and might be closed/downgraded
                BlockTableRecord modelSpaceForRead = (BlockTableRecord)trForRoo.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(dbForRoo), OpenMode.ForRead);
                foreach (ObjectId objId in modelSpaceForRead)
                {
                    Entity ent = trForRoo.GetObject(objId, OpenMode.ForRead, false, true) as Entity;
                    if (ent != null && ent.LayerId == tempLayerId)
                    {
                        ghostObjectIds.Add(objId);
                    }
                }
                
                if (ghostObjectIds.Count > 0)
                {
                    AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nFound {ghostObjectIds.Count} ghost entities to erase on layer {tempLayerName}.");
                    foreach (ObjectId ghostId in ghostObjectIds)
                    {
                        Entity ghostEnt = trForRoo.GetObject(ghostId, OpenMode.ForWrite) as Entity;
                        ghostEnt?.Erase(); // Erase the ghost entity
                    }
                }
            }
            catch (System.Exception ex)
            {
                AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nError during ghost graphics handling: {ex.Message}");
                // postPoints might be null or partially populated if error occurred during CalculatePostPositions
                // If postPoints is null, the method will return an empty placements list as per existing logic.
            }
            finally
            {
                // Restore original current layer
                if (dbForRoo.Clayer == tempLayerId && tempLayerId != ObjectId.Null && !originalLayerId.IsNull)
                {
                    dbForRoo.Clayer = originalLayerId;
                    AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nRestored original current layer.");
                }

                // Delete the temporary layer
                if (tempLayerId != ObjectId.Null && !tempLayerId.IsErased)
                {
                    try
                    {
                        LayerTable lt = (LayerTable)trForRoo.GetObject(dbForRoo.LayerTableId, OpenMode.ForWrite); // Ensure LT is open for write
                        LayerTableRecord ltrToErase = trForRoo.GetObject(tempLayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (ltrToErase != null && !ltrToErase.IsErased) {
                            // Check if layer is empty (it should be after erasing entities)
                            // This check might be complex or unnecessary if we are sure we erased everything.
                            // For simplicity, we'll try to erase it directly.
                            // It might fail if entities still reference it (e.g., if erase failed or some were missed).
                            ltrToErase.UpgradeOpen(); // Open for write to erase
                            ltrToErase.Erase(true); // Erase the layer and dependent objects if any (should be none)
                            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nTemporary layer {tempLayerName} erased.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nError deleting temporary layer {tempLayerName}: {ex.Message}");
                    }
                }
            }
            // --- End of Ghost Graphics Cleanup Logic ---

            if (postPoints == null) // If CalculatePostPositions failed or error occurred before it ran
            {
                return placements; // Return empty list
            }

            foreach (var point in postPoints)
            {
                Vector3d orientation = Vector3d.XAxis; 
                try
                {
                    Point3d closestPointOnPoly = pathPolyline.GetClosestPointTo(point, false);
                    double param = pathPolyline.GetParameterAtPoint(closestPointOnPoly);
                    Vector3d tangent = pathPolyline.GetFirstDerivative(param);
                    if (!tangent.IsZeroLength())
                    {
                        orientation = tangent.GetNormal(); 
                    }
                }
                catch (System.Exception ex) 
                {
                    AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nError calculating orientation for post at {point}: {ex.Message}. Using default.");
                }

                placements.Add(new PlacementInfo
                {
                    Type = ComponentType.Post, 
                    BlockDefinitionId = postBlockDefId, 
                    Position = point,
                    Orientation = orientation,
                    Attributes = new Dictionary<string, string>(postDefinitionAttributes) 
                });
            }
            return placements;
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}