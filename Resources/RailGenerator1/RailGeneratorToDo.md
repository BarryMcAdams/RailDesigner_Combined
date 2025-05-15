# Development Task List for AutoCAD Railing Generator

This task list is based on PRD section 7 and follows SAPPO principles for micro-tasking. Tasks are marked with [x] for completed and [ ] for pending. Update this list as tasks are completed.

## Phase 1: Project Setup
- Task 1.1: @coder - Create C# .NET 4.8 project with AutoCAD references (AcCoreMgd, AcDbMgd, AcMgd) [x]  
  SAPPO: :TechnologyVersion (AutoCAD 2025), mitigate :DependencyIssue.  
  Test: Verify project builds and loads in AutoCAD.
- Task 1.2: @architect - Design modular architecture (UI, Geometry, Blocks, Attributes, Export) [x]  
  SAPPO: Use :ModularDesign, mitigate :ScalabilityBottleneck.  
  Output: Mermaid class diagram.
- Task 1.3: @coder - Implement transaction and error handling utilities [ ]  
  SAPPO: Use :ExceptionHandlingPattern.  
  Test: Verify transaction commits/rollbacks.

## Phase 2: Component Definition
- Task 2.1: @coder - Implement RAIL_DEFINE_COMPONENT command [ ]  
  SAPPO: Watch for :UserInputError.  
  Test: Verify command triggers.
- Task 2.2: @spec-writer - Specify component type selection UI; @coder - Implement dropdown/prompt [ ]  
  SAPPO: Use :EnumPattern for type safety.  
  Test: Verify all types selectable.
- Task 2.3: @coder - Implement entity selection with filters [ ]  
  SAPPO: Mitigate :SelectionError.  
  Test: Validate only lines, polylines, arcs, circles accepted.
- Task 2.4: @coder - Implement base point prompt [ ]  
  SAPPO: Watch for :OutOfBoundsError.  
  Test: Verify point within bounding box.
- Task 2.5: @spec-writer - Specify WinForms dialog with axis prompt; @coder - Implement dialog [ ]  
  SAPPO: Mitigate :InputValidationError.  
  Test: Verify attribute input and validation.
- Task 2.6: @coder - Create Block Definition with unique name [ ]  
  SAPPO: Watch for :NamingConflict.  
  Test: Verify block in BlockTable.
- Task 2.7: @coder - Manage layers per COMPONENTTYPE [ ]  
  SAPPO: Mitigate :LayerCreationError.  
  Test: Verify layer creation and assignment.
- Task 2.8: @coder - Add error handling for selections, inputs, block creation [ ]  
  SAPPO: Mitigate :UserInputError.  
  Test: Verify error messages.

## Phase 3: Railing Generation
- Task 3.1: @coder - Implement RAIL_GENERATE command [ ]  
  SAPPO: Watch for :UserInputError.  
  Test: Verify command triggers.
- Task 3.2: @spec-writer - Specify WinForms UI with validation; @coder - Implement UI [ ]  
  SAPPO: Use :FormPattern.  
  Test: Verify input validation.
- Task 3.3: @coder - Implement polyline selection [ ]  
  SAPPO: Mitigate :SelectionError.  
  Test: Verify only Polyline/Polyline2d accepted.
- Task 3.4: @coder - Calculate Post/Picket positions [ ]  
  SAPPO: Use :AlgorithmPattern, watch for :ArithmeticError.  
  Test: Verify spacing accuracy.
- Task 3.5: @coder - Create RailingAssemblyBlock [ ]  
  SAPPO: Watch for :NamingConflict.  
  Test: Verify block creation.
- Task 3.6: @coder - Insert Post blocks with orientation and attributes [ ]  
  SAPPO: Mitigate :GeometryTransformationError.  
  Test: Verify positions and attribute transfer.
- Task 3.7: @coder - Generate Polylines for horizontal components [ ]  
  SAPPO: Mitigate :GeometryTransformationError.  
  Test: Verify geometry and layer assignment.
- Task 3.8: @coder - Attach XData to Polylines [ ]  
  SAPPO: Ensure :AttributeConsistency.  
  Test: Verify data persistence.
- Task 3.9: @coder - Insert Picket and Mounting blocks [ ]  
  SAPPO: Mitigate :GeometryTransformationError.  
  Test: Verify positions and attributes.
- Task 3.10: @coder - Insert RailingAssemblyBlock into Model Space [ ]  
  SAPPO: Watch for :InsertionError.  
  Test: Verify insertion.
- Task 3.11: @coder - Add error handling [ ]  
  SAPPO: Mitigate :UserInputError.  
  Test: Verify error messages.

## Phase 4: Data Export
- Task 4.1: @coder - Implement RAIL_EXPORT_DATA command [ ]  
  SAPPO: Use :DataExportPattern.  
  Test: Verify command triggers.
- Task 4.2: @coder - Extract data from RailingAssemblyBlock [ ]  
  SAPPO: Mitigate :AttributeConsistency.  
  Test: Verify attribute and XData extraction.
- Task 4.3: @coder - Save CSV with error handling [ ]  
  SAPPO: Mitigate :FileIOException.  
  Test: Verify file format and content.

## Phase 5: Testing and Refinement
- Task 5.1: @tester-core - Define manual test scenarios for Phase 2 [ ]  
  SAPPO: Use :UnitTestingPattern.
- Task 5.2: @tester-core - Define manual test scenarios for Phase 3 [ ]  
  SAPPO: Use :UnitTestingPattern.
- Task 5.3: @tester-core - Define manual test scenarios for Phase 4 [ ]  
  SAPPO: Use :UnitTestingPattern.
- Task 5.4: @integrator - Perform integration testing and resolve :CompatibilityIssue [ ]  
  SAPPO: Mitigate :IntegrationError.
- Task 5.5: @docs-writer - Document components, commands, and testing strategy in Markdown [ ]  
  SAPPO: Ensure :DocumentationCompleteness.