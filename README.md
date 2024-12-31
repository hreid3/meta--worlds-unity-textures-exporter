# Material Processor for Unity

This Unity Editor script processes all materials in the active scene. It validates textures, ensures they adhere to specified naming conventions, and exports them into a designated folder (`Assets/textures/meta-horizon`). The script is tailored for workflows involving Meta Horizon Worlds or similar platforms.

---

## Features
- **Material Validation**:
    - Ensures materials have required textures.
    - Verifies textures are in PNG format.
- **Texture Export**:
    - Applies naming conventions based on material type (e.g., `Metal`, `Unlit`).
    - Saves textures in a structured format for easy integration.
- **Error Handling**:
    - Provides warnings for missing or incorrect textures.
    - Displays errors for unsupported formats.
- **Progress Tracking**:
    - Includes a progress bar for processing large scenes.

---

## Installation

1. **Download or Clone**:
    - Download this repository as a ZIP file or clone it:
      ```bash
      git clone https://github.com/your-username/material-processor-unity.git
      ```

2. **Add to Your Project**:
    - Copy the `Editor` folder into your Unity project's `Assets` directory.

3. **Verify Installation**:
    - Open Unity and confirm the menu item `Tools → Process Materials` appears in the Editor.

---

## Usage

1. **Prepare Your Scene**:
    - Ensure all materials in the scene are configured with the correct textures.

2. **Run the Script**:
    - In the Unity Editor, go to:
      ```
      Tools → Process Materials
      ```
    - Follow on-screen prompts to process materials.

3. **View Exported Textures**:
    - Processed textures will be saved in:
      ```
      Assets/textures/meta-horizon
      ```

4. **Handle Errors**:
    - The script will display errors or warnings in the Console for issues like:
        - Missing base textures.
        - Non-PNG texture formats.

---

## Material Naming Rules

| Material Type        | Output Files          | Notes                                            |
|-----------------------|-----------------------|-------------------------------------------------|
| **Standard Image**    | `Image_BR.png`       | Material named `Image` in Blender.             |
| **Metal Image**       | `Image_BR.png`       | Base texture.                                   |
|                       | `Image_MEO.png`      | Metallic/roughness map.                         |
| **Unlit Image**       | `Image_B.png`        | Material named `Image_Unlit` in Blender.       |
| **Unlit Blend Image** | `Image_BA.png`       | Material named `Image_Blend` in Blender.       |
| **Transparent Image** | `Image_BR.png`       | Base texture.                                   |
|                       | `Image_MESA.png`     | Metallic/emission/alpha map.                   |
| **Masked Image**      | `Image_BA.png`       | Material named `Image_Masked` in Blender.      |
| **UIO Image**         | `Image_BA.png`       | Animated texture support.                      |

---

## Troubleshooting

### Common Errors
1. **Missing Base Textures**:
    - Error: `Visible material '<name>' is missing a base texture!`
    - Solution: Ensure all required textures are assigned in the material.

2. **Unsupported Texture Format**:
    - Error: `Texture <path> is not a PNG. Only PNG textures are supported.`
    - Solution: Convert textures to PNG format.

3. **Progress Bar Stuck**:
    - Problem: Script hangs mid-process.
    - Solution: Check the Console for errors and fix any reported issues.

---

## Example

Here’s an example of the output directory structure after processing:
