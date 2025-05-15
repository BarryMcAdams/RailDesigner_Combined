# Manual Test Scenarios for Phase 2: Component Definition

**SAPPO Context**: :Problem (Ensure RAIL_DEFINE_COMPONENT command works correctly across all steps), :Solution (Manual test scenarios using :UnitTestingPattern), :ArchitecturalPattern (:ModularTesting for isolated verification), :ComponentRole (Validation of user interactions and system responses), :Context (AutoCAD environment, Phase 2 implementation).

**Objective**: Define modular manual test scenarios to verify functional requirements, edge cases, and constraints for Phase 2 tasks. These scenarios support targeted testing and adhere to :UnitTestingPattern for clarity and repeatability. Each scenario includes steps, expected outcomes, and SAPPO considerations.

## Scenario 1: RAIL_DEFINE_COMPONENT Command Trigger (Task 2.1)
**SAPPO**: Mitigate :UserInputError, ensure command initiation.
- **Nominal Case**: User invokes command successfully.
  - Steps:
    1. Open AutoCAD and load the RailGenerator1 plugin.
    2. Type "RAIL_DEFINE_COMPONENT" in the command line and press Enter.
    3. Verify command prompt appears asking for component type selection.
  - Expected Outcome: Command starts without errors; prompt for component type is displayed.
  - Edge Case 1: Command not recognized (e.g., typo in command name).
    - Steps:
      1. Type an invalid command like "RAIL_DEFINE_COMPONEN" and press Enter.
      2. Check for error message in command line.
    - Expected Outcome: AutoCAD displays "Unknown command" or similar; no crash.
  - Edge Case 2: Plugin not loaded.
    - Steps:
      1. Start AutoCAD without loading the RailGenerator1 plugin.
      2. Attempt to type "RAIL_DEFINE_COMPONENT".
    - Expected Outcome: Command is unavailable; AutoCAD reports it as unknown.

## Scenario 2: Component Type Selection UI (Task 2.2)
**SAPPO**: Use :EnumPattern, mitigate :SelectionError.
- **Nominal Case**: User selects a valid component type.
  - Steps:
    1. Invoke RAIL_DEFINE_COMPONENT command.
    2. In the type selection UI (dropdown or prompt), select a valid type (e.g., "Post").
    3. Submit the selection.
    4. Verify feedback message is displayed (e.g., "Selected Component Type: Post").
  - Expected Outcome: Selection is accepted; flow proceeds to next step (entity selection).
  - Edge Case 1: Invalid type selection.
    - Steps:
      1. Invoke command and attempt to enter an invalid type (e.g., "InvalidType") in the prompt or select outside options in dropdown.
      2. Check for error handling.
    - Expected Outcome: Error message like "Invalid component type selected" is shown; user is prompted to retry.
  - Edge Case 2: User cancels selection.
    - Steps:
      1. Invoke command, then cancel the type selection (e.g., press Esc or Cancel button).
      2. Verify command aborts gracefully.
    - Expected Outcome: Command ends with a cancellation message; no data is processed.
  - Edge Case 3: No input provided.
    - Steps:
      1. Invoke command, leave selection blank or skip, and attempt to proceed.
      2. Check system response.
    - Expected Outcome: System prompts retry or defaults to a safe state with warning.

## Scenario 3: Entity Selection with Filters (Task 2.3)
**SAPPO**: Mitigate :SelectionError, ensure only valid entities are accepted.
- **Nominal Case**: User selects valid entities.
  - Steps:
    1. Invoke RAIL_DEFINE_COMPONENT, complete type selection (e.g., "Post").
    2. When prompted for entity selection, select a line, polyline, arc, or circle in the drawing.
    3. Submit selection.
    4. Verify selection is accepted and proceeds.
  - Expected Outcome: Only specified entity types are selected; flow continues to base point prompt.
  - Edge Case 1: Select invalid entity type (e.g., text or block).
    - Steps:
      1. Attempt to select an unsupported entity (e.g., a text object).
      2. Check for rejection.
    - Expected Outcome: System rejects selection with message like "Invalid entity type; please select line, polyline, arc, or circle."
  - Edge Case 2: No entity selected.
    - Steps:
      1. Skip selection or cancel during entity pick.
      2. Verify response.
    - Expected Outcome: Command prompts retry or aborts with error.

