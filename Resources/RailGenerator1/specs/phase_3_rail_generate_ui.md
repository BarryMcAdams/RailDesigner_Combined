# Phase 3: WinForms Dialog for RAIL_GENERATE Command Specification

**SAPPO Context**: :Problem (User needs to specify parameters for railing generation), :Solution (WinForms dialog for structured input), :ArchitecturalPattern (:FormPattern for UI consistency), :ComponentRole (Input capture for RAIL_GENERATE command), :Context (AutoCAD environment, user interaction).

**Objective**: Define the logic and flow for a WinForms dialog within the RAIL_GENERATE command. This dialog captures parameters for railing generation, such as component selection, path or dimensions, and count, with robust validation to ensure correct input before proceeding to railing creation.

## Functional Requirements
- **Component Selection**: Allow user to select predefined railing components (e.g., from a list or dropdown) to be used in generation.
- **Path Definition**: Provide options to define the railing path (e.g., select line/curve in AutoCAD or input start/end points).
- **Dimension Input**: Capture dimensions or count (e.g., length, number of posts) if path is not directly selected.
- **Validation**: Enforce input rules (e.g., valid component selection, numeric values for dimensions) to prevent :InputValidationError.
- **Feedback**: Display immediate error messages or highlight fields for invalid inputs.
- **Integration**: Pass validated data to subsequent command steps for railing generation in AutoCAD.
- **Accessibility**: Ensure dialog is user-friendly within AutoCAD, with clear labels and logical tab order.

## Constraints
- Must operate within AutoCAD's API limitations for WinForms integration.
- Must be lightweight to avoid performance impact during command execution.
- Must adhere to :ModularDesign for easy updates or extension of input fields or options.

## Edge Cases
- **Invalid Selection**: User selects no component or an invalid one - mitigate with :InputValidationError handling.
- **Path Issues**: User fails to select a valid path or inputs invalid coordinates - provide error message or fallback.
- **Dimension Errors**: User enters non-numeric or negative values for length/count - enforce validation.
- **Dialog Cancellation**: User closes or cancels the dialog - handle with :UserCancellationError.
- **Large Input**: User enters excessive values for counts or dimensions - set reasonable limits.

## Pseudocode Module 1: Dialog Structure and Fields
**SAPPO**: :FormPattern for structured UI, mitigate :UserInputError.

```plaintext
DEFINE FORM RailingGenerateDialog
    PROPERTIES:
        Title: "Generate Railing Parameters"
        Size: Width=450, Height=400
        FormStyle: Modal (block AutoCAD until closed)
    
    CONTROLS:
        // Component Selection
        Label Component_Label (Text="Railing Component (Mandatory):", Position=Top:10, Left:10)
        ComboBox Component_Selection (Items=[Dynamically populated from defined components], Position=Top:10, Left:200, Width=200)
        
        // Path Definition
        Label Path_Label (Text="Railing Path (Select or Input):", Position=Top:40, Left:10)
        RadioButton Path_Select (Text="Select Line/Curve in AutoCAD", Position=Top:40, Left:200, Group=PathGroup, Default=True)
        RadioButton Path_Manual (Text="Input Start and End Points", Position=Top:70, Left:200, Group=PathGroup)
        
        // Manual Path Inputs (Enabled only if Path_Manual selected)
        Label StartPoint_Label (Text="Start Point (X,Y):", Position=Top:100, Left:20, Enabled=False)
        TextBox StartX_Input (Position=Top:100, Left:200, Width=90, Enabled=False)
        TextBox StartY_Input (Position=Top:100, Left:300, Width=90, Enabled=False)
        Label EndPoint_Label (Text="End Point (X,Y):", Position=Top:130, Left:20, Enabled=False)
        TextBox EndX_Input (Position=Top:130, Left:200, Width=90, Enabled=False)
        TextBox EndY_Input (Position=Top:130, Left:300, Width=90, Enabled=False)
        
        // Dimension/Count Input (Alternative if manual input)
        Label Count_Label (Text="Number of Posts (Optional):", Position=Top:160, Left:10)
        TextBox Count_Input (Position=Top:160, Left:200, Width=200)
        
        // Action Buttons
        Button OK_Button (Text="OK", Position=Top:200, Left:150, Width=75, IsDefault=True)
        Button Cancel_Button (Text="Cancel", Position=Top:200, Left:250, Width=75)
    END CONTROLS
END FORM
```

**TDD Anchor 1**: Test_Dialog_Rendering
- **Input**: Launch RailingGenerateDialog.
- **Expected Output**: Dialog displays with all fields and buttons in correct positions, manual input fields disabled by default.
- **Context**: Ensures UI renders as expected in AutoCAD environment.

## Pseudocode Module 2: Input Validation Logic
**SAPPO**: :ValidationPattern, mitigate :InputValidationError.

