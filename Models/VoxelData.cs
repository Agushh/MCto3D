using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MCto3D.Models
{
    internal class VoxelData
    {
        // Las 8 esquinas desde tu punto de referencia (Atrás-Izquierda-Abajo)
        public static readonly Vector3[] Vertices =
        [
        new(0, 0, 0), // Vértice 0: Atrás - Izquierda - Abajo (Tu Origen)
        new(1, 0, 0), // Vértice 1: Atrás - Derecha - Abajo
        new(1, 1, 0), // Vértice 2: Atrás - Derecha - Arriba
        new(0, 1, 0), // Vértice 3: Atrás - Izquierda - Arriba
        new(0, 0, 1), // Vértice 4: Frente - Izquierda - Abajo
        new(1, 0, 1), // Vértice 5: Frente - Derecha - Abajo
        new(1, 1, 1), // Vértice 6: Frente - Derecha - Arriba
        new(0, 1, 1)  // Vértice 7: Frente - Izquierda - Arriba
        ];
        // Caras: 0:Arriba, 1:Abajo, 2:Derecha, 3:Izquierda, 4:Frente, 5:Atrás
        public static readonly int[][] FaceTriangles =
        [
        [3, 7, 6, 2], // Cara 0 (+Y, Superior)  -> Sentido antihorario visto desde arriba
        [4, 0, 1, 5], // Cara 1 (-Y, Inferior)  -> Sentido antihorario visto desde abajo
        [6, 5, 1, 2], // Cara 2 (+X, Derecha)   -> Sentido antihorario visto desde la derecha
        [3, 0, 4, 7], // Cara 3 (-X, Izquierda) -> Sentido antihorario visto desde la izquierda
        [7, 4, 5, 6], // Cara 4 (+Z, Frontal)   -> Sentido antihorario visto desde el frente
        [2, 1, 0, 3]  // Cara 5 (-Z, Trasera)   -> Sentido antihorario visto desde atrás
        ];

        // Matriz auxiliar para revisar a los vecinos en el mismo orden que las caras
        public static readonly int[][] NeighborOffsets =
        [
        [0, 1, 0],  // 0: Vecino de Arriba
        [0, -1, 0], // 1: Vecino de Abajo
        [1, 0, 0],  // 2: Vecino de la Derecha
        [-1, 0, 0], // 3: Vecino de la Izquierda
        [0, 0, 1],  // 4: Vecino del Frente
        [0, 0, -1]  // 5: Vecino de Atrás
        ];
    }
}
