using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Services.FileReading
{
    public interface IStructureReader
    {
        bool CanRead(string filePath);
        StructureData Read(string filePath);
    }
}
