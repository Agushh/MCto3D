<div align="center"> 

  # **MCto3D**
  
  <img width="1294" height="746" alt="image" src="https://github.com/user-attachments/assets/a7e2fa1a-069f-4b7e-9904-dc87b4f60a87" />

  
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

### 2. Geometry Modes & Topology Culling
To prevent generating overwhelmingly large 3D files (a common issue with voxel data where millions of faces are generated), the app implements structural optimization modes:
- **Solid Blocks (Optimized Geometry)**: This is the default mode. It analyzes 3D matrices to perform **Adjacency Culling**, removing hidden interior faces between connected blocks. This reduces triangle counts by up to 80%, making the model perfect for 3D printing.
  - *Flood-Fill Enclosure Detection*: As part of this mode, it optionally uses a 3D flood-fill algorithm starting from the outer bounding box to detect and cull enclosed empty pockets (hollow spaces), leaving only the outer visible shell.
- **Full Geometry**: This mode preserves absolutely all faces of every block, including internal ones. It is highly resource-intensive but necessary if the user intends to slice, section, or manipulate the internal structure of the model in a 3D editing software (like Blender) later on.

*Visual Example:*

<img width="731" height="514" alt="Full Geometry" src="https://github.com/user-attachments/assets/faebe952-547e-49a0-97bd-6ead5bd04624" />


### 3. Advanced Color Clustering Algorithms
Handling full-color Minecraft structures often results in hundreds of slightly different texture colors. For multi-color 3D printing (like Bambu Lab AMS or Prusa MMU), this must be reduced. The app provides multiple mathematical approaches:

- **Custom Palettes**:
  <img width="857" height="630" alt="image" src="https://github.com/user-attachments/assets/9a30b42e-5fa9-4c22-8830-298ab4f18880" />

  Nearest-neighbor color mapping against a user-defined RGB palette using Euclidean color distance.

- **K-Means Clustering**:
  <img width="861" height="654" alt="image" src="https://github.com/user-attachments/assets/6f8cea19-4967-4562-96ed-aab12bf38bdd" />

  Automatically groups the voxel colors into *K* clusters. 
  - *Mathematical Detail*: The centroid color is calculated using **Root Mean Square (RMS)** averaging rather than simple arithmetic means. This prevents mixed colors from becoming muddy or gray, preserving vibrancy.

- **K-Medoids Clustering**:
  <img width="861" height="648" alt="image" src="https://github.com/user-attachments/assets/9ad2ddea-1eb1-4381-abd9-245fb209f4dc" />

  Similar to K-Means, but restricts the centroid to an *actual existing block color*, ensuring pure, unmixed textures.

- **Raw Colors** (Only for visuals and testing):
  <img width="866" height="642" alt="image" src="https://github.com/user-attachments/assets/b0e11305-2be1-4e70-9825-79ed17609e81" />

  Make every block its own color, grouping the same colors together.

### 4. 3MF & STL Exporting
- **STL**: Generates monolithic mesh geometries for standard, single-color structural printing.
- **3MF**: Fully supports multi-color mesh grouping. The exporter groups triangles into distinct color objects, writing compliant XML structures for the 3MF payload. This makes it instantly compatible with modern multi-color slicers as "assemblies," meaning users don't have to manually paint models in the slicer software.

### 5. Local Files Manager ("My Files") & Native Support
The application features a built-in file management system that allows users to organize their projects seamlessly without leaving the software.
- **Native Loading**: MCto3D is fully capable of reading native Minecraft structure formats natively. Users can load raw `.nbt`, `.litematic`, and `.schematic` files straight into the viewer, converting them to 3D on-the-fly.
- **Persistent Storage**: When a user exports or saves a structure from the Dashboard, it is automatically cataloged in the local storage directory (configurable via Settings). 
- **Quick Slicer Access**: The "My Files" tab lets users browse all their saved 3D conversions (`.3mf` or `.stl`), view rich metadata (triangle count, dimensions), and open them directly in their default 3D Slicer software (e.g., Bambu Studio) with a single click.

---

## Contributing

While this documentation provides a high-level technical overview of the architecture and pipeline, specific proprietary implementations regarding exact file structuring, memory layout, and local persistent states are kept internal to protect the core intellectual property of the project.

If you are looking to contribute to the UI or standard services:
1. Ensure that your code adheres to the existing **MVVM structure**.
2. Place business logic inside `Services/`, UI bindings in `ViewModels/`, and keep `Views/` free of code-behind logic.
3. Background tasks (like mesh generation) **must** correctly implement `CancellationToken` to keep the UI thread responsive during heavy mathematical operations.

> **Note**: UI elements must be built using Avalonia's styling system and should bind to the dynamic localization dictionaries for multi-language support (English/Spanish).

---

## About the Development

Hey there! 👋 I'm an indie developer who is just starting out in the world of real software development. 

To bring this ambitious project to life, I heavily assisted myself with AI coding agents. Because of this rapid implementation process, there might be sections of the code that are incorrect, poorly optimized, or simply not following the best industry practices yet. My goal is to continuously learn and perfect this application over time, but for now, you might encounter some bugs or weird architectural decisions.

I am completely open to feedback, suggestions, and pull requests! Every comment and piece of advice is highly appreciated and will help me grow as a developer. Thank you for checking out MCto3D!

---

### 🌐 Stay Updated

If you'd like to follow the journey, download the latest stable versions, or read my devlogs, check out my landing page:
🔗 **[Visit My Website / Devlogs](https://your-website-url-here.com)**

Here I will be posting news, updates on MCto3D, and future projects I'll be working on. Stay tuned!
