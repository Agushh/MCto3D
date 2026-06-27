using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCto3D.Services
{
    public interface IColorGeneratorService
    {
        Task<Dictionary<string, byte[]>> GenerateAndLoadColors(string localAppFolder, IProgress<string> progress = null);
        Dictionary<string, byte[]> LoadColorsSync(string localAppFolder);
    }
}
