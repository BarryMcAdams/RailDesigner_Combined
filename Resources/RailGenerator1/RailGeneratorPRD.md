Product Requirements Document (PRD)

Project Title

AutoCAD Railing Generator

Goal

To provide AutoCAD 2025 users with a C# .NET 4.8 script that defines custom railing components from existing 2D drawing geometry and generates 2D plan view railing instances along a single polyline path, embedding data for Bill of Materials (BOM) and cut lists, with an option to export to CSV, ensuring robust error handling and alignment with SAPPO principles.

Scope





Platform: Executes within AutoCAD 2025 using .NET 4.8 (:TechnologyVersion).



Geometry Focus: Generates 2D plan view geometry on the XY plane (:Context).



Phases:





Phase 1: Component Definition (Block Creation) (:ComponentRole).



Phase 2: Railing Generation along a single polyline (:ComponentRole).



Component Types: Supports TopCap, BottomRail, IntermediateRail, Post, Picket, BasePlate, Mounting (:ComponentRole).



Data Embedding: Captures and embeds attribute data for BOM/cut lists (:DataModel).



Export: Provides CSV export functionality (:Feature).



Units: Assumes Decimal Inches (:EnvironmentContext).



Output: Generates railing as a single Block Reference (:ArchitecturalPattern).



SAPPO Integration: Uses SAPPO terminology (:Problem, :Solution, :ArchitecturalPattern, :TechnologyVersion) to frame requirements, mitigate issues like :CompatibilityIssue, :LogicError, :SecurityVulnerability.

Out of Scope (for this version)





3D geometry generation.



Multiple simultaneous polyline paths.



Advanced railing editing beyond block-level modifications.



Complex custom components beyond simple 2D shapes.



Fully automatic dimension calculation (user input as fallback).



Structural analysis or code compliance checks.

Target User

AutoCAD users detailing custom aluminum railings and generating BOMs (:UserContext).



1. User Stories





As a user, I want a guided process to define railing component geometry and properties from my 2D drawings so I can reuse them in railings.



As a user, I want components saved as AutoCAD Blocks with attributes (e.g., PARTNAME, DESCRIPTION) for BOM compatibility.



As a user, I want to define various component types (horizontal rails, posts, pickets, mounting hardware) to match my designs.



As a user, I want to select defined components for a specific railing instance to customize its composition.



As a user, I want to generate a 2D plan view railing along a polyline with my components and spacing rules to visualize the design.



As a user, I want the railing to include BOM data compatible with existing scripts for inventory and fabrication.



As a user, I want the generated railing as a single Block Reference for easy manipulation in AutoCAD.



As a user, I want to export railing data to a CSV file on my desktop for external use.



As a user, I want clear error messages when issues arise to troubleshoot effectively.



2. Functional Requirements

2.1 Phase 1: Component Definition (Block Creation)





REQ 1.1: Implement a command RAIL_DEFINE_COMPONENT to initiate component definition (:CommandPattern).





SAPPO: Watch for :UserInputError in command execution.



REQ 1.2: Prompt user to select a component type from a dropdown or numbered list: TopCap, BottomRail, IntermediateRail, Post, Picket, BasePlate, Mounting.





Validation: Ensure selection is valid (:LogicError mitigation).



SAPPO: Use :EnumPattern for type safety.



REQ 1.3: Prompt user to select 2D entities (lines, polylines, arcs, circles) for component geometry using Editor.GetSelection with filters.





Validation: Allow only specified entity types; reject invalid selections (:TypeMismatch).



SAPPO: Mitigate :SelectionError.



REQ 1.4: Prompt for a base point within the selected geometry’s bounding box using Editor.GetPoint.





SAPPO: Watch for :OutOfBoundsError.



REQ 1.5: Present a WinForms dialog to collect attributes:





PARTNAME (String, Mandatory): Unique identifier.



DESCRIPTION (String, Optional): Human-readable description.



COMPONENTTYPE (String, Hidden, Mandatory): Matches selected type (e.g., "POST").



MATERIAL (String, Optional): Default "Aluminum".



FINISH (String, Optional): e.g., "Powder Coat Black".



WEIGHT_DENSITY (Numeric, Optional): In lb/ft (horizontal) or lb/ea (vertical); default 0.0975 lb/in³ for aluminum, converted based on component type.



WIDTH (Numeric, Optional): Calculated from bounding box perpendicular to user-specified length direction (prompt for X/Y axis).



