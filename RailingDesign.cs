// RailingDesign.cs
using System; // For Nullable if used more explicitly

namespace RailDesigner1 // Ensure this namespace matches your project's root namespace
{
    /// <summary>
    /// Represents the design parameters for a railing.
    /// This class acts as a data transfer object (DTO) or a model
    /// to hold all configurable aspects of the railing design.
    /// </summary>
    public class RailingDesign
    {
        // General Railing Properties
        public double RailHeight { get; set; }           // Overall height of the railing from the base path to the top.
        public double BottomClearance { get; set; }      // Clearance from the base path to the bottom of the lowest rail/picket.

        // Post Properties
        public string PostSize { get; set; }             // e.g., "2x2", "3 round" (Width/Depth or Diameter)
        public double PostSpacing { get; set; }          // Center-to-center spacing for posts.
        public string PostMaterial { get; set; }         // e.g., "Steel", "Aluminum", "Wood"
        public string PostCapType { get; set; }          // e.g., "Flat", "Pyramid", "Ball"

        // Picket Properties
        public string PicketType { get; set; }           // e.g., "Vertical", "Horizontal", "GlassPanel", "Mesh", "Decorative"
        public string PicketSize { get; set; }           // e.g., "0.75x0.75", "1.5x0.5", "0.75 round" (Width/Depth or Diameter for vertical; Thickness for panels)
        public double PicketSpacing { get; set; }        // For vertical pickets: desired clear spacing or center-to-center (clarify usage).
                                                         // For horizontal pickets: vertical spacing between them.
        public string PicketMaterial { get; set; }       // e.g., "Steel", "Aluminum"
        public int NumberOfHorizontalPickets { get; set; } // If PicketType is "Horizontal"

        // Top Rail / Handrail Properties
        public string TopRailProfile { get; set; }       // e.g., "2x1 Rect", "1.5 Pipe"
        public double TopRailHeight { get; set; }        // Specific height of the top surface of the top rail (can be same as RailHeight).
                                                         // Often RailHeight defines the guardrail height, and TopRailHeight might be slightly different for ergonomics.
        public double TopCapHeight { get; set; }         // Used in some calculations; might be part of top rail or a separate cap.
                                                         // This property seems to be used for available height calculations for pickets/panels.
                                                         // Clarify if this is distinct from TopRailProfile's height.
        public string TopRailMaterial { get; set; }

        // Bottom Rail Properties (Optional)
        public bool HasBottomRail { get; set; }
        public string BottomRailProfile { get; set; }    // e.g., "1.5x1.5"
        public string BottomRailMaterial { get; set; }

        // Mount Properties
        public string MountType { get; set; }            // e.g., "Surface", "Fascia"
        public double MountSpacing { get; set; }         // Spacing for mounts if independent of posts.
        public string MountSize { get; set; }            // Dimensions of the mount.

        // Decorative Elements (if any)
        public double? DecorativeWidth { get; set; }     // Width of a central decorative panel/element (nullable if optional).
        public string DecorativeElementType { get; set; }

        // Material and Finish
        public string DefaultMaterial { get; set; }      // Default material if not specified per component.
        public string FinishColor { get; set; }          // e.g., "RAL9005 Black", "PowderCoat Bronze"

        /// <summary>
        /// Initializes a new instance of the <see cref="RailingDesign"/> class
        /// with default values.
        /// </summary>
        public RailingDesign()
        {
            // Initialize with sensible defaults
            RailHeight = 36.0;         // inches
            BottomClearance = 2.0;     // inches

            PostSize = "2x2";
            PostSpacing = 72.0;        // inches
            PostMaterial = "Aluminum";
            PostCapType = "Flat";

            PicketType = "Vertical";
            PicketSize = "0.75x0.75";  // inches
            PicketSpacing = 4.0;       // Clear spacing for vertical pickets, or vertical spacing for horizontal
            PicketMaterial = "Aluminum";
            NumberOfHorizontalPickets = 5;

            TopRailProfile = "2x1 Rect";
            TopRailHeight = 36.0;      // inches
            TopCapHeight = 1.5;        // This might be the thickness/height of the top rail/cap itself
            TopRailMaterial = "Aluminum";

            HasBottomRail = true;
            BottomRailProfile = "1.5x1.5";
            BottomRailMaterial = "Aluminum";

            MountType = "Surface";
            MountSpacing = PostSpacing; // Default mounts to align with posts or use a dedicated spacing
            MountSize = "4x4x0.25";     // inches

            DecorativeWidth = null;    // No decorative panel by default
            DecorativeElementType = "None";

            DefaultMaterial = "Aluminum";
            FinishColor = "Black Matte";
        }

        // You can add methods here for validation or derived properties if needed.
        // For example:
        // public double GetAvailablePicketHeight()
        // {
        //     double height = RailHeight - TopCapHeight; // Adjust based on TopRailProfile, etc.
        //     if (HasBottomRail)
        //     {
        //         // Subtract bottom rail height and bottom clearance
        //         // height -= (ProfileHeight(BottomRailProfile) + BottomClearance);
        //     } else {
        //         height -= BottomClearance;
        //     }
        //     return height;
        // }
    }
}