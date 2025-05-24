using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms; // REQUIRES PROJECT REFERENCE TO System.Windows.Forms.dll
using System.Drawing; // Added: REQUIRES PROJECT REFERENCE TO System.Drawing.dll
using RailDesigner1; // Added to access ComponentType from RailDesigner1 namespace

// Explicitly reference AutoCAD Application to avoid ambiguity with System.Windows.Forms.Application
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception; // Alias for AutoCAD Exception if needed

namespace RailGenerator1
{
    // Removed local ComponentType enum definition.
    // The ComponentType enum from RailDesigner1 namespace (in Utils/CommonDefinitions.cs) should be used if this file is integrated.
    // For now, this file might not compile correctly if it relies on its own ComponentType values
    // without adjustments or appropriate `using` directives for the RailDesigner1.ComponentType.

    /// <summary>
    /// Utility class for SAPPO-compliant transaction and error handling.
    /// Adheres to :ExceptionHandlingPattern and :TransactionPattern.
    /// </summary>
    public static class SappoUtilities
    {
        /// <summary>
        /// Executes an action within a transaction, handling commit and rollback.
        /// Mitigates :TransactionError and ensures :ResourceManagement.
        /// Modified to pass the Transaction object to the action.
        /// </summary>
        /// <param name="action">The action to perform within the transaction. Takes Transaction as input.</param>
        public static void ExecuteInTransaction(Action<Transaction> action)
        {
            Document doc = AcadApplication.DocumentManager.MdiActiveDocument; // Corrected: Use alias
            if (doc == null) return; // No active document
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    action?.Invoke(tr); // Execute the provided action, passing the transaction
                    tr.Commit(); // Commit if successful
                }
                catch (Autodesk.AutoCAD.Runtime.Exception rx) // Catch specific AutoCAD exceptions first (no ambiguity here)
                {
                    tr.Abort(); // Rollback on exception
                    HandleError(rx, ed); // Use error handling utility
                }
                catch (System.Exception ex) // Catch general exceptions for SAPPO :ExceptionHandling
                {
                    tr.Abort();
                    HandleError(ex, ed); // Use generic error handler
                }
                // No finally needed as `using` handles disposal
            }
        }


        [CommandMethod("RAIL_GENERATE")]
        public static void RAIL_GENERATE() // Made static to be callable as command
        {
            Document doc = AcadApplication.DocumentManager.MdiActiveDocument; // Corrected: Use alias
            if (doc == null) return;
            Editor ed = doc.Editor;

            try // Added try-catch block for the whole command
            {
                // Show the WinForms dialog for input
                var dialogResult = ShowRailingGenerateDialog();
                if (dialogResult.Item1 == DialogResult.Cancel)
                {
                    ed.WriteMessage("\nRailing generation cancelled."); // SAPPO: Handle :UserCancellationError
                    return;
                }

                if (dialogResult.Item1 != DialogResult.OK || dialogResult.Item2 == null)
                {
                    ed.WriteMessage("\nSAPPO Error: Dialog closed without valid input or validation failed. (:UserInputError)");
                    return; // Already handled validation failure inside ShowRailingGenerateDialog
                }

                var formData = dialogResult.Item2;

                string componentStr = (string)formData["Component"];
                if (!Enum.TryParse<ComponentType>(componentStr, out ComponentType componentType))
                {
                    ed.WriteMessage("\nSAPPO Error: Invalid component type string after validation. This should not happen.");
                    return;
                }

                string pathMode = (string)formData["PathMode"];
                Curve pathCurve = null; // Store the path curve for processing

                // Path Selection/Creation Logic
                if (pathMode == "Select")
                {
                    PromptEntityOptions entityOptions = new PromptEntityOptions("\nSelect a line or curve for the path: ");
                    entityOptions.SetRejectMessage("\nInvalid selection. Must be a Line, Polyline, Arc, or Circle.");
                    entityOptions.AddAllowedClass(typeof(Line), true);
                    entityOptions.AddAllowedClass(typeof(Polyline), true);
                    entityOptions.AddAllowedClass(typeof(Arc), true);
                    entityOptions.AddAllowedClass(typeof(Circle), true);
                    PromptEntityResult entityResult = ed.GetEntity(entityOptions);
                    if (entityResult.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nPath selection failed or cancelled."); // SAPPO: Handle :UserSelectionError
                        return;
                    }
                    // Open the selected curve within a transaction
                    ExecuteInTransaction(tr =>
                    {
                        // Ensure we are attempting to open the object as a Curve
                        DBObject obj = tr.GetObject(entityResult.ObjectId, OpenMode.ForRead);
                        pathCurve = obj as Curve; // Attempt to cast to Curve

                        if (pathCurve == null)
                        {
                            // If casting fails, it means the selected object isn't derived from Curve
                            ed.WriteMessage($"\nSAPPO Error: Selected object (Type: {obj?.GetType().Name ?? "Unknown"}) is not a valid Curve.");
                            throw new InvalidOperationException("Failed to read path curve.");
                        }
                        // Optional: Clone if modification or persistence beyond transaction is needed
                        // pathCurve = pathCurve.Clone() as Curve;
                    });
                    if (pathCurve == null) return; // Exit if curve couldn't be read or wasn't a Curve
                }
                else // Manual mode - Create a Line
                {
                    // These were validated in the dialog's logic, so Parse should be safe
                    double startX = double.Parse((string)formData["StartX"], CultureInfo.InvariantCulture);
                    double startY = double.Parse((string)formData["StartY"], CultureInfo.InvariantCulture);
                    double endX = double.Parse((string)formData["EndX"], CultureInfo.InvariantCulture);
                    double endY = double.Parse((string)formData["EndY"], CultureInfo.InvariantCulture);
                    Point3d startPt = new Point3d(startX, startY, 0);
                    Point3d endPt = new Point3d(endX, endY, 0);

                    // Create the line entity in the database
                    ExecuteInTransaction(tr =>
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        Line manualLine = new Line(startPt, endPt);
                        manualLine.SetDatabaseDefaults();
                        btr.AppendEntity(manualLine);
                        tr.AddNewlyCreatedDBObject(manualLine, true);
                        pathCurve = manualLine; // Use the newly created line as the path
                        ed.WriteMessage("\nManual path line created.");
                    });
                    if (pathCurve == null)
                    {
                        ed.WriteMessage("\nSAPPO Error: Failed to create manual path line.");
                        return;
                    }
                }

                // Count Parsing Logic
                int count = 0; // Default if not provided or invalid
                if (formData.ContainsKey("Count") && formData["Count"] is string countStr && !string.IsNullOrEmpty(countStr))
                {
                    // Validation already happened, TryParse mainly as safeguard
                    if (!int.TryParse(countStr, out count) || count <= 0)
                    {
                        ed.WriteMessage("\nWarning: Invalid count value provided, defaulting to 0 or automatic calculation if applicable.");
                        count = 0; // Reset count if parsing fails post-validation
                    }
                }


                // --- Placeholder for Actual Railing Generation Logic ---
                ed.WriteMessage($"\nRailing generation started for Component: {componentType}, Count: {count}");
                ed.WriteMessage($"\nPath Curve Type: {pathCurve?.GetType().Name ?? "None"}, Length: {pathCurve?.GetDistanceAtParameter(pathCurve.EndParam) ?? 0:F2}");

                // Example: Generate Posts if component is Post and count > 0
                if (componentType == ComponentType.Post && count > 0 && pathCurve != null)
                {
                    GeneratePostsAlongCurve(pathCurve, count, ed); // Removed unused Action parameter
                }
                // Corrected: ComponentType.TopCap changed to ComponentType.TopRail
                else if (componentType == ComponentType.TopRail || componentType == ComponentType.BottomRail || componentType == ComponentType.IntermediateRail)
                {
                    // For horizontal rails, we might just draw a polyline along the path
                    // Or extrude a profile block along the path (more complex)
                    // Let's use the simplified GenerateHorizontalRailPolyines example
                    // We need post positions if the rail connects posts. If no posts, just use path start/end.
                    List<Point3d> railPoints = new List<Point3d> { pathCurve.StartPoint, pathCurve.EndPoint }; // Simplified
                    GenerateHorizontalRailPolyines(componentType, railPoints, ed); // Pass simplified points
                }
                else
                {
                    ed.WriteMessage("\nGeneration logic for this component type is not yet implemented.");
                }

                // TODO: Implement detailed railing generation based on componentType, pathCurve, count, etc.

                ed.WriteMessage("\n(Placeholder) Railing generation logic finished.");
            }
            catch (System.Exception ex) // Catch errors during the command execution
            {
                HandleError(ex, ed); // Use the centralized error handler
            }
        }

        // --- Generation Helper Placeholders ---

        // Placeholder: Generate Posts
        private static void GeneratePostsAlongCurve(Curve pathCurve, int postCount, Editor ed)
        {
            if (postCount <= 0 || pathCurve == null) return;

            List<Point3d> postPositions = new List<Point3d>();
            double totalLength = pathCurve.GetDistanceAtParameter(pathCurve.EndParam);

            // Calculate spacing (handle count=1 case)
            double spacing = (postCount > 1) ? totalLength / (postCount - 1) : 0;

            ExecuteInTransaction(tr =>
            {
                // Get database from the curve object
                Database db = pathCurve.Database;

                // Find the Block Definition for "Post" component type
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId postBlockId = FindBlockByComponentType(tr, bt, ComponentType.Post.ToString().ToUpper());

                if (postBlockId == ObjectId.Null)
                {
                    ed.WriteMessage($"\nError: Block definition for component type 'POST' not found. Use RAIL_DEFINE_COMPONENT first.");
                    return; // Abort this part of the generation
                }

                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                for (int i = 0; i < postCount; i++)
                {
                    double distance = (postCount == 1) ? 0 : i * spacing; // Place first post at start if only one
                    Point3d position = pathCurve.GetPointAtDist(distance);
                    postPositions.Add(position);

                    // --- Insert Block Reference ---
                    BlockReference postRef = new BlockReference(position, postBlockId);
                    postRef.SetDatabaseDefaults(db); // Use DB defaults

                    // Optional: Set rotation based on path tangent
                    try
                    {
                        double param = pathCurve.GetParameterAtPoint(position); // Use GetParameterAtPoint with the calculated position

                        Vector3d tangent = pathCurve.GetFirstDerivative(param);
                        if (!tangent.IsZeroLength())
                        {
                            postRef.Rotation = Math.Atan2(tangent.Y, tangent.X); // Rotation in XY plane
                        }
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception geomEx) // Catch specific geometry errors
                    {
                        ed.WriteMessage($"\nWarning: Could not determine tangent for post {i + 1} at distance {distance:F2}. Rotation not set. Error: {geomEx.Message}");
                    }
                    catch (System.Exception ex) // Catch other unexpected errors
                    {
                        ed.WriteMessage($"\nWarning: An unexpected error occurred calculating tangent for post {i + 1} at distance {distance:F2}. Rotation not set. Error: {ex.Message}");
                    }


                    // Assign layer based on component type
                    string layerName = GetOrCreateLayerForComponent(db, ed, ComponentType.Post);
                    if (layerName != null)
                    {
                        postRef.Layer = layerName;
                    }

                    // Add XData for consistency
                    ResultBuffer rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, "RAIL_GENERATOR"),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, "ComponentType=" + ComponentType.Post.ToString()),
                        new TypedValue((int)DxfCode.ExtendedDataInteger32, i + 1) // Example: Post number
                    );
                    postRef.XData = rb;


                    modelSpace.AppendEntity(postRef);
                    tr.AddNewlyCreatedDBObject(postRef, true);

                    // Add attributes if needed (requires AttributeReferences based on AttributeDefinitions)
                }
                ed.WriteMessage($"\nSuccessfully inserted {postCount} post blocks.");
            });

            // Store postPositions if needed for other components like rails
            // e.g., RailGeneratorContext.CurrentPostPositions = postPositions;
        }


        // Placeholder: Generate Horizontal Rail (Simplified to Polyline)
        private static void GenerateHorizontalRailPolyines(ComponentType componentType, List<Point3d> railPoints, Editor ed)
        {
            if (railPoints == null || railPoints.Count < 2)
            {
                ed.WriteMessage("\nWarning: Cannot generate rail polyline without at least two points.");
                return;
            }

            Database db = AcadApplication.DocumentManager.MdiActiveDocument.Database; // Corrected: Use alias
            string layerName = GetOrCreateLayerForComponent(db, ed, componentType);
            if (layerName == null) return; // Layer creation failed, mitigate :LayerCreationError

            ExecuteInTransaction(tr =>
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                Polyline railPolyline = new Polyline();

                for (int i = 0; i < railPoints.Count; i++)
                {
                    // Create 2D polyline along points (assuming Z=0 or projection needed)
                    railPolyline.AddVertexAt(i, new Point2d(railPoints[i].X, railPoints[i].Y), 0, 0, 0);
                }
                railPolyline.SetDatabaseDefaults(db);
                railPolyline.Layer = layerName; // Assign layer based on component type

                // Attach XData for SAPPO :AttributeConsistency
                ResultBuffer rb = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, "RAIL_GENERATOR"),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, "ComponentType=" + componentType.ToString())
                );
                railPolyline.XData = rb;

                btr.AppendEntity(railPolyline); // Correct method to add entity
                tr.AddNewlyCreatedDBObject(railPolyline, true);
                ed.WriteMessage($"\nGenerated {componentType} polyline.");
            });
        }

        // Helper: Find Block Definition by Component Type Attribute
        private static ObjectId FindBlockByComponentType(Transaction tr, BlockTable bt, string componentTypeTag)
        {
            componentTypeTag = componentTypeTag.ToUpperInvariant(); // Ensure case-insensitive comparison

            foreach (ObjectId btrId in bt)
            {
                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                if (btr != null && !btr.IsLayout && !btr.IsAnonymous && btr.HasAttributeDefinitions)
                {
                    foreach (ObjectId objId in btr)
                    {
                        if (objId.ObjectClass.DxfName.Equals("ATTDEF", StringComparison.OrdinalIgnoreCase))
                        {
                            AttributeDefinition attDef = tr.GetObject(objId, OpenMode.ForRead) as AttributeDefinition;
                            if (attDef != null && attDef.Tag.Equals("COMPONENTTYPE", StringComparison.OrdinalIgnoreCase) && attDef.TextString.Equals(componentTypeTag, StringComparison.OrdinalIgnoreCase))
                            {
                                return btrId; // Found the block definition
                            }
                        }
                    }
                }
            }
            return ObjectId.Null; // Not found
        }


        // --- End Generation Helper Placeholders ---


        /// <summary>
        /// Handles errors by displaying a message and logging if necessary.
        /// </summary>
        private static void HandleError(System.Exception ex, Editor ed)
        {
            if (ed == null) return;
            string errorMessage = $"SAPPO Error: {ex.Message} (Type: {ex.GetType().Name})";
            ed.WriteMessage($"\n!_ERROR_!: {errorMessage}\n");
            System.Diagnostics.Debug.WriteLine($"RailGenerator Error: {ex.ToString()}");
            // Optional: Show alert dialog for critical errors
            // AcadApplication.ShowAlertDialog($"An error occurred: {errorMessage}"); // Use Alias
        }

        /// <summary>
        /// Executes an action with custom exception handling.
        /// </summary>
        public static void ExecuteWithExceptionHandling(Action action, Action<System.Exception> errorHandler)
        {
            try
            {
                action?.Invoke();
            }
            catch (System.Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }

        /// <summary>
        /// Executes a function with custom exception handling.
        /// </summary>
        public static T ExecuteWithExceptionHandling<T>(Func<T> func, Action<System.Exception> errorHandler)
        {
            try
            {
                return func();
            }
            catch (System.Exception ex)
            {
                errorHandler?.Invoke(ex);
                return default(T);
            }
        }


        /// <summary>
        /// Validates user input strings, e.g., for mandatory fields.
        /// </summary>
        public static bool ValidateStringInput(string input, string fieldName, Editor ed)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                HandleError(new ArgumentException($"{fieldName} is mandatory and cannot be empty."), ed);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates numeric input (double), e.g., for dimensions.
        /// </summary>
        public static bool ValidateNumericInput(string input, string fieldName, Editor ed, bool allowNegative, out double parsedValue)
        {
            parsedValue = double.NaN;
            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                HandleError(new FormatException($"{fieldName} must be a valid number."), ed);
                return false;
            }
            if (!allowNegative && value < 0)
            {
                HandleError(new ArgumentOutOfRangeException(fieldName, $"{fieldName} must be a non-negative number."), ed);
                return false;
            }
            parsedValue = value;
            return true;
        }

        // Overload for non-negative numbers by default
        public static double ValidateNumericInput(string input, string fieldName, Editor ed)
        {
            if (ValidateNumericInput(input, fieldName, ed, false, out double parsedValue))
            {
                return parsedValue;
            }
            return double.NaN; // Indicate failure
        }


        /// <summary>
        /// Selects an entity with specified type filters.
        /// </summary>
        public static ObjectId SelectFilteredEntity(Editor ed, Type[] allowedTypes, string promptMessage)
        {
            PromptEntityOptions entityOptions = new PromptEntityOptions(promptMessage);
            string typeList = string.Join(", ", allowedTypes.Select(t => t.Name));
            entityOptions.SetRejectMessage($"\nInvalid entity type selected. Must be one of: {typeList}. (:TypeMismatch)");

            int allowedClassCount = 0;
            foreach (var type in allowedTypes)
            {
                RXClass rxClass = RXObject.GetClass(type);
                if (rxClass != null)
                {
                    entityOptions.AddAllowedClass(type, true);
                    allowedClassCount++;
                }
                else
                {
                    ed.WriteMessage($"\nWarning: Cannot filter by type {type.Name} - RXClass not found.");
                }
            }

            if (allowedClassCount == 0)
            {
                ed.WriteMessage("\nError: No valid types provided for filtering.");
                return ObjectId.Null;
            }

            PromptEntityResult entityResult = ed.GetEntity(entityOptions);
            if (entityResult.Status == PromptStatus.OK)
            {
                return entityResult.ObjectId;
            }
            else
            {
                ed.WriteMessage("\nSAPPO Info: Entity selection cancelled or failed. (:SelectionError)");
                return ObjectId.Null;
            }
        }


        /// <summary>
        /// Prompts for and validates a base point within the bounding box of a given entity.
        /// </summary>
        public static Point3d? PromptAndValidateBasePoint(Editor ed, ObjectId entityId)
        {
            if (entityId == ObjectId.Null)
            {
                ed.WriteMessage("\nSAPPO Error: Invalid entity ID passed for base point validation.");
                return null;
            }

            Point3d? basePoint = null;
            Extents3d? extents = null;

            ExecuteInTransaction(tr =>
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    ed.WriteMessage("\nSAPPO Error: Selected entity could not be opened. (:NullPointerException)");
                    throw new InvalidOperationException("Failed to open entity for bounds check.");
                }
                try
                {
                    extents = entity.GeometricExtents;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception geomEx)
                {
                    if (geomEx.ErrorStatus == ErrorStatus.InvalidExtents)
                    {
                        ed.WriteMessage($"\nWarning: Cannot get geometric extents for entity type {entity.GetType().Name}. Base point validation skipped.");
                        extents = null;
                    }
                    else throw;
                }
            });

            PromptPointOptions basePointOptions = new PromptPointOptions("\nSpecify base point:");
            if (extents.HasValue)
            {
                basePointOptions.Message += " (Should be within geometry bounds)";
            }

            PromptPointResult basePointResult = ed.GetPoint(basePointOptions);
            if (basePointResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nSAPPO Info: Base point selection cancelled or invalid.");
                return null;
            }
            Point3d selectedPoint = basePointResult.Value;

            if (extents.HasValue)
            {
                Tolerance tol = new Tolerance(1e-6, 1e-6);
                if (!extents.Value.IsPointInExtents(selectedPoint, tol))
                {
                    ed.WriteMessage("\nSAPPO Warning: Selected base point is outside the geometry's bounding box. (:OutOfBoundsError) Proceeding anyway.");
                }
            }

            basePoint = selectedPoint;
            return basePoint;
        }

        // Helper extension method for Extents3d check
        public static bool IsPointInExtents(this Extents3d extents, Point3d pt, Tolerance tolerance)
        {
            return pt.X >= extents.MinPoint.X - tolerance.EqualPoint && pt.X <= extents.MaxPoint.X + tolerance.EqualPoint &&
                   pt.Y >= extents.MinPoint.Y - tolerance.EqualPoint && pt.Y <= extents.MaxPoint.Y + tolerance.EqualPoint &&
                   pt.Z >= extents.MinPoint.Z - tolerance.EqualPoint && pt.Z <= extents.MaxPoint.Z + tolerance.EqualPoint;
        }


        /// <summary>
        /// Prompts the user to select a component type using the ComponentType enum.
        /// </summary>
        public static ComponentType? SelectComponentType(Editor ed)
        {
            var values = Enum.GetNames(typeof(ComponentType));
            string keywordString = string.Join("/", values);
            PromptKeywordOptions options = new PromptKeywordOptions($"\nSelect component type [{keywordString}]: ");
            // Corrected: "TopCap" to "TopRail", removed "BasePlate"
            // Consider adding HandRail, UserDefined if they should be selectable here.
            options.Keywords.Add("TopRail"); 
            options.Keywords.Add("BottomRail");
            options.Keywords.Add("IntermediateRail");
            options.Keywords.Add("Post");
            options.Keywords.Add("Picket");
            // options.Keywords.Add("BasePlate"); // Removed
            options.Keywords.Add("Mounting");
            // To make it comprehensive with RailDesigner1.ComponentType, you might add:
            // options.Keywords.Add("HandRail");
            // options.Keywords.Add("UserDefined");
            options.AllowNone = false;

            PromptResult result = ed.GetKeywords(options);
            if (result.Status == PromptStatus.OK)
            {
                if (Enum.TryParse<ComponentType>(result.StringResult, out ComponentType selectedType))
                {
                    return selectedType;
                }
                else
                {
                    HandleError(new InvalidOperationException($"SAPPO Internal Error: Keyword '{result.StringResult}' not mapped to ComponentType."), ed);
                    return null;
                }
            }
            else
            {
                ed.WriteMessage("\nSAPPO Info: Component type selection cancelled or invalid. (:UserInputError)");
                return null;
            }
        }

        /// <summary>
        /// Gets or creates a layer for the given component type.
        /// </summary>
        public static string GetOrCreateLayerForComponent(Database db, Editor ed, ComponentType componentType)
        {
            string layerName = $"L-RAIL-{componentType.ToString().ToUpper()}";
            ObjectId layerId = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (!lt.Has(layerName))
                    {
                        lt.UpgradeOpen();
                        using (LayerTableRecord ltr = new LayerTableRecord())
                        {
                            ltr.Name = layerName;
                            ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7);
                            if (lt.Database.LinetypeTableId != null)
                            {
                                LinetypeTable ltt = (LinetypeTable)tr.GetObject(lt.Database.LinetypeTableId, OpenMode.ForRead);
                                ltr.LinetypeObjectId = ltt.Has("Continuous") ? ltt["Continuous"] : db.Celtype;
                            }
                            else
                            {
                                ltr.LinetypeObjectId = db.Celtype;
                            }

                            layerId = lt.Add(ltr);
                            tr.AddNewlyCreatedDBObject(ltr, true);
                            ed.WriteMessage($"\nLayer '{layerName}' created.");
                        }
                    }
                    else
                    {
                        layerId = lt[layerName];
                    }
                    tr.Commit();
                    return layerName;
                }
                catch (System.Exception ex)
                {
                    HandleError(ex, ed);
                    tr.Abort();
                    return null;
                }
            }
        }


        [CommandMethod("RAIL_DEFINE_COMPONENT")]
        public static void DefineComponentCommand()
        {
            Document doc = AcadApplication.DocumentManager.MdiActiveDocument; // Use alias
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            bool continueDefining = true;
            while (continueDefining)
            {
                ComponentType? selectedComponentType = SelectComponentType(ed);
                if (selectedComponentType == null) { ed.WriteMessage("\nComponent type selection required. Aborting definition."); break; }
                string componentType = selectedComponentType.Value.ToString();
                string componentTypeUpper = componentType.ToUpperInvariant();

                Type[] allowedTypes = { typeof(Line), typeof(Polyline), typeof(Arc), typeof(Circle), typeof(Ellipse), typeof(Spline) };
                ObjectId selectedEntityId = SelectFilteredEntity(ed, allowedTypes, "\nSelect 2D entities for component geometry:");
                if (selectedEntityId == ObjectId.Null) { ed.WriteMessage("\nGeometry selection required. Please try again."); continue; }

                Point3d? basePointNullable = PromptAndValidateBasePoint(ed, selectedEntityId);
                if (basePointNullable == null) { ed.WriteMessage("\nBase point selection required. Please try again."); continue; }
                Point3d basePoint = basePointNullable.Value;

                Dictionary<string, string> attributeValues = ShowAttributeInputDialog(componentType);
                if (attributeValues == null) { ed.WriteMessage("\nAttribute input cancelled or failed validation."); continue; }

                string partName = attributeValues["PARTNAME"];
                string weightDensityStr = attributeValues["WEIGHT_DENSITY"];
                string widthStr = attributeValues["WIDTH"];
                string heightStr = attributeValues["HEIGHT"];
                string stockLengthStr = attributeValues["STOCK_LENGTH"];

                if (!double.TryParse(weightDensityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double weightDensity) || weightDensity < 0)
                { ed.WriteMessage($"\nError: WEIGHT_DENSITY ('{weightDensityStr}') is not a valid non-negative number."); continue; }

                double width, height;
                Extents3d? bounds = null;
                ExecuteInTransaction(tr => {
                    Entity ent = tr.GetObject(selectedEntityId, OpenMode.ForRead) as Entity;
                    if (ent != null) try { bounds = ent.GeometricExtents; } catch { /* ignore */ }
                });

                if (!string.IsNullOrWhiteSpace(widthStr))
                {
                    if (!double.TryParse(widthStr, NumberStyles.Any, CultureInfo.InvariantCulture, out width) || width <= 0)
                    { ed.WriteMessage($"\nError: Provided WIDTH ('{widthStr}') is not a valid positive number."); continue; }
                }
                else if (bounds.HasValue) { width = bounds.Value.MaxPoint.X - bounds.Value.MinPoint.X; ed.WriteMessage($"\nCalculated WIDTH from bounds: {width:F4}"); }
                else { ed.WriteMessage($"\nError: WIDTH not provided and could not be calculated from geometry bounds."); continue; }

                if (!string.IsNullOrWhiteSpace(heightStr))
                {
                    if (!double.TryParse(heightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out height) || height <= 0)
                    { ed.WriteMessage($"\nError: Provided HEIGHT ('{heightStr}') is not a valid positive number."); continue; }
                }
                else if (bounds.HasValue) { height = bounds.Value.MaxPoint.Y - bounds.Value.MinPoint.Y; ed.WriteMessage($"\nCalculated HEIGHT from bounds: {height:F4}"); }
                else { ed.WriteMessage($"\nError: HEIGHT not provided and could not be calculated from geometry bounds."); continue; }

                double stockLength = double.NaN;
                if (!string.IsNullOrWhiteSpace(stockLengthStr))
                {
                    if (!double.TryParse(stockLengthStr, NumberStyles.Any, CultureInfo.InvariantCulture, out stockLength) || stockLength <= 0)
                    { ed.WriteMessage($"\nError: Provided STOCK_LENGTH ('{stockLengthStr}') is not a valid positive number."); continue; }
                }

                attributeValues["WIDTH"] = width.ToString(CultureInfo.InvariantCulture);
                attributeValues["HEIGHT"] = height.ToString(CultureInfo.InvariantCulture);
                attributeValues["WEIGHT_DENSITY"] = weightDensity.ToString(CultureInfo.InvariantCulture);
                attributeValues["STOCK_LENGTH"] = !double.IsNaN(stockLength) ? stockLength.ToString(CultureInfo.InvariantCulture) : "";

                string blockName = $"{partName}_{componentTypeUpper}_{Guid.NewGuid().ToString("N").Substring(0, 8)}".Replace(" ", "_");
                string invalidChars = @"<>/\ "":;?*|=`" + string.Join("", System.IO.Path.GetInvalidFileNameChars());
                foreach (char c in invalidChars) { blockName = blockName.Replace(c, '_'); }

                ObjectId newBlockId = ObjectId.Null;
                ExecuteInTransaction(tr =>
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    if (BlockExists(tr, db, blockName)) // Pass db
                    { throw new InvalidOperationException($"Block name '{blockName}' already exists. (:NamingConflict)"); }

                    using (BlockTableRecord btr = new BlockTableRecord { Name = blockName, Origin = Point3d.Origin })
                    {
                        bt.UpgradeOpen();
                        newBlockId = bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);

                        Entity entityToClone = (Entity)tr.GetObject(selectedEntityId, OpenMode.ForRead);
                        Entity clonedEntity = entityToClone.Clone() as Entity;
                        if (clonedEntity != null)
                        {
                            Matrix3d transform = Matrix3d.Displacement(Point3d.Origin - basePoint);
                            clonedEntity.TransformBy(transform);
                            string layerName = GetOrCreateLayerForComponent(db, ed, selectedComponentType.Value);
                            if (layerName != null) clonedEntity.Layer = layerName;
                            else ed.WriteMessage($"\nWarning: Could not set layer for cloned entity in block '{blockName}'.");
                            btr.AppendEntity(clonedEntity);
                            tr.AddNewlyCreatedDBObject(clonedEntity, true);
                        }
                        else { throw new InvalidOperationException($"Failed to clone selected entity for block '{blockName}'."); }

                        double attYOffset = 0;
                        double attSpacing = Math.Max(1, Math.Max(height, width) * 0.1);

                        Action<string, string, string, bool> AddAttDef = (tag, prompt, value, isInvisible) =>
                        {
                            using (AttributeDefinition attDef = new AttributeDefinition())
                            {
                                attDef.SetDatabaseDefaults(db);
                                attDef.Position = new Point3d(0, attYOffset, 0);
                                attDef.Tag = tag.ToUpperInvariant();
                                attDef.Prompt = prompt;
                                attDef.TextString = value ?? "";
                                attDef.Height = Math.Max(0.5, Math.Max(height, width) * 0.05);
                                attDef.Invisible = isInvisible; // Correct property name
                                attDef.HorizontalMode = TextHorizontalMode.TextLeft;
                                attDef.VerticalMode = TextVerticalMode.TextBase;
                                btr.AppendEntity(attDef);
                                tr.AddNewlyCreatedDBObject(attDef, true);
                                attYOffset -= attSpacing;
                            }
                        };

                        AddAttDef("PARTNAME", "Part Name:", attributeValues["PARTNAME"], false);
                        AddAttDef("COMPONENTTYPE", "Component Type:", componentTypeUpper, false);
                        AddAttDef("DESCRIPTION", "Description:", attributeValues["DESCRIPTION"], false);
                        AddAttDef("MATERIAL", "Material:", attributeValues["MATERIAL"], false);
                        AddAttDef("FINISH", "Finish:", attributeValues["FINISH"], false);
                        AddAttDef("WEIGHT_DENSITY", "Weight Density (per unit volume):", attributeValues["WEIGHT_DENSITY"], false);
                        AddAttDef("WIDTH", "Component Width:", attributeValues["WIDTH"], false);
                        AddAttDef("HEIGHT", "Component Height:", attributeValues["HEIGHT"], false);
                        AddAttDef("STOCK_LENGTH", "Stock Length (if applicable):", attributeValues["STOCK_LENGTH"], false);
                        AddAttDef("SPECIAL_NOTES", "Special Notes:", attributeValues["SPECIAL_NOTES"], true);
                        AddAttDef("USER_ATTRIBUTE_1", "User Attribute 1:", attributeValues["USER_ATTRIBUTE_1"], true);
                        AddAttDef("USER_ATTRIBUTE_2", "User Attribute 2:", attributeValues["USER_ATTRIBUTE_2"], true);

                        ed.WriteMessage($"\nBlock '{blockName}' defined successfully with geometry and attributes.");
                    }
                });

                if (newBlockId == ObjectId.Null) { ed.WriteMessage("\nBlock definition failed."); }

                // Prompt to define another component
                // CORRECTION: Set default keyword properly
                PromptKeywordOptions loopOptions = new PromptKeywordOptions("\nDefine another component? [Yes/No]");
                loopOptions.Keywords.Add("Yes"); // Add keywords first
                loopOptions.Keywords.Add("No");
                loopOptions.Keywords.Default = "Yes"; // Set the default keyword
                loopOptions.AppendKeywordsToMessage = true; // Append them to the prompt

                PromptResult loopResult = ed.GetKeywords(loopOptions);
                if (loopResult.Status != PromptStatus.OK || loopResult.StringResult.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    continueDefining = false; // Stop loop if "No" or cancelled/error
                }
            }
            ed.WriteMessage("\nDefine Component command finished.");
        }

        // Helper Dialog for Attribute Input
        private static Dictionary<string, string> ShowAttributeInputDialog(string componentType)
        {
            Dictionary<string, string> attributes = null;
            using (Form attributeForm = new Form())
            {
                attributeForm.Text = $"Component Attributes for {componentType}";
                attributeForm.Width = 450;
                attributeForm.Height = 520;
                attributeForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                attributeForm.StartPosition = FormStartPosition.CenterParent;

                int currentTop = 10;
                int labelWidth = 140;
                int inputLeft = 150;
                int inputWidth = 250;
                int spacing = 30;

                Func<string, string, string, bool, TextBox> AddAttributeRow =
                    (labelText, defaultValue, toolTip, isMandatory) =>
                    {
                        Label lbl = new Label() { Text = (isMandatory ? "*" : "") + labelText + ":", Top = currentTop + 3, Left = 10, Width = labelWidth };
                        TextBox txt = new TextBox() { Top = currentTop, Left = inputLeft, Width = inputWidth, Text = defaultValue };
                        using (ToolTip tt = new ToolTip()) // Dispose ToolTip
                        {
                            tt.SetToolTip(txt, toolTip);
                            if (isMandatory) tt.SetToolTip(lbl, "This field is mandatory.");
                        }
                        attributeForm.Controls.Add(lbl);
                        attributeForm.Controls.Add(txt);
                        currentTop += spacing;
                        return txt;
                    };

                TextBox txtPartName = AddAttributeRow("Part Name", "", "Unique identifier for this component definition.", true);
                TextBox txtDescription = AddAttributeRow("Description", "", "Optional description of the component.", false);
                TextBox txtMaterial = AddAttributeRow("Material", "Aluminum", "Material specification (e.g., 6061-T6).", false);
                TextBox txtFinish = AddAttributeRow("Finish", "Mill", "Surface finish (e.g., Powder Coat Black).", false);
                TextBox txtWeightDensity = AddAttributeRow("Weight Density", "0.0975", "Material density (e.g., lbs/in³ for Aluminum).", false);
                TextBox txtWidth = AddAttributeRow("Width", "", "Overall width. Leave blank to calculate from geometry.", false);
                TextBox txtHeight = AddAttributeRow("Height", "", "Overall height. Leave blank to calculate from geometry.", false);
                TextBox txtStockLength = AddAttributeRow("Stock Length", "", "Standard length available from supplier (optional).", false);
                TextBox txtSpecialNotes = AddAttributeRow("Special Notes", "", "Any additional notes for manufacturing or BOM.", false);
                TextBox txtUserAttr1 = AddAttributeRow("User Attribute 1", "", "Custom user-defined attribute.", false);
                TextBox txtUserAttr2 = AddAttributeRow("User Attribute 2", "", "Custom user-defined attribute.", false);

                Button btnOK = new Button() { Text = "OK", Top = currentTop + 10, Left = inputLeft - 50, Width = 75, DialogResult = DialogResult.OK };
                Button btnCancel = new Button() { Text = "Cancel", Top = currentTop + 10, Left = inputLeft + 50, Width = 75, DialogResult = DialogResult.Cancel };
                attributeForm.AcceptButton = btnOK;
                attributeForm.CancelButton = btnCancel;
                attributeForm.Controls.Add(btnOK);
                attributeForm.Controls.Add(btnCancel);
                attributeForm.Height = currentTop + 80;

                DialogResult result = DialogResult.Cancel;
                try { result = attributeForm.ShowDialog(); }
                finally { if (!attributeForm.IsDisposed) attributeForm.Dispose(); }

                if (result == DialogResult.OK)
                {
                    List<string> errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(txtPartName.Text)) errors.Add("*Part Name is mandatory.");
                    if (!string.IsNullOrWhiteSpace(txtWeightDensity.Text) && (!double.TryParse(txtWeightDensity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double wd) || wd < 0)) errors.Add("*Weight Density must be a non-negative number.");
                    if (!string.IsNullOrWhiteSpace(txtWidth.Text) && (!double.TryParse(txtWidth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <= 0)) errors.Add("*Width, if provided, must be a positive number.");
                    if (!string.IsNullOrWhiteSpace(txtHeight.Text) && (!double.TryParse(txtHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <= 0)) errors.Add("*Height, if provided, must be a positive number.");
                    if (!string.IsNullOrWhiteSpace(txtStockLength.Text) && (!double.TryParse(txtStockLength.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double sl) || sl <= 0)) errors.Add("*Stock Length, if provided, must be a positive number.");

                    if (errors.Count > 0)
                    {
                        MessageBox.Show("Please correct the following errors:\n\n" + string.Join("\n", errors), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // Attributes will remain null, signalling failure to the caller
                    }
                    else
                    {
                        attributes = new Dictionary<string, string>
                        {
                            { "PARTNAME", txtPartName.Text.Trim() },
                            { "DESCRIPTION", txtDescription.Text.Trim() },
                            { "MATERIAL", txtMaterial.Text.Trim() },
                            { "FINISH", txtFinish.Text.Trim() },
                            { "WEIGHT_DENSITY", txtWeightDensity.Text.Trim() },
                            { "WIDTH", txtWidth.Text.Trim() },
                            { "HEIGHT", txtHeight.Text.Trim() },
                            { "STOCK_LENGTH", txtStockLength.Text.Trim() },
                            { "SPECIAL_NOTES", txtSpecialNotes.Text.Trim() },
                            { "USER_ATTRIBUTE_1", txtUserAttr1.Text.Trim() },
                            { "USER_ATTRIBUTE_2", txtUserAttr2.Text.Trim() }
                        };
                    }
                }
            }
            return attributes; // Will be null if validation failed or cancelled
        }


        // --- Railing Generate Dialog ---
        public class RailingGenerateDialog : Form
        {
            public ComboBox Component_Selection { get; private set; }
            public RadioButton Path_Select { get; private set; }
            public RadioButton Path_Manual { get; private set; }
            public TextBox StartX_Input { get; private set; }
            public TextBox StartY_Input { get; private set; }
            public TextBox EndX_Input { get; private set; }
            public TextBox EndY_Input { get; private set; }
            public TextBox Count_Input { get; private set; }
            private GroupBox manualPathGroup;


            public RailingGenerateDialog()
            {
                InitializeComponent();
                PopulateComponentSelection();
                UpdateManualPathState();
            }

            private void InitializeComponent()
            {
                this.Text = "Generate Railing Parameters";
                this.Size = new Size(450, 350); // Requires System.Drawing reference
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.SuspendLayout(); // Suspend layout

                int currentTop = 10;
                int labelWidth = 180;
                int inputLeft = 200;
                int inputWidth = 200;
                int spacing = 30;

                Label componentLabel = new Label { Text = "Railing Component (Mandatory):", Top = currentTop + 3, Left = 10, Width = labelWidth };
                Component_Selection = new ComboBox { Top = currentTop, Left = inputLeft, Width = inputWidth, DropDownStyle = ComboBoxStyle.DropDownList };
                this.Controls.AddRange(new Control[] { componentLabel, Component_Selection });
                currentTop += spacing;

                GroupBox pathGroup = new GroupBox { Text = "Railing Path", Top = currentTop, Left = 10, Width = 410, Height = 150 };
                int pathGroupTop = 20;
                Path_Select = new RadioButton { Text = "Select Line/Curve in AutoCAD", Top = pathGroupTop, Left = 10, Width = 380, Checked = true };
                pathGroupTop += spacing;
                Path_Manual = new RadioButton { Text = "Input Start and End Points Manually", Top = pathGroupTop, Left = 10, Width = 380 };
                pathGroupTop += spacing;
                manualPathGroup = new GroupBox { Text = "Manual Coordinates", Top = pathGroupTop, Left = 10, Width = 390, Height = 80, Enabled = false };
                int manualGroupTop = 15;
                Label startPointLabel = new Label { Text = "Start Point (X, Y):", Top = manualGroupTop + 3, Left = 10, Width = 120 };
                StartX_Input = new TextBox { Top = manualGroupTop, Left = 130, Width = 110 };
                StartY_Input = new TextBox { Top = manualGroupTop, Left = 250, Width = 110 };
                manualGroupTop += spacing;
                Label endPointLabel = new Label { Text = "End Point (X, Y):", Top = manualGroupTop + 3, Left = 10, Width = 120 };
                EndX_Input = new TextBox { Top = manualGroupTop, Left = 130, Width = 110 };
                EndY_Input = new TextBox { Top = manualGroupTop, Left = 250, Width = 110 };
                manualPathGroup.Controls.AddRange(new Control[] { startPointLabel, StartX_Input, StartY_Input, endPointLabel, EndX_Input, EndY_Input });
                pathGroup.Controls.AddRange(new Control[] { Path_Select, Path_Manual, manualPathGroup });
                this.Controls.Add(pathGroup);
                Path_Select.CheckedChanged += PathMode_CheckedChanged;
                Path_Manual.CheckedChanged += PathMode_CheckedChanged;
                currentTop += pathGroup.Height + 10;

                Label countLabel = new Label { Text = "Number of Posts (Optional):", Top = currentTop + 3, Left = 10, Width = labelWidth };
                Count_Input = new TextBox { Top = currentTop, Left = inputLeft, Width = inputWidth };
                using (ToolTip countTip = new ToolTip()) // Dispose ToolTip
                {
                    countTip.SetToolTip(Count_Input, "Enter a positive integer for posts/pickets. Leave blank if not applicable.");
                }
                this.Controls.AddRange(new Control[] { countLabel, Count_Input });
                currentTop += spacing;

                Button okButton = new Button { Text = "OK", Top = currentTop + 10, Left = inputLeft - 50, Width = 75, DialogResult = DialogResult.OK };
                Button cancelButton = new Button { Text = "Cancel", Top = currentTop + 10, Left = inputLeft + 50, Width = 75, DialogResult = DialogResult.Cancel };
                okButton.Click += OkButton_Click;
                this.AcceptButton = okButton;
                this.CancelButton = cancelButton;
                this.Controls.AddRange(new Control[] { okButton, cancelButton });

                this.Height = currentTop + 80;
                this.ResumeLayout(false); // Resume layout
            }

            private void PopulateComponentSelection()
            {
                Component_Selection.Items.Clear();
                Component_Selection.Items.AddRange(Enum.GetNames(typeof(ComponentType)));
                if (Component_Selection.Items.Count > 0) Component_Selection.SelectedIndex = 0;
            }

            private void PathMode_CheckedChanged(object sender, EventArgs e) { UpdateManualPathState(); }
            private void UpdateManualPathState() { manualPathGroup.Enabled = Path_Manual.Checked; }

            private void OkButton_Click(object sender, EventArgs e)
            {
                Dictionary<string, object> formData = GatherFormData();
                var validationResult = SappoUtilities.ValidateRailingInputInternal(formData); // Call static method
                if (!validationResult.Success)
                {
                    MessageBox.Show("Please correct the following errors:\n\n" + string.Join("\n", validationResult.Errors),
                                    "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None; // Prevent dialog closing
                }
                else
                {
                    this.DialogResult = DialogResult.OK; // Allow dialog closing
                }
            }

            public Dictionary<string, object> GatherFormData()
            {
                return new Dictionary<string, object>
                {
                    { "Component", Component_Selection.SelectedItem?.ToString() },
                    { "PathMode", Path_Select.Checked ? "Select" : "Manual" },
                    { "StartX", StartX_Input.Text.Trim() },
                    { "StartY", StartY_Input.Text.Trim() },
                    { "EndX", EndX_Input.Text.Trim() },
                    { "EndY", EndY_Input.Text.Trim() },
                    { "Count", Count_Input.Text.Trim() }
                };
            }
        }


        /// <summary>
        /// Shows the RailingGenerateDialog and returns the result and validated data.
        /// </summary>
        public static (DialogResult, Dictionary<string, object>) ShowRailingGenerateDialog()
        {
            using (var dialog = new RailingGenerateDialog())
            {
                DialogResult result = DialogResult.Cancel;
                try { result = dialog.ShowDialog(); } // Validation happens in OkButton_Click
                finally { if (!dialog.IsDisposed) dialog.Dispose(); }

                if (result == DialogResult.OK)
                {
                    Dictionary<string, object> formData = dialog.GatherFormData();
                    return (result, formData);
                }
                return (result, null); // Return null data if cancelled or validation failed
            }
        }


        /// <summary>
        /// Internal validation logic used by the dialog.
        /// </summary>
        internal static (bool Success, List<string> Errors) ValidateRailingInputInternal(Dictionary<string, object> formData)
        {
            List<string> errors = new List<string>();
            bool success = true;

            if (formData["Component"] == null || string.IsNullOrWhiteSpace(formData["Component"] as string) || !Enum.TryParse<ComponentType>(formData["Component"] as string, out _))
            { errors.Add("* A valid railing component must be selected."); success = false; }

            if (formData["PathMode"] as string == "Manual")
            {
                Func<string, string, bool> isValidCoord = (val, coordName) =>
                {
                    if (string.IsNullOrWhiteSpace(val) || !double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    { errors.Add($"* Manual Path: {coordName} must be a valid number."); return false; }
                    return true;
                };

                bool sxValid = isValidCoord(formData["StartX"] as string, "Start X");
                bool syValid = isValidCoord(formData["StartY"] as string, "Start Y");
                bool exValid = isValidCoord(formData["EndX"] as string, "End X");
                bool eyValid = isValidCoord(formData["EndY"] as string, "End Y");

                if (sxValid && syValid && exValid && eyValid &&
                    double.TryParse(formData["StartX"] as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double sx) &&
                    double.TryParse(formData["StartY"] as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double sy) &&
                    double.TryParse(formData["EndX"] as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double ex) &&
                    double.TryParse(formData["EndY"] as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double ey) &&
                    sx == ex && sy == ey)
                { errors.Add("* Manual Path: Start and End points cannot be the same."); success = false; }
                else if (!(sxValid && syValid && exValid && eyValid)) // If any coordinate was invalid
                {
                    success = false; // Overall validation fails
                }
            }

            string countStr = formData["Count"] as string;
            if (!string.IsNullOrWhiteSpace(countStr))
            {
                if (!int.TryParse(countStr, out int count) || count <= 0)
                { errors.Add("* Number of Posts (if provided) must be a positive whole number."); success = false; }
            }

            return (success, errors);
        }

        // --- End Railing Generate Dialog ---


        // Helper method to check if a block exists
        private static bool BlockExists(Transaction tr, Database db, string blockName)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            return bt.Has(blockName);
        }


        /// <summary>
        /// Creates a basic RailingAssemblyBlock definition.
        /// </summary>
        public static ObjectId CreateRailingAssemblyBlock(Transaction tr, Database db, string componentType)
        {
            Editor ed = AcadApplication.DocumentManager.MdiActiveDocument.Editor; // Use alias
            string blockName = $"RailingAssemblyBlock_{componentType}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";

            if (BlockExists(tr, db, blockName))
            {
                ed.WriteMessage($"\nBlock '{blockName}' already exists. Cannot create assembly block. :NamingConflict");
                return ObjectId.Null;
            }

            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            using (BlockTableRecord btr = new BlockTableRecord { Name = blockName, Origin = Point3d.Origin })
            {
                ObjectId btrId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                using (Circle placeholderCircle = new Circle(Point3d.Origin, Vector3d.ZAxis, 5.0))
                {
                    placeholderCircle.SetDatabaseDefaults(db);
                    placeholderCircle.ColorIndex = 1;
                    btr.AppendEntity(placeholderCircle);
                    tr.AddNewlyCreatedDBObject(placeholderCircle, true);
                }

                using (AttributeDefinition attDef = new AttributeDefinition())
                {
                    attDef.SetDatabaseDefaults(db);
                    attDef.Position = new Point3d(0, -10, 0);
                    attDef.Tag = "ASSEMBLY_ID";
                    attDef.Prompt = "Enter Assembly ID:";
                    attDef.TextString = $"ASM-{componentType}-001";
                    attDef.Height = 2.0;
                    btr.AppendEntity(attDef);
                    tr.AddNewlyCreatedDBObject(attDef, true);
                }

                ed.WriteMessage($"\nDefined RailingAssemblyBlock: '{blockName}'");
                return btrId;
            }
        }


        [CommandMethod("TEST_CREATE_RAILING_BLOCK")]
        public static void TestCreateRailingBlock()
        {
            Document doc = AcadApplication.DocumentManager.MdiActiveDocument; // Use alias
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId blockId = ObjectId.Null;
            ExecuteInTransaction(tr =>
            {
                blockId = CreateRailingAssemblyBlock(tr, db, "Post"); // Pass db
                if (blockId != ObjectId.Null)
                { ed.WriteMessage($"\nRailingAssemblyBlock created successfully with ID: {blockId}"); }
                else
                { ed.WriteMessage("\nFailed to create RailingAssemblyBlock (check command line for errors)."); }
            });

            if (blockId != ObjectId.Null)
            {
                ExecuteInTransaction(tr =>
                {
                    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    using (BlockReference br = new BlockReference(ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Origin, blockId))
                    {
                        br.SetDatabaseDefaults(db);
                        modelSpace.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);
                        ed.WriteMessage("\nInserted instance of the test block at UCS origin.");
                    }
                });
            }
        }

    } // End of SappoUtilities Static Class

} // End of Namespace RailGenerator1