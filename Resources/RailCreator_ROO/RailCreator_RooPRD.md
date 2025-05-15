# Product Requirements Document (PRD) for AutoCAD 2025 Railing Design Script

## 1. Introduction

**Purpose**: Develop a script for AutoCAD 2025 to streamline railing design for an aluminum fabrication organization. Users input custom dimensions or select from 21 standard railing designs (excluding MVRS-100) via a Windows form, populating a 2D railing along a user-selected polyline in plan view (XY plane). Users can save custom designs with a unique `RailName` and `SpecialInstructions`, appending them to the JSON file. The script adheres to design rules (posts at ends, corners, and middle for spans between 10–50 inches, ≤4-inch picket spacing) and generates a Bill of Materials (BOM) with attributes (quantities, dimensions, weights, image/DWG references). Data (JSON, images, DWG files) resides on a server at `P:\X-CAD TRANSFER\MAPI_Standard_Rails\`, with network access, temporary file management, and robust error handling. All railings are aluminum (0.098 lb/in³), with posts/rails implied as hollow (e.g., 2x2x0.25) and pickets solid (e.g., 0.75 solid round) or decorative CNC’d (centered, with user-specified width and selective placement). Topcaps (e.g., HR-14) are cataloged. Posts support three mounting types (Core-Drilled, Plate, Side-Mounted) with adjusted heights. The initial output must be a 2D representation in the XY plane, with 3D adjustments (e.g., extrusion) deferred to later steps.

## 2. Main Features

### Windows Form

- **Design Overview**:
  - **Title**: “Railing Designer”.
  - **Layout**: Tabbed interface using a `TabControl` with two tabs: “Design Selection” and “Design Details”, ensuring a clean, organized structure.
  - **Appearance**:
    - Background: Light gray (RGB 240, 240, 240) for a neutral, professional look.
    - Controls: Standard Windows forms colors with dark gray borders (RGB 150, 150, 150).
    - Highlight: Soft blue (RGB 0, 120, 215) for selected tabs/buttons.
    - Font: Segoe UI, 9pt for labels/textboxes, bold for section headers.
    - Spacing: 10px padding between controls, grid-like alignment within GroupBoxes.
    - Modern Elements: Flat buttons with hover effects, subtle shadows on tabs/GroupBoxes, icons for buttons (e.g., magnifying glass for Browse, floppy disk for Save Custom Rail).
- **Tab 1: Design Selection**:
  - **Drop-Down (Design Selection)**: Labeled “Select Design”, populated with design names (e.g., VRS-200, CBRS-100, custom designs).
  - **Browse Button**: Next to the drop-down, labeled “Browse”, opens a thumbnail gallery of designs (if images exist).
  - **Preview Panel**: A `PictureBox` below the drop-down to show the selected design’s thumbnail (e.g., `VRS-200-1024x869.jpeg`) or a placeholder for “N/A”.
  - **Search Bar**: Above the drop-down, labeled “Search Designs”, filters the drop-down list as the user types (e.g., “VRS” shows VRS-100, VRS-200).
- **Tab 2: Design Details**:
  - **Grouped Fields (GroupBox)**:
    - **Posts Section**:
      - `PostSize` (Drop-down, e.g., “2x2x0.25”).
      - `MountingType` (Drop-down: Core-Drilled, Plate, Side-Mounted).
      - `Post Rotation` (Checkbox: “Rotate 90°”).
    - **Rails Section**:
      - `TopCap` (Drop-down, e.g., “HR-14”).
      - `TopCapHeight` (Textbox, e.g., “1.5”).
      - `TopCapWidth` (Textbox, e.g., “2”).
      - `TopCapWall` (Textbox, e.g., “0.25”).
      - `BottomRail` (Drop-down, e.g., “2x1x0.25”).
      - `IntermediateRail` (Drop-down, e.g., “0” or “1x1x0.25”).
    - **Pickets Section**:
      - `PicketType` (Drop-down: Vertical, Horizontal, PERF, MESH, GLASS, DECO).
      - `PicketSize` (Drop-down, e.g., “0.75 solid round”).
      - `PicketPlacement` (Drop-down: Centered, Inside, Outside, N/A).
      - Dynamic prompt for `DecorativeWidth` (Textbox, appears if `PicketType` is “DECO”).
    - **General Section**:
      - `RailHeight` (Textbox, e.g., “37”).
      - `RailName` (Textbox, enabled for custom designs, e.g., “CustomRail_001”).
      - `SpecialInstructions` (Drop-down, e.g., “Use stainless steel bolts”).
  - **Buttons (Bottom of Form)**:
    - **Confirm** (Proceeds to polyline selection).
    - **Save Custom Rail** (Saves to JSON).
    - **Open DWG** (Opens DWG file, disabled if “N/A”).
    - **Cancel** (Closes form).
- **Responsiveness**: Form is resizable, with controls anchored to adjust layout (e.g., `Anchor` properties set to Top, Left, Right).
- **Tab Navigation**: Users can switch tabs without losing data, with validation on Confirm/Save.
- **Error Feedback**: Error messages (e.g., “Invalid post size”) in a `MessageBox` or status label at the form’s bottom.
- **Accessibility**: Logical tab order (e.g., PostSize → MountingType → TopCap), tooltips for clarity (e.g., “Enter post dimensions, e.g., 2x2x0.25”).

### Data Entry Support

- **CSV Template**: Provided with columns matching JSON fields, pre-filled with `DesignName`, `ImageLink`, and `DwgLink` for all 21 designs, used for initial JSON population.
- **Conversion Script**: Available to convert CSV to JSON, ensuring accurate formatting and validation (implemented separately).

### Polyline Selection

- Prompt user to select a 2D polyline in plan view (XY plane), supporting straight segments and arcs.
- Validate polyline selection; display error if invalid (e.g., 3D polyline). Offer retry or cancel option.

### 2D Railing Population

- Generate 2D railing along polyline in the XY plane:
  - **Posts**: Placed at start, end, corners (vertices), and intermediate points to maintain 10–50-inch spans. Post spacing may vary per segment (e.g., straight, curved, angled), with each segment’s spacing evenly divided based on its length and corners. Formula: `Num_Posts = ceil(L / 50) + 1` per segment, adjusted for minimum 10-inch spacing. Drawn as rectangles (e.g., 2x4x0.25, rotated per setting). Post length adjusted per mounting type.
  - **Rails**: TopCap/bottom rails as polylines/rectangles, following polyline path in 2D. Topcaps drawn as predefined shapes.
  - **Pickets**:
    - Vertical: Circles/squares (e.g., 0.75 solid round, 0.625 sq. solid) at ≤4-inch spacing, flat on the XY plane.
    - Horizontal: Lines/rectangles, placed Centered, Inside, or Outside, flat on the XY plane.
    - Decorative CNC’d: Centered between posts, with standard pickets spaced around it based on user-specified width. Selective placement to be finalized.
- Components drawn as 2D entities with attributes for later BOM extraction.

### Attributes for BOM

- Attributes per component:
  - Quantity (e.g., `Num_Posts = 3`, `Num_Pickets = 17`).
  - Dimensions (e.g., `Rail_Length = 65 inches`, `Post_Size = 2x4x0.25`, `Picket_Size = 0.75 solid round`, `Post_Length`, `TopCapWidth`, `TopCapWall`).
  - Weight (calculated from aluminum density, 0.098 lb/in³).
  - Image Reference (file path, e.g., `P:\X-CAD TRANSFER\MAPI_Standard_Rails\VRS-200-1024x869.jpeg` or “N/A”).
  - DWG Reference (file path, e.g., `P:\X-CAD TRANSFER\MAPI_Standard_Rails\VRS-200.dwg` or “N/A”).
  - Topcap Type (e.g., `HR-14`).
  - Picket Type (e.g., `Standard`, `Decorative CNC`).
  - Mounting Type (e.g., `Core-Drilled`).
  - Special Instructions (e.g., “Use stainless steel bolts”).

### BOM Extraction

- Use AutoCAD’s Data Extraction tool to export attributes to a CSV file, including quantities, dimensions, weights, file paths, and special instructions.

### Data Storage

- **JSON File** (preferred):
  - Stores 21 standard designs and appended custom designs, with component details, mounting type, TopCap dimensions, image paths, DWG paths, and SpecialInstructions, located at `P:\X-CAD TRANSFER\MAPI_Standard_Rails\RailDesigns.json`.
  - Custom designs have `ImageLink` and `DwgLink` as “N/A” or user-specified paths.
  - For development, a local copy of `RailDesigns.json` is included in the project folder (e.g., `C:\path\to\RailingPlugin\RailDesigns.json`) and added to the project via **Add &gt; Existing Item**. The script will be coded to read/write to the server path for production, with a configurable path for development (local copy) vs. production (server).
- **Topcap Catalog**: Separate JSON section for topcaps (e.g., HR-14, #133) with profiles, dimensions, image/DWG paths, and descriptions.
- **CSV Alternative**: Columns for each field (e.g., `DesignName,PostSize,MountingType,...`). Less flexible for topcaps/decorative pickets.
- **Images/DWGs**: Stored at `P:\X-CAD TRANSFER\MAPI_Standard_Rails\`. Custom designs may use placeholder paths.
- **Material Sizes**: Handled via form drop-downs (e.g., 2x2x0.25, 2x4x0.25 for posts) and textboxes for custom sizes; no separate file.
- **Version Control**: Check JSON file version to ensure the script uses the latest data, with user notification for mismatches.

### Temporary Folder Management

- Copy DWG files to a temporary desktop folder (`TEMP`) when opened.
- Create folder if it doesn’t exist; ensure permissions and cleanup after use.
- Handle errors (e.g., insufficient permissions, disk space). Skip for custom designs with “N/A” DWG paths.

### Material Handling

- **Posts/Rails**: Implied hollow aluminum (e.g., 2x2x0.25, 2x1x0.25). No “hollow” in descriptions.
- **Pickets**: Solid aluminum rounds (e.g., 0.75 solid round) or squares (e.g., 0.625 sq. solid), with “solid” in descriptions, or decorative CNC’d (centered, user-specified width).
- **Topcaps**: Unique profiles (e.g., HR-14, \~2x1.5 with radius). Cataloged in JSON or form drop-down.
- **Material**: All aluminum, density 0.098 lb/in³.

### Error Handling

- **Network Errors**: Retry or notify user for server file access failures (JSON, images, DWGs). Prompt for local JSON if server fails.
- **Data Errors**: Handle missing/malformed JSON data with user-friendly messages (e.g., “Failed to load design data”).
- **File Errors**: Manage DWG copy failures (e.g., permissions, disk space) with notifications.
- **Input Errors**: Validate custom inputs and display errors (e.g., “Invalid post size”).
- **JSON Write Errors**: Handle write failures when saving custom rails (e.g., server access denied).
- **Logging**: Record actions/errors (e.g., “Failed to load JSON”) to a log file for troubleshooting.

## 3. Potential Missing Elements

- **Offline Mode**: Cache data locally for unreliable network scenarios.
- **Decorative Picket Placement**: Finalize selective placement logic (e.g., user specifies sections with decorative pickets).
- **Side-Mount Specifics**: Define post length calculations for side-mounted railings.
- **Custom Design Images/DWGs**: Option to generate or link images/DWGs for custom designs (e.g., save a preview image).

## 4. Challenges

- **Custom Dialog Creation**: Building a form with image browsing, dynamic field population, mounting options, and DWG opening requires advanced AutoCAD programming (e.g., .NET).
- **Network File Access**: Managing latency, permissions, and connectivity issues for server-based JSON, images, and DWGs.
- **Temporary Folder Management**: Handling permissions, folder creation, and cleanup to avoid clutter or security risks.
- **Data Parsing**: Robust parsing of JSON with error checking for format changes.
- **DWG File Handling**: Copying/opening DWG files, ensuring compatibility with AutoCAD 2025, handling large files.
- **Image Display Performance**: Efficiently displaying images without slowing the form (e.g., lazy loading).
- **Security**: Preventing unauthorized file access or overwrites during server-to-desktop copying or JSON updates.
- **Decorative Picket Placement**: Calculating dynamic spacing around centered CNC’d pickets, especially on curved polylines.

## 5. Next Steps

- Confirm JSON structure and topcap catalog.
- Finalize decorative picket placement and side-mount logic when ready.
- Design Windows form layout with mounting options, rotation modifier, image browsing, custom rail saving, and decorative picket prompts.
- **Set up Visual Studio 2022 with a Class Library (.NET Framework) project targeting .NET Framework 4.8 for AutoCAD plugin development**:
  - Create a new Class Library (.NET Framework) project named `RailingPlugin`.
  - Set the target framework to .NET Framework 4.8.
  - Add references to AutoCAD .NET API assemblies (`accoremgd.dll`, `acdbmgd.dll`, `acmgd.dll`).
  - Add `System.Windows.Forms` for the user interface.
  - Install `Newtonsoft.Json` via NuGet for JSON handling:
    - `Newtonsoft.Json` (Json.NET) is a library for serializing and deserializing JSON data, used to read and write `RailDesigns.json`.
    - NuGet is the package manager for .NET, integrated into Visual Studio, to install and manage libraries.
    - In Visual Studio, install via the NuGet Package Manager (search for `Newtonsoft.Json`, install the latest version, e.g., 13.0.3), or use the Package Manager Console (`Install-Package Newtonsoft.Json`).
  - **Structure files**:
    - `Plugin.cs`: Main plugin entry point and AutoCAD command definition.
    - `Form.cs` and `Form.Designer.cs`: Windows form for user interface.
    - `Data.cs`: JSON data handling (loading/saving designs).
    - `Generator.cs`: Coordinator for railing generation logic.
    - `PostGenerator.cs`: Handles post placement logic.
    - `RailGenerator.cs`: Handles TopCap and bottom rail drawing.
    - `PicketGenerator.cs`: Handles picket placement for all types.
    - `BOM.cs`: BOM extraction to CSV (to be implemented).
    - `RailingPlugin.sln`: Solution file.
    - `RailingPlugin.csproj`: Project file.
    - `AssemblyInfo.cs`: Assembly metadata.
    - `RailDesigns.json`: JSON file with designs and topcaps.
      - Added to the project via **Add &gt; Existing Item**, copied to the local project folder for development.
      - For production, the script will read/write to `P:\X-CAD TRANSFER\MAPI_Standard_Rails\RailDesigns.json`, with a configurable path for development (local copy) vs. production (server).
  - **Version Control Setup**:
    - Initialize a local Git repository in the project folder (`git init`).
    - Add a `.gitignore` file to exclude `bin/`, `obj/`, and `*.user` files.
    - Commit initial files: `git add .`, `git commit -m "Initial project setup with PRD, JSON, and dependencies"`.
    - Optionally, create a remote repository (e.g., GitHub) after the first milestone (e.g., UI implementation): `git remote add origin <url>`, `git push -u origin main`.
- Implement and test with sample polylines (straight/curved) and BOM extraction.

**Context Integration**

- Your prior work with AutoLISP/VBA for spiral staircases (March 5–April 21, 2025) shows familiarity with user forms and attribute extraction, informing the need for a robust Windows form and BOM CSV output.
- Your March 18, 2025, BOM export script request aligns with the current goal of attribute-based CSV extraction.
- The shift from stretch-based dynamic blocks (April 14, 2025) to this polyline-based script avoids past issues like `Num_Posts` errors by calculating in-code.
- Your experience with .NET 4.8 projects (April 12, 2025) supports the Class Library (.NET Framework) setup.

**Next Steps**

- Ensure `RailDesigns.json` is properly added to the project (either as a local copy or link to the server path).
- Begin coding the plugin, starting with the UI (`Form.cs`), JSON handling (`Data.cs`), and core functionality (`Plugin.cs`).
- Iterate on railing generation (`Generator.cs`, `PostGenerator.cs`, `RailGenerator.cs`, `PicketGenerator.cs`) and BOM extraction (`BOM.cs`).