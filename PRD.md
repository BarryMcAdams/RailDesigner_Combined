# Product Requirements Document (PRD) - RailDesigner1: Unified Railing Generator

**Project Title:** RailDesigner1: Unified Railing Generator

**Goal:** To create a single, robust AutoCAD 2025 script using C# .NET 4.8 that allows users to define railing components directly from existing 2D drawing geometry, generate a 2D plan view railing along a selected polyline path utilizing the precise post and picket placement algorithms from `RailCreator_Roo`, and enable export of accurate Bill of Materials (BOM) data to CSV. The script emphasizes in-drawing component definition and preservation of critical placement logic, adhering to SPARC principles.

**Scope:**

*   **Platform:** Executes within AutoCAD 2025 using .NET 4.8 (:TechnologyVersion).
*   **Geometry Focus:** Generates 2D plan view geometry on the XY plane (:Context).
*   **Core Functionality:**
    *   **Unified Generation Command:** Implement command `RAIL_GENERATE` to initiate the entire railing creation process, including inline component definition, polyline selection, and geometry generation (:CommandPattern).
    *   **Inline Component Definition:** Within the `RAIL_GENERATE` command flow, prompt the user sequentially to select 2D entities for specific component types (TopCap, BottomRail, IntermediateRail, Post, Picket, BasePlate, Mounting, User-Defined). After selection, prompt for a base point and collect component attributes via a single WinForms dialog. Create an AutoCAD Block Definition from the selected geometry and attributes, replacing the original entities with a Block Reference at the specified base point. (:ComponentRole: Block Definition, derived from RailGenerator1).
    *   **Polyline Selection:** Prompt the user to select a single 2D Polyline (Polyline/Polyline2d) after component definitions are complete (:ComponentRole: Input Handler).
    *   **Railing Generation:** Generate 2D railings along the user-selected polyline based on the *newly defined component blocks* and the calculations from the preserved placement logic (:ComponentRole: Geometry Assembly).
    *   **Placement Logic:** ***Crucially preserves and utilizes the exact post and picket placement algorithms from `RailCreator_Roo` (`PicketGenerator.cs`, `PostGenerator.cs`)***. These algorithms are assumed to contain internal rules for ends, corners, spacing constraints (e.g., 10-50 inch post spans, <=4 inch picket spacing), and decorative picket handling, requiring minimal external parameter input beyond the polyline geometry itself (:ComponentRole: Structural Calculator, Detail Renderer, derived from RailCreator_Roo).
    *   **Output Structure:** Generates railing as individual entities (Posts/Pickets/Mounts as Block References referencing the newly defined blocks, Rails as Polylines) with attached attributes/XData for flexibility, NOT a single block reference for the whole assembly (:ArchitecturalPattern, differs from RailGenerator1).
    *   **Data Management:** Attributes embedded in component blocks and attached to rail polylines via XData (:ComponentRole: Data Management).
    *   **BOM Export:** Implement a separate command `RAIL_EXPORT_BOM` to provide CSV export functionality by extracting data from the generated entities (:Feature, combines both).
*   **Units:** Assumes Decimal Inches (:EnvironmentContext).
*   **SPARC Integration:** Uses SPARC terminology (:Problem, :Solution, etc.) to frame requirements and mitigate risks (:CompatibilityIssue, :LogicError, :AlgorithmIntegrity).

**Out of Scope (for this version):**

*   A dedicated UI for selecting standard railing designs from a library (JSON file).
*   Saving/Loading complete railing *design configurations*. This script focuses on defining components *per generation*.
*   3D geometry generation.
*   Multiple simultaneous polyline paths.
*   Advanced railing editing beyond entity manipulation.
*   Structural analysis or code compliance checks.
*   Handling decorative picket placement rules beyond what is *strictly contained* in the preserved `PicketGenerator.cs` logic. Specific offsets, detailed decorative patterns, or user control over decorative picket placement frequency/location are deferred.
*   Handling complex offsets (e.g., specific rail offsets) during generation beyond what is *strictly contained* in the preserved `RailCreator_Roo` logic.
*   Material weight calculations during generation; Railing Height is for BOM data capture only.

