# RailDesigner1: Unified Railing Generator for AutoCAD 2025

## Overview
RailDesigner1 is a C# .NET 4.8 plugin for AutoCAD 2025 that enables users to define railing components directly from 2D drawing geometry, generate 2D plan view railings along a selected polyline path, and export a Bill of Materials (BOM) to CSV. It utilizes the precise post and picket placement algorithms from the referenced `RailCreator_Roo` system and adheres to SPARC principles for robust, maintainable code.

This plugin was developed based on the Product Requirements Document (`PRD.md`).

## Project Status
This project has undergone significant development to implement core features. It should now be largely functional for defining components, generating railings, and exporting BOMs. Real-world testing in AutoCAD 2025 by the user is essential to validate all functionalities.

## Features
- **Unified Generation Command (`RAIL_GENERATE`)**: Guides users through:
    - **Inline Component Definition**: Sequentially define components (Posts, Pickets, Top Rail, Bottom Rail, etc.) by selecting existing 2D entities in your drawing, specifying a base point, and inputting detailed attributes via a popup form. The system creates AutoCAD blocks from your geometry and attributes.
    - **Polyline Path Selection**: Select a 2D polyline to define the path of the railing.
    - **Railing Geometry Generation**: Automatically generates the 2D railing assembly along the polyline, placing the defined component blocks using the preserved placement logic from `RailCreator_Roo` for posts and pickets. Rails (Top, Bottom) are generated as polylines with XData.
- **Bill of Materials Export (`RAIL_EXPORT_BOM`)**:
    - Select generated railing components in the drawing.
    - Exports a comprehensive BOM to a CSV file, including quantities, lengths, materials, finishes, and calculated weights.

## Installation and Deployment
1.  **Compile:** Compile the C# project `RailDesigner1` in Visual Studio (ensure target framework is .NET 4.8). This will produce `RailDesigner1.dll`.
2.  **Load in AutoCAD:**
    *   Open AutoCAD 2025.
    *   Type `NETLOAD` in the command line.
    *   Browse to and select the `RailDesigner1.dll` file.
3.  **Commands:** The following commands will be available:
    *   `RAIL_GENERATE`
    *   `RAIL_EXPORT_BOM`

## Usage

### 1. Generating a Railing (`RAIL_GENERATE`)

The `RAIL_GENERATE` command orchestrates the entire process:

1.  **Run the Command:** Type `RAIL_GENERATE` in the AutoCAD command line and press Enter.
2.  **Component Definition:**
    *   The system will prompt you to define a series of components (e.g., "POST", "PICKET", "TOPRAIL", "BOTTOMRAIL").
    *   For each component:
        *   **Select Geometry:** Select the 2D AutoCAD entities that represent the component's shape.
        *   **Specify Base Point:** Pick a base point for the component. This point will be used as the insertion point for blocks and the origin within the block definition.
        *   **Enter Attributes:** An "Attribute Definition" form will appear.
            *   Fill in details like `PARTNAME` (used for the block name), `DESCRIPTION`, `MATERIAL`, `FINISH`, `WEIGHT_DENSITY`, `RailingHeight` (overall railing height for BOM), etc.
            *   `COMPONENTTYPE` is set automatically.
            *   Ensure `PARTNAME` and `RailingHeight` are provided. Numeric fields like `WEIGHT_DENSITY` and `RailingHeight` must be valid numbers.
            *   Click "OK" to confirm attributes or "Cancel". If you cancel, the `RAIL_GENERATE` command will typically abort.
        *   The system will create an AutoCAD block definition from your geometry and attributes. The original entities you selected will be erased and replaced with an instance of this new block.
    *   This process will repeat for each component type required by the system.
3.  **Select Polyline Path:**
    *   After all components are defined, you will be prompted to "Select a polyline for the railing path:".
    *   Select a single 2D Polyline or Polyline2d in your drawing.
