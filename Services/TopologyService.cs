using MCto3D.Models;
using System.Collections.Generic;

namespace MCto3D.Services
{
    public class TopologyService
    {
        public void ProcessEnclosedSpaces(int[,,] grid, int airId)
        {
            int sizeX = grid.GetLength(0);
            int sizeY = grid.GetLength(1);
            int sizeZ = grid.GetLength(2);
            
            bool[,,] visited = new bool[sizeX, sizeY, sizeZ];
            int maxBoundaryCount = 0;
            Vector3Int bestSeed = new Vector3Int(-1, -1, -1);
            
            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };
            
            // 1. Encontrar el componente de aire (vacío) que más toca los bordes del Bounding Box.
            // Asumimos que el componente con mayor área de contacto con los bordes es el "Exterior real".
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (grid[x, y, z] == airId && !visited[x, y, z])
                        {
                            int boundaryCount = 0;
                            Queue<Vector3Int> q = new Queue<Vector3Int>();
                            q.Enqueue(new Vector3Int(x, y, z));
                            visited[x, y, z] = true;
                            
                            while (q.Count > 0)
                            {
                                var curr = q.Dequeue();
                                
                                // Chequear si este voxel está en algún borde
                                if (curr.X == 0 || curr.X == sizeX - 1 || 
                                    curr.Y == 0 || curr.Y == sizeY - 1 || 
                                    curr.Z == 0 || curr.Z == sizeZ - 1)
                                {
                                    boundaryCount++;
                                }
                                
                                for (int i = 0; i < 6; i++)
                                {
                                    int nx = curr.X + dx[i];
                                    int ny = curr.Y + dy[i];
                                    int nz = curr.Z + dz[i];
                                    
                                    if (nx >= 0 && nx < sizeX && ny >= 0 && ny < sizeY && nz >= 0 && nz < sizeZ)
                                    {
                                        if (grid[nx, ny, nz] == airId && !visited[nx, ny, nz])
                                        {
                                            visited[nx, ny, nz] = true;
                                            q.Enqueue(new Vector3Int(nx, ny, nz));
                                        }
                                    }
                                }
                            }
                            
                            // Si este componente toca más bordes que el anterior máximo, lo consideramos el nuevo "Exterior"
                            if (boundaryCount > maxBoundaryCount)
                            {
                                maxBoundaryCount = boundaryCount;
                                bestSeed = new Vector3Int(x, y, z);
                            }
                        }
                    }
                }
            }
            
            // 2. Marcar definitivamente el "Exterior"
            bool[,,] isOutside = new bool[sizeX, sizeY, sizeZ];
            if (bestSeed.X != -1)
            {
                Queue<Vector3Int> q = new Queue<Vector3Int>();
                q.Enqueue(bestSeed);
                isOutside[bestSeed.X, bestSeed.Y, bestSeed.Z] = true;
                
                while (q.Count > 0)
                {
                    var curr = q.Dequeue();
                    for (int i = 0; i < 6; i++)
                    {
                        int nx = curr.X + dx[i];
                        int ny = curr.Y + dy[i];
                        int nz = curr.Z + dz[i];
                        
                        if (nx >= 0 && nx < sizeX && ny >= 0 && ny < sizeY && nz >= 0 && nz < sizeZ)
                        {
                            if (grid[nx, ny, nz] == airId && !isOutside[nx, ny, nz])
                            {
                                isOutside[nx, ny, nz] = true;
                                q.Enqueue(new Vector3Int(nx, ny, nz));
                            }
                        }
                    }
                }
            }

            // 3. Reemplazar TODO lo interior (aire atrapado o bloques sólidos ocultos) por -2
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        // Si este vóxel es el "Aire Exterior", lo dejamos en paz y pasamos al siguiente
                        if (isOutside[x, y, z])
                            continue; 

                        // Si es un bloque (cualquier valor distinto de -1)
                        if (grid[x, y, z] != airId)
                        {
                            bool isVisible = false;

                            // Revisamos los 6 bloques vecinos que lo rodean
                            for (int i = 0; i < 6; i++)
                            {
                                int nx = x + dx[i];
                                int ny = y + dy[i];
                                int nz = z + dz[i];

                                // Condición A: Está en el límite absoluto del grid (borde del mapa)
                                if (nx < 0 || nx >= sizeX || ny < 0 || ny >= sizeY || nz < 0 || nz >= sizeZ)
                                {
                                    isVisible = true;
                                    break; // Ya sabemos que es visible, dejamos de chequear vecinos
                                }
                                // Condición B: Toca un bloque de "Aire Exterior"
                                else if (isOutside[nx, ny, nz])
                                {
                                    isVisible = true;
                                    break;
                                }
                            }

                            // Si no cumple ninguna condición, está atrapado adentro de la carcasa
                            if (!isVisible)
                            {
                                grid[x, y, z] = -2;
                            }
                        }
                        else
                        {
                            // Si llegó acá, es aire (-1), pero como sabemos que no es isOutside,
                            // significa que es un hueco de aire atrapado adentro.
                            grid[x, y, z] = -2;
                        }
                    }
                }
            }
        }
    }
}

