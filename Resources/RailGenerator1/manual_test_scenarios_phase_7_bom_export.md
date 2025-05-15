# Manual Test Scenarios for Phase 7: BOM Export (RAIL_EXPORT_BOM Command)

**SAPPO Context**: :Problem (Ensure accurate and robust BOM export functionality), :Solution (Manual test scenarios using :UnitTestingPattern), :ArchitecturalPattern (:ModularTesting for isolated verification), :ComponentRole (Validation of export logic and data handling), :Context (AutoCAD environment, Phase 7 implementation based on completed tasks).

**Objective**: Define modular manual test scenarios to verify the functional requirements, edge cases, and constraints for the RAIL_EXPORT_BOM command as per Task 7.2. These scenarios cover successful exports for different railing types and component data, error handling for invalid selections, and data inconsistencies. Scenarios are based strictly on architecture.md (module interactions), PRD.md (REQ 5.4 CSV format specifications), and existing test documentation. Each scenario includes steps, expected outcomes, and SAPPO considerations to ensure clarity, repeatability, and alignment with project standards.

## Scenario 1: Successful BOM Export for Straight Polyline Railing (Nominal Case)
**SAPPO**: Mitigate :DataConsistencyError, ensure accurate export for standard cases.
- **Description**: Test BOM export for a railing generated along a straight polyline with all components defined correctly.
- **Steps**:
  1. Open AutoCAD and load the RailDesigner1 plugin.
  2. Use RAIL_GENERATE command to define components (e.g., Post, Picket, TopCap, BottomRail) with valid attributes.
  3. Select a straight polyline path and generate the railing geometry.
  4. Invoke RAIL_EXPORT_BOM command.
  5. When prompted, select the generated railing entities using a window selection.
  6. Confirm the CSV file is saved to the desktop (e.g., RailingBOM_YYYYMMDD_HHMMSS.csv).
  7. Open the CSV file and verify the contents.
- **Expected Outcome**: CSV file is created successfully with correct headers and data. Columns include COMPONENTTYPE, PARTNAME/ProfileName, DESCRIPTION, QUANTITY, INSTALLED_LENGTH, INSTALLED_HEIGHT, WIDTH, HEIGHT, MATERIAL, FINISH, WEIGHT, SPECIAL_NOTES, USER_ATTRIBUTE_1, USER_ATTRIBUTE_2. Data matches the generated entities (e.g., accurate quantities, lengths calculated from straight path, weight based on attributes).
- **Edge Case 1**: Export with minimal attributes.
  - **Steps**: Repeat steps with components having only mandatory attributes (e.g., PARTNAME, COMPONENTTYPE).
  - **Expected Outcome**: Export succeeds; optional fields in CSV are empty or default, but required data is present without errors.

## Scenario 2: Successful BOM Export for Curved Polyline Railing (Nominal Case)
**SAPPO**: Mitigate :GeometryTransformationError, ensure accurate length calculations for non-linear paths.
- **Description**: Test BOM export for a railing generated along a curved polyline to verify length and quantity calculations.
- **Steps**:
  1. Generate a railing using RAIL_GENERATE with a curved polyline path.
  2. Ensure components are defined with valid attributes.
  3. Invoke RAIL_EXPORT_BOM and select the entities.
  4. Verify the CSV output.
- **Expected Outcome**: CSV accurately reflects the curved path, with INSTALLED_LENGTH calculated based on the actual polyline length (e.g., using AutoCAD's length property). Quantities and weights are correct, adhering to REQ 5.4 specifications.
- **Edge Case 1**: Complex curved path with many vertices.
  - **Steps**: Use a polyline with multiple arcs and segments, generate railing, export BOM.
  - **Expected Outcome**: Export handles complexity without errors; lengths are precise, no data loss or inconsistencies.

## Scenario 3: Error Handling for Invalid Entity Selection (Edge Case)
**SAPPO**: Mitigate :SelectionError, ensure only relevant entities are processed.
- **Description**: Test the command's response to selecting invalid or unrelated entities during BOM export.
- **Steps**:
  1. Invoke RAIL_EXPORT_BOM command.
  2. Intentionally select entities that are not part of the railing (e.g., text objects, dimensions, or non-railing blocks).
  3. Attempt to proceed with the export.
  4. Check for error messages and command behavior.
- **Expected Outcome**: System rejects invalid selection with a clear error message (e.g., "Selected entities do not match expected railing components. Please select only railing blocks and polylines."). Command does not crash and may prompt for re-selection or abort.
- **Edge Case 1**: No entities selected.
  - **Steps**: Invoke command and skip selection or cancel the prompt.
  - **Expected Outcome**: Error message like "No entities selected for BOM export" is displayed; command aborts gracefully without creating a file.

## Scenario 4: Error Handling for Data Inconsistencies (Edge Case)
**SAPPO**: Mitigate :DataConsistencyError, ensure robust handling of missing or mismatched data.
- **Description**: Test BOM export when there are inconsistencies in component data, such as missing attributes or mismatched XData.
- **Steps**:
  1. Generate a railing with some components having incomplete attributes (e.g., missing MATERIAL or FINISH).
  2. Invoke RAIL_EXPORT_BOM and select the entities.
  3. Verify how the system handles missing data.
- **Expected Outcome**: Export may proceed with warnings or fail gracefully. Missing data fields in CSV should be blank or use default values, with an error message logged or displayed (e.g., "Warning: Missing attributes for some components; data may be incomplete."). Weight calculations should handle null values without errors, possibly omitting or approximating based on available data per REQ 5.4.
- **Edge Case 1**: Mismatched XData on polylines.
  - **Steps**: Manually edit a polyline's XData to remove or alter required keys (e.g., remove PARTNAME), then attempt BOM export.
  - **Expected Outcome**: System detects inconsistency, displays an error like "Inconsistent data found in selected entities. Export aborted." or exports partial data with warnings.

## Scenario 5: Successful Export with Multiple Component Types (Nominal Case)
**SAPPO**: Mitigate :DataAggregationError, ensure all component data is aggregated correctly.
- **Description**: Test BOM export for a railing with diverse component types to verify comprehensive data capture.
- **Steps**:
  1. Define and generate a railing with all component types (Posts, Pickets, TopCap, BottomRail, IntermediateRail, BasePlate, Mounting).
  2. Ensure attributes are set variably (e.g., different materials, finishes).
  3. Invoke RAIL_EXPORT_BOM, select all entities, and export.
  4. Review the CSV file.
- **Expected Outcome**: CSV contains rows for each component type with accurate QUANTITY, INSTALLED_LENGTH, WEIGHT (calculated using WEIGHT_DENSITY), and other attributes. Total aggregation is correct, with no duplication or omission.

## Scenario 6: Performance and Large-Scale Export (Edge Case)
**SAPPO**: Mitigate :PerformanceIssue, ensure export handles large datasets.
- **Description**: Test BOM export for a railing with a high number of components to check for performance issues or errors.
- **Steps**:
  1. Generate a long polyline with many segments, resulting in numerous posts and pickets.
  2. Invoke RAIL_EXPORT_BOM and select the entities.
  3. Measure export time and check for errors.
- **Expected Outcome**: Export completes within reasonable time (e.g., <10 seconds for typical large railings). No crashes or data corruption; CSV accurately reflects all entities.

**Summary**: These manual test scenarios are modular, covering nominal and edge cases for the RAIL_EXPORT_BOM command based on REQ 5.4 and architecture.md. They ensure verification of successful exports, error handling, and data consistency, aligning with project testing standards and SAPPO principles. Total scenarios promote comprehensive coverage and ease of execution in AutoCAD.