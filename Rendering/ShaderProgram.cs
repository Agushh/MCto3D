using Avalonia.OpenGL;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MCto3D.Rendering
{
    public class ShaderProgram : IDisposable
    {
        private readonly GlInterface _gl;
        public int ProgramId { get; private set; }

        public ShaderProgram(GlInterface gl, string vertexSrc, string fragmentSrc)
        {
            _gl = gl;
            int vertexShader = CompileShader(GlConsts.GL_VERTEX_SHADER, vertexSrc);
            int fragmentShader = CompileShader(GlConsts.GL_FRAGMENT_SHADER, fragmentSrc);

            ProgramId = _gl.CreateProgram();
            _gl.AttachShader(ProgramId, vertexShader);
            _gl.AttachShader(ProgramId, fragmentShader);
            _gl.LinkProgram(ProgramId);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        public void Use()
        {
            _gl.UseProgram(ProgramId);
        }

        public int GetUniformLocation(string name)
        {
            return _gl.GetUniformLocationString(ProgramId, name);
        }

        public unsafe void SetUniformMatrix4(int location, Matrix4x4 matrix)
        {
            if (location != -1)
                _gl.UniformMatrix4fv(location, 1, false, &matrix.M11);
        }

        public void SetUniform1i(int location, int value)
        {
            if (location != -1)
                _gl.Uniform1i(location, value);
        }

        public void SetUniform1f(int location, float value)
        {
            if (location != -1)
                _gl.Uniform1f(location, value);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void glUniform3f_t(int location, float v0, float v1, float v2);

        public void SetUniform3f(int location, float x, float y, float z)
        {
            if (location == -1) return;
            IntPtr funcPtr = _gl.GetProcAddress("glUniform3f");
            if (funcPtr != IntPtr.Zero)
            {
                Marshal.GetDelegateForFunctionPointer<glUniform3f_t>(funcPtr)(location, x, y, z);
            }
        }

        private unsafe int CompileShader(int type, string source)
        {
            int shader = _gl.CreateShader(type);
            _gl.ShaderSourceString(shader, source);
            _gl.CompileShader(shader);

            int status;
            _gl.GetShaderiv(shader, GlConsts.GL_COMPILE_STATUS, &status);
            if (status == 0)
            {
                byte[] infoLog = new byte[2048];
                int length;
                fixed (byte* pInfoLog = infoLog)
                {
                    _gl.GetShaderInfoLog(shader, 2048, out length, pInfoLog);
                }
                string error = System.Text.Encoding.UTF8.GetString(infoLog, 0, length);
                Debug.WriteLine($"ERROR COMPILANDO SHADER {(type == GlConsts.GL_VERTEX_SHADER ? "VERTEX" : "FRAGMENT")}: {error}");
            }
            return shader;
        }

        public void Dispose()
        {
            if (ProgramId != 0)
            {
                _gl.DeleteProgram(ProgramId);
                ProgramId = 0;
            }
        }
    }
}
