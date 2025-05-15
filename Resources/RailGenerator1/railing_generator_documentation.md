# AutoCAD Railing Generator Documentation

**SAPPO Context**: :Problem (Users need clear guidance on using the Railing Generator), :Solution (Comprehensive Markdown documentation), :ArchitecturalPattern (:ModularDesign for clarity), :ComponentRole (Documentation for user interaction and system understanding), :Context (AutoCAD environment, multi-phase implementation).

## Overview
This document provides detailed information on the AutoCAD Railing Generator, a plugin designed to automate railing design within AutoCAD 2025 using .NET 4.8. It covers setup, configuration, usage, integration, and testing strategies, adhering to SAPPO principles like :DocumentationCompleteness and :ModularDesign to mitigate :ScalabilityBottleneck and :UserInputError.

## Setup
### Prerequisites
- **AutoCAD 2025**: Ensure AutoCAD 2025 is installed on your system.
- **.NET Framework 4.8**: Required for plugin compatibility.
- **Railing Generator Plugin**: Load the compiled `RailGenerator1.dll` into AutoCAD via the `NETLOAD` command.

### Installation
1. **Compile the Project**: Open `RailGenerator1.sln` in Visual Studio, build the solution targeting .NET 4.8, and ensure references to AutoCAD libraries (AcCoreMgd, AcDbMgd, AcMgd) are resolved.
2. **Load the Plugin**: In AutoCAD, type `NETLOAD` in the command line, browse to the compiled DLL (e.g., `bin/Debug/RailGenerator1.dll`), and load it.
3. **Verification**: Confirm loading by typing a command like `RAIL_DEFINE_COMPONENT`. If recognized, the plugin is ready.

**SAPPO Note**: Mitigate :DependencyIssue by verifying AutoCAD and .NET versions during setup.

## Configuration
The plugin operates within AutoCAD's environment without external configuration files. Configuration occurs dynamically through command dialogs:
- **Layer Management**: Automatically creates layers per component type (e.g., `RAIL-Post`) during `RAIL_DEFINE_COMPONENT`.
- **Component Attributes**: Defined via WinForms dialogs during command execution.
- **Export Settings**: CSV export paths are user-specified during `RAIL_EXPORT_DATA`.

**SAPPO Note**: Use :ModularDesign to keep configuration tied to commands, avoiding hardcoded values and mitigating :ConfigurationIssue.

## Usage
### Commands
#### 1. RAIL_DEFINE_COMPONENT
- **Purpose**: Define individual railing components (e.g., Post, Picket) with attributes and geometry.
- **Workflow**:
  1. Type `RAIL_DEFINE_COMPONENT` in AutoCAD command line.
  2. Select component type from dropdown (e.g., Post).
  3. Select valid entities (line, polyline, arc, circle).
  4. Specify base point within drawing bounds.
  5. Input attributes and axis orientation via WinForms dialog (e.g., PARTNAME, MATERIAL, AXIS as X/Y/Custom).
  6. Confirm block creation and layer assignment.
- **Error Handling**: Validates inputs to prevent :InputValidationError and :SelectionError; aborts on cancellation with :UserCancellationError.
- **Reference**: See specs/phase_2_winforms_dialog_axis_prompt.md for dialog details.

#### 2. RAIL_GENERATE
- **Purpose**: Generate a complete railing assembly along a selected path with specified parameters.
- **Workflow**:
  1. Type `RAIL_GENERATE` in AutoCAD command line.
  2. Select a polyline as the railing path.
  3. Input parameters via WinForms dialog (component type, spacing, counts).
  4. Generate assembly with posts, pickets, and horizontal components.
  5. Insert RailingAssemblyBlock into Model Space.
- **Error Handling**: Ensures valid polyline selection to mitigate :SelectionError; handles spacing errors with :ArithmeticError checks.
- **Reference**: See specs/phase_3_rail_generate_ui.md for UI specifications.

