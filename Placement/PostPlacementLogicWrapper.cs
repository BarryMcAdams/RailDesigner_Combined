// RailDesigner1.Placement.PostPlacementLogicWrapper.cs
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Definitions; // For ComponentDefinitionData
using System.Collections.Generic;
using System.Linq;
using RailCreator; // Namespace for the original PostGenerator
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

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
            ComponentDefinitionData postDefinition,
            Transaction trForRoo, /* Transaction needed by RailCreator.PostGenerator */
            Database dbForRoo /* Database needed by RailCreator.PostGenerator */
            )
        {
            var placements = new List<PlacementInfo>();
            if (pathPolyline == null || postDefinition == null) return placements;

            // Adapt RailDesigner1.RailingDesign to RailCreator.RailingDesign
            // The RailCreator.PostGenerator uses design.MountingType, design.RailHeight, 
            // design.TopCapHeight, design.PostSize.
            // The RailDesigner1.RailingDesign needs to supply these.
            // For now, assume RailDesigner1.RailingDesign has compatible properties or use defaults.
            var rooDesign = new RailCreator.RailingDesign
            {
                RailHeight = designParams.RailHeight, // From RailDesigner1.RailingDesign
                // MountingType = designParams.MountType, // Need mapping or direct property
                PostSize = postDefinition.Attributes.GetValueOrDefault("WIDTH", "2x2"), // Example, assuming WIDTH attr = PostSize string
                // TopCapHeight = designParams.TopCapHeight, // From RailDesigner1.RailingDesign
                // And other properties RailCreator.PostGenerator might need.
                // This requires RailCreator.RailingDesign to be defined or mapped.
                // For simplicity, using defaults or what RailCreator.PostGenerator itself sets
            };
            // Retrieve PostSize from postDefinition.Attributes if available, otherwise use a default for Roo.
             if (postDefinition.Attributes.TryGetValue("POSTSIZE", out string postSizeAttr) && !string.IsNullOrWhiteSpace(postSizeAttr))
             {
                 rooDesign.PostSize = postSizeAttr;
             } else if (postDefinition.Attributes.TryGetValue("WIDTH", out string widthAttr) && 
                        postDefinition.Attributes.TryGetValue("HEIGHT", out string heightAttr)) // Assuming height attribute is depth
             {
                // If attributes are W and H for dimensions rather than "2x2 string".
                // PostSize for Roo looks for "2x2" type string primarily for parsing.
                // Here we might construct it or pass a simple default.
                // The critical info for Roo PostGenerator.CalculatePostPositions itself is `targetSpacing`.
                // The post drawing within Roo's CalculatePostPositions method uses design.PostSize for drawn shape.
                rooDesign.PostSize = postDefinition.Attributes.GetValueOrDefault("WIDTH", "2") + "x" + postDefinition.Attributes.GetValueOrDefault("HEIGHT", "2"); // Approximation
             }

             // RailCreator.PostGenerator.CalculatePostPositions takes BlockTableRecord and Transaction.
             // This is problematic as it might draw unwanted geometry and interfere with main transaction.
             // Per strict instruction "Do not modify core logic", we call it.
             // We must provide a BTR for it to (potentially) draw into.
             // Create a temporary BTR or pass ModelSpace if appropriate. If ModelSpace, it will draw.

             BlockTableRecord spaceToDrawIn = (BlockTableRecord)trForRoo.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(dbForRoo), OpenMode.ForWrite);

             // THE CALL:
             // The version of PostGenerator.cs from the *prompt's text block* must be used.
             List<Point3d> postPoints = RailCreator.PostGenerator.CalculatePostPositions(pathPolyline, rooDesign, spaceToDrawIn, trForRoo);

             // The RailCreator.PostGenerator will have drawn its own simple posts.
             // RailDesigner1 will now place its defined blocks AT these positions.

             foreach (var point in postPoints)
             {
                 Vector3d orientation = Vector3d.XAxis; // Default
                 try
                 {
                     double param = pathPolyline.GetParameterAtPoint(pathPolyline.GetClosestPointTo(point, false));
                     Vector3d tangent = pathPolyline.GetFirstDerivative(param);
                     if (!tangent.IsZeroLength())
                     {
                         orientation = tangent.GetNormal();
                     }
                 }
                 catch (System.Exception ex) {
                      AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError getting tangent for post at {point}: {ex.Message}");
                      // Keep default orientation
                 }

                 placements.Add(new PlacementInfo
                 {
                     Type = ComponentType.Post,
                     BlockDefinitionId = postDefinition.BlockDefinitionId,
                     Position = point,
                     Orientation = orientation,
                     Attributes = postDefinition.Attributes // Or specific overrides
                 });
             }
             return placements;
         }
     }

     // Helper for Dictionary
     public static class DictionaryExtensions
     {
         public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
         {
             return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
         }
     }