using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry; // Though not directly used in this snippet, often needed with AutoCAD commands
using System;
using System.Collections.Generic;
// Ensure RailDesigner1 namespace is available if ComponentDefiner, RailingDesign, etc. are in it.
// Assuming they are in the same namespace or RailDesigner1 is referenced correctly.

namespace RailDesigner1
{
    public class RailGenerateCommand
    {
        [CommandMethod("RAIL_GENERATE")]
        public void RailGenerate()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                // Attempt to write to console or a log file if editor is not available
                System.Diagnostics.Debug.WriteLine("No active document.");
                return;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database; // Not directly used here but good practice to have

            try
            {
                ComponentDefiner definer = new ComponentDefiner();
                List<string> componentTypesToDefine = new List<string> { "POST", "PICKET" };

                Dictionary<string, ObjectId> componentBlockIds = new Dictionary<string, ObjectId>();
                Dictionary<string, Dictionary<string, string>> componentAttributes = new Dictionary<string, Dictionary<string, string>>();

                foreach (string componentType in componentTypesToDefine)
                {
                    ed.WriteMessage($"\n--- Defining {componentType} component ---");
                    ObjectId blockId = definer.DefineComponent(componentType, out Dictionary<string, string> attributes);

                    if (blockId == ObjectId.Null || attributes == null || attributes.Count == 0)
                    {
                        ed.WriteMessage($"\nComponent definition cancelled or failed for {componentType}. Aborting RAIL_GENERATE command.\n");
                        return;
                    }

                    componentBlockIds[componentType] = blockId;
                    componentAttributes[componentType] = attributes;
                    ed.WriteMessage($"\nSuccessfully defined {componentType} component: {attributes["PARTNAME"]}.\n");
                }

                ed.WriteMessage("\n--- All components defined successfully ---\n");

                // Polyline Selection
                PromptEntityResult per = ed.GetEntity("\nSelect a polyline for the railing path: ");
                if (per.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nNo polyline selected. Aborting command.\n");
                    return;
                }
                ObjectId polyId = per.ObjectId;

                // RailingDesign Object
                RailingDesign design = new RailingDesign(); // Use default values initially

                // Attempt to set PostSpacing from defined POST attributes
                // Assuming "POSTSPACING" is a key the user might add in AttributeForm for the POST.
                // The value for "POSTSPACING" should be a number (e.g., "1000" or "48.0").
                if (componentAttributes.TryGetValue("POST", out var postAttrs))
                {
                    if (postAttrs.TryGetValue("POSTSPACING", out string spacingStr)) // Case-sensitive, ensure matches AttributeForm entry
                    {
                        if (double.TryParse(spacingStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double postSpacingValue))
                        {
                            design.PostSpacing = postSpacingValue;
                            ed.WriteMessage($"\nUsing PostSpacing from POST attributes: {design.PostSpacing}\n");
                        }
                        else
                        {
                            ed.WriteMessage($"\nWarning: POSTSPACING attribute ('{spacingStr}') for POST is not a valid number. Using default PostSpacing: {design.PostSpacing}\n");
                        }
                    }
                    else
                    {
                        ed.WriteMessage($"\nNote: POSTSPACING attribute not found for POST. Using default PostSpacing: {design.PostSpacing}\n");
                    }
                }
                else
                {
                     ed.WriteMessage($"\nNote: POST component attributes not found. Using default PostSpacing: {design.PostSpacing}\n");
                }


                // Call RailingGeometryGenerator
                // The TransactionManager is passed from the Document
                // No separate transaction is started here as GenerateRailingGeometry handles its own.
                RailingGeometryGenerator generator = new RailingGeometryGenerator();
                generator.GenerateRailingGeometry(doc.TransactionManager, polyId, componentBlockIds, design);

                ed.WriteMessage("\nRailing generation process completed.\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nAn unexpected error occurred in RAIL_GENERATE command: {ex.Message}\nStackTrace: {ex.StackTrace}\n");
            }
        }
    }
}
