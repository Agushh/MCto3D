using System.Collections.Generic;

namespace MCto3D.Services
{
    public class TopologyService : ITopologyService
    {
        public struct Vector3Int
        {
            public int X; public int Y; public int Z;
            public Vector3Int(int x, int y, int z) { X = x; Y = y; Z = z; }
        }

        public void ProcessEnclosedSpaces(int[,,] grid)
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
                        if (grid[x, y, z] == -1 && !visited[x, y, z])
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
                                        if (grid[nx, ny, nz] == -1 && !visited[nx, ny, nz])
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
                            if (grid[nx, ny, nz] == -1 && !isOutside[nx, ny, nz])
                            {
                                isOutside[nx, ny, nz] = true;
                                q.Enqueue(new Vector3Int(nx, ny, nz));
                            }
                        }
                    }
                }
            }
            
            // 3. Reemplazar los vacíos que NO son exteriores (quedaron atrapados o son cortes menores) por -2 (Relleno)
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (grid[x, y, z] == -1 && !isOutside[x, y, z])
                        {
                            grid[x, y, z] = -2;
                        }
                    }
                }
            }
        }
    }
}

