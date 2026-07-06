using MCto3D.Models;
using MCto3D.Services.ColorProcesing;
using MCto3D.Services.ColorProcesing.Algorithms;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MCto3D.Services.ColorProcesing.Factory
{
    public interface IColorAlgorithmFactory
    {
        IColorAlgorithmService GetAlgorithm(ColorAlgorithm algorithmType);
    }
    public class ColorAlgorithmFactory : IColorAlgorithmFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public ColorAlgorithmFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IColorAlgorithmService GetAlgorithm(ColorAlgorithm algorithmType)
        {
            return algorithmType switch
            {
                ColorAlgorithm.SingleColor => _serviceProvider.GetRequiredService<SingleColorService>(),
                ColorAlgorithm.RawColors => _serviceProvider.GetRequiredService<RawColorService>(),
                ColorAlgorithm.KMeans => _serviceProvider.GetRequiredService<KMeansColorsService>(),
                ColorAlgorithm.KMedoids => _serviceProvider.GetRequiredService<KMedoidsColorsService>(),
                ColorAlgorithm.Palette => _serviceProvider.GetRequiredService<UserPaletteColorsService>(),
                _ => throw new ArgumentException("Algoritmo no soportado", nameof(algorithmType))
            };
        }
    }
}
