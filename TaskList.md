 Development Task List (SPARC-Principled) - RailDesigner1

**Phase 0: Setup & Source Code Integration**

*   **Task 0.1:** @coder - Create new C# Class Library project (.NET 4.8) named `RailDesigner1` in the specified root directory (`C:\Users\barrya\source\repos\RailDesigner1\`). Add AutoCAD & Forms references. (`RailDesigner1.sln`, `RailDesigner1.csproj`). (:TechnologyVersion, :DependencyIssue mitigation).
    *   Test: Project builds successfully.
*   **Task 0.2:** @architect - Define final class structure diagram (Mermaid) and textual diagram based on the refined PRD structure (e.g., `RailGenerateCommand`, `ComponentDefiner`, `AttributeForm`, `RailingGeometryGenerator`, `PlacementLogicWrappers`, `BomExporter`). (:ModularDesign).
    *   Output: `Architecture.md`.
*   **Task 0.3:** @coder - **Isolate and Copy Placement Logic:** Carefully copy `PicketGenerator.cs` and `PostGenerator.cs` from the Resources folder into the `RailDesigner1` project. *Do NOT modify their internal calculation logic.* Create initial wrapper classes/methods (`PostPlacementLogicWrapper`, `PicketPlacementLogicWrapper`) that will eventually call the core methods in the copied files. Document any immediate dependencies from the original `RailCreator_Roo` project required *just for these two files to compile* (e.g., simple structs, enums, math helpers - copy minimum necessary code). (:AlgorithmIntegrity).
    *   Test: Project still builds *after* copying and creating initial wrappers. If not, identify and add minimal necessary helper code from RailCreator_Roo until compile succeeds.
*   **Task 0.4:** @coder - Implement common utilities: Transaction Manager wrapper, Error Handler/Logger, AutoCAD object helpers (e.g., getting BlockTable, LayerTable). (:ExceptionHandlingPattern, :ResourceManagement).
    *   Test: Verify basic logging and transaction handling in a simple test command.

**Phase 1: Unified Generation Command Orchestration & Component Definition Flow**

*   **Task 1.1:** @coder - Implement `RAIL_GENERATE` command stub in `Plugin.cs`. This command will initiate the sequence of steps. (:CommandPattern).
*   **Task 1.2:** @coder - Implement the sequential flow within `RailGenerateCommand.cs` or the command handler:
    *   Loop/Sequence for defining components (call `ComponentDefiner` methods).
    *   Call method for polyline selection.
    *   Call method for railing generation.
    *   Handle cancellation or errors at each step.
    *   SAPPO: Use a state machine or clear sequence logic (:SequencePattern).
    *   Test: Verify the command executes the steps sequentially (use placeholders/stubs for now).
*   **Task 1.3:** @coder - Implement `ComponentDefiner.cs`: Add methods for sequential prompting of Component Type (e.g., `PromptForComponentDefinition("POST")`, `PromptForComponentDefinition("PICKET")`). These methods will contain the flow for entity selection, base point prompt, showing the attribute form, creating the block, adding attributes, managing layers, and replacing original entities. (:BlockPattern, :AttributeConsistency).
    *   Test: Run the partial `RAIL_GENERATE` command, verify prompts appear sequentially for different component types.

**Phase 2: Attribute Definition Form**

*   **Task 2.1:** @spec-writer - Design the layout and fields for the single `AttributeForm.cs` WinForms dialog. Specify how fields will be enabled/labeled based on the current `COMPONENTTYPE`.
*   **Task 2.2:** @coder - Implement `AttributeForm.cs` layout and controls. Add properties/methods to configure fields based on the `COMPONENTTYPE` being defined. Add validation logic. (:FormPattern, :InputValidationError mitigation).
    *   Test: Show the form programmatically, verify layout and dynamic field display based on a passed component type string. Verify basic validation.
*   **Task 2.3:** @coder - Integrate `AttributeForm` into `ComponentDefiner.PromptForComponentDefinition` methods. Pass the current component type, show the form, retrieve entered attributes.
    *   Test: Run the partial `RAIL_GENERATE` command, verify the form pops up with relevant fields after selecting geometry for a component type. Verify entered data is captured.

**Phase 3: Block Creation & Replacement (within Component Definition Flow)**

*   **Task 3.1:** @coder - Implement block creation logic within `ComponentDefiner.PromptForComponentDefinition`. Get geometry from selection, copy to a new Block Definition, name it using the PARTNAME attribute, add AttributeDefinitions based on form data. (:BlockPattern, :AttributeConsistency).
    *   Test: Run definition flow for one component type. Verify block exists in BlockTable with correct geometry and AttributeDefinitions.
*   **Task 3.2:** @coder - Implement layer management logic within `ComponentDefiner`. Assign geometry inside the block to `L-RAIL-<COMPONENTTYPE>`. Create layer if missing. (:LayerManagement).
    *   Test: Verify correct layer is used/created.