**Target User:** AutoCAD users in aluminum fabrication detailing custom and standard railings, requiring accurate placement based on established rules and BOM generation, preferring to define components directly from drawing geometry (:UserContext).

---

## 1. User Stories

*   As a user, I want a single command to start the railing generation process.
*   As a user, I want the script to guide me through selecting existing 2D geometry for each type of railing component needed (Posts, Pickets, etc.).
*   As a user, after selecting geometry for a component, I want a form to pop up where I can easily enter attributes like Part Name, Description, and Material for that component.
*   As a user, I want the script to automatically create an AutoCAD Block Definition from my selected geometry and attributes for each component type.
*   As a user, I want the script to replace the original geometry I selected with an instance of the newly created block at a base point I specify.
*   As a user, once I've defined the necessary components, I want to select a 2D polyline (straight or curved) as the path for my railing.
*   As a user, I want the script to automatically place the *defined* post and picket blocks along the polyline using the company's specific rules *exactly* as defined in the provided placement logic files (ends, corners, max spacing).
*   As a user, I want the generated railing to consist of individual, editable entities (Blocks for posts/pickets/mounts, Polylines for rails) with embedded attribute and XData.
*   As a user, I want a separate command to export a detailed BOM/Cut List to a CSV file based on the generated entities.
*   As a user, I want clear error messages if something goes wrong (invalid selection, missing required component definition, issues during block creation).

---

## 2. Functional Requirements

**REQ 1.0: Unified Generation Command & Flow**

*   **REQ 1.1:** Implement command `RAIL_GENERATE` to launch the main process flow (:CommandPattern).
*   **REQ 1.2:** The `RAIL_GENERATE` command will sequentially execute the following steps:
    *   Prompt for and define component blocks (see REQ 2.0).
    *   Prompt for and select the polyline path (see REQ 3.0).
    *   Generate railing geometry using preserved logic and defined components (see REQ 4.0).
    *   SAPPO: Use a state machine or clear sequential logic flow within the command handler to manage the steps (:StatePattern, :SequencePattern). Handle cancellation at each major step.

**REQ 2.0: Inline Component Definition (within RAIL_GENERATE)**

*   **REQ 2.1:** For each required component type (e.g., Posts, Pickets, TopCap, BottomRail, IntermediateRail, BasePlate, Mounting, User-Defined), the script will:
    *   Prompt user via command line or simple dialog list for the current Component Type being defined.
    *   Prompt user to select 2D entities for geometry using `Editor.GetSelection` with filters for relevant entity types (lines, polylines, arcs, circles, ellipses). Allow single or multiple entities per component.
        *   SAPPO: Mitigate :SelectionError, :TypeMismatch.
    *   Prompt user for a base point for the selected geometry using `Editor.GetPoint`.
        *   SAPPO: Watch for :UserInputError.
    *   Present a **single WinForms dialog** to collect attributes for this specific component type. The dialog fields will dynamically adjust or include relevant fields based on the current `COMPONENTTYPE` (e.g., Pickets might have `HEIGHT` and `WEIGHT_DENSITY` per linear inch or each, Rails might have `WIDTH` and `WEIGHT_DENSITY` per linear inch, Posts might have `HEIGHT` and `WEIGHT_DENSITY` per each). The dialog must include:
        *   `PARTNAME` (String, Mandatory): Unique identifier, used as Block Name.
        *   `DESCRIPTION` (String, Optional).
        *   `MATERIAL` (String, Optional).
        *   `FINISH` (String, Optional).
        *   `WEIGHT_DENSITY` (Numeric, Optional): Weight per linear inch or per item, based on component type (e.g., 0.098 lb/in³ for aluminum).
        *   `STOCK_LENGTH` (Numeric, Optional): Standard procurement size.
        *   `SPECIAL_NOTES` (String, Optional): Free text.
        *   `USER_ATTRIBUTE_1`, `USER_ATTRIBUTE_2` (String, Optional): Custom fields.
        *   A hidden, mandatory attribute `COMPONENTTYPE` matching the currently prompted type (e.g., "POST", "PICKET").
        *   A field for `RailingHeight` (Numeric, Mandatory) which captures the *intended final installed height* for BOM purposes, not geometry generation.
        *   Validation: Ensure mandatory fields are filled; validate numeric inputs.
        *   SAPPO: Mitigate :InputValidationError, use :FormPattern.
    *   Create a Block Definition in the BlockTable named `<PARTNAME>`. If `<PARTNAME>` exists, confirm overwrite or prompt for a new name.
        *   SAPPO: Watch for :NamingConflict.
    *   Copy selected entities into the Block Definition using `Entity.Clone`, offset relative to the base point.
        *   SAPPO: Mitigate :GeometryTransformationError.
    *   Add AttributeDefinitions for all specified attributes to the Block Definition.
        *   SAPPO: Ensure :AttributeConsistency.
    *   Assign geometry within the Block Definition to a layer `L-RAIL-<COMPONENTTYPE>`. Create layer if missing with default properties (Color 7, Continuous).
        *   SAPPO: Mitigate :LayerCreationError.
    *   **Replace Original Entities:** Delete the original selected entities and insert a Block Reference of the newly created `<PARTNAME>` block at the specified base point in the drawing space.
        *   SAPPO: Mitigate :InsertionError, :DeletionError.
