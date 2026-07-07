# MCto3D

<div align="center">
  <!-- TODO: Add hero image/logo here -->
  <img src="docs/images/logo.png" alt="MCto3D Logo" width="200"/>
  
  **MCto3D** is a powerful desktop application designed to convert Minecraft structure files (`.nbt`, `.litematic`, `.schematic`) into highly optimized 3D models (`.3mf`, `.stl`) suitable for 3D printing, rendering, and CAD workflows. It accurately preserves block geometry, dynamically extracts texture colors, and features advanced color-clustering algorithms for multi-color 3D printing.
</div>

---

## Technical Overview

This repository is built with maintainability, modularity, and high performance in mind. It employs modern C# practices to handle heavy mesh generation and color clustering efficiently, ensuring a smooth, non-blocking User Experience (UX) even when processing large-scale structures.

### Technology Stack
- **Framework**: .NET 10 (C#)
- **UI Framework**: Avalonia UI (Cross-platform GUI)
- **Architecture Pattern**: MVVM (Model-View-ViewModel) via `CommunityToolkit.Mvvm`
- **3D Rendering**: Custom OpenGL / Silk.NET integration for real-time interactive mesh previewing
- **Dependency Injection**: Used extensively across services (e.g., mesh generation, asset loading, settings) to ensure modularity.

---

## Architecture & Patterns

The application strictly separates logic from UI through the **MVVM** pattern, combined with **Dependency Injection (DI)**.

- **Views (`.axaml`)**: Handle only UI binding, themes, and layout definitions (e.g., `DashboardView`, `SettingsView`).
- **ViewModels**: Manage application state, expose `ICommands` (via `[RelayCommand]`), and bridge the UI with background services.
- **Services**: Pure logic components registered via DI (e.g., `IStructureLoaderService`, `IMeshService`, `IColorSeparatorService`). 

*Example of Service Injection:*
```csharp
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMeshService _meshService;
    private readonly IStructureLoaderService _structureLoader;

    public DashboardViewModel(IMeshService meshService, IStructureLoaderService structureLoader)
    {
        _meshService = meshService;
        _structureLoader = structureLoader;
    }
}
```

This decoupling ensures algorithms can be swapped, upgraded, or unit-tested in complete isolation.

---

## Core Mechanisms & Algorithms

### 1. Multi-Format Asset Parsing
The application doesn't just read vanilla NBTs; it supports popular modding formats like **Litematica (`.litematic`)** and older **Schematics (`.schematic`)**. 

**Design Decision: Dynamic Asset Reading vs. Hardcoding**
Instead of hardcoding a massive dictionary of block IDs to static RGB values or distributing copyrighted textures within the repository, **MCto3D dynamically extracts base game assets** directly from the user's local Minecraft installation (`.jar` or resource packs). 
- **Why?** First and foremost, this strict separation ensures **zero copyright infringement**, as no proprietary Mojang assets are shipped with the software. Secondly, it guarantees *perfect color accuracy* and instant forward-compatibility with future Minecraft versions (as new blocks are read automatically without requiring code updates), while also allowing support for custom resource packs. The engine mathematically maps each block ID to its exact dominant color at runtime.

### 2. Mesh Generation & Topology Culling
To prevent generating overwhelmingly large 3D files (a common issue with voxel data where millions of faces are generated), the app implements structural optimization:
- **Adjacency Culling**: Analyzes 3D matrices to cull hidden interior faces between connected blocks, reducing triangle counts by up to 80%.
- **Flood-Fill Enclosure Detection**: Optionally uses a 3D flood-fill algorithm starting from the outer bounding box to detect and cull enclosed empty pockets (hollow spaces), leaving only the outer visible shell for perfect 3D printing.

### 3. Advanced Color Clustering Algorithms
Handling full-color Minecraft structures often results in hundreds of slightly different texture colors. For multi-color 3D printing (like Bambu Lab AMS or Prusa MMU), this must be reduced. The app provides multiple mathematical approaches:

- **Custom Palettes**: Nearest-neighbor color mapping against a user-defined RGB palette using Euclidean color distance.
- **K-Means Clustering**: Automatically groups the voxel colors into *K* clusters. 
  - *Mathematical Detail*: The centroid color is calculated using **Root Mean Square (RMS)** averaging rather than simple arithmetic means. This prevents mixed colors from becoming muddy or gray, preserving vibrancy.
- **K-Medoids Clustering**: Similar to K-Means, but restricts the centroid to an *actual existing block color*, ensuring pure, unmixed textures.

*Visual Example:*
> *TODO: Insert a comparison image showing a structure in Raw Colors vs K-Means (16 colors) vs Custom Palette.*
> `![Color Clustering Comparison](docs/images/color_algorithms.png)`

### 4. 3MF & STL Exporting
- **STL**: Generates monolithic mesh geometries for standard, single-color structural printing.
- **3MF**: Fully supports multi-color mesh grouping. The exporter groups triangles into distinct color objects, writing compliant XML structures for the 3MF payload. This makes it instantly compatible with modern multi-color slicers as "assemblies," meaning users don't have to manually paint models in the slicer software.

---

## Contributing

While this documentation provides a high-level technical overview of the architecture and pipeline, specific proprietary implementations regarding exact file structuring, memory layout, and local persistent states are kept internal to protect the core intellectual property of the project.

If you are looking to contribute to the UI or standard services:
1. Ensure that your code adheres to the existing **MVVM structure**.
2. Place business logic inside `Services/`, UI bindings in `ViewModels/`, and keep `Views/` free of code-behind logic.
3. Background tasks (like mesh generation) **must** correctly implement `CancellationToken` to keep the UI thread responsive during heavy mathematical operations.

> **Note**: UI elements must be built using Avalonia's styling system and should bind to the dynamic localization dictionaries for multi-language support (English/Spanish).
