// ComponentDefiner.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using RailDesigner1.Utils; // For ErrorLogger
using RailCreator; // Correct namespace based on generator files
using Autodesk.AutoCAD.Geometry; // For geometry types
using System.Collections.Generic; // Required for List<Point3d>

namespace RailDesigner1
{
    public class ComponentDefiner
    {
        private Database _database;
        private Editor _editor;

        public ComponentDefiner(Database db, Editor ed)
        {
            _database = db;
            _editor = ed;
            _editor.WriteMessage("\nComponentDefiner initialized.\n");
            ErrorLogger.LogMessage("ComponentDefiner initialized.");
        }

        public void DefineComponentLoop()
        {
            bool continueLoop = true;
            while (continueLoop)
            {
                // Prompt for component type
                PromptResult prType = _editor.GetString("\nEnter component type (Post/Picket, or 'done' to exit): ");
                if (prType.Status != PromptStatus.OK) break; // Exit if canceled
                string componentType = prType.StringResult.Trim().ToLower();

                if (componentType == "done")
                {
                    continueLoop = false;
                    continue; // Exit the loop
                }

                // Prompt for polyline selection BEFORE the transaction if possible,
                // but often needed inside if reusing transaction for multiple operations.
                PromptEntityResult prEntity = _editor.GetEntity("\nSelect a polyline for component placement: ");
                if (prEntity.Status != PromptStatus.OK) continue; // Skip if canceled

                // Store calculated post positions outside the switch if pickets might need them
                List<Point3d> calculatedPostPositions = null;

                using (Transaction tr = _database.TransactionManager.StartTransaction())
                {
                    // Get the BlockTableRecord for the current space (Model Space)
                    BlockTable bt = (BlockTable)tr.GetObject(_database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(_database.CurrentSpaceId, OpenMode.ForWrite); // Open ForWrite needed to add entities

                    if (tr.GetObject(prEntity.ObjectId, OpenMode.ForRead) is Polyline polyline)
                    {
                        // Prompt for design parameters or use defaults
                        PromptResult prRailHeight = _editor.GetString("\nEnter rail height (e.g., 36.0): ");
                        double railHeight = 36.0;
                        if (prRailHeight.Status == PromptStatus.OK && double.TryParse(prRailHeight.StringResult, out double height)) { railHeight = height; }

                        PromptResult prMountingType = _editor.GetString("\nEnter mounting type (e.g., Plate): ");
                        string mountingType = "Plate";
                        if (prMountingType.Status == PromptStatus.OK) { mountingType = prMountingType.StringResult; }

                        PromptResult prPicketType = _editor.GetString("\nEnter picket type (e.g., Vertical): ");
                        string picketType = "Vertical";
                        if (prPicketType.Status == PromptStatus.OK) { picketType = prPicketType.StringResult; }

                        // *** ADD PROMPTS FOR OTHER NEEDED RailingDesign PROPERTIES ***
                        // E.g., PostSize, PicketSize, TopCapHeight are used by generators
                        PromptResult prPostSize = _editor.GetString("\nEnter Post Size (e.g., 2x2): ");
                        string postSize = "2x2"; // Default
                        if (prPostSize.Status == PromptStatus.OK) { postSize = prPostSize.StringResult; }

                        PromptResult prPicketSize = _editor.GetString("\nEnter Picket Size (e.g., 0.5x0.5 or 0.75 round): ");
                        string picketSize = "0.5x0.5"; // Default
                        if (prPicketSize.Status == PromptStatus.OK) { picketSize = prPicketSize.StringResult; }

                        PromptResult prTopCapHeight = _editor.GetString("\nEnter Top Cap Height (e.g., 1.5): ");
                        double topCapHeight = 1.5; // Default
                        if (prTopCapHeight.Status == PromptStatus.OK && double.TryParse(prTopCapHeight.StringResult, out double tcHeight)) { topCapHeight = tcHeight; }

                        // FIX: Explicitly use RailCreator.RailingDesign and include ALL needed properties
                        RailCreator.RailingDesign design = new RailCreator.RailingDesign
                        {
                            RailHeight = railHeight,
                            MountingType = mountingType,
                            PicketType = picketType,
                            PostSize = postSize,       // Add this
                            PicketSize = picketSize,   // Add this
                            TopCapHeight = topCapHeight, // Add this
                            // Add DecorativeWidth if PlaceDecorativePickets is ever called
                            // DecorativeWidth = 2.0 // Example default
                        };

                        switch (componentType)
                        {
                            case "post":
                                // FIX: Call generator, pass VALID btr and tr, STORE the result
                                calculatedPostPositions = PostGenerator.CalculatePostPositions(polyline, design, currentSpace, tr);
                                _editor.WriteMessage($"\nGenerated {calculatedPostPositions?.Count ?? 0} posts.");
                                break;

                            case "picket":
                                // FIX: Pickets NEED post positions. Calculate them first if not already done.
                                // Option 1: Assume posts were *just* calculated in a previous step (might be fragile)
                                // Option 2: Recalculate posts silently here if needed
                                // Option 3 (Robust): Require posts to be generated *before* pickets can be placed.

                                // Let's go with Option 2 (Recalculate silently if 'calculatedPostPositions' is null):
                                if (calculatedPostPositions == null)
                                {
                                    _editor.WriteMessage("\nPost positions not available, calculating them now for picket placement...");
                                    // We call CalculatePostPositions but might not *draw* them if the user only asked for pickets.
                                    // However, the current PostGenerator *always* draws them. You might want separate methods
                                    // for 'calculate positions' vs 'calculate and draw posts'.
                                    // For now, we'll call the existing one which draws them too.
                                    calculatedPostPositions = PostGenerator.CalculatePostPositions(polyline, design, currentSpace, tr);
                                    if (calculatedPostPositions == null || calculatedPostPositions.Count < 2)
                                    {
                                        _editor.WriteMessage("\nError: Could not determine valid post positions for picket placement.");
                                        break; // Exit case "picket"
                                    }
                                }

                                if (calculatedPostPositions != null && calculatedPostPositions.Count >= 2)
                                {
                                    // FIX: Call PlacePickets with the calculated positions and valid btr/tr
                                    // Decide which picket placement method to call based on design?
                                    if (design.PicketType.ToLower().Contains("deco"))
                                    {
                                        // You'd need to prompt for or set design.DecorativeWidth here too
                                        // design.DecorativeWidth = 2.0; // Example
                                        // PicketGenerator.PlaceDecorativePickets(polyline, design, calculatedPostPositions, currentSpace, tr);
                                        _editor.WriteMessage("\nDecorative picket placement not fully implemented in this loop yet.");
                                    }
                                    else if (design.PicketSize.ToLower().Contains("glass") || design.PicketSize.ToLower().Contains("mesh") || design.PicketSize.ToLower().Contains("perf"))
                                    {
                                        // PicketGenerator.PlaceSpecialPickets(polyline, design, calculatedPostPositions, currentSpace, tr);
                                         _editor.WriteMessage("\nSpecial (Glass/Mesh/Perf) picket placement not fully implemented in this loop yet.");
                                    }
                                    else // Includes Vertical and Horizontal based on PicketGenerator logic
                                    {
                                         PicketGenerator.PlacePickets(polyline, design, calculatedPostPositions, currentSpace, tr);
                                    }
                                    _editor.WriteMessage("\nPlaced pickets.");
                                }
                                else
                                {
                                     _editor.WriteMessage("\nCannot place pickets: Need at least two post positions.");
                                }
                                break;

                            default:
                                _editor.WriteMessage("\nInvalid component type. Please enter 'Post' or 'Picket'.");
                                break;
                        }
                    }
                    else
                    {
                        _editor.WriteMessage("\nSelected entity is not a polyline.");
                    }
                    tr.Commit(); // Commit transaction after operations
                } // End using Transaction
            } // End while loop
        } // End DefineComponentLoop
    } // End class ComponentDefiner
} // End namespace RailDesigner1