# Manual Test Scenarios for Phase 3: Railing Generation

## Overview
This document defines manual test scenarios for the RAIL_GENERATE command in the AutoCAD Railing Generator project, based on specifications in specs/phase_3_rail_generate_ui.md and task 5.2 from todo.md. Scenarios follow SAPPO :UnitTestingPattern principles, ensuring modularity, coverage of functional requirements, edge cases, and constraints. Each scenario includes preconditions, steps, expected results, and SAPPO notes for testability and error mitigation.

SAPPO: :UnitTestingPattern is applied to structure scenarios with clear :Problem identification, :Solution verification, and edge case handling to mitigate issues like :UserInputError and :GeometryTransformationError.

## Test Scenarios

### Test Scenario 3.1.1: RAIL_GENERATE Command Trigger
- **Description:** Verify that the RAIL_GENERATE command can be invoked successfully.
- **Preconditions:** AutoCAD is running, Railing Generator plugin is loaded, no active commands.
- **Steps:**
  1. Type "RAIL_GENERATE" in the AutoCAD command line.
  2. Press Enter.
- **Expected Results:** Command prompt appears, requesting polyline selection. No errors occur.
- **SAPPO Notes:** Mitigate :UserInputError by ensuring command recognition; test for :CommandNotFound error.

### Test Scenario 3.1.2: RAIL_GENERATE Command with Invalid Invocation
- **Description:** Verify error handling when command is invoked incorrectly.
- **Preconditions:** AutoCAD is running, Railing Generator plugin is loaded.
- **Steps:**
  1. Type an invalid command like "RAIL_GENERATE_INVALID".
  2. Press Enter.
- **Expected Results:** AutoCAD reports "Unknown command" or similar; Railing Generator does not crash.
- **SAPPO Notes:** Watch for :UserInputError; ensure graceful failure per :ExceptionHandlingPattern.

### Test Scenario 3.2.1: WinForms Dialog Input Validation - Valid Inputs
- **Description:** Verify that the WinForms dialog accepts valid inputs and proceeds to generation.
- **Preconditions:** A valid polyline exists in the drawing; RAIL_GENERATE command invoked.
- **Steps:**
  1. Select a polyline when prompted.
  2. In the dialog, set railing type to "Standard", post spacing to 4 feet, picket spacing to 1 foot, enable top rail.
  3. Click "OK".
- **Expected Results:** Dialog closes, railing generation proceeds without errors; assembly is created.
- **SAPPO Notes:** Use :FormPattern for input validation; test :AttributeConsistency in generated components.

### Test Scenario 3.2.2: WinForms Dialog Input Validation - Invalid Inputs
- **Description:** Verify that the dialog rejects invalid inputs and provides feedback.
- **Preconditions:** A valid polyline exists; RAIL_GENERATE command invoked.
- **Steps:**
  1. Select a polyline.
  2. In the dialog, set post spacing to 0 (invalid).
  3. Attempt to click "OK".
- **Expected Results:** Error message or highlighting indicates invalid input; dialog does not close until corrected.
- **SAPPO Notes:** Mitigate :InputValidationError; ensure user feedback per specs.

### Test Scenario 3.3.1: Polyline Selection - Valid Polyline
- **Description:** Verify correct handling of valid polyline selection.
- **Preconditions:** Drawing contains a Polyline or Polyline2d entity.
- **Steps:**
  1. Invoke RAIL_GENERATE.
  2. Select a valid polyline.
- **Expected Results:** Command proceeds to dialog; no selection errors.
- **SAPPO Notes:** Mitigate :SelectionError; ensure only Polyline/Polyline2d accepted.

### Test Scenario 3.3.2: Polyline Selection - Invalid Entity Types
- **Description:** Verify error handling for selecting invalid entity types.
- **Preconditions:** Drawing contains mixed entities (e.g., line, circle).
- **Steps:**
  1. Invoke RAIL_GENERATE.
  2. Attempt to select a line or circle.
- **Expected Results:** Error message indicates invalid selection; command does not proceed.
- **SAPPO Notes:** Watch for :SelectionError; enforce entity filter.

### Test Scenario 3.4.1: Post/Picket Position Calculation - Standard Spacing
- **Description:** Verify accurate calculation of post and picket positions with standard inputs.
- **Preconditions:** Valid polyline selected, dialog inputs set (e.g., post spacing 4 feet).
- **Steps:**
  1. Complete dialog with valid inputs.
  2. Generate railing.
- **Expected Results:** Posts placed at correct intervals along polyline; pickets spaced evenly between posts.
- **SAPPO Notes:** Use :AlgorithmPattern; test spacing accuracy to mitigate :ArithmeticError.

### Test Scenario 3.4.2: Post/Picket Position Calculation - Edge Spacing Values
- **Description:** Verify handling of minimum and maximum spacing values.
- **Preconditions:** Valid polyline; dialog set with min/max spacing (e.g., post spacing 1 foot or 10 feet).
- **Steps:**
  1. Set post spacing to minimum (1 foot) and generate.
  2. Set post spacing to maximum (10 feet) and generate.
- **Expected Results:** Positions calculated correctly; no division by zero or overlap issues.
- **SAPPO Notes:** Cover edge cases per constraints; mitigate :OutOfBoundsError.

### Test Scenario 3.5.1: RailingAssemblyBlock Creation
- **Description:** Verify creation of the RailingAssemblyBlock.
- **Preconditions:** Railing generation completed successfully.
- **Steps:**
  1. Check the Block Table in AutoCAD.
- **Expected Results:** A new block named "RailingAssembly" or similar exists with correct components.
- **SAPPO Notes:** Watch for :NamingConflict; ensure block is modular and testable.

### Test Scenario 3.6.1: Post Insertion with Attributes and Orientation
- **Description:** Verify correct insertion of posts with attributes and orientation.
- **Preconditions:** Railing generated.
- **Steps:**
  1. Inspect inserted posts in the drawing.
- **Expected Results:** Posts oriented along polyline, attributes (e.g., height) match inputs, XData present.
- **SAPPO Notes:** Mitigate :GeometryTransformationError; ensure :AttributeConsistency.

### Test Scenario 3.7.1: Horizontal Component Generation
- **Description:** Verify generation of polylines for horizontal components.
- **Preconditions:** Railing generated with horizontal components enabled.
- **Steps:**
  1. Inspect the drawing for polylines.
- **Expected Results:** Polylines on correct layers, with XData attached, no intersections.
- **SAPPO Notes:** Mitigate :GeometryTransformationError; test layer assignment.

### Test Scenario 3.8.1: XData Attachment
- **Description:** Verify persistent attachment of XData to polylines.
- **Preconditions:** Railing generated.
- **Steps:**
  1. Select a polyline component.
  2. View XData in AutoCAD properties.
- **Expected Results:** XData contains expected metadata (e.g., type, spacing).
- **SAPPO Notes:** Ensure :AttributeConsistency; test data persistence after save/load.

### Test Scenario 3.9.1: Picket and Mounting Block Insertion
- **Description:** Verify correct insertion of pickets and mounting blocks.
- **Preconditions:** Railing generated.
- **Steps:**
  1. Inspect picket and mounting block positions.
- **Expected Results:** Blocks inserted at calculated positions, attributes transferred correctly.
- **SAPPO Notes:** Mitigate :GeometryTransformationError; ensure positioning accuracy.

### Test Scenario 3.10.1: RailingAssemblyBlock Insertion into Model Space
- **Description:** Verify successful insertion of the assembly block.
- **Preconditions:** Generation complete.
- **Steps:**
  1. Check Model Space for the block reference.
- **Expected Results:** Block inserted correctly, can be selected and manipulated.
- **SAPPO Notes:** Watch for :InsertionError; ensure modularity.

### Test Scenario 3.11.1: Error Handling in Railing Generation
- **Description:** Verify comprehensive error handling across the command.
- **Preconditions:** Various invalid setups (e.g., degenerate polyline, invalid inputs).
- **Steps:**
  1. Test with open polyline and warn.
  2. Test with invalid spacing in dialog.
  3. Test with conflicting attributes.
- **Expected Results:** Appropriate error messages displayed, command aborts safely.
- **SAPPO Notes:** Mitigate :UserInputError and other errors; ensure robust failure modes.

## Summary
These scenarios cover all Phase 3 tasks (3.1-3.11) with a focus on functional requirements, edge cases, and constraints. Total of 13 scenarios for modularity and comprehensive testing. SAPPO :UnitTestingPattern applied to facilitate automation.