4.  **Geometry Generation:**
    *   The plugin will then generate the railing geometry along the selected polyline:
        *   **Posts and Pickets:** Instances of the "POST" and "PICKET" blocks you defined will be placed according to the original `RailCreator_Roo` placement algorithms.
        *   **Rails (Top/Bottom):** "TOPRAIL" and "BOTTOMRAIL" will be generated as new polyline entities along the path, at vertical offsets determined by the `RailingHeight` and `BottomClearance` values (from the default `RailingDesign` object, which could be further customized). These rail polylines will have XData attached, derived from the attributes you defined for them.
5.  **Completion:** Messages in the command line will indicate progress and completion.

### 2. Exporting Bill of Materials (`RAIL_EXPORT_BOM`)

1.  **Run the Command:** Type `RAIL_EXPORT_BOM` in the AutoCAD command line and press Enter.
2.  **Select Components:**
    *   You will be prompted to "Select railing components for BOM:".
    *   Select all the generated railing entities (posts, pickets, rails) that you want to include in the BOM.
3.  **Save BOM File:**
    *   A "Save File" dialog will appear.
    *   Choose a location and filename for your CSV file. The default name is `RailingBOM_<timestamp>.csv`.
4.  **CSV Output:**
    *   The BOM is exported to the specified CSV file, containing columns such as `COMPONENTTYPE`, `PARTNAME/ProfileName`, `DESCRIPTION`, `QUANTITY`, `INSTALLED_LENGTH`, `MATERIAL`, `FINISH`, `WEIGHT`, etc.

## Implemented Logic & Key Files
*   **`RailGenerateCommand.cs`**: Handles the `RAIL_GENERATE` command flow.
*   **`ComponentDefiner.cs`**: Manages the inline definition of components (geometry selection, attribute form, block creation).
*   **`AttributeForm.cs`**: The WinForms dialog for entering component attributes.
*   **`RailingGeometryGenerator.cs`**: Orchestrates the generation of railing geometry, calling placement wrappers.
*   **`RailDesigner1/Wrappers/RailCreatorPicketPlacementWrapper.cs`**: Wrapper to use original `RailCreator.PicketGenerator.cs` for picket placement. Uses a temporary layer strategy to extract positions and clean up helper graphics.
*   **`RailDesigner1/Placement/PostPlacementLogicWrapper.cs`**: Wrapper to use original `RailCreator.PostGenerator.cs` for post placement. Also uses a temporary layer strategy for cleanup.
*   **`BomExporter.cs`**: Handles the `RAIL_EXPORT_BOM` command logic.
*   **Original `RailCreator_Roo` logic:**
    *   `PicketGenerator.cs` (copied into project root from `Resources/RailCreator_ROO/`)
    *   `PostGenerator.cs` (located in `Resources/RailCreator_ROO/`)

## Known Issues / Future Considerations
*   **`.csproj` File:** The `architecture.md` file mentions a potential XML issue in the `RailDesigner1.csproj` file. This may need manual review and correction if project loading or building is affected.
*   **Shared Utilities:** The `ComponentType` enum and `DictionaryExtensions.GetValueOrDefault` helper method are currently defined in multiple files (`RailingGeometryGenerator.cs`, `BomExporter.cs`). Ideally, these should be refactored into a shared utility file (e.g., `Utils/CommonDefinitions.cs`) for better maintainability.
*   **Ghost Graphics Cleanup:** While temporary layer strategies are implemented for post and picket generation to clean up helper graphics drawn by the original `RailCreator_Roo` functions, rigorous testing is needed to ensure this cleanup is perfect in all scenarios.
*   **RailingDesign Object:** The main `RailingDesign` object (which holds parameters like default `PostSpacing`, `RailHeight`, `BottomClearance`) is currently instantiated with default values in `RailGenerateCommand.cs`. Future enhancements could include a UI to edit these design parameters or load them from a configuration. The `PostSpacing` can be influenced by a "POSTSPACING" attribute on the POST component.

## Contributing
Contributions are welcome. Please review the project's `PRD.md` and `TaskList.md` for historical context on requirements and tasks.

---
This documentation has been updated to reflect the current implemented state of the project.
```
