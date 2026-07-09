<div align="center"> 
<picture>
  <img width="15%" height="15%" alt="MCto3D Logo" src="https://github.com/user-attachments/assets/7c8e904a-6826-410c-a8e0-84d4cbf87b2d" />
</picture>

<div align="center"> <STRONG> <H1> MCTo3D </H1> </STRONG> </div> 

<div align="center">
  <a href="https://github.com/Agushh/MCto3D/releases"><img src="https://img.shields.io/github/v/release/Agushh/MCto3D?include_prereleases&color=blue&label=Latest%20Release" alt="Latest Release" /></a>
  <img src="https://img.shields.io/github/downloads/Agushh/MCto3D/total?color=brightgreen" alt="Total Downloads" />
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/UI-Avalonia-8A338F?logo=avalonia&logoColor=white" alt="Avalonia UI" />
</div>
<br/>

A powerful desktop application designed to convert Minecraft structure files into highly optimized 3D models suitable for 3D printing and rendering.
It accurately preserves block geometry, adds texture colors, and applies color-clustering algorithms for multi-color 3D printing.
</div>

<img width="100%" height="739" alt="MainMenu" src="https://github.com/user-attachments/assets/0f46b91e-cd09-4e6c-8d8a-9228d5dd8d86" />


## Table of Contents
- [Key Features](#key-features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Build](#build)
- [How to use](#how-to-use)
- [Legal Actions](#legal-actions)
- [Credits & Acknowledgments](#credits--acknowledgments)
- [How to contribute](#how-to-contribute)
- [Donations](#donations)
- [Stay Updated](#-stay-updated)
- [License](#license)

## Key Features
- Reads `.nbt`, `.schematic`, and `.litematic` files.
- Generates `.stl` and `.3mf` models.
- Multi-color model generator with algorithms for optimized models:
  - **`K-Means`**: Groups colors based on representative colors.
  - **`K-Medoids`**: Similar to K-Means, but uses actual model colors for representation.
  - **`Custom Palette`**: Allows you to set up your own colors and arranges the model based on that palette.
  - **`Raw Colors`**: Best for rendering or testing, offering a high-quality look of the model.
- Generates cubic and full geometry models.
- Built-in saving system for generated models.
- Pre-loaded Vanilla Minecraft structures ready to use.
- Spanish and English language support.

> IMPORTANT: The program requires Minecraft to be installed on your PC to run. See more in the ["Legal Actions"](#legalactions) section.

## Prerequisites
Before using or building the application, ensure you meet the following requirements:
- **Operating System:** Windows 10 or Windows 11 (64-bit).
- **Minecraft Java Edition:** Must be installed on your system to extract local assets for block geometry and textures.
- **.NET 10 SDK:** Only required if you intend to build the project from source.

## Installation

`MCto3D` is available for Windows. 

Download the latest build from the [Releases page](https://github.com/Agushh/MCto3D/releases). Once downloaded, simply unzip the folder and run `MCto3D.exe`.
> Important: Because this is an independent project, it doesn't currently have a digital signature, so Windows Defender or other antivirus software might alert you. This software is open-source, so the code is available online for you to build yourself. More information in the [Build Guide](#build).


## Build 
`MCto3D` is free and open source.

To build the project, you will need to have the [.NET 10 (or newer) SDK](https://dotnet.microsoft.com/download) installed.

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Agushh/MCto3D.git
   cd MCto3D
   ```

2. **Build the project:**
   Run the following command to compile the application:
   ```bash
   dotnet build
   ```

3. **(Optional) Run the project:**
   If you want to run it directly from the source code:
   ```bash
   dotnet run
   ```

4. **(Optional) Publish as a standalone executable:**
   To create a self-contained executable for Windows (so users don't need to install .NET to run it), use:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```
   *The compiled executable will be located in the `bin/Release/.../publish` folder.*


## How to use

> Here is a full video of using the software! [Youtube](youtube.com)

Once you have successfully [installed](#installation) the program, it will need to make a local copy of the Minecraft game assets. You can change the location of these local files later in the settings.
When the assets load successfully, the program will be ready to work. 

From there, head to the Convert section (by clicking the big "Convert" button or navigating from the left menu). Then, select your desired `.nbt`, `.schematic`, or `.litematic` file. The real-time renderer will load your model and provide options to customize it.

### What can you customize? 

In this version, you can change the model's export format between `.stl` and `.3mf`. You can also create a colored 3D model using the available algorithms (you must select `.3mf` first) and change the model's geometry. If you need a simple model or the structure is very large, you can use **solid blocks**, which will create full cube geometries for all blocks (16x16 pixels). For a more detailed build, you can choose **full geometries**, which creates detailed models for each specific block shape (like stairs, fences, and others). 

<img height="350" alt="Full Geometry - Solid Blocks Comparison" src="https://github.com/user-attachments/assets/cefa0ecf-d26e-4301-a99f-8c2864c04133" />

<img height="350" alt="Types of algorythms" src="https://github.com/user-attachments/assets/b24c1e47-7a91-4544-b23e-ee003ff8f987" />


### What can I do with the model?
Once you finish customizing, you can do any of the following: 
 - Export locally (saves the file to your system).
 - Open it directly in your slicer.
 - Save it within the app for future use (with a custom preview image!).

## Legal Actions

Because this project is independent, I cannot pack the Minecraft assets directly into the app, as this violates Minecraft and Microsoft's copyright.

How do we handle it? The application reads the local files from your pre-installed game (don't worry, it will not damage or delete anything) and then saves copies as local files to generate the models.  

## Credits & Acknowledgments
This project is built using several amazing open-source libraries:
- **[Avalonia UI](https://avaloniaui.net/):** The core cross-platform UI framework used to build this desktop application.
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet):** Used for managing the MVVM architecture cleanly and efficiently.
- **[fNbt](https://github.com/fragmer/fNbt):** Used to efficiently read and parse `.nbt`, `.schematic`, and `.litematic` files.
- **[StbImageSharp](https://github.com/StbSharp/StbImageSharp):** Used for fast image processing and texture handling.

## How to contribute

1. Clone repo and create a new branch
2. Make changes and test
3. Submit pull request with comprehensive description of changes

## Donations

This is free, open-source software. If you'd like to support the development of future projects, or say thanks for this one, you can help buying me a coffee!
(link and image placeholder)

### 🌐 Stay Updated

If you'd like to follow the journey, download the latest stable versions, or read my devlogs, check out my landing page:
🔗 **[Visit My Website / Devlogs](https://your-website-url-here.com)**

Here I will be posting news, updates on MCto3D, and future projects I'll be working on. **You can also find the official Roadmap and upcoming features there!** Stay tuned!

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