HEIGHT (Numeric, Optional): Calculated from bounding box (vertical for posts/pickets, thickness for rails).



STOCK_LENGTH (Numeric, Optional): Standard length/height for procurement.



SPECIAL_NOTES (String, Optional): Free text.



USER_ATTRIBUTE_1, USER_ATTRIBUTE_2 (String, Optional): Custom fields.



Validation: Ensure mandatory fields are filled; validate numeric inputs.



SAPPO: Mitigate :InputValidationError, :CalculationError in bounding box logic.



REQ 1.6: Allow skipping optional attributes via “Skip” button or empty input.



REQ 1.7: Create a Block Definition in the BlockTable named <PARTNAME>_<GUID> to ensure uniqueness.





SAPPO: Watch for :NamingConflict.



REQ 1.8: Copy selected entities into the Block Definition using Entity.Clone, offset relative to the base point.





SAPPO: Mitigate :GeometryTransformationError.



REQ 1.9: Add AttributeDefinitions for all specified attributes, setting prompts and defaults.





SAPPO: Ensure :AttributeConsistency.



REQ 1.10: Assign geometry to a layer named L-RAIL-<COMPONENTTYPE> (e.g., "L-RAIL-POST").



REQ 1.11: Create missing layers with properties: Color 7, Continuous Linetype.





SAPPO: Mitigate :LayerCreationError.



REQ 1.12: Prompt user to define another component, looping back to REQ 1.2 if “Yes”.





SAPPO: Use :LoopPattern, watch for :InfiniteLoopError.

2.2 Phase 2: Railing Generation





REQ 2.1: Implement a command RAIL_GENERATE to start railing generation (:CommandPattern).





SAPPO: Watch for :UserInputError.



REQ 2.2: Present a WinForms UI with:





Dropdowns listing blocks by COMPONENTTYPE (scanned from BlockTable), including “None” option.



Input fields:





Railing Height (Numeric, Mandatory): Overall height in inches.



Bottom Rail Offset (Numeric, Optional): From post base, default 0.



Intermediate Rail Offset (Numeric, Optional): Vertical offset.



Post Spacing Rule (Dropdown): Max Distance or Fixed Number.



Picket Spacing Rule (Dropdown): Max Distance or Fixed Number.



Mounting Type (Dropdown): Base Plate, Core Drill, Wall Mount.



Validation: Ensure positive numbers, mandatory fields filled.



SAPPO: Mitigate :InputValidationError, use :FormPattern.



REQ 2.3: Prompt for a 2D Polyline path using Editor.GetEntity with filters for Polyline/Polyline2d.





SAPPO: Mitigate :SelectionError.



REQ 2.4: Calculate Post positions along the polyline based on spacing rule, handling vertices and segments.





SAPPO: Use :AlgorithmPattern, watch for :ArithmeticError.



REQ 2.5: Calculate Picket positions between Posts per spacing rule.





SAPPO: Mitigate :CalculationError.



REQ 2.6: Create a temporary Block Definition named RailingAssembly_<Timestamp>.





SAPPO: Watch for :NamingConflict.



REQ 2.7: Within the RailingAssemblyBlock:





Insert Post blocks at calculated positions, oriented to path tangents using BlockReference.TransformBy, transferring attributes via AttributeReference.TextString.



For horizontal components (TopCap, BottomRail, IntermediateRail):





Generate Polylines as rectangles (WIDTH x INSTALLED_LENGTH) along each segment.



Assign to L-RAIL-<COMPONENTTYPE> layer.



Attach attributes (PARTNAME, DESCRIPTION, MATERIAL, FINISH, WEIGHT_DENSITY, SPECIAL_NOTES, USER_ATTRIBUTE_1, USER_ATTRIBUTE_2) and INSTALLED_LENGTH via XData.



Insert Picket blocks at calculated positions, oriented, with attributes and INSTALLED_HEIGHT via XData.



Insert Mounting blocks (e.g., BasePlate) per Mounting Type, with attributes.



SAPPO: Mitigate :GeometryTransformationError, :AttributeConsistency.



REQ 2.8: Insert the RailingAssemblyBlock into Model Space at the polyline’s start point.





SAPPO: Watch for :InsertionError.

2.3 Common Requirements





REQ 3.1 (Error Handling): Implement robust error handling:





Validate inputs (non-numeric, empty mandatory fields).



Check entity types in Phase 1.



