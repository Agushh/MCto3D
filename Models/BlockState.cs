using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Models
{
    public struct BlockState
    {
        public string Name; // ej: "minecraft:acacia_stairs"
        public Dictionary<string, string> Properties; // ej: ["facing"] = "east", ["half"] = "bottom"

        // Un método útil para cuando necesites buscar este bloque en tu caché de Blockstates
        public string GetPropertiesString()
        {
            if (Properties == null || Properties.Count == 0) return "";

            List<string> props = new List<string>();
            foreach (var kvp in Properties)
            {
                props.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join(",", props);
        }
    }
}