*   **REQ 2.2:** Ensure a way to identify the defined blocks later during generation. Embedding the `COMPONENTTYPE` attribute in the Block Definition seems the most robust method.
*   **REQ 2.3:** Allow the user to indicate when they have finished defining components (e.g., via a "Done Defining Components" option after the last prompted type or a button on the attribute form).

**REQ 3.0: Polyline Selection (within RAIL_GENERATE)**

*   **REQ 3.1:** After component definition is complete, prompt user to select a single 2D Polyline (Polyline or Polyline2d) using `Editor.GetEntity`.
    *   SAPPO: Mitigate :SelectionError.
*   **REQ 3.2:** Validate selection is a 2D polyline. Provide clear error message if invalid.

**REQ 4.0: Railing Generation (within RAIL_GENERATE)**

*   **REQ 4.1:** Identify the required component block definitions (Posts, Pickets, TopCap, etc.) in the BlockTable based on their `COMPONENTTYPE` attribute, which was set during the inline definition phase (REQ 2.1).
    *   SAPPO: Handle cases where a required component type block was not defined by the user.
*   **REQ 4.2:** **Preserve and Utilize Placement Logic:** Instantiate and call the core methods from the copied `PicketGenerator.cs` and `PostGenerator.cs` files. Pass the selected Polyline object to these methods. Rely on the internal logic within these files to calculate post and picket placement points and orientations based on their inherent rules (spacing, corners, etc.).
    *   SAPPO: Critical :AlgorithmIntegrity. Ensure the data structures passed to the preserved logic match its expectations. Create wrapper methods if needed, but do not alter the core calculation logic within the copied files. Mitigate :CalculationError.
