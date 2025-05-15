
Project Structure (Conceptual Additions/Modifications):

      
RailDesigner1/
├── Plugin.cs                       // Main entry point, command definitions
├── RailGenerateCommandHandler.cs   // Orchestrates RAIL_GENERATE
├── ComponentDefiner.cs             // Logic for defining components as blocks
├── AttributeForm.cs                // WinForms dialog for component attributes
├── PostPlacementLogicWrapper.cs    // Wrapper for RailCreator_Roo Post logic
├── PicketPlacementLogicWrapper.cs  // Wrapper for RailCreator_Roo Picket logic
├── MountPlacementLogic.cs          // Simple Mount placement (e.g., at Post locations)
├── RailingGeometryGenerator.cs     // Generates final railing entities
├── BomExporter.cs                  // Logic for RAIL_EXPORT_BOM
├── RailingDesign.cs                // Data model for overall railing (already provided)
├── LayerUtils.cs                   // Layer management utilities (can be part of SappoUtilities)
└── Models/                         // Folder for simple data structures
    └── ComponentDefinitionData.cs  // Stores info about a defined component
    └── BomEntry.cs                 // Stores data for a BOM line item
    └── PlacementInfo.cs            // Stores position/orientation for a component instance

    

IGNORE_WHEN_COPYING_START
Use code with caution.
IGNORE_WHEN_COPYING_END

Key File Implementations (Conceptual Code):

1. Plugin.cs (Entry Points)

      
// Plugin.cs
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Windows.Forms; // For DialogResult
using RailDesigner1.Commands; // Assuming commands are in this namespace
using RailDesigner1.Utils;    // For Error logging or common utilities

[assembly: CommandClass(typeof(RailDesigner1.Plugin))]

