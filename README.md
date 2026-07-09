<div align="center"> 
<picture>
  <img width="15%" height="15%" alt="MCto3D Logo" src="https://github.com/user-attachments/assets/7c8e904a-6826-410c-a8e0-84d4cbf87b2d" />
</picture>

<div align="center"> <STRONG> <H1> MCTo3D </H1> </STRONG> </div> 
a powerful desktop application designed to convert Minecraft structure files into highly optimized 3D models suitable for 3D printing and rendering.
It accurately preserves block geometry, add texture color and do color-clustering algorithms for multi-color 3D printing.
</div>


<img width="100%"  alt="image" src="https://github.com/user-attachments/assets/23162e8c-a4a3-4712-a17f-4d09bfa3e1c5" />

## Key Features
- `.nbt`, `.schematic`, `.litematic` Files reading.
- `.stl`, `.3mf` Model Generator.
- MultiColor Model Generator with algorytms for optimized Models.
  - **`K Means`**: for making groups of color based on representational colors.
  - **`K Mediods`**: same as means but using model colors for representation.
  - **`Custom Palette`**: for setting up your colors and let the arange the model based on the palette.
  - **`Raw Colors`**: only for rendering or testing and offers a high Quality look of the model.
- Cubic and Full geometrys model generator.
- Saving system for generated models.
- Vanilla Minecraft structures natively loaded ready to use.
- Spanish and English support.

>IMPORTANT : The program needs Minecraft to be installed on your PC to run. See more on the section ["Legal Actions"](#LegalActions)

## Installation

`MCto3D` is available for Windows. 

Download the latest build from the [Releases page](https://github.com/Agushh/MCto3D/releases). When you install it, you only need to Unzip the folder and execute the `MCto3D.exe`.
>Important : Because this is my first proyect, i don't have a Digital Signature on the project, so windows defender and other AntiVirus can alert you. This Software is open source, so the code is online for you to build it. More information on [Build Guide](#Build)


## Build 
`MCto3D` is free and open source.

For building the project, you will need to have installed .NET 10 or newer.
Download the source code, and then execute : 
(code)


## How to use

> Here is a full video of using the software! [Youtube](youtube.com)

Once you have [Installed](#Installation) the program successfully, it will need to make a copy of the minecraft game assets on a local files. You can change that local files later on settings.
When it loads the assets successfully, the program will be ready to work. 

From there, you need to ahead to Convert Section (clicking the big button that says convert, or navigating from the left menu). Then you can select your `.nbt`, `.schematic` or `.litematic` file that you want, and then the realtime renderer will load, showing your model, and offers you the options for customizing your model.

### What you can customize? 

On this version, you can change the export format of the model from stl to 3mf, you can also create a colored 3D Model using the algorythms that are currently available (you need to select 3mf first), and you can also change the geometry of the model. If you need a simple model or the structure is too big, you can use solid blocks, that will create full geometrys for all the blocks (16px * 16px), or if you want a more detailed build, you can choose full geometries, that will create detailed models for each block ( like stairs, fences, and others ). 

<img width="30%" height="514" alt="Full Geometry - Solid blocks Comparative" src="https://github.com/user-attachments/assets/faebe952-547e-49a0-97bd-6ead5bd04624" />

<img width="30%" height="514" alt"Algorythms Comparative" src"second Image"/>

### What can i do with the model?
Once you finalize customization, you can do one of the followings : 
 - Export locally (saves the file on your system)
 - Open directly on your slicer.
 - Save it on the app for future ( With a custom preview image! )

## LegalActions

Because this project is independent, i can not pack the minecraft assets directly on the app. It violates Minecraft and Microsoft CopyRight.

How we handle it? We make that our application reads the local files from your pre installed game (don't scare, it will not damage or delete anything) and then save it as local files for making the models.  

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

Here I will be posting news, updates on MCto3D, and future projects I'll be working on. Stay tuned!
