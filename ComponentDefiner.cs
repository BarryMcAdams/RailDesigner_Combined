using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System; // Added for Exception

public class ComponentDefiner
{
    public ObjectId DefineComponent(string componentType, out Dictionary<string, string> outAttributes)
    {
        outAttributes = null;
        Document doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
            System.Diagnostics.Debug.WriteLine("DefineComponent called without an active document.");
            // Consider logging to AutoCAD editor if available, though Application.DocumentManager might be null too
            return ObjectId.Null;
        }
        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            // Check if there is an active document.
            // This is crucial because we might need to interact with AutoCAD's editor.
            // Entity Selection
            ObjectIdCollection selectedObjectIds = null;
            PromptSelectionResult selectionResult;
            do
            {
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nSelect 2D entities for the component geometry: ";
                pso.SingleOnly = false;
                selectionResult = ed.GetSelection(pso);

                if (selectionResult.Status == PromptStatus.Cancel)
                {
                    ed.WriteMessage("\nComponent definition cancelled by user during entity selection.\n");
                    outAttributes = new Dictionary<string, string>(); // Ensure outAttributes is not null
                    return ObjectId.Null;
                }
                if (selectionResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nNo entities selected. Please try again or press ESC to cancel.\n");
                }
                else
                {
                    selectedObjectIds = new ObjectIdCollection(selectionResult.Value.GetObjectIds());
                }
            } while (selectionResult.Status != PromptStatus.OK && selectionResult.Status != PromptStatus.Cancel);

            if (selectedObjectIds == null || selectedObjectIds.Count == 0)
            {
                outAttributes = new Dictionary<string, string>();
                return ObjectId.Null; // Should be handled by loop or earlier cancel
            }