*   **Task 3.3:** @coder - Implement replacement logic within `ComponentDefiner`. Delete original selected entities. Insert a Block Reference of the newly created block at the specified base point. Populate AttributeReferences with values from the form. (:GeometryTransformationError, :DeletionError, :InsertionError, :AttributeConsistency).
    *   Test: Define a component, verify original entities are gone and replaced by a block reference in the drawing. Verify block reference attributes match form input.

**Phase 4: Polyline Selection**

*   **Task 4.1:** @coder - Implement Polyline selection prompt within `RailGenerateCommand` flow, after component definition is complete. Use `Editor.GetEntity` with filters. (:SelectionError mitigation).
    *   Test: Run `RAIL_GENERATE` up to this point. Verify polyline selection prompt appears and accepts only 2D polylines.

**Phase 5: Railing Generation Core Logic**

*   **Task 5.1:** @coder - Implement `RailingGeometryGenerator.cs` class. Add a method (e.g., `GenerateRailing(Polyline pathPolyline, Dictionary<string, ObjectId> componentBlocks)`).
*   **Task 5.2:** @coder - In `RailGenerateCommand`, identify the object IDs of the block definitions created/used during component definition (e.g., by scanning the BlockTable for blocks with the expected `COMPONENTTYPE` attribute). Pass these IDs to `RailingGeometryGenerator`.
*   **Task 5.3:** @coder - Refine `PostPlacementLogicWrapper.cs` and `PicketPlacementLogicWrapper.cs`. These classes will contain methods that take the Polyline path and return calculated points/orientations by calling the core logic within the *copied* `PicketGenerator.cs` and `PostGenerator.cs` files (Task 0.3). Adapt data structures as needed to interface with the original logic. (:AlgorithmIntegrity, :InterfaceMismatch mitigation).
    *   Test: Feed sample polyline geometry into the wrapper methods and debug/verify the calculated positions/orientations match expected output based on the original logic's behavior. This is CRITICAL.
*   **Task 5.4:** @coder - In `RailingGeometryGenerator`, call the wrapper methods (Task 5.3) to get placement data.
*   **Task 5.5:** @coder - Implement Block Reference insertion for Posts, Pickets, Mounts based on placement data. Populate AttributeReferences using values from the *defined Block Definition's* AttributeDefinitions. (:GeometryTransformation, :AttributeConsistency).
*   **Task 5.6:** @coder - Implement Polyline generation for Rails. Create geometry along path segments. Attach XData containing attribute data pulled from the respective defined rail Block Definition's attributes. (:GeometryManipulation, :DataPersistence).
*   **Task 5.7:** @coder - Implement layer management for all generated entities. (:LayerManagement).

**Phase 6: BOM Export**

*   **Task 6.1:** @coder - Implement `RAIL_EXPORT_BOM` command in `Plugin.cs`.
*   **Task 6.2:** @coder - Implement `BomExporter.cs` class. Add methods for entity selection/filtering (filtering by `COMPONENTTYPE` attribute/XData, layer names). (:SelectionError mitigation).
*   **Task 6.3:** @coder - Implement data extraction logic from Block References (Attributes) and Polylines (XData). Calculate weight and determine INSTALLED_HEIGHT/LENGTH. (:DataExtraction).
*   **Task 6.4:** @coder - Implement CSV file generation and saving logic. Format columns per REQ 5.4. Handle file writing errors. (:DataExportPattern, :FileIOException).
    *   Test: Generate a railing, run BOM export, verify CSV file content and format accuracy.

**Phase 7: Testing, Refinement & Documentation**

*   **Task 7.1:** @tester-core - Define detailed manual test cases covering the full `RAIL_GENERATE` flow (component definition variations, polyline types - straight, arc, mixed, multi-segment corners).
*   **Task 7.2:** @tester-core - Define detailed manual test cases for `RAIL_EXPORT_BOM` covering different generated railing types and component data.
*   **Task 7.3:** @tester-core - Execute manual test plan. Log defects.
*   **Task 7.4:** @coder - Fix defects identified during testing. (:CodeQuality).
*   **Task 7.5:** @integrator - Perform end-to-end testing focusing on the integration of all components and accuracy of the preserved placement logic outputs and BOM data. (:IntegrationTesting, :AlgorithmIntegrity).
*   **Task 7.6:** @docs-writer - Create `README.md` explaining installation, usage (`RAIL_GENERATE`, `RAIL_EXPORT_BOM` command flow, attribute form use), and basic troubleshooting.
*   **Task 7.7:** @coder - Code cleanup, add comments, ensure adherence to standards. Ensure all SAPPO considerations (Transaction management, Layer management, Error handling, Attribute/XData consistency) are robustly implemented.