#### 3. RAIL_EXPORT_DATA
- **Purpose**: Export Bill of Materials (BOM) data from a RailingAssemblyBlock to CSV.
- **Workflow**:
  1. Type `RAIL_EXPORT_DATA` in AutoCAD command line.
  2. Select a RailingAssemblyBlock.
  3. Specify CSV save path.
  4. Export extracted attributes and XData.
- **Error Handling**: Manages missing attributes with :AttributeConsistency checks and file issues with :FileIOException handling.
- **Reference**: See Phase 4 tasks in todo.md for implementation scope.

**SAPPO Note**: Commands follow :WorkflowPattern for consistent user experience, mitigating :FlowInterruption.

### Integration
- **AutoCAD API**: Leverages AutoCAD .NET API for entity manipulation, block creation, and data management.
- **Modular Architecture**: Divided into UI, Geometry, Blocks, Attributes, and Export modules for loose coupling and high cohesion.
  - **UI Module**: Manages WinForms dialogs for input collection.
  - **Geometry Module**: Handles polyline processing and component positioning.
  - **Blocks Module**: Creates and inserts block definitions/references.
  - **Attributes Module**: Manages attribute and XData consistency.
  - **Export Module**: Outputs BOM data to CSV for external use.
- **External Use**: CSV exports integrate with BOM scripts or spreadsheets for project management.

**SAPPO Note**: Architecture uses :ModularDesign to facilitate integration and mitigate :ScalabilityBottleneck. See architecture_diagram.md for class relationships.

## Testing Strategy
Testing follows a Targeted Testing Strategy using SAPPO :UnitTestingPattern, focusing on Core Logic Testing and Contextual Integration Testing for each phase. Below are key manual test scenarios:

### Phase 2: Component Definition (RAIL_DEFINE_COMPONENT)
- **Command Trigger**: Verify `RAIL_DEFINE_COMPONENT` starts without errors (mitigate :UserInputError).
- **Type Selection**: Ensure valid component type selection with error handling for invalid inputs (mitigate :SelectionError).
- **Entity/Base Point**: Test valid/invalid entity selection and base point input (mitigate :OutOfBoundsError).
- **Dialog Validation**: Confirm WinForms dialog validates mandatory fields and numeric inputs (mitigate :InputValidationError).
- **Block/Layer Creation**: Check block uniqueness and layer assignment (mitigate :NamingConflict, :LayerCreationError).
- **Reference**: manual_test_scenarios_phase_2.md for full scenarios.

### Phase 3: Railing Generation (RAIL_GENERATE)
- **Command Trigger**: Verify `RAIL_GENERATE` initiates correctly (mitigate :UserInputError).
- **Polyline Selection**: Ensure only valid polylines are accepted (mitigate :SelectionError).
- **Position Calculation**: Test post/picket spacing accuracy across standard/edge values (mitigate :ArithmeticError).
- **Assembly Insertion**: Confirm RailingAssemblyBlock creation and insertion (mitigate :InsertionError).
- **Reference**: manual_test_scenarios_phase_3.md for detailed tests.

### Phase 4: Data Export (RAIL_EXPORT_DATA)
- **Command Trigger**: Verify `RAIL_EXPORT_DATA` prompts for block selection (mitigate :UserInputError).
- **Data Extraction**: Ensure attribute/XData extraction matches block properties (mitigate :AttributeConsistency).
- **CSV Export**: Test file saving, format, and handling of large datasets (mitigate :FileIOException, :ScalabilityBottleneck).
- **Reference**: manual_test_scenarios_phase_4.md for complete scenarios.

**SAPPO Note**: Testing strategy supports immediate Code->Test->Fix cycle, ensuring :Testability and mitigating :IntegrationError.

## Summary
This documentation covers setup, configuration, usage of commands (`RAIL_DEFINE_COMPONENT`, `RAIL_GENERATE`, `RAIL_EXPORT_DATA`), integration with AutoCAD's architecture, and a comprehensive testing strategy. It adheres to SAPPO :DocumentationCompleteness, providing users with actionable guidance while maintaining :ModularDesign for future updates.

**SAPPO Note**: Mitigates :UserInputError and :ScalabilityBottleneck through clear instructions and modular structure. References specs and test scenarios for deeper technical insight.