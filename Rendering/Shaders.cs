namespace MCto3D.Rendering
{
    public static class Shaders
    {
        // ---------------- MODEL SHADERS ----------------
        // Vertex Shader: Pasa posiciones, normales, color local, y también la posición absoluta del mundo (WorldPos) para fake AO y grid.
        public const string ModelVertexShader = @"#version 300 es
precision highp float;
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aColor;

uniform mat4 model;
uniform mat4 mvp; // MVP premultiplicado para evitar Z-fighting en bordes

out vec3 FragColor;
out vec3 Normal;
out vec3 FragPos;
out vec3 LocalPos; // NUEVO: Posición original sin trasladar para la cuadrícula

void main()
{
    vec4 worldPosition = model * vec4(aPos, 1.0);
    FragPos = vec3(worldPosition);
    LocalPos = aPos;
    Normal = aNormal; // Las normales de bloques no necesitan rotarse
    FragColor = aColor; 
    gl_Position = mvp * vec4(aPos, 1.0);
}";

        // Fragment Shader para el modelo: Iluminación estilizada de 3 puntos (Key, Fill, Rim) + Pseudo AO basado en altura Z
        public const string ModelFragmentShader = @"#version 300 es
precision highp float;
out vec4 FragColorOut;

in vec3 FragColor;
in vec3 Normal;
in vec3 FragPos;
in vec3 LocalPos;

uniform int useModelColor;
uniform vec3 modelColor;
uniform float minZ; 
uniform float meshHeight; 
uniform vec3 cameraPos; 

void main()
{
    vec3 baseColor = (useModelColor == 1) ? modelColor : FragColor;
    
    // Iluminación Plana Estilo Minecraft
    float light = 0.6; // Y (Front/Back)
    if (abs(Normal.x) > 0.5) light = 0.8; // X (Sides)
    if (Normal.z > 0.5) light = 1.0; // +Z (Top)
    if (Normal.z < -0.5) light = 0.5; // -Z (Bottom)
    
    vec3 result = baseColor * light;
    
    // Fake Ambient Occlusion dinámico adaptado a la altura total
    float height = FragPos.z - minZ;
    float gradientZone = max(meshHeight * 0.4, 20.0); 
    float heightNorm = clamp(height / gradientZone, 0.0, 1.0); 
    
    float ao = mix(0.5, 1.0, heightNorm); 
    
    float downFactor = clamp(dot(Normal, vec3(0,0,-1)), 0.0, 1.0);
    ao = mix(ao, ao * 0.8, downFactor);

    result = result * ao;
    
    // GRADIENTE DEPENDIENTE DE LA VISTA (Rompe la uniformidad de paredes planas grandes)
    vec3 viewDir = normalize(cameraPos - FragPos);
    float NdotV = max(dot(Normal, viewDir), 0.0);
    float viewGradient = mix(0.85, 1.0, NdotV); // Oscurece sutilmente las zonas donde la vista es tangencial
    
    result = result * viewGradient;
    
    // ATENUACIÓN POR PROFUNDIDAD SUTIL (Fog/Depth Shadow)
    float distToCam = distance(FragPos, cameraPos);
    float maxDist = max(meshHeight * 2.0, 200.0);
    float depthShadow = clamp(1.0 - (distToCam / maxDist), 0.8, 1.0);
    result = result * depthShadow;
    
    FragColorOut = vec4(result, 1.0);
}";

        // ---------------- FLOOR SHADERS ----------------
        public const string FloorVertexShader = @"#version 300 es
precision highp float;
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal; // Dummy
layout (location = 2) in vec3 aColor;

uniform mat4 view;
uniform mat4 projection;

out vec3 FragPos;
out vec3 FragColor;

void main()
{
    FragPos = aPos;
    FragColor = aColor;
    gl_Position = projection * view * vec4(aPos, 1.0);
}";

        public const string FloorFragmentShader = @"#version 300 es
precision highp float;
out vec4 FragColorOut;

in vec3 FragPos;
in vec3 FragColor;

uniform float fogDistance;

// Función de ruido estático por bloque
float randomBlock(vec2 p) {
    vec2 block = floor(p);
    return fract(sin(dot(block, vec2(12.9898, 78.233))) * 43758.5453);
}

void main()
{
    // Distancia al centro del modelo (0,0) para desvanecimiento
    float dist = distance(FragPos.xy, vec2(0.0));
    float fade = clamp(1.0 - (dist / fogDistance), 0.0, 1.0);
    
    // Ruido estático aleatorio por cada celda de 1x1
    float n = randomBlock(FragPos.xy);
    float noiseVariation = (n - 0.5) * 0.08; // +/- 4% variación
    
    // Grid (Cuadrícula)
    vec2 gridPos = fract(FragPos.xy);
    float gridLine = step(0.98, gridPos.x) + step(0.98, gridPos.y); // Línea más fina
    gridLine = clamp(gridLine, 0.0, 1.0);
    
    // Color base
    vec3 baseColor = FragColor + noiseVariation;
    vec3 gridColor = baseColor * 0.85; // Líneas un poco más oscuras
    
    vec3 finalColor = mix(baseColor, gridColor, gridLine * 0.6);
    
    FragColorOut = vec4(finalColor, fade * 0.8);
}";


        // ---------------- SKY SHADERS ----------------
        // Se renderiza como un quad full screen
        public const string SkyVertexShader = @"#version 300 es
precision highp float;
layout (location = 0) in vec2 aPos;

out vec2 uv;

void main()
{
    uv = aPos * 0.5 + 0.5; // De [-1, 1] a [0, 1]
    gl_Position = vec4(aPos, 0.999, 1.0); // Z muy lejana
}";

        // Nubes estilo Minecraft procedurales
        public const string SkyFragmentShader = @"#version 300 es
precision highp float;
out vec4 FragColorOut;

in vec2 uv;
uniform float u_time;
uniform vec3 cameraDir; // Para orientar el cielo si la cámara gira, aunque lo haremos simple aquí.
uniform mat4 invViewProj; // Matriz inversa para calcular la dirección del rayo

vec2 hash(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i + vec2(0.0, 0.0)).x, hash(i + vec2(1.0, 0.0)).x, u.x),
               mix(hash(i + vec2(0.0, 1.0)).x, hash(i + vec2(1.0, 1.0)).x, u.x), u.y);
}

