# Manual Test Scenarios for Phase 4 - Data Export

This document defines manual test scenarios for Phase 4 of the AutoCAD Railing Generator project, focusing on the data export functionality. Scenarios are structured using SAPPO :UnitTestingPattern to ensure modularity, covering functional requirements, edge cases, and constraints. Each scenario includes a test ID, description, preconditions, steps, expected results, and SAPPO relevance.

## Test Scenarios

### Test ID: TS4.1.1
**Description:** Verify that the RAIL_EXPORT_DATA command triggers correctly and prompts for necessary inputs.
**Preconditions:** 
- AutoCAD is running with the Railing Generator plugin loaded.
- A RailingAssemblyBlock exists in the drawing.
**Steps:**
1. Type "RAIL_EXPORT_DATA" in the AutoCAD command line.
2. Confirm that the command is recognized and a prompt appears for selecting the RailingAssemblyBlock or other required inputs.
3. Proceed through any dialog prompts to initiate export.
**Expected Results:** 
- Command executes without errors.
- User is prompted for block selection or other inputs as per specifications.
**SAPPO Relevance:** Uses :UnitTestingPattern to ensure command invocation; mitigates :UserInputError by validating command recognition.

### Test ID: TS4.1.2
**Description:** Test edge case where no RailingAssemblyBlock exists in the drawing.
**Preconditions:** 
- AutoCAD is running with the plugin loaded.
- No RailingAssemblyBlock is present in the model space.
**Steps:**
1. Type "RAIL_EXPORT_DATA" in the command line.
2. Attempt to proceed with the command.
**Expected Results:** 
- Error message is displayed indicating no valid block found.
- Command does not proceed to export.
**SAPPO Relevance:** Covers edge case for :AttributeConsistency; ensures graceful handling of missing data.

### Test ID: TS4.2.1
**Description:** Verify data extraction from RailingAssemblyBlock with standard attributes.
**Preconditions:** 
- A RailingAssemblyBlock with complete attributes and XData is present.
**Steps:**
1. Invoke RAIL_EXPORT_DATA command.
2. Select the RailingAssemblyBlock.
3. Confirm data extraction process.
4. Export to a temporary CSV file.
5. Open the CSV file and check for expected attribute data (e.g., post positions, picket counts).
**Expected Results:** 
- All attributes and XData are correctly extracted and written to CSV.
- Data matches the block's properties.
**SAPPO Relevance:** Applies :UnitTestingPattern for data integrity; mitigates :AttributeConsistency issues.

### Test ID: TS4.2.2
**Description:** Test data extraction with incomplete or missing attributes in RailingAssemblyBlock.
**Preconditions:** 
- A RailingAssemblyBlock with some attributes missing or corrupted XData.
**Steps:**
1. Invoke RAIL_EXPORT_DATA command.
2. Select the faulty RailingAssemblyBlock.
3. Attempt export.
4. Check the exported CSV for handling of missing data.
**Expected Results:** 
- Error message or warning is issued for inconsistent attributes.
- Export either skips invalid data or uses default values as specified, without crashing.
**SAPPO Relevance:** Addresses edge case for :AttributeConsistency; ensures robust error handling.

### Test ID: TS4.3.1
**Description:** Verify CSV file saving with valid data and correct format.
**Preconditions:** 
- A valid RailingAssemblyBlock exists.
**Steps:**
1. Invoke RAIL_EXPORT_DATA command.
2. Select the block and specify a save path (e.g., "export.csv").
3. Confirm export completion.
4. Open the CSV file in a text editor or spreadsheet software.
**Expected Results:** 
- CSV file is created with correct headers and data rows.
- Data is comma-separated and properly formatted.
**SAPPO Relevance:** Uses :UnitTestingPattern for output validation; mitigates :FileIOException by ensuring successful write.

### Test ID: TS4.3.2
**Description:** Test error handling for file I/O issues, such as invalid save path or permissions.
**Preconditions:** 
- Set up a scenario where the save path is invalid (e.g., non-existent directory or read-only folder).
**Steps:**
1. Invoke RAIL_EXPORT_DATA command.
2. Attempt to save to an invalid path.
3. Observe system response.
**Expected Results:** 
- Error message is displayed (e.g., "Cannot write to file" or similar).
- Command handles exception without crashing AutoCAD.
**SAPPO Relevance:** Covers constraint for :FileIOException; ensures reliability under adverse conditions.

### Test ID: TS4.3.3
**Description:** Verify export with large datasets to check for performance and data integrity.
**Preconditions:** 
- A RailingAssemblyBlock with a large number of components (e.g., 1000 posts).
**Steps:**
1. Invoke RAIL_EXPORT_DATA command.
2. Select the block and export to CSV.
3. Check file size and contents for completeness.
**Expected Results:** 
- Export completes within reasonable time.
- All data is present and uncorrupted in the CSV.
**SAPPO Relevance:** Addresses potential :ScalabilityBottleneck; uses :UnitTestingPattern for stress testing.

These scenarios are modular, focusing on individual units of functionality from Phase 4 tasks, and can be executed manually to validate the implementation. They incorporate SAPPO principles to ensure comprehensive coverage of functional requirements, edge cases, and constraints.