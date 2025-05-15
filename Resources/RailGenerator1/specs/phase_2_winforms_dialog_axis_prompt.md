# Phase 2.5: WinForms Dialog with Axis Prompt Specification

**SAPPO Context**: :Problem (User needs to input component attributes and axis orientation), :Solution (WinForms dialog for structured input), :ArchitecturalPattern (:FormPattern for UI consistency), :ComponentRole (Attribute and axis input for RAIL_DEFINE_COMPONENT command), :Context (AutoCAD environment, user interaction).

**Objective**: Define the logic and flow for a WinForms dialog within the RAIL_DEFINE_COMPONENT command. This dialog captures component attributes and axis orientation with validation, ensuring user inputs are correct before proceeding to block creation.

## Functional Requirements
- **Attribute Input**: Collect mandatory and optional component attributes (e.g., PARTNAME, DESCRIPTION, MATERIAL) via text fields.
- **Axis Prompt**: Allow selection of axis orientation (e.g., X, Y, or custom) for component alignment via dropdown or radio buttons.
- **Validation**: Enforce input rules (e.g., mandatory fields, numeric values) to prevent :InputValidationError.
- **Feedback**: Provide immediate error messages or field highlighting for invalid inputs.
- **Integration**: Pass validated data to subsequent command steps (block creation, layer assignment).
- **Accessibility**: Ensure dialog is user-friendly within AutoCAD's environment, with clear labels and logical tab order.

## Constraints
- Must operate within AutoCAD's API limitations for WinForms integration.
- Must be lightweight to avoid performance impact during command execution.
- Must adhere to :ModularDesign for easy updates or extension of attribute fields or axis options.

## Edge Cases
- **Invalid Input**: User enters invalid data (e.g., empty mandatory field, non-numeric value for dimensions) - mitigate with :InputValidationError handling.
- **Dialog Cancellation**: User closes or cancels the dialog - handle with :UserCancellationError.
- **Axis Conflict**: User selects an axis incompatible with geometry (if applicable) - provide fallback or error message.
- **Large Input**: User enters excessively long text for optional fields - truncate or limit input length.

## Pseudocode Module 1: Dialog Structure and Fields
**SAPPO**: :FormPattern for structured UI, mitigate :UserInputError.

```plaintext
DEFINE FORM ComponentAttributesDialog
    PROPERTIES:
        Title: "Component Attributes and Axis Orientation"
        Size: Width=400, Height=500
        FormStyle: Modal (block AutoCAD until closed)
    
    CONTROLS:
        // Mandatory Fields
        Label PARTNAME_Label (Text="PARTNAME (Mandatory):", Position=Top:10, Left:10)
        TextBox PARTNAME_Input (Position=Top:10, Left:150, Width=200)
        
        // Optional Fields (examples)
        Label DESCRIPTION_Label (Text="DESCRIPTION (Optional):", Position=Top:40, Left:10)
        TextBox DESCRIPTION_Input (Position=Top:40, Left:150, Width=200)
        
        Label MATERIAL_Label (Text="MATERIAL (Optional):", Position=Top:70, Left:10)
        TextBox MATERIAL_Input (Position=Top:70, Left:150, Width=200, Default="Aluminum")
        
        Label WIDTH_Label (Text="WIDTH (Optional, numeric):", Position=Top:100, Left:10)
        TextBox WIDTH_Input (Position=Top:100, Left:150, Width=200)
        
        // Axis Orientation Selection
        Label AXIS_Label (Text="AXIS ORIENTATION:", Position=Top:130, Left:10)
        ComboBox AXIS_Selection (Items=["X-Axis", "Y-Axis", "Custom"], Position=Top:130, Left:150, Width=200)
        
        // Action Buttons
        Button OK_Button (Text="OK", Position=Top:160, Left:100, Width=75, IsDefault=True)
        Button Cancel_Button (Text="Cancel", Position=Top:160, Left:200, Width=75)
    END CONTROLS
END FORM
```

**TDD Anchor 1**: Test_Dialog_Rendering
- **Input**: Launch ComponentAttributesDialog.
- **Expected Output**: Dialog displays with all fields and buttons in correct positions.
- **Context**: Ensures UI renders as expected in AutoCAD environment.

## Pseudocode Module 2: Input Validation Logic
**SAPPO**: :ValidationPattern, mitigate :InputValidationError.

