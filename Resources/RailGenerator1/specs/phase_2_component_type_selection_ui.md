# Phase 2.2: Component Type Selection UI Specification

**SAPPO Context**: :Problem (User needs to select a component type for railing definition), :Solution (UI dropdown or prompt for type selection), :ArchitecturalPattern (:EnumPattern for type safety), :ComponentRole (UI for RAIL_DEFINE_COMPONENT command), :Context (AutoCAD environment, user interaction).

**Objective**: Define the logic and flow for a component type selection UI within the RAIL_DEFINE_COMPONENT command. This UI ensures users can select from predefined component types with type safety and clear feedback, supporting the modular architecture defined in Phase 1.

## Functional Requirements
- **User Input**: Allow selection of component type (e.g., Post, Picket, Rail, Mounting) via a dropdown or command-line prompt.
- **Type Safety**: Restrict input to predefined types to prevent :UserInputError.
- **Feedback**: Provide immediate confirmation of the selected type to the user.
- **Integration**: Pass the selected type to subsequent steps (entity selection, block creation) in the command flow.
- **Accessibility**: Ensure the UI is usable within AutoCAD's command interface or as a dialog if needed.

## Constraints
- Must operate within AutoCAD's API limitations for UI elements (e.g., command prompts or basic WinForms if extended).
- Must be lightweight to avoid performance impact during command execution.
- Must adhere to :ModularDesign for easy updates or extension of component types.

## Edge Cases
- **Invalid Selection**: User attempts to input or select a non-existent type (mitigate with :EnumPattern).
- **UI Interruption**: User cancels the selection process (handle with :ExceptionHandlingPattern).
- **No Selection**: User skips selection or provides no input (default to a safe state or prompt retry).

## Pseudocode Module 1: Component Type Enumeration
**SAPPO**: :EnumPattern for type safety, mitigate :UserInputError.

```plaintext
DEFINE ENUM ComponentType
    VALUES:
        - Post
        - Picket
        - Rail
        - Mounting
    END ENUM
```

**TDD Anchor 1**: Test_ComponentType_Enum_Validation
- **Input**: Attempt to create ComponentType with invalid value.
- **Expected Output**: Throws :UserInputError or rejects invalid input.
- **Context**: Ensures type safety at the data level.

## Pseudocode Module 2: UI Selection Logic
**SAPPO**: :FormPattern (if dialog) or :CommandPattern (if prompt), mitigate :SelectionError.

```plaintext
FUNCTION SelectComponentType
    INPUT: None
    OUTPUT: Selected ComponentType or Error

    STEP 1: Initialize UI (Dropdown or Prompt)
        IF UI is Dropdown (WinForms or AutoCAD dialog)
            Populate Dropdown with ComponentType values
        ELSE IF UI is Command Prompt
            Display list of ComponentType values with indices
        END IF

    STEP 2: Capture User Selection
        Wait for user input
        IF input received
            Validate input against ComponentType enum
            IF valid
                Return selected ComponentType
            ELSE
                Raise :UserInputError with message "Invalid component type selected"
            END IF
        ELSE IF user cancels
            Raise :UserCancellationError with message "Selection cancelled"
        END IF

    STEP 3: Handle No Input (Timeout or Skip)
        IF no input after timeout (if applicable)
            Return default ComponentType (e.g., Post) OR prompt retry
        END IF
END FUNCTION
```

**TDD Anchor 2**: Test_ComponentType_Selection_Valid
- **Input**: User selects a valid type (e.g., "Post").
- **Expected Output**: Returns ComponentType.Post.
- **Context**: Verifies core selection functionality.

**TDD Anchor 3**: Test_ComponentType_Selection_Invalid
- **Input**: User inputs invalid type (e.g., "InvalidType").
- **Expected Output**: Throws :UserInputError with appropriate message.
- **Context**: Ensures error handling for invalid input.

**TDD Anchor 4**: Test_ComponentType_Selection_Cancel
- **Input**: User cancels selection.
- **Expected Output**: Throws :UserCancellationError.
- **Context**: Validates cancellation handling.

## Pseudocode Module 3: Feedback Mechanism
**SAPPO**: :FeedbackPattern, ensure :UserExperienceConsistency.

```plaintext
FUNCTION ProvideSelectionFeedback
    INPUT: Selected ComponentType
    OUTPUT: None

    STEP 1: Format confirmation message
        Message = "Selected Component Type: " + ComponentType.ToString()

    STEP 2: Display feedback
        IF UI is Command Prompt
            Write message to AutoCAD command line
        ELSE IF UI is Dialog
            Update dialog label or show message box
        END IF
END FUNCTION
```

**TDD Anchor 5**: Test_Selection_Feedback_Display
- **Input**: ComponentType.Post passed to feedback function.
- **Expected Output**: Message "Selected Component Type: Post" displayed in UI or command line.
- **Context**: Confirms user feedback is provided.

## Flow Logic
**SAPPO**: :WorkflowPattern, mitigate :FlowInterruption.

```plaintext
MAIN FLOW: Component Type Selection for RAIL_DEFINE_COMPONENT
    STEP 1: Initialize process
        Call SelectComponentType()
        IF error (e.g., :UserCancellationError)
            Abort command with message "Component selection cancelled"
            Exit flow
        END IF

    STEP 2: Store result
        Store selected ComponentType in command context or session variable

    STEP 3: Provide feedback
        Call ProvideSelectionFeedback(selectedType)

    STEP 4: Transition to next command phase
        Proceed to entity selection (Task 2.3) with selected ComponentType
END MAIN FLOW
```

## Summary for Implementation
- **Component Types**: Use an enum to enforce type safety.
- **UI Approach**: Support both command-line prompts (primary for AutoCAD simplicity) and optional WinForms dialog for enhanced UX.
- **Error Handling**: Validate all inputs, handle cancellation, and provide clear feedback to mitigate :UserInputError.
- **Modularity**: Keep selection logic separate from feedback and subsequent command steps for loose coupling.

**SAPPO Note**: This specification adheres to :EnumPattern for type safety and integrates with the :ModularDesign established in Phase 1 architecture. It sets up @coder for implementation and @tester-core for targeted testing of UI interaction and validation.