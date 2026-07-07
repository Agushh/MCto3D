using System;
using System.Numerics;

namespace MCto3D.Rendering
{
    public class Camera
    {
        public float Distance { get; set; } = 50.0f;
        public float Pitch { get; set; } = (float)Math.PI / 8f;
        public float Yaw { get; set; } = (float)Math.PI / 4f;

        public Matrix4x4 GetViewMatrix()
        {
            Vector3 cameraPos = new Vector3(
                Distance * (float)Math.Sin(Yaw) * (float)Math.Cos(Pitch),
                Distance * (float)Math.Cos(Yaw) * (float)Math.Cos(Pitch),
                Distance * (float)Math.Sin(Pitch)
            );
            return Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);
        }

        public Matrix4x4 GetProjectionMatrix(float width, float height)
        {
            float aspect = width / height;
            return Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, aspect, 1.0f, 4000f);
        }

        public void HandleZoom(float delta, float baseRadius)
        {
            float maxZoom = baseRadius * 10f;
            float minZoom = baseRadius * 0.5f;
            Distance -= delta * (baseRadius * 0.1f);
            Distance = Math.Clamp(Distance, minZoom, maxZoom);
        }

        public void HandlePan(float deltaX, float deltaY, bool limitPitch)
        {
            Yaw += deltaX * 0.01f;
            Pitch += deltaY * 0.01f;
            if (limitPitch)
            {
                Pitch = Math.Clamp(Pitch, 0.05f, 1.5f);
            }
            else
            {
                Pitch = Math.Clamp(Pitch, -1.5f, 1.5f);
            }
        }
    }
}
