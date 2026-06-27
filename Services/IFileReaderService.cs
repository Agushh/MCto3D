using System;
using System.Collections.Generic;

namespace MCto3D.Services
{
    public interface IFileReaderService
    {
        structureData readNBT(string filePath);
        structureData readLitematic(string filePath);
        multiColorStructureData CreateMultiColorData(structureData strData, int? k, List<System.Drawing.Color> userColors, bool useKMedoids = false, bool useRawColors = false, bool fillHoles = false, bool useCustomFillColor = false, System.Drawing.Color customFillColor = default);
    }
}
