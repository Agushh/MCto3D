using System;
using System.Threading.Tasks;

namespace MCto3D.Services
{
    public interface IAssetExtractorService
    {
        Task<string> ExtractLegalAssets(string localFilesPath, IProgress<string> progress = null, string minecraftLocation = "default");
    }
}
