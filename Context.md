# MCto3D - Deep Project Context & Agent Reference Manual

## 1. Project Philosophy & Core Objective
**MCto3D** is an independent, non-commercial desktop application developed to convert Minecraft structures (`.nbt`, `.schematic`, `.litematic`) into optimized 3D models (`.stl`, `.3mf`). It bridges the gap between Minecraft building and 3D printing/rendering.

**Design Philosophy:**
- **No "AI-Generic" UI:** The interface must feel highly custom, premium, and human-designed.
- **Performance First:** Converting large structures is intensive. Background tasks, cancellation tokens, and decoupled services are mandatory.
- **Zero Copyright Infringement:** Minecraft assets (textures, block models) are never distributed with the app. They are parsed dynamically from the user's local Minecraft `.jar` files (e.g., `%appdata%\.minecraft\versions`).

## 2. Technology Stack & Frameworks
- **Platform & Language:** .NET 10, C# 13 (Nullable contexts enabled).
- **UI Framework:** Avalonia UI v12.0.2 (Cross-platform desktop framework).
- **Architecture:** MVVM using `CommunityToolkit.Mvvm`.
- **Key Libraries:**
  - `fNbt`: For parsing NBT structure files.
  - `StbImageSharp`: For extracting and averaging pixel colors from PNG textures.
  - `Avalonia.Controls.ColorPicker`: For custom palette management.
- **Rendering:** Custom OpenGL pipeline for real-time 3D previewing of blocks.

## 3. Detailed Architecture Map
The project is strictly structured around MVVM. Do not mix UI logic with data logic.

### 3.1. Models (`/Models`)
Represent raw data and state.
- `StructureData.cs` / `VoxelData.cs` / `Vector3Int.cs`: Core representations of 3D grids and block placements.
- `SavedProject.cs`: Handles metadata for projects saved within the "My Files" section.
- `CustomPalette.cs` / `ColorAlgorithm.cs`: Data structures for the multi-color processing system.

### 3.2. Services (`/Services`)
Business logic is heavily decoupled into specific folders:
- **`FileReading`**: Parses `.nbt`, `.schematic`, `.litematic` into standardized `StructureData`.
- **`AssetsProcessing`**: Connects to the local Minecraft `.jar`, reads block states, and extracts the appropriate `.json` models and `.png` textures to determine block geometry and average color.
- **`ColorProcessing`**: The heart of the `.3mf` export. Contains logic for K-Means, Nearest Neighbor (K-Medoids), and Raw Color mapping.
- **`ExportedFilesWriting`**: Handles writing standard `.stl` (monochrome) and custom XML `.3mf` structures.
- **`TopologyService.cs`**: Uses Flood-Fill algorithms to cull internal/unseen faces. *Crucial:* It ignores the bottom/top faces if the structure is open, to prevent incorrect hole-filling.
- **`MeshService.cs`**: Generates the final 3D meshes based on the VoxelData and topology culling.
- **`LanguageService.cs`**: Handles English (default) and Spanish translations dynamically.

### 3.3. ViewModels (`/ViewModels`)
Use `[ObservableProperty]` and `[RelayCommand]`.
- `DashboardViewModel.cs`: The core conversion UI. Handles file picking, geometry mode selection, and color algorithms.
- `SettingsViewModel.cs`: Manages global settings, local asset paths, and language toggles.
- `MyFilesViewModel.cs`: Manages saved projects.
- `PaletteManagerViewModel.cs`: Handles the CRUD operations for user-defined custom color palettes.
- `MainWindowViewModel.cs`: The routing engine managing the current view.
- `WelcomeViewModel.cs` / `LoadingViewModel.cs`: Overlay screens for first-time setup and heavy asynchronous loading.

### 3.4. Views (`/Views`)
Avalonia `.axaml` files. 
- **Rule:** No inline styles (`Background="#FFF"`, `Margin="10"`). All styling must be extracted to `<UserControl.Styles>` or global dictionaries.
- **Rule:** Uses customized Window decorations. Do not remove the custom title bar.
- **Rule:** The UI uses responsive `Grid` layouts. Do not use absolute `Canvas` positioning unless specifically requested.