*   **REQ 4.3:** Based on the calculated positions and orientations from the placement logic (REQ 4.2) and the identified component blocks (REQ 4.1):
    *   Insert Post blocks (using the defined Post block's name) as BlockReferences at calculated positions/orientations. Populate AttributeReferences using data from the defined block's AttributeDefinitions (which hold the values entered in the form, REQ 2.1).
    *   Insert Picket blocks (using the defined Picket block's name) as BlockReferences at calculated positions/orientations. Populate AttributeReferences. Handle different implied picket types as per the preserved logic.
    *   Insert Mounting component blocks as needed based on the defined Mounting block, if applicable.
    *   Generate TopCap and Bottom/Intermediate Rails as Polylines following the selected path polyline segments. Attach relevant properties (PARTNAME/Profile Name, MATERIAL, FINISH, INSTALLED_LENGTH, WEIGHT_DENSITY) via XData, pulling this data from the attributes entered during the inline definition of the respective rail components (REQ 2.1).
    *   SAPPO: Mitigate :GeometryTransformationError, :AttributeConsistency, :DataPersistence (XData).
*   **REQ 4.4:** Assign all generated entities (Block References, Polylines) to appropriate layers (e.g., `L-RAIL-POST`, `L-RAIL-TOPCAP`, `L-RAIL-PICKET`). Create layers if missing.

**REQ 5.0: Data & BOM Export**

*   **REQ 5.1:** Implement command `RAIL_EXPORT_BOM` (:CommandPattern). This command operates independently of `RAIL_GENERATE` but acts on the entities created by it.
*   **REQ 5.2:** Prompt user to select the generated railing entities (allow window/crossing selection). Filter for expected entity types (BlockReferences with relevant COMPONENTTYPE attribute, Polylines on rail layers with XData) to avoid including unrelated drawing objects.
    *   SAPPO: Mitigate :SelectionError.
*   **REQ 5.3:** Iterate through selected entities:
    *   For BlockReferences (Posts, Pickets, Mounts): Extract AttributeReference values (PARTNAME, DESCRIPTION, MATERIAL, FINISH, etc.) and calculate quantity. Capture RailingHeight attribute value.
    *   For Polylines (Rails): Extract XData (PARTNAME/ProfileName, MATERIAL, FINISH, WEIGHT_DENSITY) and calculate length (`INSTALLED_LENGTH`).
    *   For both types, capture COMPONENTTYPE. Calculate WEIGHT based on INSTALLED_LENGTH/HEIGHT/Quantity and WEIGHT_DENSITY.
*   **REQ 5.4:** Compile data into a list suitable for CSV export. Include columns like: `COMPONENTTYPE`, `PARTNAME`/`ProfileName`, `DESCRIPTION`, `QUANTITY`, `INSTALLED_LENGTH`, `INSTALLED_HEIGHT` (from Post/Picket/RailingHeight attribute), `WIDTH`, `HEIGHT`, `MATERIAL`, `FINISH`, `WEIGHT` (calculated), `SPECIAL_NOTES`, `USER_ATTRIBUTE_1`, `USER_ATTRIBUTE_2`.
*   **REQ 5.5:** Save data to `<Desktop>/RailingBOM_<yyyyMMdd_HHmmss>.csv` with a header row.
    *   SAPPO: Mitigate :FileIOException, :DataFormatError.

**REQ 6.0: Common Requirements**

*   **REQ 6.1 (Error Handling):** Implement robust `try-catch` blocks for AutoCAD API calls, file operations (CSV export), form interactions, calculations, and user input. Provide informative messages via `Editor.WriteMessage` or `MessageBox`. Log errors to a file (:ExceptionHandlingPattern). Handle user cancellation gracefully at prompts/forms.
*   **REQ 6.2 (Transaction Management):** Use AutoCAD transactions (`using (Transaction tr = ...)`) for all database modifications (Block creation, entity generation/modification/deletion, layer creation) (:TransactionPattern, :ResourceManagement). Open objects for read/write as needed.
*   **REQ 6.3 (Coordinate Systems):** Define components relative to (0,0,0) in Block Definitions. Use `BlockReference.TransformBy` for placement and orientation in WCS (:GeometryTransformation). Ensure polyline geometry is treated as 2D (XY plane).
*   **REQ 6.4 (Layer Management):** Use naming convention `L-RAIL-<COMPONENTTYPE>`; create layers only if absent using default properties.
*   **REQ 6.5 (Attribute/XData Management):** Ensure AttributeDefinitions are correctly added to Block Definitions. Populate AttributeReferences in Block Insertions using values from the definition form. Use Registered Application names for XData attached to rails (e.g., "RAILDESIGNER1_RAILDATA"). Handle data extraction from both sources for BOM.

---

## 3. Non-Functional Requirements

*   **NFR 3.1 (Usability):** The sequence of prompts and the attribute form should be intuitive for AutoCAD users. Error messages should be clear.
*   **NFR 3.2 (Performance):** Railing generation should be reasonably fast (<10-15 seconds) for typical polylines (<100 vertices). Block creation/replacement should be efficient.
*   **NFR 3.3 (Maintainability):** Code should be modular (separate classes for the main command flow, component definition logic, the attribute form, railing generation orchestration, placement logic wrappers, BOM export), well-commented, and follow C#/.NET best practices (:CodeQuality, :ModularDesign).
*   **NFR 3.4 (Platform):** Target AutoCAD 2025, .NET 4.8 (:TechnologyVersion).
*   **NFR 3.5 (Security):** Validate all user inputs. Be mindful of file path manipulation during CSV export (:SecurityVulnerability mitigation).
*   **NFR 3.6 (Testability):** Design components for testability. The core placement logic, once wrapped, should ideally be testable with predefined polyline inputs and expected position outputs. The attribute form data collection should be testable. Block creation and attribute population should be verifiable. (:UnitTestingPattern).
*   **NFR 3.7 (Algorithm Preservation):** The primary non-functional goal is the accurate preservation and integration of the `RailCreator_Roo` post/picket placement logic from the specified files (:AlgorithmIntegrity). This logic should function exactly as it did in its original context when fed polyline data.

---

## 4. Technical Design Notes

*   **Dependencies:** AutoCAD APIs (AcCoreMgd, AcDbMgd, AcMgd), `System.Windows.Forms`, potentially `Newtonsoft.Json` if used for config (though JSON design file is out), but not strictly required based on current scope.
*   **Project Structure:** Use separate classes: `Plugin.cs` (command entry points), `RailGenerateCommand.cs` (orchestrates the generation flow), `ComponentDefiner.cs` (handles entity selection, base point, form interaction, block creation/replacement), `AttributeForm.cs` (the single WinForms dialog), `RailingGeometryGenerator.cs` (calls placement logic and creates AutoCAD entities), `PostPlacementLogicWrapper.cs`, `PicketPlacementLogicWrapper.cs` (adaptors/wrappers for the *copied* logic files), `BomExporter.cs`.
*   **Placement Logic Integration:** Carefully copy the relevant `.cs` files (`PicketGenerator.cs`, `PostGenerator.cs`) into the new project. Create thin wrapper classes or methods that instantiate/call the core logic within these copied files, translating inputs (the selected Polyline) and outputs (lists of points/orientations) to fit the `RailingGeometryGenerator.cs`. *Avoid modifying the logic within the copied files.*
*   **Attribute Form Logic:** The `AttributeForm` will need logic to show/hide/label fields based on the `COMPONENTTYPE` currently being defined. It will return a dictionary or custom object containing the entered attribute values.
*   **XData:** Use a consistent Registered Application name. Store key-value pairs for relevant rail data.
*   **Block Identification:** After the component definition phase, `RailingGeometryGenerator.cs` must retrieve the necessary block definitions (Posts, Pickets, etc.). Scanning the BlockTable for blocks with the expected `COMPONENTTYPE` attribute added during definition (REQ 2.1) is recommended.

---

## 5. Testing Strategy

*   **Targeted Testing Strategy:**
    *   **Component Definition Testing:** Manually test `RAIL_GENERATE` flow section for defining each component type (Posts, Pickets, Rails, etc.). Use varied 2D geometry. Verify form displays correctly, attribute values are captured, block is created with correct geometry/layers/attributes, and original entities are replaced.
    *   **Placement Logic Testing:** (Most Critical) If feasible, create isolated tests or perform rigorous manual testing by providing known polyline inputs and visually verifying the calculated placement points/orientations from the *wrapped* `PostPlacementLogic` and `PicketPlacementLogic`. Compare output against expected results from `RailCreator_Roo`.
    *   **Integration Testing (Generation):** Test the full `RAIL_GENERATE` flow: define all necessary components -> select polyline (straight, curved, complex with multiple vertices) -> verify visual output in AutoCAD (posts, pickets, rails, mounts) against the expected placement rules from the preserved logic. Verify attributes on blocks and XData on polylines.
    *   **BOM Export Testing:** Test `RAIL_EXPORT_BOM`. Select generated entities. Verify CSV file content (quantities, lengths, attributes, calculated weight) and format are accurate and complete based on the generated geometry and defined attributes.
    *   **Error Handling Testing:** Test scenarios like invalid entity selection, cancelling prompts, missing component definitions before generation, invalid attribute input, file writing errors during BOM export. Verify informative error messages.
*   **Manual Testing (Primary):** Execute defined test cases covering all user stories and functional requirements through the AutoCAD interface. Visual inspection of generated geometry is essential.

---

## 6. Deployment/Installation

*   Remind the user to compile the C# project `RailDesigner1` in Visual Studio targeting .NET 4.8.
*   The output will be a `.dll` assembly (e.g., `RailDesigner1.dll`).
*   Load the `.dll` into AutoCAD 2025 using the `NETLOAD` command.
*   Mention any potential supporting files required (though none specified yet beyond the DLL).

---