```plaintext
FUNCTION ValidateRailingInput
    INPUT: FormData (Dictionary of field values)
    OUTPUT: ValidationResult (Success=True/False, Errors=List of messages)

    STEP 1: Initialize result
        ValidationResult result = { Success=True, Errors=[] }

    STEP 2: Validate Component Selection
        IF FormData["Component"] is Empty or Not in AvailableComponents
            result.Success = False
            result.Errors.Add("A valid railing component must be selected.")
        END IF

    STEP 3: Validate Path Definition
        IF FormData["PathMode"] = "Manual"
            FOR EACH CoordField in [StartX, StartY, EndX, EndY]
                IF FormData[CoordField] is Not Empty
                    IF Not Parseable as Double
                        result.Success = False
                        result.Errors.Add(CoordField + " must be a valid number.")
                    END IF
                ELSE
                    result.Success = False
                    result.Errors.Add(CoordField + " cannot be empty for manual path.")
                END IF
            END FOR
        END IF
        // Note: For "Select" mode, validation occurs post-dialog via AutoCAD interaction

    STEP 4: Validate Count Input
        IF FormData["Count"] is Not Empty
            IF Not Parseable as Integer OR Value <= 0
                result.Success = False
                result.Errors.Add("Number of posts must be a positive integer.")
            END IF
        END IF

    RETURN result
END FUNCTION
```

**TDD Anchor 2**: Test_Validation_Component_Empty
- **Input**: Component field not selected or empty.
- **Expected Output**: Validation fails with error "A valid railing component must be selected."
- **Context**: Ensures mandatory component selection.

**TDD Anchor 3**: Test_Validation_ManualPath_Invalid
- **Input**: Manual path selected, coordinate fields empty or non-numeric.
- **Expected Output**: Validation fails with specific error messages for each invalid field.
- **Context**: Verifies manual path input validation.

## Pseudocode Module 3: Dialog Interaction Flow
**SAPPO**: :WorkflowPattern, ensure :UserExperienceConsistency.

```plaintext
FUNCTION ShowRailingGenerateDialog
    INPUT: None (or PreFilledData if edit mode)
    OUTPUT: DialogResult (OK/Cancel), FormData if OK

    STEP 1: Initialize Dialog
        Create instance of RailingGenerateDialog
        Populate Component_Selection with available components from system
        IF PreFilledData exists
            Populate fields with PreFilledData
        END IF

    STEP 2: Enable/Disable Controls Dynamically
        On Path_Manual.CheckedChanged:
            Enable StartPoint and EndPoint input fields if Path_Manual is selected
            Disable them if Path_Select is selected

    STEP 3: Show Dialog and Wait for Response
        DialogResult = Display Dialog as Modal

    STEP 4: Handle Response
        IF DialogResult = OK
            FormData = Collect field values
            ValidationResult = ValidateRailingInput(FormData)
            IF ValidationResult.Success = False
                Display errors (MessageBox or field highlights)
                Return to STEP 3 (re-show dialog)
            ELSE
                Return { DialogResult=OK, Data=FormData }
            END IF
        ELSE // Cancel or Close
            Return { DialogResult=Cancel, Data=Null }
        END IF
END FUNCTION
```

**TDD Anchor 4**: Test_Dialog_OK_ValidInput
- **Input**: User selects component, chooses path mode, fills valid data, clicks OK.
- **Expected Output**: Dialog returns OK with collected data.
- **Context**: Confirms successful submission with valid input.

**TDD Anchor 5**: Test_Dialog_Cancel
- **Input**: User clicks Cancel.
- **Expected Output**: Dialog returns Cancel with no data.
- **Context**: Validates cancellation handling.

## Flow Logic
**SAPPO**: :WorkflowPattern, mitigate :FlowInterruption.

```plaintext
MAIN FLOW: Railing Generation Input for RAIL_GENERATE
    STEP 1: Initialize process at command invocation
        Result = Call ShowRailingGenerateDialog()
        IF Result.DialogResult = Cancel
            Abort command with message "Railing generation cancelled" (:UserCancellationError)
            Exit flow
        END IF

    STEP 2: Handle Path Selection if Needed
        IF Result.Data["PathMode"] = "Select"
            Prompt user in AutoCAD to select line/curve
            IF Selection fails or cancelled
                Abort command with message "Path selection failed" (:UserSelectionError)
                Exit flow
            END IF
            Store selected path in command context
        ELSE // Manual
            Store manual coordinates in command context
        END IF

    STEP 3: Store validated data
        Store Result.Data in command context (component, path mode, counts if any)

    STEP 4: Provide feedback
        Display confirmation message "Railing parameters set successfully."

    STEP 5: Transition to next command phase
        Proceed to railing generation with collected parameters
END MAIN FLOW
```

## Summary for Implementation
- **Dialog Design**: Use WinForms for structured UI with component selection, path definition, and optional counts.
- **Validation**: Implement strict checks for component selection, path inputs, and numeric values to prevent :InputValidationError.
- **Path Flexibility**: Support both direct selection in AutoCAD and manual coordinate input for railing path.
- **Error Handling**: Provide clear feedback on invalid inputs and handle dialog cancellation gracefully.
- **Modularity**: Keep dialog logic separate from command flow for loose coupling and reusability.

**SAPPO Note**: This specification adheres to :FormPattern for UI consistency and integrates with the :ModularDesign established in prior phases. It prepares @coder for implementation of the dialog and @tester-core for targeted testing of input validation and path handling.