Verify block existence and COMPONENTTYPE in Phase 2.



Handle layer creation failures.



Display errors via command line or MessageBox.



SAPPO: Mitigate :UserInputError, :ValidationError, :ExceptionHandling.



REQ 3.2 (Layer Management): Use naming convention L-RAIL-<COMPONENTTYPE>; create only if absent.





SAPPO: Mitigate :LayerCreationError.



REQ 3.3 (Attribute Management): Ensure AttributeDefinitions are added to Block Definitions and AttributeReferences are populated in Block Insertions; use XData for Polylines.





SAPPO: Ensure :AttributeConsistency.



REQ 3.4 (CSV Export): Implement a RAIL_EXPORT_DATA command or UI button to:





Iterate entities in a selected RailingAssemblyBlock.



Extract AttributeReferences from BlockReferences and XData from Polylines.



Compile rows with columns: PARTNAME, DESCRIPTION, INSTALLED_LENGTH, INSTALLED_HEIGHT, WIDTH, HEIGHT, MATERIAL, FINISH, WEIGHT_DENSITY, SPECIAL_NOTES, USER_ATTRIBUTE_1, USER_ATTRIBUTE_2, COMPONENTTYPE.



Save to <Desktop>/RailingBOM_<yyyyMMdd_HHmmss>.csv with header row.



Handle file writing errors.



SAPPO: Mitigate :FileIOException, use :DataExportPattern.



3. Non-Functional Requirements





NFR 3.1 (Usability): Ensure prompts and UI are intuitive for AutoCAD users familiar with command-line and dialogs (:UserExperience).



NFR 3.2 (Performance): Generate railings in seconds for polylines with <100 segments (:PerformanceIssue mitigation).



NFR 3.3 (Maintainability): Use modular, commented C# code following .NET conventions (:CodeQuality).



NFR 3.4 (Platform): Target AutoCAD 2025, .NET 4.8 (:TechnologyVersion).



NFR 3.5 (Security): Prohibit hard-coded secrets; validate all inputs (:SecurityVulnerability mitigation).



NFR 3.6 (Testability): Design for immediate Targeted Testing Strategy:





Core Logic Testing: Verify calculations, block creation, attribute handling.



Contextual Integration Testing: Ensure components integrate with AutoCAD’s BlockTable and Model Space (:TestDrivenDevelopment).



4. Technical Design Notes





Transactions: Use using (Transaction tr = db.TransactionManager.StartTransaction()) for all database operations; open objects for Read/Write and commit/rollback appropriately (:TransactionPattern, :ResourceManagement).



Coordinate Systems: Store geometry in Block Definitions relative to (0,0,0); transform via BlockReference.TransformBy in World Coordinate System (:GeometryTransformation).



Geometry Handling: Use Entity.Clone for copying entities; generate Polylines with Polyline.AddVertexAt using GetPointAtDist and GetParameterAtPoint (:GeometryManipulation).



Attribute Transfer: Update AttributeReference.TextString post-insertion; use XData (RegAppTable) for Polylines (:DataPersistence).



Layer Management: Access LayerTable via db.LayerTableId.Open; check existence before creating (:LayerManagement).



SAPPO Considerations:





Mitigate :NullPointerException in entity access.



Avoid :MemoryLeak by disposing objects.



Watch for :StackOverflowError in recursive UI loops.



5. Testing Strategy





Manual Testing (Primary):





Define components with varied geometry (lines, arcs) and attributes (skip optional ones).



Generate railings on simple and complex polylines (lines, arcs, corners).



Verify visual output (ZOOM, PAN), block structure (BEDIT), attributes (ATTEDIT), and CSV content.



Test edge cases: empty selections, invalid inputs, missing layers.



Automated Testing (Optional):





Unit tests for spacing calculations, attribute transfer, and XData handling.



SAPPO: Use :UnitTestingPattern, target :LogicError, :CalculationError.



Targeted Testing Strategy:





Core Logic Testing: Verify block creation, attribute assignment, geometry calculations.



Contextual Integration Testing: Ensure blocks integrate with AutoCAD’s BlockTable and Model Space; validate CSV export compatibility.



SAPPO: Mitigate :InterfaceMismatch, :IntegrationError.



6. Deployment/Installation





Compile the C# project into a .dll assembly.



Load in AutoCAD 2025 via NETLOAD command.



Include optional support files (e.g., command icons) if needed.