void main()
{
    // Reconstruir dirección del rayo en el mundo
    vec4 clipSpace = vec4(uv * 2.0 - 1.0, 1.0, 1.0);
    vec4 worldPos = invViewProj * clipSpace;
    vec3 rayDir = normalize(worldPos.xyz / worldPos.w);
    
    // Color de cielo base (Celeste Minecraft)
    vec3 skyColor = mix(vec3(0.5, 0.7, 0.95), vec3(0.2, 0.4, 0.8), max(rayDir.z, 0.0));
    
    if (rayDir.z > 0.1) // Solo dibujamos nubes en la mitad superior
    {
        // Intersección con plano de nubes (z = constante)
        float t = 50.0 / rayDir.z;
        vec2 cloudPos = (rayDir.xy * t) * 0.02; // Escala de las nubes
        
        // Movimiento de nubes mucho más lento
        cloudPos.x += u_time * 0.05;
        
        // Pixelar las coordenadas para efecto Minecraft
        vec2 blockyPos = floor(cloudPos * 2.0) / 2.0;
        
        // Ruido fractal simple
        float n = noise(blockyPos);
        n += 0.5 * noise(blockyPos * 2.0);
        
        // Threshold para hacer islas de nubes cortadas
        if (n > 0.85)
        {
            // Nube base
            vec3 cloudColor = vec3(1.0, 1.0, 1.0);
            
            // Sombra en los bordes para volumen falso
            float shadowN = noise(blockyPos + vec2(0.5, -0.5));
            if (shadowN > 0.85) {
                // Interior de la nube
            } else {
                // Borde de la nube
                cloudColor = vec3(0.85, 0.85, 0.9);
            }
            
            // Desvanecer nubes muy lejanas
            float cloudFade = clamp(1.0 - (t / 600.0), 0.0, 1.0);
            skyColor = mix(skyColor, cloudColor, cloudFade);
        }
    }
    
    FragColorOut = vec4(skyColor, 1.0);
}";
    }
}