            // Base Point Selection
            PromptPointResult ppr;
            PromptPointOptions ppo = new PromptPointOptions("\nSpecify base point for the component:");
            ppr = ed.GetPoint(ppo);

            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nComponent definition cancelled by user during base point selection.\n");
                outAttributes = new Dictionary<string, string>();
                return ObjectId.Null;
            }
            Point3d basePoint = ppr.Value;

            // Attribute Collection
            AttributeForm form = new AttributeForm(componentType);
            DialogResult formResult = form.ShowDialog();

            if (formResult != DialogResult.OK)
            {
                ed.WriteMessage($"\nAttribute definition for component type '{componentType}' was cancelled.\n");
                outAttributes = form.Attributes ?? new Dictionary<string, string>(); // Use attributes if available (e.g. defaults), else new
                return ObjectId.Null;
            }
            outAttributes = form.Attributes;

            // Block Creation Logic
            ObjectId newBlockId = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    string proposedBlockName = outAttributes["PARTNAME"]; // Assuming PARTNAME is always present
                    
                    // Handle Existing Block
                    while (bt.Has(proposedBlockName))
                    {
                        PromptKeywordOptions pko = new PromptKeywordOptions($"\nBlock '{proposedBlockName}' already exists. Overwrite, Rename, or Cancel? [Overwrite/Rename/Cancel]:", "Overwrite Rename Cancel");
                        pko.AppendKeywordsToMessage = true;
                        PromptResult pkr = ed.GetKeywords(pko);

                        if (pkr.Status != PromptStatus.OK) // Includes Cancel by ESC
                        {
                            ed.WriteMessage("\nBlock definition cancelled.\n");
                            tr.Abort();
                            return ObjectId.Null;
                        }

                        switch (pkr.StringResult)
                        {
                            case "Overwrite":
                                // Simplified overwrite: proceed to create. AutoCAD might disallow if instances exist without further handling.
                                // For a robust solution, one would need to find and delete/update all references.
                                // For now, we'll try to delete the existing BlockTableRecord if it has no references.
                                ObjectId existingBlockId = bt.GetAt(proposedBlockName);
                                if (!bt.IsWriteEnabled) tr.GetObject(db.BlockTableId, OpenMode.ForWrite); // Ensure write mode for BlockTable
                                BlockTableRecord existingBtr = (BlockTableRecord)tr.GetObject(existingBlockId, OpenMode.ForRead);
                                if (existingBtr.IsAnonymous || existingBtr.IsLayout || existingBtr.IsFromExternalReference || existingBtr.IsFromOverlayReference)
                                {
                                     ed.WriteMessage($"\nBlock '{proposedBlockName}' cannot be directly overwritten as it is a special type of block. Please choose Rename or Cancel.\n");
                                     continue; // Re-prompt
                                }
                                // Check for references (simplified check)
                                // ObjectIdCollection refs = existingBtr.GetBlockReferenceIds(true, true);
                                // if(refs.Count > 0) {
                                //    ed.WriteMessage($"\nBlock '{proposedBlockName}' has instances and cannot be easily overwritten without deleting them. Choose Rename or Cancel, or manually delete instances.\n");
                                //    continue; // Re-prompt
                                // }
                                // If proceeding with overwrite, delete the old BTR
                                existingBtr.UpgradeOpen();
                                existingBtr.Erase(); // Erase the BTR
                                ed.WriteMessage($"\nExisting block '{proposedBlockName}' will be overwritten.\n");
                                goto CreateBlock; // Jump to block creation
                            case "Rename":
                                PromptStringOptions psoRename = new PromptStringOptions("\nEnter new block name: ");
                                psoRename.AllowSpaces = false; // Typically block names don't have spaces
                                PromptResult prRename = ed.GetString(psoRename);
                                if (prRename.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(prRename.StringResult))
                                {
                                    ed.WriteMessage("\nRename cancelled or invalid name.\n");
                                    continue; // Re-prompt Overwrite/Rename/Cancel
                                }
                                proposedBlockName = prRename.StringResult;
                                outAttributes["PARTNAME"] = proposedBlockName; // Update attributes
                                ed.WriteMessage($"\nBlock will be named '{proposedBlockName}'.\n");
                                break; // Exits switch, will re-check bt.Has(proposedBlockName) in while loop
                            case "Cancel":
                                ed.WriteMessage("\nBlock definition cancelled.\n");
                                tr.Abort();
                                return ObjectId.Null;
                        }
                    }

                CreateBlock:
                    if (!bt.IsWriteEnabled) // Ensure BlockTable is open for write if not already
                    {
                        tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                    }

                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = proposedBlockName;
                        btr.Origin = Point3d.Origin; // Entities will be transformed relative to basePoint

                        Matrix3d transform = Matrix3d.Displacement(Point3d.Origin - basePoint);

                        foreach (ObjectId objId in selectedObjectIds)
                        {
                            Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                            Entity clone = (Entity)ent.Clone();
                            clone.TransformBy(transform);
                            btr.AppendEntity(clone);
                            // No need to add clone to transaction explicitly as it's owned by btr
                        }

                        newBlockId = bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);

                        // Add AttributeDefinitions
                        double attDefYOffset = -10.0; // Initial Y position for first attribute definition
                        double attDefSpacing = -5.0; // Spacing between attribute definitions

                        foreach (KeyValuePair<string, string> attPair in outAttributes)
                        {
                            using (AttributeDefinition attDef = new AttributeDefinition())
                            {
                                attDef.Tag = attPair.Key.ToUpperInvariant(); // Tag should be uppercase
                                attDef.TextString = attPair.Value;
                                attDef.Position = new Point3d(0, attDefYOffset, 0); // Stack them below origin
                                attDef.Height = 2.5; // Example height
                                // Set other properties like TextStyleId if needed
                                // attDef.TextStyleId = db.Textstyle; // Current text style

                                if (attPair.Key.Equals("COMPONENTTYPE", StringComparison.OrdinalIgnoreCase))
                                {
                                    attDef.Invisible = true;
                                }
                                btr.AppendAttribute(attDef);
                                tr.AddNewlyCreatedDBObject(attDef, true);
                                attDefYOffset += attDefSpacing - attDef.Height; // Adjust for next position
                            }
                        }
                    }

                    // Entity Replacement
                    BlockTableRecord msBtr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    using (BlockReference br = new BlockReference(basePoint, newBlockId))
                    {
                        msBtr.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);

                        // Add AttributeReferences by iterating through BTR's AttributeDefinitions
                        BlockTableRecord blockDef = (BlockTableRecord)tr.GetObject(newBlockId, OpenMode.ForRead);
                        foreach (ObjectId attDefId in blockDef)
                        {
                            DBObject obj = tr.GetObject(attDefId, OpenMode.ForRead);
                            if (obj is AttributeDefinition)
                            {
                                AttributeDefinition ad = (AttributeDefinition)obj;
                                using (AttributeReference ar = new AttributeReference())
                                {
                                    ar.SetAttributeFromBlock(ad, br.BlockTransform);
                                    ar.TextString = ad.TextString; // Set value from definition's default
                                    // Adjust position if necessary, though SetAttributeFromBlock handles it
                                    br.AttributeCollection.AppendAttribute(ar);
                                    tr.AddNewlyCreatedDBObject(ar, true);
                                }
                            }
                        }
                    }

                    // Delete Original Entities
                    foreach (ObjectId objId in selectedObjectIds)
                    {
                        Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                        ent.Erase();
                    }
                    
                    tr.Commit();
                    ed.WriteMessage($"\nComponent '{proposedBlockName}' defined successfully.\n");
                }
                catch (System.Exception exInner)
                {
                    ed.WriteMessage($"\nError during block definition or entity replacement: {exInner.Message}\nStackTrace: {exInner.StackTrace}\n");
                    tr.Abort();
                    outAttributes = outAttributes ?? new Dictionary<string, string>(); // Ensure not null
                    return ObjectId.Null;
                }
            }
            return newBlockId;
        }
        catch (System.Exception exOuter)
        {
            string errorMessage = $"\nAn unexpected error occurred in DefineComponent for '{componentType}': {exOuter.Message}\nStackTrace: {exOuter.StackTrace}\n";
            if (Application.DocumentManager.MdiActiveDocument != null && Application.DocumentManager.MdiActiveDocument.Editor != null)
            {
               Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(errorMessage);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(errorMessage);
            }
            outAttributes = outAttributes ?? new Dictionary<string, string>(); // Ensure not null
            return ObjectId.Null;
        }
    }
}
