using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace MCto3D.Services
{
    public class NativeModelResolver_Service
    {
        private static string BasePath => Path.Combine(AppSettings_Service.LocalFilesPath, "MinecraftExtractedAssets", "assets", "minecraft");
        private static string BlockstatesPath => Path.Combine(BasePath, "blockstates");
        private static string ModelsPath => Path.Combine(BasePath, "models");

        // Caché: BlockStateKey -> Cuboides
        private static Dictionary<string, List<(Vector3 from, Vector3 to)>> _cache = new();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static List<(Vector3 from, Vector3 to)> ResolveGeometry(string blockName, Dictionary<string, string> properties)
        {
            string cleanName = blockName.Split('[')[0].Replace("minecraft:", "");
            
            // Creamos un string único para este estado
            string propString = properties != null && properties.Count > 0 
                ? string.Join(",", properties.Select(kv => $"{kv.Key}={kv.Value}").OrderBy(x => x)) 
                : "normal";
            
            string cacheKey = $"{cleanName}[{propString}]";

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            string bsFile = Path.Combine(BlockstatesPath, cleanName + ".json");
            if (!File.Exists(bsFile))
            {
                // Fallback: tratar de leer directamente el modelo block/cleanName
                var fallback = ResolveModelAndElements("block/" + cleanName, 0, 0);
                _cache[cacheKey] = fallback;
                return fallback;
            }

            try
            {
                string jsonContent = File.ReadAllText(bsFile);
                JsonNode root = JsonNode.Parse(jsonContent);

                if (root["variants"] != null)
                {
                    var result = ResolveVariants(root["variants"].AsObject(), properties);
                    _cache[cacheKey] = result;
                    return result;
                }
                else if (root["multipart"] != null)
                {
                    var result = ResolveMultipart(root["multipart"].AsArray(), properties);
                    if (result.Count > 0)
                    {
                        _cache[cacheKey] = result;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeResolver] Error {cleanName}: {ex.Message}");
            }

            // Si llegamos aquí o si el bloque (como mushroom_stem) falló en su multipart y devolvió 0 elementos, 
            // intentamos como último recurso resolver su modelo directo asumiendo nombre estandar.
            var directFallback = ResolveModelAndElements("block/" + cleanName, 0, 0);
            if (directFallback.Count > 0)
            {
                _cache[cacheKey] = directFallback;
                return directFallback;
            }

            _cache[cacheKey] = new List<(Vector3, Vector3)>();
            return _cache[cacheKey];
        }

        private static List<(Vector3 from, Vector3 to)> ResolveVariants(JsonObject variants, Dictionary<string, string> properties)
        {
            // Intentamos buscar una variante que coincida con todas las propiedades
            foreach (var variant in variants)
            {
                string variantKey = variant.Key; // ej: "facing=east,half=bottom" o "" (normal)
                
                bool match = true;
                if (variantKey != "" && variantKey != "normal")
                {
                    string[] conditions = variantKey.Split(',');
                    foreach (var cond in conditions)
                    {
                        var parts = cond.Split('=');
                        if (parts.Length == 2)
                        {
                            if (!properties.ContainsKey(parts[0]) || properties[parts[0]] != parts[1])
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                }

                if (match)
                {
                    // variant.Value puede ser un Objeto o un Array de objetos (aleatorio). Tomamos el primero.
                    JsonObject modelObj = null;
                    if (variant.Value is JsonArray arr && arr.Count > 0)
                        modelObj = arr[0].AsObject();
                    else if (variant.Value is JsonObject obj)
                        modelObj = obj;

                    if (modelObj != null)
                    {
                        string modelName = modelObj["model"]?.GetValue<string>().Replace("minecraft:", "");
                        int rotX = modelObj["x"] != null ? modelObj["x"].GetValue<int>() : 0;
                        int rotY = modelObj["y"] != null ? modelObj["y"].GetValue<int>() : 0;

                        return ResolveModelAndElements(modelName, rotX, rotY);
                    }
                }
            }

            // Fallback al primero si no hubo coincidencia perfecta
            if (variants.Count > 0)
            {
                var first = variants.ElementAt(0).Value;
                JsonObject modelObj = first is JsonArray arr && arr.Count > 0 ? arr[0].AsObject() : first as JsonObject;
                if (modelObj != null)
                {
                    string modelName = modelObj["model"]?.GetValue<string>().Replace("minecraft:", "");
                    int rotX = modelObj["x"] != null ? modelObj["x"].GetValue<int>() : 0;
                    int rotY = modelObj["y"] != null ? modelObj["y"].GetValue<int>() : 0;
                    return ResolveModelAndElements(modelName, rotX, rotY);
                }
            }
            return new List<(Vector3, Vector3)>();
        }

        private static List<(Vector3 from, Vector3 to)> ResolveMultipart(JsonArray multipart, Dictionary<string, string> properties)
        {
            List<(Vector3 from, Vector3 to)> finalCuboids = new();

            foreach (var part in multipart)
            {
                if (part is JsonObject partObj)
                {
                    bool conditionMet = true;

                    if (partObj.ContainsKey("when") && partObj["when"] is JsonObject whenObj)
                    {
                        conditionMet = EvaluateWhenCondition(whenObj, properties);
                    }

                    if (conditionMet && partObj.ContainsKey("apply"))
                    {
                        var applyNode = partObj["apply"];
                        
                        JsonObject modelObj = null;
                        if (applyNode is JsonArray arr && arr.Count > 0)
                            modelObj = arr[0].AsObject();
                        else if (applyNode is JsonObject obj)
                            modelObj = obj;

                        if (modelObj != null)
                        {
                            string modelName = modelObj["model"]?.GetValue<string>().Replace("minecraft:", "");
                            int rotX = modelObj.ContainsKey("x") ? modelObj["x"].GetValue<int>() : 0;
                            int rotY = modelObj.ContainsKey("y") ? modelObj["y"].GetValue<int>() : 0;

                            var elements = ResolveModelAndElements(modelName, rotX, rotY);
                            finalCuboids.AddRange(elements);
                        }
                    }
                }
            }

            return finalCuboids;
        }

        private static bool EvaluateWhenCondition(JsonObject whenObj, Dictionary<string, string> properties)
        {
            if (whenObj.ContainsKey("OR") && whenObj["OR"] is JsonArray orArray)
            {
                foreach (var cond in orArray)
                {
                    if (cond is JsonObject condObj && EvaluateSingleCondition(condObj, properties))
                        return true;
                }
                return false;
            }

            return EvaluateSingleCondition(whenObj, properties);
        }

        private static bool EvaluateSingleCondition(JsonObject condObj, Dictionary<string, string> properties)
        {
            if (properties == null) return false;

            foreach (var kvp in condObj)
            {
                string expectedValue = kvp.Value?.ToString();
                if (string.IsNullOrEmpty(expectedValue)) continue;

                if (!properties.ContainsKey(kvp.Key)) return false;
                
                string actualValue = properties[kvp.Key];
                
                bool match = false;
                foreach (var val in expectedValue.Split('|'))
                {
                    if (val == actualValue)
                    {
                        match = true;
                        break;
                    }
                }
                
                if (!match) return false;
            }
            return true;
        }

        private static List<(Vector3 from, Vector3 to)> ResolveModelAndElements(string modelName, int rotX, int rotY, int depth = 0)
        {
            if (depth > 10) return new List<(Vector3, Vector3)>(); // Prevenir loop infinito
            if (string.IsNullOrEmpty(modelName)) return new List<(Vector3, Vector3)>();

            string cleanName = modelName.Replace("minecraft:", "");
            string modelFile = Path.Combine(ModelsPath, cleanName + ".json");
            
            if (!File.Exists(modelFile)) return new List<(Vector3, Vector3)>();

            string jsonContent = File.ReadAllText(modelFile);
            JsonNode root = JsonNode.Parse(jsonContent);

            if (root["elements"] != null)
            {
                List<(Vector3 from, Vector3 to)> cuboids = new();
                foreach (var element in root["elements"].AsArray())
                {
                    var from = element["from"].AsArray();
                    var to = element["to"].AsArray();

                    float fx = from[0].GetValue<float>();
                    float fy = from[1].GetValue<float>();
                    float fz = from[2].GetValue<float>();

                    float tx = to[0].GetValue<float>();
                    float ty = to[1].GetValue<float>();
                    float tz = to[2].GetValue<float>();

                    // Filtro de impresión 3D: Ignoramos caras planas/2D (flores, enredaderas, etc.)
                    float sizeX = Math.Abs(tx - fx);
                    float sizeY = Math.Abs(ty - fy);
                    float sizeZ = Math.Abs(tz - fz);

                    if (sizeX <= 0 || sizeY <= 0 || sizeZ <= 0)
                    {
                        continue;
                    }

                    // Aplicamos rotación nativa si el blockstate lo pidió
                    if (rotX != 0 || rotY != 0)
                    {
                        Vector3 vf = RotateNativePoint(new Vector3(fx, fy, fz), rotX, rotY);
                        Vector3 vt = RotateNativePoint(new Vector3(tx, ty, tz), rotX, rotY);

                        cuboids.Add((
                            new Vector3(Math.Min(vf.X, vt.X), Math.Min(vf.Y, vt.Y), Math.Min(vf.Z, vt.Z)),
                            new Vector3(Math.Max(vf.X, vt.X), Math.Max(vf.Y, vt.Y), Math.Max(vf.Z, vt.Z))
                        ));
                    }
                    else
                    {
                        cuboids.Add((new Vector3(fx, fy, fz), new Vector3(tx, ty, tz)));
                    }
                }
                return cuboids;
            }
            else if (root["parent"] != null)
            {
                return ResolveModelAndElements(root["parent"].GetValue<string>(), rotX, rotY, depth + 1);
            }

            return new List<(Vector3, Vector3)>();
        }

        private static Vector3 RotateNativePoint(Vector3 point, int rotX, int rotY)
        {
            Vector3 center = new Vector3(8f, 8f, 8f);
            Vector3 centered = point - center;
            
            Matrix4x4 rot = Matrix4x4.Identity;
            if (rotX != 0)
                rot *= Matrix4x4.CreateRotationX((float)(rotX * Math.PI / 180f));
            if (rotY != 0)
                rot *= Matrix4x4.CreateRotationY((float)(-rotY * Math.PI / 180f)); // Minecraft usa giro antihorario negativo para Y a veces, revisar visualmente

            Vector3 rotated = Vector3.Transform(centered, rot);
            return rotated + center;
        }
        public static List<string> GetTexturesForBlock(string blockName, Dictionary<string, string> properties)
        {
            string cleanName = blockName.Split('[')[0].Replace("minecraft:", "");
            string bsFile = Path.Combine(BlockstatesPath, cleanName + ".json");
            List<string> modelNames = new();

            if (!File.Exists(bsFile))
            {
                modelNames.Add("block/" + cleanName);
            }
            else
            {
                try
                {
                    string jsonContent = File.ReadAllText(bsFile);
                    JsonNode root = JsonNode.Parse(jsonContent);

                    if (root["variants"] != null)
                    {
                        var variants = root["variants"].AsObject();
                        bool foundMatch = false;
                        foreach (var variant in variants)
                        {
                            string variantKey = variant.Key;
                            bool match = true;
                            if (variantKey != "" && variantKey != "normal")
                            {
                                foreach (var cond in variantKey.Split(','))
                                {
                                    var parts = cond.Split('=');
                                    if (parts.Length == 2 && (properties == null || !properties.ContainsKey(parts[0]) || properties[parts[0]] != parts[1]))
                                    {
                                        match = false; break;
                                    }
                                }
                            }
                            if (match)
                            {
                                foundMatch = true;
                                JsonObject modelObj = null;
                                if (variant.Value is JsonArray arr && arr.Count > 0) modelObj = arr[0].AsObject();
                                else if (variant.Value is JsonObject obj) modelObj = obj;
                                if (modelObj != null) modelNames.Add(modelObj["model"]?.GetValue<string>().Replace("minecraft:", ""));
                                break;
                            }
                        }
                        if (!foundMatch && variants.Count > 0)
                        {
                            var first = variants.ElementAt(0).Value;
                            JsonObject modelObj = first is JsonArray arr && arr.Count > 0 ? arr[0].AsObject() : first as JsonObject;
                            if (modelObj != null) modelNames.Add(modelObj["model"]?.GetValue<string>().Replace("minecraft:", ""));
                        }
                    }
                    else if (root["multipart"] != null)
                    {
                        foreach (var part in root["multipart"].AsArray())
                        {
                            if (part is JsonObject partObj)
                            {
                                bool conditionMet = true;
                                if (partObj.ContainsKey("when") && partObj["when"] is JsonObject whenObj)
                                    conditionMet = EvaluateWhenCondition(whenObj, properties);
                                
                                if (conditionMet && partObj.ContainsKey("apply"))
                                {
                                    var applyNode = partObj["apply"];
                                    JsonObject modelObj = applyNode is JsonArray arr && arr.Count > 0 ? arr[0].AsObject() : applyNode as JsonObject;
                                    if (modelObj != null) modelNames.Add(modelObj["model"]?.GetValue<string>().Replace("minecraft:", ""));
                                }
                            }
                        }
                    }
                }
                catch (Exception) { }
            }

            if (modelNames.Count == 0)
                modelNames.Add("block/" + cleanName);

            List<string> textures = new();
            foreach(var modelName in modelNames)
                textures.AddRange(GetTexturesFromModel(modelName, 0));
            
            return textures.Distinct().ToList();
        }

        private static List<string> GetTexturesFromModel(string modelName, int depth)
        {
            if (depth > 10 || string.IsNullOrEmpty(modelName)) return new List<string>();
            string cleanName = modelName.Replace("minecraft:", "");
            string modelFile = Path.Combine(ModelsPath, cleanName + ".json");
            
            if (!File.Exists(modelFile)) return new List<string>();

            List<string> textures = new();
            try
            {
                string jsonContent = File.ReadAllText(modelFile);
                JsonNode root = JsonNode.Parse(jsonContent);

                if (root["textures"] != null && root["textures"] is JsonObject texObj)
                {
                    foreach (var kvp in texObj)
                    {
                        string texPath = kvp.Value?.GetValue<string>();
                        if (!string.IsNullOrEmpty(texPath) && !texPath.StartsWith("#"))
                        {
                            string texName = Path.GetFileName(texPath.Replace("minecraft:", ""));
                            textures.Add(texName);
                        }
                    }
                }

                if (root["parent"] != null)
                {
                    string parent = root["parent"].GetValue<string>();
                    if (!parent.StartsWith("builtin/"))
                        textures.AddRange(GetTexturesFromModel(parent, depth + 1));
                }
            }
            catch (Exception) { }
            
            return textures;
        }
    }
}