```plaintext
FUNCTION ValidateDialogInput
    INPUT: FormData (Dictionary of field values)
    OUTPUT: ValidationResult (Success=True/False, Errors=List of messages)

    STEP 1: Initialize result
        ValidationResult result = { Success=True, Errors=[] }

    STEP 2: Validate Mandatory Fields
        IF FormData["PARTNAME"] is Empty or Whitespace
            result.Success = False
            result.Errors.Add("PARTNAME is mandatory and cannot be empty.")
        END IF

    STEP 3: Validate Numeric Fields (e.g., WIDTH, HEIGHT if provided)
        FOR EACH NumericField in [WIDTH, HEIGHT, WEIGHT_DENSITY, STOCK_LENGTH]
            IF FormData[NumericField] is Not Empty
                IF Not Parseable as Double OR Value < 0
                    result.Success = False
                    result.Errors.Add(NumericField + " must be a non-negative number.")
                END IF
            END IF
        END FOR

    STEP 4: Validate Axis Selection
        IF FormData["AXIS"] Not in ["X-Axis", "Y-Axis", "Custom"]
            result.Success = False
            result.Errors.Add("Invalid axis orientation selected.")
        END IF

    RETURN result
END FUNCTION
```

**TDD Anchor 2**: Test_Validation_MandatoryField_Empty
- **Input**: PARTNAME field empty.
- **Expected Output**: Validation fails with error "PARTNAME is mandatory and cannot be empty."
- **Context**: Ensures mandatory field validation works.

**TDD Anchor 3**: Test_Validation_NumericField_Invalid
- **Input**: WIDTH field set to "invalid" or negative value.
- **Expected Output**: Validation fails with error "WIDTH must be a non-negative number."
- **Context**: Verifies numeric input validation.

## Pseudocode Module 3: Dialog Interaction Flow
**SAPPO**: :WorkflowPattern, ensure :UserExperienceConsistency.

```plaintext
FUNCTION ShowComponentAttributesDialog
    INPUT: None (or PreFilledData if edit mode)
    OUTPUT: DialogResult (OK/Cancel), FormData if OK

    STEP 1: Initialize Dialog
        Create instance of ComponentAttributesDialog
        IF PreFilledData exists
            Populate fields with PreFilledData
        END IF

    STEP 2: Show Dialog and Wait for Response
        DialogResult = Display Dialog as Modal

    STEP 3: Handle Response
        IF DialogResult = OK
            FormData = Collect field values
            ValidationResult = ValidateDialogInput(FormData)
            IF ValidationResult.Success = False
                Display errors (MessageBox or field highlights)
                Return to STEP 2 (re-show dialog)
            ELSE
                Return { DialogResult=OK, Data=FormData }
            END IF
        ELSE // Cancel or Close
            Return { DialogResult=Cancel, Data=Null }
        END IF
END FUNCTION
```

**TDD Anchor 4**: Test_Dialog_OK_ValidInput
- **Input**: User fills all mandatory fields correctly, clicks OK.
- **Expected Output**: Dialog returns OK with collected data.
- **Context**: Confirms successful submission with valid input.

**TDD Anchor 5**: Test_Dialog_Cancel
- **Input**: User clicks Cancel.
- **Expected Output**: Dialog returns Cancel with no data.
- **Context**: Validates cancellation handling.

## Flow Logic
**SAPPO**: :WorkflowPattern, mitigate :FlowInterruption.

```plaintext
MAIN FLOW: Attribute and Axis Input for RAIL_DEFINE_COMPONENT
    STEP 1: Initialize process after entity and base point selection
        Result = Call ShowComponentAttributesDialog()
        IF Result.DialogResult = Cancel
            Abort command with message "Attribute input cancelled" (:UserCancellationError)
            Exit flow
        END IF

    STEP 2: Store validated data
        Store Result.Data in command context or session variable (attributes and axis orientation)

    STEP 3: Provide feedback
        Display confirmation message "Attributes and axis orientation set successfully."

    STEP 4: Transition to next command phase
        Proceed to block creation (Task 2.6) with collected attributes and axis data
END MAIN FLOW
```

## Summary for Implementation
- **Dialog Design**: Use WinForms for a structured UI with mandatory and optional fields, plus axis orientation selection.
- **Validation**: Implement strict checks for mandatory fields and numeric inputs to prevent :InputValidationError.
- **Axis Prompt**: Include axis orientation as a key input for component alignment, supporting X, Y, or Custom options.
- **Error Handling**: Provide clear feedback on invalid inputs and handle dialog cancellation gracefully.
- **Modularity**: Keep dialog logic separate from command flow for loose coupling and reusability.

**SAPPO Note**: This specification adheres to :FormPattern for UI consistency and integrates with the :ModularDesign established in Phase 1 architecture. It sets up @coder for implementation of the dialog and @tester-core for targeted testing of input validation and axis selection.