SAPPO: Mitigate :DeploymentError, :CompatibilityIssue.



7. Development Task List

This aligns with the SAPPO Orchestrator’s micro-tasking and immediate Code->Test->Fix cycle.

Phase 1: Project Setup





Task 1.1: @coder - Create C# .NET 4.8 project with AutoCAD references (AcCoreMgd, AcDbMgd, AcMgd).





SAPPO: :TechnologyVersion (AutoCAD 2025), mitigate :DependencyIssue.



Test: Verify project builds and loads in AutoCAD.



Task 1.2: @architect - Design modular architecture (UI, Geometry, Blocks, Attributes, Export).





SAPPO: Use :ModularDesign, mitigate :ScalabilityBottleneck.



Output: Mermaid class diagram.



Task 1.3: @coder - Implement transaction and error handling utilities.





SAPPO: Use :ExceptionHandlingPattern.



Test: Verify transaction commits/rollbacks.

Phase 2: Component Definition





Task 2.1: @coder - Implement RAIL_DEFINE_COMPONENT command.





Test: Verify command triggers.



Task 2.2: @spec-writer - Specify component type selection UI.





@coder - Implement dropdown/prompt.



Test: Verify all types selectable.



Task 2.3: @coder - Implement entity selection with filters.





Test: Validate only lines, polylines, arcs, circles accepted.



Task 2.4: @coder - Implement base point prompt.





Test: Verify point within bounding box.



Task 2.5: @spec-writer - Specify WinForms dialog with axis prompt.





@coder - Implement dialog.



Test: Verify attribute input and validation.



Task 2.6: @coder - Create Block Definition with unique name.





Test: Verify block in BlockTable.



Task 2.7: @coder - Manage layers per COMPONENTTYPE.





Test: Verify layer creation and assignment.



Task 2.8: @coder - Add error handling for selections, inputs, block creation.





Test: Verify error messages.

Phase 3: Railing Generation





Task 3.1: @coder - Implement RAIL_GENERATE command.





Test: Verify command triggers.



Task 3.2: @spec-writer - Specify WinForms UI with validation.





@coder - Implement UI.



Test: Verify input validation.



Task 3.3: @coder - Implement polyline selection.





Test: Verify only Polyline/Polyline2d accepted.



Task 3.4: @coder - Calculate Post/Picket positions.





Test: Verify spacing accuracy.



Task 3.5: @coder - Create RailingAssemblyBlock.





Test: Verify block creation.



Task 3.6: @coder - Insert Post blocks with orientation and attributes.





Test: Verify positions and attribute transfer.



Task 3.7: @coder - Generate Polylines for horizontal components.





Test: Verify geometry and layer assignment.



Task 3.8: @coder - Attach XData to Polylines.





Test: Verify data persistence.



Task 3.9: @coder - Insert Picket and Mounting blocks.





Test: Verify positions and attributes.



Task 3.10: @coder - Insert RailingAssemblyBlock into Model Space.





Test: Verify insertion.



Task 3.11: @coder - Add error handling.





Test: Verify error messages.

Phase 4: Data Export





Task 4.1: @coder - Implement RAIL_EXPORT_DATA command.





Test: Verify command triggers.



Task 4.2: @coder - Extract data from RailingAssemblyBlock.





Test: Verify attribute and XData extraction.



Task 4.3: @coder - Save CSV with error handling.





Test: Verify file format and content.

Phase 5: Testing and Refinement





Task 5.1: @tester-core - Define manual test scenarios for Phase 2.



Task 5.2: @tester-core - Define manual test scenarios for Phase 3.



Task 5.3: @tester-core - Define manual test scenarios for Phase 4.



Task 5.4: @integrator - Perform integration testing and resolve :CompatibilityIssue.



Task 5.5: @docs-writer - Document components, commands, and testing strategy in Markdown.



8. SAPPO Considerations





:SecurityVulnerability: Prohibit hard-coded secrets; validate inputs to prevent :InjectionVulnerability.



:PerformanceIssue: Optimize for polylines <100 segments; avoid excessive entity cloning.



:CompatibilityIssue: Ensure .NET 4.8 and AutoCAD 2025 compatibility.



:LogicError: Validate calculations in spacing and attribute handling.



:IntegrationError: Test BlockTable and Model Space interactions.



:TargetedTestingStrategy: Apply Core Logic Testing (calculations, block creation) and Contextual Integration Testing (AutoCAD database integration) for each task.