namespace RailDesigner1
{
    public enum ComponentType // Centralized enum
    {
        Post,
        Picket,
        TopCap,
        BottomRail,
        IntermediateRail,
        BasePlate,
        Mounting,
        UserDefined // For generic user-defined blocks
    }

    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nRailDesigner1 Plugin Loaded. Commands: RAIL_GENERATE, RAIL_EXPORT_BOM, RAIL_DEFINE_COMPONENT.\n");
            // Initialize logger if any
            ErrorLogger.Initialize("RailDesigner1_Log.txt");
        }

        public void Terminate()
        {
            ErrorLogger.Close();
        }

        [CommandMethod("RAIL_GENERATE", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void RailGenerate()
        {
            var handler = new RailGenerateCommandHandler();
            handler.Execute();
        }

        [CommandMethod("RAIL_DEFINE_COMPONENT", CommandFlags.Modal)]
        public static void DefineComponent()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var definer = new ComponentDefiner(doc.Database, doc.Editor);
            definer.DefineComponentLoop();
        }

        [CommandMethod("RAIL_EXPORT_BOM", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public static void RailExportBom()
        {
            var exporter = new BomExporter();
            exporter.ExportBom();
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

2. RailGenerateCommandHandler.cs (New/Replaces existing stub)

      
// RailGenerateCommandHandler.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Placement; // For wrappers
using RailDesigner1.Geometry; // For RailingGeometryGenerator
using RailDesigner1.Defintions; // For ComponentDefiner and ComponentDefinitionData
using RailDesigner1.UI;       // For AttributeForm (if directly used, though less likely here)
using RailDesigner1.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RailDesigner1.Commands
{
    public class RailGenerateCommandHandler
    {
        private Document _doc;
        private Database _db;
        private Editor _ed;
        private List<ComponentDefinitionData> _definedComponents;

        public RailGenerateCommandHandler()
        {
            _doc = Application.DocumentManager.MdiActiveDocument;
            if (_doc == null)
            {
                throw new InvalidOperationException("No active document.");
            }
            _db = _doc.Database;
            _ed = _doc.Editor;
            _definedComponents = new List<ComponentDefinitionData>();
        }

        public void Execute()
        {
            try
            {
                // Step 1: Component Definition Phase (or load existing definitions)
                // For this pass, assume components are defined. In full impl, this would be a loop or check.
                // Let's try to load any existing component blocks that match the criteria
                LoadExistingComponentDefinitions();

                if (!_definedComponents.Any(c => c.Type == ComponentType.Post) ||
                    !_definedComponents.Any(c => c.Type == ComponentType.Picket))
                {
                    _ed.WriteMessage("\nEssential components (Post, Picket) are not defined. Please define them using RAIL_DEFINE_COMPONENT.");
                    // Optionally, launch DefineComponentLoop here.
                    // ComponentDefiner definer = new ComponentDefiner(_db, _ed);
                    // definer.DefineComponentLoop(); // This updates a shared list or _definedComponents
                    // LoadExistingComponentDefinitions(); // Reload after definition
                    // if (!_definedComponents.Any(c => c.Type == ComponentType.Post) || ...) return;
                    return;
                }
                _ed.WriteMessage($"\nLoaded {_definedComponents.Count} component definitions.");


                // Step 2: Select Path Polyline
                ObjectId polylineId = SelectPathPolyline();
                if (polylineId == ObjectId.Null) return;

                // Step 3: Prepare RailingDesign parameters (could be from a dialog)
                RailingDesign design = new RailingDesign(); // Using default values for now


                // Step 4: Instantiate Placement Wrappers
                var postPlacer = new PostPlacementLogicWrapper();
                var picketPlacer = new PicketPlacementLogicWrapper();
                // var mountPlacer = new MountPlacementLogic(); // If mounts are needed

                List<PlacementInfo> allPlacements = new List<PlacementInfo>();

                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    Polyline pathPolyline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;
                    if (pathPolyline == null)
                    {
                        _ed.WriteMessage("\nFailed to read path polyline.");
                        tr.Abort();
                        return;
                    }

                    // Get Component Definitions
                    ComponentDefinitionData postDef = _definedComponents.FirstOrDefault(c => c.Type == ComponentType.Post);
                    ComponentDefinitionData picketDef = _definedComponents.FirstOrDefault(c => c.Type == ComponentType.Picket);
                    ComponentDefinitionData mountDef = _definedComponents.FirstOrDefault(c => c.Type == ComponentType.Mounting);

                    // -- Generate Post Placements --
                    if (postDef != null)
                    {
                        var postPlacements = postPlacer.CalculatePlacements(pathPolyline, design, postDef, tr, _db);
                        allPlacements.AddRange(postPlacements);
                    } else {
                        _ed.WriteMessage("\nWarning: Post component not defined. Posts will not be generated.");
                    }


                    // -- Generate Picket Placements --
                    // Picket placement needs post positions as input for the RailCreator.PicketGenerator logic
                    List<Point3d> actualPostPositions = allPlacements
                        .Where(p => p.Type == ComponentType.Post)
                        .Select(p => p.Position)
                        .ToList();

                    if (picketDef != null && actualPostPositions.Count > 1)
                    {
                        var picketPlacements = picketPlacer.CalculatePlacements(pathPolyline, design, picketDef, actualPostPositions, postDef, tr, _db);
                        allPlacements.AddRange(picketPlacements);
                    } else if (picketDef == null) {
                         _ed.WriteMessage("\nWarning: Picket component not defined. Pickets will not be generated.");
                    } else if (actualPostPositions.Count <=1 && picketDef != null) {
                        _ed.WriteMessage("\nWarning: Not enough posts to place pickets between them.");
                    }


                    // -- Generate Mount Placements (Example: at Post locations) --
                    if (mountDef != null && postDef != null) // if mount component defined
                    {
                        // Simple Mount placement: at each post location
                        foreach (var postPlacement in allPlacements.Where(p=> p.Type == ComponentType.Post))
                        {
                            allPlacements.Add(new PlacementInfo
                            {
                                Type = ComponentType.Mounting,
                                BlockDefinitionId = mountDef.BlockDefinitionId, // Ensure mountDef is not null
                                Position = postPlacement.Position,
                                Orientation = postPlacement.Orientation,
                                Attributes = mountDef.Attributes // Or specific mount attributes
                            });
                        }
                         _ed.WriteMessage($"\nAdded {allPlacements.Count(p=>p.Type == ComponentType.Mounting)} mounting placements at post locations.");
                    }


                    // Step 5: Generate Geometry
                    RailingGeometryGenerator generator = new RailingGeometryGenerator(_db, _ed);
                    generator.Generate(tr, pathPolyline, allPlacements, _definedComponents, design);

                    tr.Commit();
                    _ed.WriteMessage("\nRailing generation completed successfully.");
                }
            }
            catch (System.Exception ex)
            {
                ErrorLogger.Log($"RAIL_GENERATE Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                _ed.WriteMessage($"\nError during railing generation: {ex.Message} (See log for details)");
                MessageBox.Show($"Error: {ex.Message}\nSee RailDesigner1_Log.txt for more details.", "Railing Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadExistingComponentDefinitions()
        {
            _definedComponents.Clear();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null || btr.IsAnonymous || btr.IsLayout || btr.IsFromExternalReference || btr.IsDependent) continue;

                    Dictionary<string, string> attributes = new Dictionary<string, string>();
                    string componentTypeStr = null;

                    if (btr.HasAttributeDefinitions)
                    {
                        foreach (ObjectId attId in btr)
                        {
                            if (attId.ObjectClass.DxfName.Equals("ATTDEF"))
                            {
                                AttributeDefinition attDef = tr.GetObject(attId, OpenMode.ForRead) as AttributeDefinition;
                                if (attDef != null)
                                {
                                    attributes[attDef.Tag.ToUpperInvariant()] = attDef.TextString;
                                    if (attDef.Tag.Equals("COMPONENTTYPE", StringComparison.OrdinalIgnoreCase))
                                    {
                                        componentTypeStr = attDef.TextString;
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(componentTypeStr) && attributes.ContainsKey("PARTNAME"))
                    {
                        if (Enum.TryParse<ComponentType>(componentTypeStr, true, out ComponentType cType))
                        {
                            _definedComponents.Add(new ComponentDefinitionData
                            {
                                Name = attributes["PARTNAME"],
                                Type = cType,
                                BlockDefinitionId = btrId,
                                BlockName = btr.Name,
                                Attributes = attributes,
                                GeometryEntityIds = btr.Cast<ObjectId>().ToList() // Placeholder
                            });
                        }
                    }
                }
                tr.Commit(); // Or Abort for read-only
            }
        }


        private ObjectId SelectPathPolyline()
        {
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect a single 2D polyline for the railing path: ");
            peo.SetRejectMessage("\nInvalid selection. Must be a Polyline or Polyline2d.");
            peo.AddAllowedClass(typeof(Polyline), true);
            peo.AddAllowedClass(typeof(Polyline2d), true); // As per PRD

            PromptEntityResult per = _ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                _ed.WriteMessage("\nPolyline selection cancelled.");
                return ObjectId.Null;
            }
            // Further validation (e.g., planarity if necessary) could go here.
            return per.ObjectId;
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

3. ComponentDefiner.cs (Populate stubs)

      
// RailDesigner1.Defintions.ComponentDefiner.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.UI;
using RailDesigner1.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms; // For DialogResult


namespace RailDesigner1.Defintions
{
    public class ComponentDefinitionData
    {
        public string Name { get; set; } // PARTNAME
        public ComponentType Type { get; set; }
        public ObjectId BlockDefinitionId { get; set; }
        public string BlockName { get; set; }
        public List<ObjectId> GeometryEntityIds { get; set; } = new List<ObjectId>(); // Original entities before block
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        // Add other relevant data like base point if needed outside definition scope
    }

    public class ComponentDefiner
    {
        private Database _db;
        private Editor _ed;
        public List<ComponentDefinitionData> DefinedComponents { get; private set; }

        public ComponentDefiner(Database db, Editor ed)
        {
            _db = db;
            _ed = ed;
            DefinedComponents = new List<ComponentDefinitionData>();
        }

        public void DefineComponentLoop()
        {
            bool continueDefining = true;
            while (continueDefining)
            {
                PromptKeywordOptions pkoType = new PromptKeywordOptions("\nDefine component type [Post/Picket/TopCap/BottomRail/IntermediateRail/BasePlate/Mounting/UserDefined/Done]: ",
                    "Post Picket TopCap BottomRail IntermediateRail BasePlate Mounting UserDefined Done");
                pkoType.Keywords.Default = "Post"; // Example default
                PromptResult prType = _ed.GetKeywords(pkoType);

                if (prType.Status != PromptStatus.OK || prType.StringResult.Equals("Done", StringComparison.OrdinalIgnoreCase))
                {
                    continueDefining = false;
                    break;
                }

                if (Enum.TryParse<ComponentType>(prType.StringResult, true, out ComponentType componentType))
                {
                    DefineSingleComponent(componentType);
                }
                else
                {
                    _ed.WriteMessage("\nInvalid component type selected.");
                }
            }
            _ed.WriteMessage($"\nComponent definition finished. {DefinedComponents.Count} components defined in this session.");
        }

        private bool DefineSingleComponent(ComponentType type)
        {
            _ed.WriteMessage($"\nDefining component type: {type}");

            // 1. Select 2D entities
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\nSelect 2D entities for {type} (Lines, Polylines, Arcs, Circles, Ellipses): ";
            
            TypedValue[] filterList = new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<or"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"), // For 2D Polyline, not 3D
                new TypedValue((int)DxfCode.Start, "ARC"),
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                new TypedValue((int)DxfCode.Operator, "or>")
            };
            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionResult psr = _ed.GetSelection(pso, filter);

            if (psr.Status != PromptStatus.OK)
            {
                _ed.WriteMessage("\nEntity selection cancelled.");
                return false;
            }
            ObjectId[] selectedIds = psr.Value.GetObjectIds();

            // 2. Get base point
            PromptPointOptions ppo = new PromptPointOptions("\nSpecify base point for the selected geometry: ");
            ppo.UseBasePoint = false;
            PromptPointResult ppr = _ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                _ed.WriteMessage("\nBase point selection cancelled.");
                return false;
            }
            Point3d basePointWcs = ppr.Value;

            // 3. Attribute Form
            Dictionary<string, string> attributes;
            using (var form = new AttributeForm(type))
            {
                if (Application.ShowModalDialog(form) != DialogResult.OK)
                {
                    _ed.WriteMessage("\nAttribute input cancelled.");
                    return false;
                }
                attributes = form.Attributes;
            }
            attributes["COMPONENTTYPE"] = type.ToString().ToUpperInvariant(); // Ensure COMPONENTTYPE is set

            string partName = attributes["PARTNAME"];
            if (string.IsNullOrWhiteSpace(partName))
            {
                _ed.WriteMessage("\nPARTNAME is a required attribute.");
                return false;
            }
            
            string blockName = SanitizeBlockName(partName); // Use part name as base for block name

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);

                    // Handle naming conflicts
                    if (bt.Has(blockName))
                    {
                        PromptKeywordOptions pkoConflict = new PromptKeywordOptions($"\nBlock '{blockName}' already exists. [Overwrite/NewName/Cancel]: ", "Overwrite NewName Cancel");
                        PromptResult prConflict = _ed.GetKeywords(pkoConflict);
                        if (prConflict.Status != PromptStatus.OK) { tr.Abort(); return false; }

                        switch (prConflict.StringResult)
                        {
                            case "Overwrite":
                                // Delete existing block logic here (complex, involves checking references)
                                // For simplicity, we'll rename the new one slightly if overwrite is too complex for this snippet.
                                // True overwrite: Get existing blockId, erase it carefully.
                                _ed.WriteMessage("\nOverwrite chosen. (Simple handling: will try to redefine if possible, may error if referenced).");
                                // This might still fail if instances exist and cannot be automatically updated.
                                // A safer overwrite might involve deleting existing attdefs/entities from btr if it exists.
                                // Or truly delete and recreate, if no instances exist.
                                break;
                            case "NewName":
                                PromptStringOptions psoName = new PromptStringOptions($"\nEnter new unique name for block (based on '{partName}'): ");
                                psoName.DefaultValue = blockName + "_New";
                                PromptResult prNewName = _ed.GetString(psoName);
                                if (prNewName.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(prNewName.StringResult))
                                { tr.Abort(); return false; }
                                blockName = SanitizeBlockName(prNewName.StringResult);
                                if (bt.Has(blockName)) {
                                    _ed.WriteMessage($"\nNew name '{blockName}' also exists. Aborting.");
                                    tr.Abort(); return false;
                                }
                                break;
                            case "Cancel":
                            default:
                                tr.Abort(); return false;
                        }
                    }

                    ObjectId btrId;
                    BlockTableRecord btr;

                    if (bt.Has(blockName)) // If trying to overwrite
                    {
                        bt.UpgradeOpen();
                        btrId = bt[blockName];
                        btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForWrite);
                        // Clear existing geometry and attribute definitions from btr
                        List<ObjectId> toErase = new List<ObjectId>();
                        foreach(ObjectId idInBtr in btr) { toErase.Add(idInBtr); }
                        foreach(ObjectId idToErase in toErase)
                        {
                            DBObject objToErase = tr.GetObject(idToErase, OpenMode.ForWrite);
                            objToErase.Erase();
                        }
                        _ed.WriteMessage($"\nCleared existing block '{blockName}' for redefinition.");
                    }
                    else
                    {
                        bt.UpgradeOpen();
                        btr = new BlockTableRecord();
                        btr.Name = blockName;
                        btr.Origin = Point3d.Origin; // Base point of entities will be relative to this
                        btrId = bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);
                    }


                    // 4. Clone entities into Block Definition, offset relative to base point
                    string layerName = LayerUtils.GetOrCreateLayer(_db, type.ToString()); // Pass component type string
                    ObjectId componentLayerId = LayerUtils.GetOrCreateLayerId(tr, _db, layerName);

                    Matrix3d transform = Matrix3d.Displacement(Point3d.Origin - basePointWcs);
                    List<Entity> originalEntitiesToReplace = new List<Entity>();

                    foreach (ObjectId id in selectedIds)
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                        Entity clonedEnt = (Entity)ent.Clone();
                        clonedEnt.TransformBy(transform);
                        clonedEnt.LayerId = componentLayerId; // Assign to L-RAIL-<COMPONENTTYPE> layer
                        btr.AppendEntity(clonedEnt);
                        tr.AddNewlyCreatedDBObject(clonedEnt, true);
                        originalEntitiesToReplace.Add(ent); // Keep track for replacement
                    }

                    // 5. Add AttributeDefinitions
                    double attDefYPos = 0; // Start Y position for attributes, adjust as needed
                    // Calculate a sensible text height and spacing based on block extents or default
                    // Extents3d blockExtents = btr.GeometricExtents; // Get after geometry is added
                    // double textHeight = blockExtents.MaxPoint.Y > 0 ? blockExtents.MaxPoint.Y * 0.05 : 1.0;
                    double textHeight = DetermineAttributeTextHeight(btr, tr); // Helper
                    attDefYPos = -textHeight; // Initial Y position below origin (example)

                    foreach (var attrKvp in attributes)
                    {
                        AttributeDefinition attDef = new AttributeDefinition();
                        attDef.Position = new Point3d(0, attDefYPos, 0); // Stack them or position thoughtfully
                        attDef.Tag = attrKvp.Key.ToUpperInvariant();
                        attDef.Prompt = $"Enter {attrKvp.Key}:";
                        attDef.TextString = attrKvp.Value;
                        attDef.Height = textHeight; 
                        attDef.LayerId = componentLayerId; // Attributes on the same layer
                        
                        // Make certain attributes invisible if desired
                        if (attrKvp.Key.Equals("USER_ATTRIBUTE_1") || attrKvp.Key.Equals("USER_ATTRIBUTE_2") || attrKvp.Key.Equals("SPECIAL_NOTES"))
                        {
                            attDef.Invisible = true;
                        }

                        btr.AppendEntity(attDef);
                        tr.AddNewlyCreatedDBObject(attDef, true);
                        attDefYPos -= (textHeight * 1.2); // Move down for next attribute
                    }

                    // 6. Replace original entities with BlockReference
                    if (originalEntitiesToReplace.Any())
                    {
                        BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        BlockReference blkRef = new BlockReference(basePointWcs, btrId);
                        blkRef.Layer = LayerUtils.GetOrCreateLayer(_db, type.ToString() + "_REF"); // Optional: Refs on different sub-layer

                        // Add attribute references
                        foreach (ObjectId objIdInBtr in btr)
                        {
                            if (objIdInBtr.ObjectClass.DxfName == "ATTDEF")
                            {
                                AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(objIdInBtr, OpenMode.ForRead);
                                AttributeReference attRef = new AttributeReference();
                                attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                                attRef.TextString = attDef.TextString; // Set value from definition default
                                blkRef.AttributeCollection.AppendAttribute(attRef);
                                tr.AddNewlyCreatedDBObject(attRef, true);
                            }
                        }
                        modelSpace.AppendEntity(blkRef);
                        tr.AddNewlyCreatedDBObject(blkRef, true);

                        // Delete originals
                        foreach (Entity entToDel in originalEntitiesToReplace)
                        {
                            // Ent might already have been opened for read. Need for write.
                            Entity entWritable = (Entity)tr.GetObject(entToDel.ObjectId, OpenMode.ForWrite);
                            entWritable.Erase();
                        }
                    }
                    
                    DefinedComponents.Add(new ComponentDefinitionData
                    {
                        Name = partName, Type = type, BlockDefinitionId = btrId, BlockName = blockName, Attributes = attributes
                    });

                    tr.Commit();
                    _ed.WriteMessage($"\nComponent '{partName}' ({type}) defined as block '{blockName}' and instance placed.");
                    return true;
                }
                catch (System.Exception ex)
                {
                    ErrorLogger.Log($"Error defining component {type}: {ex.Message}\n{ex.StackTrace}");
                    _ed.WriteMessage($"\nError defining component: {ex.Message}");
                    tr.Abort();
                    return false;
                }
            }
        }
        
        private double DetermineAttributeTextHeight(BlockTableRecord btr, Transaction tr)
        {
            Extents3d extents = new Extents3d(Point3d.Origin, Point3d.Origin);
            bool hasGeometry = false;
            foreach (ObjectId id in btr)
            {
                DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                if (obj is Entity ent && !(obj is AttributeDefinition))
                {
                    if (!hasGeometry)
                    {
                        extents = ent.GeometricExtents;
                        hasGeometry = true;
                    }
                    else
                    {
                        extents.AddExtents(ent.GeometricExtents);
                    }
                }
            }

            if (hasGeometry && extents.MinPoint != extents.MaxPoint)
            {
                double maxDim = Math.Max(extents.MaxPoint.X - extents.MinPoint.X, extents.MaxPoint.Y - extents.MinPoint.Y);
                return Math.Max(0.25, maxDim * 0.05); // 5% of max dimension, or 0.25 minimum
            }
            return 1.0; // Default if no geometry or point-like
        }

        public static string SanitizeBlockName(string name)
        {
            // Replace invalid characters for block names.
            // AutoCAD invalid block name chars: < > / \ " : ; ? * | = `
            string invalidChars = "<>/\\\":;?*|=`";
            string sanitizedName = name;
            foreach (char c in invalidChars)
            {
                sanitizedName = sanitizedName.Replace(c, '_');
            }
            return sanitizedName.Replace(" ", "_"); // Replace spaces
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

4. AttributeForm.cs (New - WinForms Dialog)
This requires more extensive UI code (designer + code-behind). Below is a conceptual structure.

      
// RailDesigner1.UI.AttributeForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace RailDesigner1.UI
{
    public partial class AttributeForm : Form
    {
        public Dictionary<string, string> Attributes { get; private set; }
        private ComponentType _componentType;

        private TextBox txtPartName;
        private TextBox txtDescription;
        private TextBox txtMaterial;
        private TextBox txtFinish;
        private TextBox txtWeightDensity;
        private TextBox txtStockLength;
        private TextBox txtSpecialNotes;
        private TextBox txtUserAttribute1;
        private TextBox txtUserAttribute2;
        private TextBox txtRailingHeight; // General Railing Height attribute for the component itself if needed
        
        // Dynamic fields
        private TextBox txtComponentHeight; // e.g., For Pickets ("HEIGHT")
        private Label lblComponentHeight;
        private TextBox txtComponentWidth;  // e.g., For Rails ("WIDTH")
        private Label lblComponentWidth;

        public AttributeForm(ComponentType componentType)
        {
            _componentType = componentType;
            Attributes = new Dictionary<string, string>();
            InitializeComponent();
            CustomizeFieldsForComponentType();
        }

        private void InitializeComponent()
        {
            this.Text = $"Define Attributes for {_componentType}";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 450;
            this.SuspendLayout();

            int currentTop = 10;
            int labelWidth = 120;
            int inputLeft = 130;
            int inputWidth = 280;
            int spacing = 28;

            // Standard Fields
            txtPartName = AddRow("Part Name*:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtDescription = AddRow("Description:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtMaterial = AddRow("Material:", "Aluminum", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtFinish = AddRow("Finish:", "Mill Finish", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtWeightDensity = AddRow("Weight Density:", "0.0975", ref currentTop, labelWidth, inputLeft, inputWidth, spacing); // e.g. lbs/in^3 or lbs/in
            txtStockLength = AddRow("Stock Length:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtRailingHeight = AddRow("Railing Height:", "36", ref currentTop, labelWidth, inputLeft, inputWidth, spacing); // For consistency with PRD attributes

            // Component-specific dynamic fields (initially hidden or managed by CustomizeFields)
            lblComponentHeight = new Label { Text = "Picket Height*:", Top = currentTop + 3, Left = 10, Width = labelWidth, Visible = false };
            txtComponentHeight = new TextBox { Top = currentTop, Left = inputLeft, Width = inputWidth, Visible = false };
            this.Controls.Add(lblComponentHeight);
            this.Controls.Add(txtComponentHeight);
            
            lblComponentWidth = new Label { Text = "Rail Width*:", Top = currentTop + spacing + 3, Left = 10, Width = labelWidth, Visible = false };
            txtComponentWidth = new TextBox { Top = currentTop + spacing, Left = inputLeft, Width = inputWidth, Visible = false };
            this.Controls.Add(lblComponentWidth);
            this.Controls.Add(txtComponentWidth);
            currentTop += spacing * 2; // Reserve space for potentially two dynamic fields

            txtSpecialNotes = AddRow("Special Notes:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtUserAttribute1 = AddRow("User Attr 1:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtUserAttribute2 = AddRow("User Attr 2:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);

            Button btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = inputLeft, Top = currentTop + 10, Width = 80 };
            Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = inputLeft + 90, Top = currentTop + 10, Width = 80 };
            btnOk.Click += BtnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            this.Height = currentTop + 80;
            this.ResumeLayout(false);
        }

        private TextBox AddRow(string labelText, string defaultValue, ref int currentTop, int labelWidth, int inputLeft, int inputWidth, int spacing)
        {
            Label lbl = new Label { Text = labelText, Top = currentTop + 3, Left = 10, Width = labelWidth };
            TextBox txt = new TextBox { Top = currentTop, Left = inputLeft, Width = inputWidth, Text = defaultValue };
            this.Controls.Add(lbl);
            this.Controls.Add(txt);
            currentTop += spacing;
            return txt;
        }
        
        private void CustomizeFieldsForComponentType()
        {
            // Adjust visibility and labels based on component type
            if (_componentType == ComponentType.Picket)
            {
                lblComponentHeight.Text = "Picket Height*:"; // Specific attribute "HEIGHT" for Pickets
                lblComponentHeight.Visible = true;
                txtComponentHeight.Visible = true;
            }
            else if (_componentType == ComponentType.TopCap || 
                     _componentType == ComponentType.BottomRail || 
                     _componentType == ComponentType.IntermediateRail)
            {
                lblComponentWidth.Text = "Rail Width*:"; // Specific attribute "WIDTH" for Rails
                lblComponentWidth.Visible = true;
                txtComponentWidth.Visible = true;
            }
            // Add other customizations if needed
        }


        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                this.DialogResult = DialogResult.None; // Keep form open
                return;
            }

            Attributes["PARTNAME"] = txtPartName.Text.Trim();
            Attributes["DESCRIPTION"] = txtDescription.Text.Trim();
            Attributes["MATERIAL"] = txtMaterial.Text.Trim();
            Attributes["FINISH"] = txtFinish.Text.Trim();
            Attributes["WEIGHT_DENSITY"] = txtWeightDensity.Text.Trim();
            Attributes["STOCK_LENGTH"] = txtStockLength.Text.Trim();
            Attributes["SPECIAL_NOTES"] = txtSpecialNotes.Text.Trim();
            Attributes["USER_ATTRIBUTE_1"] = txtUserAttribute1.Text.Trim();
            Attributes["USER_ATTRIBUTE_2"] = txtUserAttribute2.Text.Trim();
            Attributes["RAILINGHEIGHT"] = txtRailingHeight.Text.Trim(); // PRD mentioned "RailingHeight" as a general attribute

            // Handle dynamic fields based on type
            if (_componentType == ComponentType.Picket && txtComponentHeight.Visible)
            {
                Attributes["HEIGHT"] = txtComponentHeight.Text.Trim(); // "HEIGHT" for Picket PRD
            }
            if ((_componentType == ComponentType.TopCap || _componentType == ComponentType.BottomRail || _componentType == ComponentType.IntermediateRail) && txtComponentWidth.Visible)
            {
                Attributes["WIDTH"] = txtComponentWidth.Text.Trim(); // "WIDTH" for Rail PRD
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtPartName.Text))
            {
                MessageBox.Show("Part Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPartName.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtWeightDensity.Text) && !double.TryParse(txtWeightDensity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Weight Density must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtWeightDensity.Focus();
                return false;
            }
            // Validate RailingHeight as number
            if (!string.IsNullOrWhiteSpace(txtRailingHeight.Text) && !double.TryParse(txtRailingHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Railing Height must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRailingHeight.Focus();
                return false;
            }
            
            // Validate PICKET_HEIGHT if visible and Picket
            if (_componentType == ComponentType.Picket && txtComponentHeight.Visible)
            {
                if (string.IsNullOrWhiteSpace(txtComponentHeight.Text) || !double.TryParse(txtComponentHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <=0)
                {
                    MessageBox.Show("Picket Height must be a valid positive number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtComponentHeight.Focus();
                    return false;
                }
            }

            // Validate RAIL_WIDTH if visible and a Rail type
            if ((_componentType == ComponentType.TopCap || _componentType == ComponentType.BottomRail || _componentType == ComponentType.IntermediateRail) && txtComponentWidth.Visible)
            {
                 if (string.IsNullOrWhiteSpace(txtComponentWidth.Text) || !double.TryParse(txtComponentWidth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <=0)
                {
                    MessageBox.Show("Rail Width must be a valid positive number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtComponentWidth.Focus();
                    return false;
                }
            }


            // Add more validation as needed (e.g., stock length positive)
             if (!string.IsNullOrWhiteSpace(txtStockLength.Text))
            {
                if(!double.TryParse(txtStockLength.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double sl) || sl <=0)
                {
                     MessageBox.Show("Stock Length must be a positive number if specified.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtStockLength.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

5. PostPlacementLogicWrapper.cs (New/Replaces existing)

      
// RailDesigner1.Placement.PostPlacementLogicWrapper.cs
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Defintions; // For ComponentDefinitionData
using System.Collections.Generic;
using System.Linq;
using RailCreator; // Namespace for the original PostGenerator
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace RailDesigner1.Placement
{
    public class PlacementInfo // Can be moved to a Models.cs file
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
}

// Helper for Dictionary
public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

6. PicketPlacementLogicWrapper.cs (New/Replaces existing)

      
// RailDesigner1.Placement.PicketPlacementLogicWrapper.cs
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Defintions;
using System;
using System.Collections.Generic;
using System.Globalization;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
// NOTE: We are NOT calling RailCreator.PicketGenerator.PlacePickets because it draws and doesn't return points.
// Instead, we re-implement its SPACING LOGIC here as per "retrieving calculated positions and orientations" requirement.

namespace RailDesigner1.Placement
{
    public class PicketPlacementLogicWrapper
    {
        public List<PlacementInfo> CalculatePlacements(
            Polyline pathPolyline,
            RailingDesign designParams, /* RailDesigner1.RailingDesign */
            ComponentDefinitionData picketDefinition,
            List<Point3d> postPositionsWcs, // World CS post positions
            ComponentDefinitionData postDefinition, // To get post width
            Transaction tr, // Needed if RailCreator.PicketGenerator were called
            Database db    // Needed if RailCreator.PicketGenerator were called
            )
        {
            var placements = new List<PlacementInfo>();
            if (pathPolyline == null || picketDefinition == null || postPositionsWcs.Count < 2 || postDefinition == null)
            {
                return placements;
            }

            // Get Picket Width from its definition attributes
            // PicketGenerator.cs from prompt expects PicketSize as "WIDTHxDEPTH" or "WIDTH ROUND" string
            // PicketGenerator also calculates picketWidth by parsing PicketSize.
            // Here, our picketDefinition is a block, it has WIDTH attribute
            double picketWidth = 0.75; // Default
            if (picketDefinition.Attributes.TryGetValue("WIDTH", out string picketWidthStr))
            {
                if (!double.TryParse(picketWidthStr, NumberStyles.Any, CultureInfo.InvariantCulture, out picketWidth) || picketWidth <= 0)
                {
                    AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nWarning: Invalid Picket WIDTH attribute ('{picketWidthStr}'). Using default {0.75}.");
                    picketWidth = 0.75;
                }
            }
            else
            {
                 AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nWarning: Picket WIDTH attribute missing. Using default {picketWidth}.");
            }

            // Get Post Width from its definition attributes
            double postWidth = 2.0; // Default
             if (postDefinition.Attributes.TryGetValue("WIDTH", out string postWidthStr))
            {
                if (!double.TryParse(postWidthStr, NumberStyles.Any, CultureInfo.InvariantCulture, out postWidth) || postWidth <= 0)
                {
                     AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nWarning: Invalid Post WIDTH attribute ('{postWidthStr}'). Using default {2.0}.");
                    postWidth = 2.0;
                }
            } else {
                 AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nWarning: Post WIDTH attribute missing. Using default {postWidth}.");
            }


            // Core logic adapted from RailCreator.PicketGenerator.PlacePickets's vertical picket spacing
            for (int i = 0; i < postPositionsWcs.Count - 1; i++)
            {
                Point3d startPostPtWcs = postPositionsWcs[i];
                Point3d endPostPtWcs = postPositionsWcs[i + 1];

                // This logic assumes posts are oriented perpendicular to the rail path segment.
                // And postWidth is the dimension along the railing path.
                double segmentLength = startPostPtWcs.DistanceTo(endPostPtWcs);
                double insideDistance = segmentLength - postWidth; // Assuming postWidth is taken equally from both ends of segment measured center-to-center

                if (insideDistance <= picketWidth) // Not enough space for even one picket
                {
                    continue;
                }

                double maxClearSpacing = 4.0; // As per original logic
                int numPickets = 1;
                double clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1.0);
                
                // Find optimal number of pickets
                while (clearSpacing >= maxClearSpacing && numPickets < (int)(insideDistance / picketWidth)) // Ensure we don't have too many
                {
                    numPickets++;
                    clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1.0);
                }
                // One last check/adjustment if spacing is still too large.
                if (clearSpacing >= maxClearSpacing && numPickets * picketWidth < insideDistance ) // if still too large, add one more if possible
                {
                     numPickets++;
                     clearSpacing = (insideDistance - numPickets * picketWidth) / (numPickets + 1.0);
                }


                if(clearSpacing < 0) // if calculation results in negative spacing, something is wrong or not enough space
                {
                    // maybe only place one picket if space permits?
                    if (insideDistance >= picketWidth) {
                        numPickets = 1;
                        clearSpacing = (insideDistance - picketWidth) / 2.0;
                    } else {
                        numPickets = 0; // no pickets
                    }
                }


                double onCenterSpacing = clearSpacing + picketWidth;
                
                // Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
                // ed.WriteMessage($"\nSegment {i}: Posts {startPostPtWcs} to {endPostPtWcs}. Length={segmentLength:F2}\", Inside Dist={insideDistance:F2}\", Pickets={numPickets}, Clear Space={clearSpacing:F4}\", O.C. Spacing={onCenterSpacing:F4}\"");


                Vector3d segmentVector = (endPostPtWcs - startPostPtWcs).GetNormal();

                // Place pickets along the segment (defined by post centers)
                for (int j = 0; j < numPickets; j++)
                {
                    // Distance from the effective start of the picket area (i.e., inside face of start post)
                    double distFromInsidePostFace = clearSpacing + (j * onCenterSpacing) + (picketWidth / 2.0);
                    // Distance from the center of the start post along the segment vector
                    double distFromStartPostCenter = (postWidth / 2.0) + distFromInsidePostFace;
                    
                    Point3d picketPositionWcs = startPostPtWcs + (segmentVector * distFromStartPostCenter);

                    // Orientation for picket - typically perpendicular to segment, or aligned with polyline tangent
                    Vector3d orientation = Vector3d.XAxis;
                    try
                    {
                        // It's better to get tangent from the original polyline, not the straight segment between posts, if posts follow curves.
                        double param = pathPolyline.GetParameterAtPoint(pathPolyline.GetClosestPointTo(picketPositionWcs, false));
                        Vector3d tangent = pathPolyline.GetFirstDerivative(param);
                        if (!tangent.IsZeroLength())
                        {
                            orientation = tangent.GetNormal();
                        }
                    }
                    catch(System.Exception ex)
                    {
                        // If error getting tangent (e.g. at polyline vertex), use segment vector as fallback for orientation normal.
                        // This typically means orientation for rotation (around Z axis) will be angle of segmentVector.
                        orientation = segmentVector;
                        AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError getting polyline tangent for picket: {ex.Message}. Using segment direction.");
                    }
                    

                    placements.Add(new PlacementInfo
                    {
                        Type = ComponentType.Picket,
                        BlockDefinitionId = picketDefinition.BlockDefinitionId,
                        Position = picketPositionWcs,
                        Orientation = orientation, // The orientation for picket block might be perpendicular or parallel to this
                        Attributes = picketDefinition.Attributes
                    });
                }
            }
            return placements;
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

7. RailingGeometryGenerator.cs (Significant modifications from stub)

      
// RailDesigner1.Geometry.RailingGeometryGenerator.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Defintions;
using RailDesigner1.Placement;
using RailDesigner1.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RailDesigner1.Geometry
{
    public class RailingGeometryGenerator
    {
        private Database _db;
        private Editor _ed;

        public RailingGeometryGenerator(Database db, Editor ed)
        {
            _db = db;
            _ed = ed;
        }

        public void Generate(Transaction tr, Polyline pathPolyline,
                             List<PlacementInfo> placements,
                             List<ComponentDefinitionData> componentDefinitions,
                             RailingDesign design)
        {
            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(_db.CurrentSpaceId, OpenMode.ForWrite);

            // Insert Block-based components (Posts, Pickets, Mounting)
            foreach (var placement in placements.Where(p => p.BlockDefinitionId != ObjectId.Null))
            {
                BlockReference br = new BlockReference(placement.Position, placement.BlockDefinitionId);
                
                // Calculate rotation angle from orientation vector (tangent to path)
                // Assuming Block is defined with X-axis along its length
                double angle = 0;
                if (placement.Orientation.Length > Tolerance.Global.EqualPoint && !placement.Orientation.IsParallelTo(Vector3d.ZAxis))
                {
                     angle = Math.Atan2(placement.Orientation.Y, placement.Orientation.X);
                }
                br.Rotation = angle;
                
                ComponentDefinitionData defData = componentDefinitions.FirstOrDefault(d => d.BlockDefinitionId == placement.BlockDefinitionId);
                string layerName = LayerUtils.GetOrCreateLayer(_db, placement.Type.ToString());
                br.Layer = layerName;

                // Populate Attributes
                if (defData != null && defData.Attributes != null)
                {
                    BlockTableRecord blockDefBtr = (BlockTableRecord)tr.GetObject(placement.BlockDefinitionId, OpenMode.ForRead);
                    foreach (ObjectId objIdInBtr in blockDefBtr)
                    {
                        if (objIdInBtr.ObjectClass.DxfName == "ATTDEF")
                        {
                            AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(objIdInBtr, OpenMode.ForRead);
                            AttributeReference attRef = new AttributeReference();
                            attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                            
                            // Set value from definition's defaults or overrides
                            if (placement.Attributes != null && placement.Attributes.TryGetValue(attDef.Tag.ToUpperInvariant(), out string overrideValue))
                            {
                                attRef.TextString = overrideValue;
                            }
                            else if (defData.Attributes.TryGetValue(attDef.Tag.ToUpperInvariant(), out string defValue))
                            {
                                attRef.TextString = defValue;
                            }
                            else {
                                attRef.TextString = attDef.TextString; // Fallback to AttDef's own default
                            }
                            
                            br.AttributeCollection.AppendAttribute(attRef);
                            tr.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }
                modelSpace.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
            }
            _ed.WriteMessage($"\nPlaced {placements.Count(p => p.BlockDefinitionId != ObjectId.Null)} block references.");


            // Generate Rail-type components (TopCap, BottomRail, IntermediateRail) as Polylines with XData
            GenerateRailPolyline(tr, modelSpace, pathPolyline, ComponentType.TopCap, componentDefinitions, design);
            GenerateRailPolyline(tr, modelSpace, pathPolyline, ComponentType.BottomRail, componentDefinitions, design);
            // GenerateRailPolyline(tr, modelSpace, pathPolyline, ComponentType.IntermediateRail, componentDefinitions, design); // If distinct IR
        }

        private void GenerateRailPolyline(Transaction tr, BlockTableRecord modelSpace, Polyline pathPolyline,
                                         ComponentType railType, List<ComponentDefinitionData> componentDefinitions,
                                         RailingDesign design)
        {
            ComponentDefinitionData railDef = componentDefinitions.FirstOrDefault(c => c.Type == railType);
            if (railDef == null)
            {
                _ed.WriteMessage($"\n{railType} component not defined. Skipping generation.");
                return;
            }

            Polyline railPoly = (Polyline)pathPolyline.Clone(); // Clone base path
            
            // Example: Offset vertically for TopCap/BottomRail
            // This is a simplification. True rail geometry might be more complex (profile sweep).
            double verticalOffset = 0;
            switch (railType)
            {
                case ComponentType.TopCap:
                    // Assuming RailingHeight from design is to top of TopCap.
                    // RailingHeight from railDef attribute "RAILINGHEIGHT" or design param "design.RailHeight"
                    double railHeightTop = design.RailHeight;
                    if(railDef.Attributes.TryGetValue("RAILINGHEIGHT", out string rhStr) && double.TryParse(rhStr, out double rh)) railHeightTop = rh;
                    verticalOffset = railHeightTop;
                    // If TopCap definition itself has a Height/Thickness, that should be considered for the offset point.
                    // For now, offset polyline origin.
                    break;
                case ComponentType.BottomRail:
                    // Assuming BottomClearance is defined in RailingDesign.
                    // This offset should be to the *bottom* of the BottomRail.
                    verticalOffset = design.BottomClearance; 
                    // If BottomRail block has a "HEIGHT" attribute, then true Z offset is design.BottomClearance + BottomRail.HEIGHT/2 (if centered)
                    break;
                // Add IntermediateRail logic if needed, potentially multiple based on RailingDesign settings
            }

            if(Math.Abs(verticalOffset) > Tolerance.Global.EqualPoint)
            {
                 Matrix3d transform = Matrix3d.Displacement(new Vector3d(0, 0, verticalOffset));
                 railPoly.TransformBy(transform);
            }

            string layerName = LayerUtils.GetOrCreateLayer(_db, railType.ToString());
            railPoly.Layer = layerName;

            // Attach XData
            ResultBuffer rb = new ResultBuffer();
            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, "RAILDESIGNER1_RAILDATA"));
            
            foreach (var attr in railDef.Attributes)
            {
                // Example XData storage. Adjust types as necessary (Real, Int, etc.)
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"{attr.Key.ToUpperInvariant()}={attr.Value}"));
            }
            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"INSTALLED_LENGTH={railPoly.Length.ToString(CultureInfo.InvariantCulture)}"));
            // Add specific rail type attributes if not already in railDef.Attributes, e.g. COMPONENTTYPE from railType enum string
             rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"COMPONENTTYPE={railType.ToString().ToUpperInvariant()}"));


            railPoly.XData = rb;
            modelSpace.AppendEntity(railPoly);
            tr.AddNewlyCreatedDBObject(railPoly, true);
            _ed.WriteMessage($"\nGenerated {railType} polyline.");
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

8. BomExporter.cs

      
// RailDesigner1.BomExporter.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using RailDesigner1.Defintions; // For ComponentDefinitionData
using RailDesigner1.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace RailDesigner1
{
    public class BomEntry
    {
        public string ComponentType { get; set; }
        public string PartName { get; set; }
        public string Description { get; set; }
        public double Quantity { get; set; } // Double for lengths, int for counts
        public string InstalledLength { get; set; } // String to handle "N/A" or value
        public string InstalledHeight { get; set; } // From block attribute "HEIGHT" or picket-specific
        public string Width { get; set; }           // From block attribute "WIDTH"
        public string HeightAttribute { get; set; } // The attribute "HEIGHT" from definition for pickets/posts
        public string Material { get; set; }
        public string Finish { get; set; }
        public string Weight { get; set; } // Calculated, formatted string
        public string SpecialNotes { get; set; }
        public string UserAttribute1 { get; set; }
        public string UserAttribute2 { get; set; }
        public double RawWeightValue { get; set; } // For summing before formatting
    }

    public class BomExporter
    {
        private Document _doc;
        private Database _db;
        private Editor _ed;

        public BomExporter()
        {
            _doc = AcApp.DocumentManager.MdiActiveDocument;
            if (_doc == null) throw new InvalidOperationException("No active document.");
            _db = _doc.Database;
            _ed = _doc.Editor;
        }

        public void ExportBom()
        {
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect generated railing entities (Block References and Polylines) for BOM: ";
            // No specific filter here, will filter by COMPONENTTYPE attribute / XData later
            PromptSelectionResult psr = _ed.GetSelection(pso);

            if (psr.Status != PromptStatus.OK)
            {
                _ed.WriteMessage("\nSelection cancelled. BOM export aborted.");
                return;
            }

            List<BomEntry> bomItems = new List<BomEntry>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in psr.Value)
                {
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    BomEntry entry = null;
                    if (ent is BlockReference br)
                    {
                        entry = ExtractDataFromBlockReference(br, tr);
                    }
                    else if (ent is Polyline pl) // Polyline, Polyline2d, Polyline3d
                    {
                        entry = ExtractDataFromPolyline(pl, tr);
                    }
                    // else if (ent is Polyline2d p2d) { entry = ExtractDataFromPolyline(p2d, tr); } // etc.

                    if (entry != null)
                    {
                        bomItems.Add(entry);
                    }
                }
                tr.Commit(); // Or Abort for read-only
            }

            if (!bomItems.Any())
            {
                _ed.WriteMessage("\nNo relevant railing entities found in selection. BOM not generated.");
                return;
            }

            // Aggregate quantities
            var aggregatedBom = bomItems
                .GroupBy(item => new { item.PartName, item.ComponentType }) // Group by PartName & Type for aggregation
                .Select(g => new BomEntry
                {
                    ComponentType = g.Key.ComponentType,
                    PartName = g.Key.PartName,
                    Description = g.First().Description, // Assume description is consistent
                    Quantity = g.Sum(i => i.Quantity),   // Sum quantities for counts, lengths handled by summing RawWeightValue
                    InstalledLength = g.Any(i => !string.IsNullOrEmpty(i.InstalledLength) && i.InstalledLength != "N/A") ? g.Sum(i => double.TryParse(i.InstalledLength, out double l) ? l : 0).ToString("F2") : "N/A",
                    InstalledHeight = g.First().InstalledHeight, // Assumed consistent or not aggregatable beyond display
                    Width = g.First().Width,
                    HeightAttribute = g.First().HeightAttribute,
                    Material = g.First().Material,
                    Finish = g.First().Finish,
                    RawWeightValue = g.Sum(i => i.RawWeightValue),
                    SpecialNotes = g.First().SpecialNotes, // Show one example or concatenate
                    UserAttribute1 = g.First().UserAttribute1,
                    UserAttribute2 = g.First().UserAttribute2
                }).ToList();
            
            foreach(var item in aggregatedBom) // Set formatted weight after aggregation
            {
                item.Weight = item.RawWeightValue > 0 ? item.RawWeightValue.ToString("F2") : "N/A";
            }


            WriteCsv(aggregatedBom);
        }

        private BomEntry ExtractDataFromBlockReference(BlockReference br, Transaction tr)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (br.AttributeCollection.Count > 0)
            {
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef != null)
                    {
                        attributes[attRef.Tag.ToUpperInvariant()] = attRef.TextString;
                    }
                }
            }
            else // Try to get attributes from block definition if instance has none (should not happen for well-defined blocks)
            {
                BlockTableRecord btrDef = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                 if (btrDef.HasAttributeDefinitions)
                {
                    foreach (ObjectId defId in btrDef)
                    {
                        if (defId.ObjectClass.DxfName == "ATTDEF")
                        {
                            AttributeDefinition attDef = tr.GetObject(defId, OpenMode.ForRead) as AttributeDefinition;
                            if (attDef != null)
                            {
                                attributes[attDef.Tag.ToUpperInvariant()] = attDef.TextString; // Use default from definition
                            }
                        }
                    }
                }
            }


            if (!attributes.TryGetValue("COMPONENTTYPE", out string compTypeStr) ||
                !attributes.TryGetValue("PARTNAME", out string partNameStr))
            {
                return null; // Not a valid component block for BOM
            }

            BomEntry entry = new BomEntry
            {
                ComponentType = compTypeStr,
                PartName = partNameStr,
                Description = attributes.GetValueOrDefault("DESCRIPTION", ""),
                Quantity = 1, // Each block reference is one item, aggregation handles total
                InstalledLength = "N/A", // Typically not for individual blocks unless it's a special attribute
                InstalledHeight = attributes.GetValueOrDefault("RAILINGHEIGHT", attributes.GetValueOrDefault("HEIGHT","N/A")), // Prefer specific "HEIGHT" attribute for Picket/Post actual dimension
                Width = attributes.GetValueOrDefault("WIDTH", "N/A"),
                HeightAttribute = attributes.GetValueOrDefault("HEIGHT", "N/A"),
                Material = attributes.GetValueOrDefault("MATERIAL", ""),
                Finish = attributes.GetValueOrDefault("FINISH", ""),
                SpecialNotes = attributes.GetValueOrDefault("SPECIAL_NOTES", ""),
                UserAttribute1 = attributes.GetValueOrDefault("USER_ATTRIBUTE_1", ""),
                UserAttribute2 = attributes.GetValueOrDefault("USER_ATTRIBUTE_2", "")
            };
            
            // Weight Calculation
            // Assume WEIGHT_DENSITY is weight per item for blocks OR weight per unit length if stock_length implies linear
            // If HEIGHT attribute exists (for pickets/posts), use it.
            // If component implies length (e.g. custom defined rail segment block), might have LENGTH attribute.
            double weightDensity = 0;
            double.TryParse(attributes.GetValueOrDefault("WEIGHT_DENSITY", "0"), NumberStyles.Any, CultureInfo.InvariantCulture, out weightDensity);
            
            double componentHeightForWeight = 0;
            // "HEIGHT" attr represents actual vertical dimension of the post/picket from its own definition
            if (double.TryParse(entry.HeightAttribute, NumberStyles.Any, CultureInfo.InvariantCulture, out componentHeightForWeight) && componentHeightForWeight > 0)
            {
                 // Assume WEIGHT_DENSITY for pickets/posts could be per unit of this "HEIGHT" attribute
                 // Or, WEIGHT_DENSITY is simply weight per piece, then componentHeightForWeight is not used directly here unless density is volumetric.
                 // If WEIGHT_DENSITY is flat weight per item:
                 entry.RawWeightValue = weightDensity * entry.Quantity; 
            }
            else // If no height, or density isn't per unit height
            {
                entry.RawWeightValue = weightDensity * entry.Quantity; // Assumes density is per item
            }


            return entry;
        }

        private BomEntry ExtractDataFromPolyline(Polyline pl, Transaction tr)
        {
            ResultBuffer xdata = pl.GetXData("RAILDESIGNER1_RAILDATA");
            if (xdata == null) return null;

            Dictionary<string, string> xdataMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (TypedValue tv in xdata.AsArray().Skip(1)) // Skip RegAppName
            {
                if (tv.TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                {
                    string[] parts = tv.Value.ToString().Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        xdataMap[parts[0]] = parts[1];
                    }
                }
            }

            if (!xdataMap.TryGetValue("COMPONENTTYPE", out string compTypeStr) ||
                !xdataMap.TryGetValue("PARTNAME", out string partNameStr))
            {
                return null; // Not a valid rail polyline for BOM
            }

            BomEntry entry = new BomEntry
            {
                ComponentType = compTypeStr,
                PartName = partNameStr,
                Description = xdataMap.GetValueOrDefault("DESCRIPTION", ""),
                Quantity = pl.Length, // For polylines (rails), quantity is their length for BOM summing
                InstalledLength = pl.Length.ToString("F2", CultureInfo.InvariantCulture),
                InstalledHeight = xdataMap.GetValueOrDefault("RAILINGHEIGHT", "N/A"), // Rails usually don't have varying height
                Width = xdataMap.GetValueOrDefault("WIDTH", "N/A"), // Rails usually have a profile width
                HeightAttribute = "N/A",
                Material = xdataMap.GetValueOrDefault("MATERIAL", ""),
                Finish = xdataMap.GetValueOrDefault("FINISH", ""),
                SpecialNotes = xdataMap.GetValueOrDefault("SPECIAL_NOTES", ""),
                UserAttribute1 = xdataMap.GetValueOrDefault("USER_ATTRIBUTE_1", ""),
                UserAttribute2 = xdataMap.GetValueOrDefault("USER_ATTRIBUTE_2", "")
            };
            
            // Weight calculation for Polylines (Rails)
            // Assume WEIGHT_DENSITY in XData is weight per unit length
            double weightDensity = 0;
            double.TryParse(xdataMap.GetValueOrDefault("WEIGHT_DENSITY", "0"), NumberStyles.Any, CultureInfo.InvariantCulture, out weightDensity);
            entry.RawWeightValue = weightDensity * pl.Length;

            return entry;
        }

        private void WriteCsv(List<BomEntry> bomItems)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, $"RailingBOM_{timestamp}.csv");

            try
            {
                StringBuilder sb = new StringBuilder();
                // Header
                sb.AppendLine("COMPONENTTYPE,PARTNAME,DESCRIPTION,QUANTITY,INSTALLED_LENGTH,INSTALLED_HEIGHT,WIDTH,HEIGHT,MATERIAL,FINISH,WEIGHT,SPECIAL_NOTES,USER_ATTRIBUTE_1,USER_ATTRIBUTE_2");

                foreach (var item in bomItems)
                {
                    sb.AppendLine(string.Join(",",
                        Quote(item.ComponentType), Quote(item.PartName), Quote(item.Description),
                        (item.InstalledLength != "N/A" && item.InstalledLength != null) ? Quote(item.Quantity.ToString("F2")) : Quote(item.Quantity.ToString("F0")), // Quantity: length or count
                        Quote(item.InstalledLength), Quote(item.InstalledHeight),
                        Quote(item.Width), Quote(item.HeightAttribute), // "HEIGHT" column maps to HeightAttribute from component block
                        Quote(item.Material), Quote(item.Finish), Quote(item.Weight),
                        Quote(item.SpecialNotes), Quote(item.UserAttribute1), Quote(item.UserAttribute2)
                    ));
                }

                File.WriteAllText(filePath, sb.ToString());
                _ed.WriteMessage($"\nBOM exported successfully to: {filePath}");
            }
            catch (System.Exception ex)
            {
                ErrorLogger.Log($"BOM Export Error: {ex.Message}\n{ex.StackTrace}");
                _ed.WriteMessage($"\nError exporting BOM: {ex.Message} (See log for details)");
                 AcApp.ShowAlertDialog($"Error saving BOM CSV: {ex.Message}");
            }
        }

        private string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

9. LayerUtils.cs (New or merged into SappoUtilities)

      
// RailDesigner1.Utils.LayerUtils.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors; // For Color
using Autodesk.AutoCAD.EditorInput; // For Editor (optional, for messages)

namespace RailDesigner1.Utils
{
    public static class LayerUtils
    {
        // Returns layer name e.g. L-RAIL-POST
        public static string GetOrCreateLayer(Database db, string componentTypeString, short colorIndex = 7, string linetypeName = "Continuous")
        {
            string layerName = $"L-RAIL-{componentTypeString.ToUpperInvariant()}";
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
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

                        LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                        if (ltt.Has(linetypeName))
                        {
                            ltr.LinetypeObjectId = ltt[linetypeName];
                        }
                        else
                        {
                            // Try to load if not "Continuous" and missing
                            if(!linetypeName.Equals("Continuous", System.StringComparison.OrdinalIgnoreCase)) {
                                try { db.LoadLinetypeFile(linetypeName, "acad.lin"); } // Common linetype file
                                catch { ed?.WriteMessage($"\nWarning: Could not load linetype '{linetypeName}'. Using Continuous."); }
                                if(ltt.Has(linetypeName)) ltr.LinetypeObjectId = ltt[linetypeName];
                                else ltr.LinetypeObjectId = ltt["Continuous"]; // Fallback
                            } else {
                                 ltr.LinetypeObjectId = ltt["Continuous"]; // Should always exist
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
                    ErrorLogger.Log($"Error creating layer '{layerName}': {ex.Message}");
                    ed?.WriteMessage($"\nError creating layer '{layerName}': {ex.Message}");
                    tr.Abort();
                    return db.Clayer; // Return current layer name on failure
                }
            }
        }
        
        public static ObjectId GetOrCreateLayerId(Transaction tr, Database db, string layerName, short colorIndex = 7, string linetypeName = "Continuous")
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
            {
                return lt[layerName];
            }
            else
            {
                lt.UpgradeOpen(); // Downgrade handled by transaction commit/abort
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                };

                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (ltt.Has(linetypeName)) { ltr.LinetypeObjectId = ltt[linetypeName]; }
                else { ltr.LinetypeObjectId = db.Celtype; } // Fallback

                ObjectId layerId = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
                return layerId;
            }
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

10. Error Logger Utility ErrorLogger.cs (New)

      
// RailDesigner1.Utils.ErrorLogger.cs
using System;
using System.IO;

namespace RailDesigner1.Utils
{
    public static class ErrorLogger
    {
        private static string _logFilePath;
        private static StreamWriter _streamWriter;
        private static readonly object _lock = new object();

        public static void Initialize(string logFileName)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _logFilePath = Path.Combine(desktopPath, logFileName);
                // Open in append mode, create if not exists
                _streamWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                Log($"--- Log session started: {DateTime.Now} ---");
            }
            catch (Exception ex)
            {
                // Cannot log to file, maybe show alert dialog or write to command line
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
                // Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Logger init error: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            lock (_lock)
            {
                if (_streamWriter != null)
                {
                    try
                    {
                        _streamWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                    }
                    catch (Exception ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"Failed to write to log: {ex.Message}");
                    }
                }
                else {
                     System.Diagnostics.Debug.WriteLine($"LOG (stream writer null): {message}");
                }
            }
        }

        public static void Close()
        {
            lock (_lock)
            {
                if (_streamWriter != null)
                {
                    Log($"--- Log session ended: {DateTime.Now} ---");
                    _streamWriter.Flush();
                    _streamWriter.Close();
                    _streamWriter.Dispose();
                    _streamWriter = null;
                }
            }
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

MountPlacementLogic.cs
For MountPlacementLogic, as it's not deeply specified beyond being a component type, a simple approach is to place mounts at post locations if a "Mounting" component is defined.

      
// RailDesigner1.Placement.MountPlacementLogic.cs
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RailDesigner1.Defintions;
using System.Collections.Generic;
using System.Linq;

namespace RailDesigner1.Placement
{
    public class MountPlacementLogic
    {
        // This is a very basic implementation. More complex logic could be added.
        public List<PlacementInfo> CalculatePlacements(
            List<PlacementInfo> postPlacements, // Use the calculated post placements
            ComponentDefinitionData mountDefinition)
        {
            var mountInfos = new List<PlacementInfo>();
            if (mountDefinition == null || postPlacements == null || !postPlacements.Any())
            {
                return mountInfos;
            }

            foreach (var postInfo in postPlacements.Where(p => p.Type == ComponentType.Post))
            {
                mountInfos.Add(new PlacementInfo
                {
                    Type = ComponentType.Mounting,
                    BlockDefinitionId = mountDefinition.BlockDefinitionId,
                    Position = postInfo.Position, // Place mount at the same position as the post
                    Orientation = postInfo.Orientation, // Align with post orientation
                    Attributes = mountDefinition.Attributes
                });
            }
            return mountInfos;
        }
    }
}

    

IGNORE_WHEN_COPYING_START
Use code with caution. C#
IGNORE_WHEN_COPYING_END

This would be called in RailGenerateCommandHandler.cs similar to Post/Picket placement.

Final Notes on RailCreator_Roo Files:

    The key is that the PostPlacementLogicWrapper calls RailCreator.PostGenerator.CalculatePostPositions(). This Roo method, as per the user-supplied version in the prompt, returns List<Point3d> and its parameters include BlockTableRecord btr, Transaction tr. The wrapper needs to pass these, meaning RailCreator.PostGenerator will draw its simple posts. These are ignored by RailDesigner1 which places its own blocks.

    For pickets, the PicketPlacementLogicWrapper re-implements the spacing logic from RailCreator.PicketGenerator.PlacePickets to "retrieve calculated positions and orientations" without calling the PlacePickets method directly (which doesn't return what's needed and just draws). This adheres to "not modifying core logic" by adapting the algorithm not the file.

This set of C# class structures and key methods should provide a strong foundation to complete the plugin. Remember to fill in all TODOs, refine error handling, manage AutoCAD transactions carefully (using blocks), and test thoroughly.