## 4. Deep Dive: Core Algorithms

### 4.1. Color Clustering (For 3D Printing)
To export multi-color `.3mf` files (e.g., for BambuLab AMS which supports a limited number of spools):
1. **Asset Color Averaging:** `AssetsProcessing` reads block textures. For blocks like logs, it averages the `top`, `bottom`, and `side` textures separately.
2. **Clustering:**
   - **K-Means:** Finds `K` number of optimal color centroids mathematically.
   - **K-Medoids / Custom Palette:** Takes a fixed list of colors (either standard or user-defined) and snaps every block's color to the closest match using 3D distance in the RGB/HSV color space.
   - **Raw Colors:** Skips clustering. Every block retains its exact average texture color. Used for high-fidelity rendering, not printing.

### 4.2. Mesh Generation & Topology
- **Solid Blocks:** Simplifies every block into a 16x16x16 cube. Fast and optimized.
- **Full Geometry:** Reads the actual Minecraft block model JSON to generate complex shapes (stairs, slabs, fences).
- **Topology Optimization (Culling):** `TopologyService.cs` runs a flood-fill algorithm. If a block face touches another opaque block face, the face is discarded. If `FillHoles` is enabled, internal empty air pockets are filled with solid geometry to prevent hollow 3D prints.

## 5. Known Issues, Quirks & Historical Context
Agents must read this section before attempting fixes, as many "bugs" are intentional workarounds.

1. **The 3MF Slicer Compatibility Hack:**
   - OrcaSlicer and BambuStudio will not recognize a `.3mf` file as "multi-color" unless the colors are split into separate `<object>` nodes in the XML.
   - *Past Issue:* We once added "Empty Geometries" to force the slicer to see multiple objects. **Always test 3MF exports** in a slicer when modifying `ExportedFilesWriting`.
2. **OpenGL Renderer Lighting & Fog (`/Rendering`):**
   - The custom OpenGL implementation has specific fog that scales based on the bounding box of the model.
   - *Past Issue:* Tall models had lighting cutoffs because the light source didn't scale correctly with the Y-axis. The center of the fog must originate from the model's center, not the camera.
3. **UI Freezes on Settings / Palette Manager:**
   - *Past Issue:* A `StackOverflowException` and UI freeze occurred because a `Command` binding in an `ItemsControl` traversed the visual tree infinitely (`$parent[UserControl]`). 
   - *Solution:* Always use named controls (`#RootControl.DataContext.CommandName`) for bindings inside DataTemplates.
4. **Slider Debouncing:**
   - Updating the 3D model color in real-time when dragging a slider caused 30+ mesh generations per second, crashing the app.
   - *Solution:* `CancellationToken` and a delay/debounce mechanism are implemented. Do not remove them.
5. **Localization Failures:**
   - Do not hardcode strings like `"Custom"`. Use `LanguageService`. We previously had bugs where changing the language left ghost texts because the ViewModel properties weren't updated dynamically.

## 6. Strict Rules for AI Agents
1. **Do Not Hallucinate Features:** If asked to fix the renderer, look at `/Rendering/Shaders.cs` and `ShaderProgram.cs`. Do not invent new Avalonia 3D controls; we use a raw OpenGL context.
2. **Preserve the MVVM Pattern:** If you need to show an error message, do not do it from a Service. Pass the error to the ViewModel and let the View bind to an Error property or use an interaction dialog.
3. **Refactoring is Restricted:** Do not delete interfaces or combine classes (like `ColorClustering` interfaces) unless the user specifically asks for an architectural refactoring plan.
4. **Handle Execution Policies:** PowerShell scripts (like the context extractor) were blocked by Windows Execution Policies. When writing build scripts or terminal commands, account for `-ExecutionPolicy Bypass`.
5. **Language:** The project, commits, and documentation must be in **English**.