## Scenario 4: Base Point Prompt (Task 2.4)
**SAPPO**: Mitigate :OutOfBoundsError, ensure point is within bounds.
- **Nominal Case**: User inputs a valid base point.
  - Steps:
    1. After entity selection, prompt for base point appears.
    2. Click a point within the drawing's bounding box.
    3. Verify point is accepted.
  - Expected Outcome: Point is stored and flow proceeds to axis prompt.
  - Edge Case 1: Point outside bounding box.
    - Steps:
      1. Attempt to input a point far outside the valid range (e.g., coordinates exceeding drawing limits).
      2. Check for error.
    - Expected Outcome: System raises :OutOfBoundsError and prompts for a valid point.
  - Edge Case 2: User cancels point input.
    - Steps:
      1. Cancel the point prompt (e.g., press Esc).
      2. Verify command behavior.
    - Expected Outcome: Command aborts or returns to previous step.

## Scenario 5: WinForms Dialog for Axis Prompt (Task 2.5)
**SAPPO**: Use :FormPattern, mitigate :InputValidationError.
- **Nominal Case**: User inputs valid axis and offset.
  - Steps:
    1. After base point prompt, axis dialog appears.
    2. Select a valid axis (e.g., X) and enter a valid offset (e.g., 5.0).
    3. Submit the dialog.
    4. Verify feedback and progression.
  - Expected Outcome: Data is validated and passed to next steps; dialog closes successfully.
  - Edge Case 1: Invalid input (e.g., non-numeric offset).
    - Steps:
      1. Enter invalid data (e.g., "abc" in offset field).
      2. Attempt to submit and check response.
    - Expected Outcome: Error message displayed; dialog remains open for correction.
  - Edge Case 2: Out-of-range offset.
    - Steps:
      1. Enter an offset outside allowed range (e.g., 150 if max is 100).
      2. Submit and verify.
    - Expected Outcome: :OutOfBoundsError or validation failure with retry prompt.
  - Edge Case 3: User cancels dialog.
    - Steps:
      1. Click Cancel or close the dialog.
      2. Check command flow.
    - Expected Outcome: Command aborts with cancellation error.

## Scenario 6: Block Definition Creation (Task 2.6)
**SAPPO**: Mitigate :NamingConflict, ensure block is created.
- **Nominal Case**: Successful block definition.
  - Steps:
    1. Complete prior steps in command.
    2. Verify a new block with a unique name (e.g., based on component type) is added to the BlockTable.
    3. Check block properties in AutoCAD.
  - Expected Outcome: Block is created and visible in the block palette.
  - Edge Case 1: Naming conflict (e.g., block name already exists).
    - Steps:
      1. Manually create a block with the same name beforehand.
      2. Run command and attempt block creation.
      3. Check for handling.
    - Expected Outcome: System appends a suffix or raises :NamingConflict error with user notification.
  - Edge Case 2: Failure during creation.
    - Steps:
      1. Simulate a scenario where block creation might fail (e.g., drawing locked).
      2. Verify error handling.
    - Expected Outcome: Error message displayed; command aborts gracefully.

## Scenario 7: Layer Management per Component Type (Task 2.7)
**SAPPO**: Mitigate :LayerCreationError, ensure layer assignment.
- **Nominal Case**: Layers created and assigned correctly.
  - Steps:
    1. Run command through to layer assignment.
    2. For a selected component type, verify a new layer is created (e.g., "RAIL-Post").
    3. Check that entities are assigned to the correct layer.
  - Expected Outcome: Layer exists and entities are on the proper layer.
  - Edge Case 1: Layer already exists.
    - Steps:
      1. Pre-create a layer with the same name.
      2. Run command and verify behavior.
    - Expected Outcome: System uses existing layer or handles conflict without error.
  - Edge Case 2: Permission issues (e.g., layer table locked).
    - Steps:
      1. Simulate restricted access and attempt layer creation.
      2. Check response.
    - Expected Outcome: :LayerCreationError raised with user-friendly message.

## Scenario 8: Error Handling Across Phase 2 (Task 2.8)
**SAPPO**: Mitigate :UserInputError, ensure robust error messages.
- **Nominal Case**: No errors during command execution.
  - Steps:
    1. Run full command flow with valid inputs.
    2. Verify no unexpected errors.
  - Expected Outcome: Command completes successfully.
  - Edge Case 1: Cumulative errors (e.g., invalid input in multiple steps).
    - Steps:
      1. Intentionally provide invalid input in sequence (e.g., wrong type, invalid entity, out-of-bounds point).
      2. Check error propagation and user feedback.
    - Expected Outcome: Each error is handled independently with clear messages; command does not crash.
  - Edge Case 2: System interruptions (e.g., AutoCAD crashes or user aborts).
    - Steps:
      1. During command execution, force an interruption.
      2. Verify state recovery.
    - Expected Outcome: Command rolls back changes or handles interruption without corrupting drawing.

**Summary**: These manual test scenarios are modular, covering each Phase 2 task with nominal and edge cases. They align with :UnitTestingPattern for targeted verification, ensuring coverage of functional requirements, constraints, and SAPPO principles. Total scenarios promote testability and ease of execution in AutoCAD.