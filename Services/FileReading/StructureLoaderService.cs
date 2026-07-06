using fNbt;
using MCto3D.Models;
using MCto3D.Services.ColorProcesing;
using MCto3D.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using Tmds.DBus.Protocol;

namespace MCto3D.Services.FileReading
{
    public class StructureLoaderService
    {

        private readonly IEnumerable<IStructureReader> _readers;

        public StructureLoaderService(IEnumerable<IStructureReader> readers)
        {
            _readers = readers;
        }

        public StructureData Load(string path)
        {
            var reader = _readers.FirstOrDefault(r => r.CanRead(path));
            if (reader == null) throw new NotSupportedException("Formato no soportado");
            return reader.Read(path);
        }
    }
}

