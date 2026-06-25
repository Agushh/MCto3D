using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MCto3D.Models
{
    public struct Triangle
    {
        public Vector3 V1, V2, V3, Normal;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Normal = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
        }
    }
        internal class Mesh
    {

    }
}
