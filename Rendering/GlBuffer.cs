using Avalonia.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace MCto3D.Rendering
{
    public class GlBuffer : IDisposable
    {
        private readonly GlInterface _gl;
        public int VaoId { get; private set; }
        public int VboId { get; private set; }
        public int VertexCount { get; private set; }

        public unsafe GlBuffer(GlInterface gl)
        {
            _gl = gl;

            int[] vbos = new int[1];
            fixed (int* pVbos = vbos) gl.GenBuffers(1, pVbos);
            VboId = vbos[0];

            int[] vaos = new int[1];
            fixed (int* pVaos = vaos) gl.GenVertexArrays(1, pVaos);
            VaoId = vaos[0];
        }

        public void SetData(float[] vertexData, int vertexCount)
        {
            VertexCount = vertexCount;
            if (vertexData == null || vertexData.Length == 0) return;

            _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, VboId);
            GCHandle handle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                _gl.BufferData(GlConsts.GL_ARRAY_BUFFER, (IntPtr)(vertexData.Length * sizeof(float)), ptr, GlConsts.GL_STATIC_DRAW);
            }
            finally
            {
                handle.Free();
            }
            _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, 0);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void glEnableVertexAttribArray_t(int index);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void glVertexAttribPointer_t(int index, int size, int type, byte normalized, int stride, IntPtr pointer);

        public void BindAndDrawWithAttributes(int posSize = 3, int normalSize = 3, int colorSize = 3, int texSize = 0)
        {
            if (VertexCount == 0) return;

            _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, VboId);
            
            IntPtr enableAttribPtr = _gl.GetProcAddress("glEnableVertexAttribArray");
            IntPtr attribPointerPtr = _gl.GetProcAddress("glVertexAttribPointer");
            
            if (enableAttribPtr != IntPtr.Zero && attribPointerPtr != IntPtr.Zero)
            {
                var enableAttrib = Marshal.GetDelegateForFunctionPointer<glEnableVertexAttribArray_t>(enableAttribPtr);
                var attribPointer = Marshal.GetDelegateForFunctionPointer<glVertexAttribPointer_t>(attribPointerPtr);

                int stride = (posSize + normalSize + colorSize + texSize) * sizeof(float);
                int offset = 0;
                int index = 0;

                if (posSize > 0)
                {
                    enableAttrib(index);
                    attribPointer(index, posSize, GlConsts.GL_FLOAT, 0, stride, (IntPtr)offset);
                    offset += posSize * sizeof(float);
                    index++;
                }

                if (normalSize > 0)
                {
                    enableAttrib(index);
                    attribPointer(index, normalSize, GlConsts.GL_FLOAT, 0, stride, (IntPtr)offset);
                    offset += normalSize * sizeof(float);
                    index++;
                }

                if (colorSize > 0)
                {
                    enableAttrib(index);
                    attribPointer(index, colorSize, GlConsts.GL_FLOAT, 0, stride, (IntPtr)offset);
                    offset += colorSize * sizeof(float);
                    index++;
                }

                if (texSize > 0)
                {
                    enableAttrib(index);
                    attribPointer(index, texSize, GlConsts.GL_FLOAT, 0, stride, (IntPtr)offset);
                    offset += texSize * sizeof(float);
                    index++;
                }
            }

            _gl.DrawArrays(GlConsts.GL_TRIANGLES, 0, VertexCount);
            _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, 0);
        }

        public unsafe void Dispose()
        {
            if (VboId != 0)
            {
                int[] vbos = new[] { VboId };
                fixed (int* pVbos = vbos) _gl.DeleteBuffers(1, pVbos);
                VboId = 0;
            }
            if (VaoId != 0)
            {
                int[] vaos = new[] { VaoId };
                fixed (int* pVaos = vaos) _gl.DeleteVertexArrays(1, pVaos);
                VaoId = 0;
            }
        }
    }
}
