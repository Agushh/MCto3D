# MCto3D (Alpha 1.0)
## Documentación Oficial

MCto3D es una herramienta de conversión diseñada para tomar estructuras de Minecraft (NBT) y transformarlas en modelos 3D optimizados para impresión 3D o renderizado (3MF/STL), preservando geometría de bloques y colores.

### 📌 Características Principales
- **Carga de estructuras NBT**: Soporte para formatos estándar de Minecraft.
- **Exportación 3MF y STL**: Generación de modelos 3D de alta precisión.
- **Visualizador 3D en tiempo real**: Renderizado interactivo del modelo.
- **Múltiples Algoritmos de Color**: Selección inteligente de colores para el modelo.
- **Modo Ensamblaje**: Exportación separada por piezas según el color para facilitar la impresión 3D.
- **Multilenguaje**: Soporte para Español e Inglés.

---

### 🎨 Algoritmos de Color
La aplicación ofrece distintos enfoques para procesar los colores de los bloques, permitiendo al usuario decidir el balance entre exactitud visual y cantidad de colores:

1. **Color Monocromático**: Asigna un solo color a todo el modelo.
2. **Paleta Personalizada**: Permite al usuario elegir colores específicos. Los colores del NBT se mapean al color más cercano de la paleta elegida utilizando distancias matemáticas RGB.
3. **Paleta Predefinida**: Selecciona automáticamente los colores más representativos usando algoritmos de clustering, limitando a 4, 8, 16 o 32 colores.
4. **K-Means (Color Promedio)**: Agrupa los colores en K grupos (K-Means) y promedia los colores de cada grupo usando la Media Cuadrática (RMS) para evitar que los colores se vuelvan grises o apagados.
5. **K-Medoids (Color Real)**: Similar a K-Means, pero el centroide de cada grupo es un color *real* existente en la textura del bloque, evitando mezclas artificiales.
6. **Colores Crudos (Sin procesar)**: Toma los colores nativos puros extraídos de las texturas originales del juego, sin reducir su cantidad.

---

### ⚙️ Modos de Geometría
- **Geometrías Optimizadas**: Solo genera los triángulos visibles, eliminando las caras internas ocultas entre bloques. Esto reduce drásticamente el peso del archivo 3D.
- **Geometrías Completas**: Conserva absolutamente todas las caras de cada bloque (incluso las internas). Útil si el modelo será seccionado o manipulado internamente en un programa de edición 3D.

---

### 📂 Estructura de Archivos
- **Formatos de Entrada**: `.nbt` (Minecraft Structure Format).
- **Formatos de Salida**: 
  - `.3mf`: Ideal para impresión a color, soporta metadatos y ensamblajes.
  - `.stl`: Formato estándar geométrico (no soporta color).
- **Lectura en Dashboard**: El Dashboard ahora es capaz de cargar directamente archivos `.3mf` previamente exportados para una previsualización veloz.

---

### 🛠️ Configuración (Settings)
- **Modo Ensamblaje (3MF)**: Permite decidir si el archivo 3MF se exportará como múltiples objetos agrupados (para poder pintarlos/imprimirlos por separado) o como una sola malla sólida.
- **Renderizado (Viewport)**:
  - Alternar visualización del suelo (Grid/Plano).
  - Modificar colores visuales de fondo y suelo temporalmente para previsualización.
- **Idiomas**: Cambio en tiempo real de idioma en la interfaz.

---

### 🚀 Flujo de Trabajo (Workflow)
1. Abrir la pestaña **Mis Archivos**.
2. Presionar **+ Importar NBT** para buscar un archivo de Minecraft en el equipo.
3. Se abrirá el Dashboard del modelo seleccionado.
4. (Opcional) Modificar el **Algoritmo de Color** según los requisitos visuales.
5. Verificar la previsualización 3D interactiva.
6. Configurar la **Escala del Bloque** y el **Modo de Geometría**.
7. Seleccionar un formato y presionar **Exportar**.
8. El archivo puede abrirse en Slicers compatibles (PrusaSlicer, Cura, Bambu Studio) directamente desde la UI.

---
_MCto3D Alpha 1.0 - Generado automáticamente_
