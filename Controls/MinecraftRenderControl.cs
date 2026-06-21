using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MCto3D.Controls;

public class MinecraftRenderControl : OpenGlControlBase
{
    // === DEPENDENCY PROPERTIES ===
    public static readonly StyledProperty<List<Triangle>> TrianglesProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, List<Triangle>>(nameof(Triangles));

    public static readonly StyledProperty<bool> AutoRotateProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, bool>(nameof(AutoRotate));

    public static readonly StyledProperty<bool> ShowFloorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, bool>(nameof(ShowFloor));

    public static readonly StyledProperty<Avalonia.Media.Color> FloorColorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, Avalonia.Media.Color>(nameof(FloorColor), Avalonia.Media.Color.Parse("#1A1A1A"));

    public static readonly StyledProperty<float?> FixedCameraAngleProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, float?>(nameof(FixedCameraAngle));

    public static readonly StyledProperty<bool> OverrideModelColorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, bool>(nameof(OverrideModelColor));

    public static readonly StyledProperty<Avalonia.Media.Color> ModelColorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, Avalonia.Media.Color>(nameof(ModelColor), Avalonia.Media.Color.Parse("#FFFFFF"));

    public List<Triangle> Triangles
    {
        get => GetValue(TrianglesProperty);
        set => SetValue(TrianglesProperty, value);
    }

    public bool AutoRotate
    {
        get => GetValue(AutoRotateProperty);
        set => SetValue(AutoRotateProperty, value);
    }

    public bool ShowFloor
    {
        get => GetValue(ShowFloorProperty);
        set => SetValue(ShowFloorProperty, value);
    }

    public Avalonia.Media.Color FloorColor
    {
        get => GetValue(FloorColorProperty);
        set => SetValue(FloorColorProperty, value);
    }

    public float? FixedCameraAngle
    {
        get => GetValue(FixedCameraAngleProperty);
        set => SetValue(FixedCameraAngleProperty, value);
    }

    public bool OverrideModelColor
    {
        get => GetValue(OverrideModelColorProperty);
        set => SetValue(OverrideModelColorProperty, value);
    }

    public Avalonia.Media.Color ModelColor
    {
        get => GetValue(ModelColorProperty);
        set => SetValue(ModelColorProperty, value);
    }

    // === OPENGL RESOURCES ===
    private int _shaderProgram;
    private int _vbo;
    private int _vao;
    private int _floorVbo;
    private int _floorVao;
    private int _vertexCount;
    private bool _needsGeometryUpdate;
    private bool _needsFloorUpdate;

    // === CAMERA & INTERACTION ===
    private float _cameraDistance = 50.0f;
    private float _cameraPitch = (float)Math.PI / 8f; // Rotación X (Vertical)
    private float _cameraYaw = (float)Math.PI / 4f;   // Rotación Y (Horizontal)
    
    private Point _lastMousePosition;
    private bool _isDragging;
    private Avalonia.Controls.Control? _eventParent;

    // Matrix Locations
    private int _modelLoc;
    private int _viewLoc;
    private int _projLoc;
    private int _useModelColorLoc;
    private int _modelColorLoc;

    private Vector3 _meshCenter;
    private float _meshMinZ;
    private float _meshRadius = 1.0f;

    private GlInterface _gl;
    private TaskCompletionSource<List<WriteableBitmap>>? _captureTcs;
    private int _depthBuffer;
    private PixelSize _depthBufferSize;

    // Shader Source Code
    // VERTEX SHADER: Transforma los vértices 3D a la pantalla 2D y pasa el color al Fragment Shader.
    private const string VertexShaderSource = @"#version 300 es
precision mediump float;
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 FragColor;
out vec3 Normal;
out vec3 FragPos;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * aNormal;  
    FragColor = aColor; // Pasamos el color directamente
    gl_Position = projection * view * vec4(FragPos, 1.0);
}";

    // FRAGMENT SHADER: Decide el color final de cada pixel usando iluminación difusa básica.
    private const string FragmentShaderSource = @"#version 300 es
precision mediump float;
out vec4 FragColorOut;

in vec3 FragColor;
in vec3 Normal;
in vec3 FragPos;

uniform int useModelColor;
uniform vec3 modelColor;

void main()
{
    // Iluminación simple (Luz direccional desde arriba a la derecha)
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.5));
    float diff = max(dot(norm, lightDir), 0.2); // 0.2 es luz ambiental base

    vec3 baseColor = (useModelColor == 1) ? modelColor : FragColor;
    vec3 result = baseColor * diff;
    FragColorOut = vec4(result, 1.0);
}";

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TrianglesProperty)
        {
            _needsGeometryUpdate = true;
            RequestNextFrameRendering();
        }
        else if (change.Property == BoundsProperty)
        {
            RequestNextFrameRendering();
        }
        else if (change.Property == ShowFloorProperty || change.Property == FloorColorProperty || change.Property == OverrideModelColorProperty || change.Property == ModelColorProperty)
        {
            if (change.Property == FloorColorProperty) _needsFloorUpdate = true;
            RequestNextFrameRendering();
        }
        else if (change.Property == FixedCameraAngleProperty)
        {
            if (FixedCameraAngle.HasValue)
            {
                _cameraYaw = FixedCameraAngle.Value * (float)Math.PI / 180f;
                _cameraPitch = (float)Math.PI / 6f; // Un poco desde arriba
            }
            RequestNextFrameRendering();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RequestNextFrameRendering();

        if (Parent is Avalonia.Controls.Control parentControl)
        {
            _eventParent = parentControl;
            _eventParent.PointerPressed += Parent_PointerPressed;
            _eventParent.PointerReleased += Parent_PointerReleased;
            _eventParent.PointerMoved += Parent_PointerMoved;
            _eventParent.PointerWheelChanged += Parent_PointerWheelChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_eventParent != null)
        {
            _eventParent.PointerPressed -= Parent_PointerPressed;
            _eventParent.PointerReleased -= Parent_PointerReleased;
            _eventParent.PointerMoved -= Parent_PointerMoved;
            _eventParent.PointerWheelChanged -= Parent_PointerWheelChanged;
            _eventParent = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void Parent_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(this);
            e.Handled = true;
        }
    }

    private void Parent_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private void Parent_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var deltaX = currentPos.X - _lastMousePosition.X;
            var deltaY = currentPos.Y - _lastMousePosition.Y;

            _cameraYaw += (float)deltaX * 0.01f; // Invertido para rotación natural horizontal
            _cameraPitch += (float)deltaY * 0.01f; // Invertido para Z-up natural

            // Limitar pitch para evitar rotación de cabeza o traspasar suelo
            if (ShowFloor)
            {
                _cameraPitch = Math.Clamp(_cameraPitch, 0.05f, 1.5f);
            }
            else
            {
                _cameraPitch = Math.Clamp(_cameraPitch, -1.5f, 1.5f);
            }

            _lastMousePosition = currentPos;
            RequestNextFrameRendering();
        }
    }

    private void Parent_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        float maxZoom = _meshRadius * 10f; // Máximo alejamiento (10x el radio)
        float minZoom = _meshRadius * 0.5f; // Máximo acercamiento
        
        _cameraDistance -= (float)e.Delta.Y * (_meshRadius * 0.1f); // Velocidad de zoom proporcional
        _cameraDistance = Math.Clamp(_cameraDistance, minZoom, maxZoom); 
        
        e.Handled = true; // Prevenir el desplazamiento del ScrollViewer
        RequestNextFrameRendering();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        _gl = gl;
        Debug.WriteLine("OpenGL Init Started...");

        // 1. Compilar Shaders
        int vertexShader = CompileShader(gl, GlConsts.GL_VERTEX_SHADER, VertexShaderSource);
        int fragmentShader = CompileShader(gl, GlConsts.GL_FRAGMENT_SHADER, FragmentShaderSource);

        _shaderProgram = gl.CreateProgram();
        gl.AttachShader(_shaderProgram, vertexShader);
        gl.AttachShader(_shaderProgram, fragmentShader);
        gl.LinkProgram(_shaderProgram);

        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        // 2. Obtener locaciones de uniformes (matrices)
        _modelLoc = gl.GetUniformLocationString(_shaderProgram, "model");
        _viewLoc = gl.GetUniformLocationString(_shaderProgram, "view");
        _projLoc = gl.GetUniformLocationString(_shaderProgram, "projection");
        _useModelColorLoc = gl.GetUniformLocationString(_shaderProgram, "useModelColor");
        _modelColorLoc = gl.GetUniformLocationString(_shaderProgram, "modelColor");

        // 3. Crear Buffers (VBO y VAO)
        int[] vbos = new int[2];
        fixed (int* pVbos = vbos) gl.GenBuffers(2, pVbos);
        _vbo = vbos[0];
        _floorVbo = vbos[1];

        int[] vaos = new int[2];
        fixed (int* pVaos = vaos) gl.GenVertexArrays(2, pVaos);
        _vao = vaos[0];
        _floorVao = vaos[1];

        // 4. Configurar OpenGL global (el depth buffer se ata en el render loop)
        gl.Enable(GlConsts.GL_DEPTH_TEST);
        gl.Disable(GlConsts.GL_CULL_FACE); // Asegurar que no se descarten caras (winding order)

        // Subir geometría inicial si existe
        _needsGeometryUpdate = true;
        _needsFloorUpdate = true;
    }

    protected override unsafe void OnOpenGlDeinit(GlInterface gl)
    {
        gl.DeleteProgram(_shaderProgram);
        int[] vbos = new[] { _vbo, _floorVbo };
        fixed (int* pVbos = vbos) gl.DeleteBuffers(2, pVbos);

        int[] vaos = new[] { _vao, _floorVao };
        fixed (int* pVaos = vaos) gl.DeleteVertexArrays(2, pVaos);
        base.OnOpenGlDeinit(gl);
    }

    private void DoUpdateGeometry()
    {
        if (_gl == null)
        {
            Debug.WriteLine("DoUpdateGeometry called but _gl is null.");
            return; // OpenGL aún no inicializado
        }

        var triList = Triangles;
        if (triList == null || triList.Count == 0)
        {
            Debug.WriteLine("UpdateGeometry: triList is null or empty.");
            _vertexCount = 0;
            RequestNextFrameRendering();
            return;
        }

        Debug.WriteLine($"UpdateGeometry: Processing {triList.Count} triangles...");

        // Estructura de Vértice: Pos (3), Normal (3), Color (3) -> 9 floats per vertex
        // 3 vértices por triángulo
        float[] vertexData = new float[triList.Count * 3 * 9];
        int index = 0;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var tri in triList)
        {
            minX = Math.Min(minX, Math.Min(tri.V1.X, Math.Min(tri.V2.X, tri.V3.X)));
            minY = Math.Min(minY, Math.Min(tri.V1.Y, Math.Min(tri.V2.Y, tri.V3.Y)));
            minZ = Math.Min(minZ, Math.Min(tri.V1.Z, Math.Min(tri.V2.Z, tri.V3.Z)));
            
            maxX = Math.Max(maxX, Math.Max(tri.V1.X, Math.Max(tri.V2.X, tri.V3.X)));
            maxY = Math.Max(maxY, Math.Max(tri.V1.Y, Math.Max(tri.V2.Y, tri.V3.Y)));
            maxZ = Math.Max(maxZ, Math.Max(tri.V1.Z, Math.Max(tri.V2.Z, tri.V3.Z)));

            // Default color: Light purple/gray block
            float r = 0.8f, g = 0.8f, b = 0.9f;

            // Vertex 1
            vertexData[index++] = tri.V1.X; vertexData[index++] = tri.V1.Y; vertexData[index++] = tri.V1.Z;
            vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
            vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;

            // Vertex 2
            vertexData[index++] = tri.V2.X; vertexData[index++] = tri.V2.Y; vertexData[index++] = tri.V2.Z;
            vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
            vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;

            // Vertex 3
            vertexData[index++] = tri.V3.X; vertexData[index++] = tri.V3.Y; vertexData[index++] = tri.V3.Z;
            vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
            vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;
        }

        _meshCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
        _meshMinZ = minZ;
        
        // Calcular cámara dinámicamente según la Bounding Sphere
        float dx = maxX - minX;
        float dy = maxY - minY;
        float dz = maxZ - minZ;
        _meshRadius = (float)Math.Sqrt(dx*dx + dy*dy + dz*dz) / 2f;

        if (_meshRadius > 0)
        {
            // FOV es 45 grados (PI/4). Usamos trigonometría para asegurar que la esfera completa quepa en pantalla.
            // Distancia = Radio / Sin(FOV/2)
            _cameraDistance = _meshRadius / (float)Math.Sin(Math.PI / 8f) * 1.2f; // 20% de margen visual
        }

        // Preparar VBO Data
        _vertexCount = triList.Count * 3;

        // BIND VAO FIRST
        _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, _vbo);
        
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

        _needsFloorUpdate = true; // El piso debe ajustarse al nuevo minZ
        Debug.WriteLine("Geometry uploaded to GPU successfully.");
        RequestNextFrameRendering();
    }

    private unsafe void DoUpdateFloor()
    {
        if (_gl == null || !ShowFloor) return;

        float s = Math.Max(500f, _meshRadius * 10f); // Tamaño dinámico del suelo para que siempre tape el horizonte
        float cx = _meshCenter.X;
        float cy = _meshCenter.Y;
        float z = _meshMinZ - 0.5f; // Un poco debajo de la base

        float minX = cx - s;
        float maxX = cx + s;
        float minY = cy - s;
        float maxY = cy + s;

        float r = FloorColor.R / 255f;
        float g = FloorColor.G / 255f;
        float b = FloorColor.B / 255f;

        float[] floorData = new float[]
        {
            minX, minY, z,  0, 0, 1,  r, g, b,
            maxX, minY, z,  0, 0, 1,  r, g, b,
            maxX, maxY, z,  0, 0, 1,  r, g, b,

            minX, minY, z,  0, 0, 1,  r, g, b,
            maxX, maxY, z,  0, 0, 1,  r, g, b,
            minX, maxY, z,  0, 0, 1,  r, g, b,
        };

        _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, _floorVbo);
        
        GCHandle handle = GCHandle.Alloc(floorData, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            _gl.BufferData(GlConsts.GL_ARRAY_BUFFER, (IntPtr)(floorData.Length * sizeof(float)), ptr, GlConsts.GL_STATIC_DRAW);
        }
        finally
        {
            handle.Free();
        }

        _gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, 0);
    }

    public Task<List<WriteableBitmap>> GenerateThumbnails(int width, int height)
    {
        _captureTcs = new TaskCompletionSource<List<WriteableBitmap>>();
        RequestNextFrameRendering();
        return _captureTcs.Task;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void glReadPixels_t(int x, int y, int width, int height, int format, int type, IntPtr pixels);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void glDepthMask_t(byte mask);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void glDepthFunc_t(int func);

    private unsafe List<WriteableBitmap> DoGenerateThumbnails(GlInterface gl, int width, int height)
    {
        List<WriteableBitmap> result = new();
        if (_vertexCount == 0) return result;

        IntPtr readPixelsPtr = gl.GetProcAddress("glReadPixels");
        if (readPixelsPtr == IntPtr.Zero) return result;
        var readPixelsFunc = Marshal.GetDelegateForFunctionPointer<glReadPixels_t>(readPixelsPtr);

        int[] fbo = new int[1];
        int[] tex = new int[1];
        int[] rbo = new int[1];

        fixed (int* pfbo = fbo) gl.GenFramebuffers(1, pfbo);
        fixed (int* ptex = tex) gl.GenTextures(1, ptex);
        fixed (int* prbo = rbo) gl.GenRenderbuffers(1, prbo);

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fbo[0]);

        gl.BindTexture(GlConsts.GL_TEXTURE_2D, tex[0]);
        gl.TexImage2D(GlConsts.GL_TEXTURE_2D, 0, GlConsts.GL_RGBA, width, height, 0, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, IntPtr.Zero);
        gl.FramebufferTexture2D(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0, GlConsts.GL_TEXTURE_2D, tex[0], 0);

        gl.BindRenderbuffer(GlConsts.GL_RENDERBUFFER, rbo[0]);
        gl.RenderbufferStorage(GlConsts.GL_RENDERBUFFER, GlConsts.GL_DEPTH_COMPONENT16, width, height);
        gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, rbo[0]);

        gl.Viewport(0, 0, width, height);
        gl.Enable(GlConsts.GL_DEPTH_TEST);
        
        IntPtr depthMaskPtr = gl.GetProcAddress("glDepthMask");
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(1);

        IntPtr depthFuncPtr = gl.GetProcAddress("glDepthFunc");
        if (depthFuncPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthFunc_t>(depthFuncPtr)(0x0203);

        float[] angles = new[] { 45f, 135f, 225f, 315f };
        
        float aspect = (float)width / height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, aspect, 0.1f, 10000f);
        Matrix4x4 model = Matrix4x4.CreateTranslation(-_meshCenter);

        gl.UseProgram(_shaderProgram);
        SetUniformMatrix(gl, _projLoc, projection);
        SetUniformMatrix(gl, _modelLoc, model);

        byte[] pixels = new byte[width * height * 4];
        byte[] bgraPixels = new byte[width * height * 4];

        foreach (float angle in angles)
        {
            gl.ClearColor(19f/255f, 16f/255f, 30f/255f, 1.0f); // Fondo para miniaturas
            gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

            float yaw = angle * (float)Math.PI / 180f;
            float pitch = (float)Math.PI / 6f; // 30 grados
            Vector3 cameraPos = new Vector3(
                _cameraDistance * (float)Math.Sin(yaw) * (float)Math.Cos(pitch),
                _cameraDistance * (float)Math.Cos(yaw) * (float)Math.Cos(pitch),
                _cameraDistance * (float)Math.Sin(pitch)
            );
            Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);
            SetUniformMatrix(gl, _viewLoc, view);

            if (ShowFloor)
            {
                gl.Uniform1i(_useModelColorLoc, 0);
                BindVboAndDraw(gl, _floorVbo, 6);
            }

            if (OverrideModelColor)
            {
                gl.Uniform1i(_useModelColorLoc, 1);
                SetUniformVec3(gl, _modelColorLoc, ModelColor.R / 255f, ModelColor.G / 255f, ModelColor.B / 255f);
            }
            else
            {
                gl.Uniform1i(_useModelColorLoc, 0);
            }

            BindVboAndDraw(gl, _vbo, _vertexCount);

            fixed (byte* p = pixels)
            {
                readPixelsFunc(0, 0, width, height, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, (IntPtr)p);
            }

            // Flip Y y RGB a BGR
            for (int y = 0; y < height; y++)
            {
                int srcY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    int srcIdx = (srcY * width + x) * 4;
                    int dstIdx = (y * width + x) * 4;
                    bgraPixels[dstIdx + 0] = pixels[srcIdx + 2]; // B
                    bgraPixels[dstIdx + 1] = pixels[srcIdx + 1]; // G
                    bgraPixels[dstIdx + 2] = pixels[srcIdx + 0]; // R
                    bgraPixels[dstIdx + 3] = pixels[srcIdx + 3]; // A
                }
            }

            var bmp = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Opaque);
            using (var buf = bmp.Lock())
            {
                Marshal.Copy(bgraPixels, 0, buf.Address, bgraPixels.Length);
            }
            result.Add(bmp);
        }

        fixed (int* pfbo = fbo) gl.DeleteFramebuffers(1, pfbo);
        fixed (int* ptex = tex) gl.DeleteTextures(1, ptex);
        fixed (int* prbo = rbo) gl.DeleteRenderbuffers(1, prbo);

        return result;
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_captureTcs != null && !_captureTcs.Task.IsCompleted)
        {
            var imgs = DoGenerateThumbnails(gl, 512, 512);
            _captureTcs.SetResult(imgs);
            _captureTcs = null;
        }

        if (_needsGeometryUpdate)
        {
            DoUpdateGeometry();
            _needsGeometryUpdate = false;
        }
        if (_needsFloorUpdate)
        {
            DoUpdateFloor();
            _needsFloorUpdate = false;
        }

        float width = (float)Bounds.Width;
        float height = (float)Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            RequestNextFrameRendering();
            return;
        }

        var size = new PixelSize((int)width, (int)height);
        if (size != _depthBufferSize)
        {
            if (_depthBuffer != 0) 
            {
                int[] oldDb = new[] { _depthBuffer };
                fixed(int* pDb = oldDb) gl.DeleteRenderbuffers(1, pDb);
            }
            
            int[] db = new int[1];
            fixed(int* pDb = db) gl.GenRenderbuffers(1, pDb);
            _depthBuffer = db[0];
            
            gl.BindRenderbuffer(GlConsts.GL_RENDERBUFFER, _depthBuffer);
            gl.RenderbufferStorage(GlConsts.GL_RENDERBUFFER, GlConsts.GL_DEPTH_COMPONENT16, size.Width, size.Height);
            _depthBufferSize = size;
        }

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
        gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, _depthBuffer);

        gl.Viewport(0, 0, size.Width, size.Height);

        gl.Viewport(0, 0, size.Width, size.Height);

        gl.Enable(GlConsts.GL_DEPTH_TEST);

        IntPtr depthMaskPtr = gl.GetProcAddress("glDepthMask");
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(1);

        IntPtr depthFuncPtr = gl.GetProcAddress("glDepthFunc");
        if (depthFuncPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthFunc_t>(depthFuncPtr)(0x0203);

        // Limpiar el fondo. Color oscuro de nuestra paleta (#0B0B13 => rgb 11, 11, 19)
        gl.ClearColor(11f/255f, 11f/255f, 19f/255f, 1.0f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

        if (_vertexCount == 0)
        {
            if (AutoRotate) RequestNextFrameRendering();
            return;
        }

        if (AutoRotate)
        {
            _cameraYaw -= 0.005f; // Rotación automática suave sobre el eje horizontal
            RequestNextFrameRendering(); // Bucle infinito
        }

        gl.UseProgram(_shaderProgram);

        // 1. Proyección: Field of View, Aspect Ratio, Near, Far
        float aspect = width / height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, aspect, 0.1f, 10000f);

        // 2. Vista (Cámara Arcball adaptada a que Z es Arriba)
        Vector3 cameraPos = new Vector3(
            _cameraDistance * (float)Math.Sin(_cameraYaw) * (float)Math.Cos(_cameraPitch),
            _cameraDistance * (float)Math.Cos(_cameraYaw) * (float)Math.Cos(_cameraPitch),
            _cameraDistance * (float)Math.Sin(_cameraPitch)
        );
        Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);

        // 3. Modelo (Posición del objeto en el mundo). Lo trasladamos para que el centro de la malla quede en (0,0,0)
        Matrix4x4 model = Matrix4x4.CreateTranslation(-_meshCenter);

        // Subir matrices al Shader
        SetUniformMatrix(gl, _projLoc, projection);
        SetUniformMatrix(gl, _viewLoc, view);
        SetUniformMatrix(gl, _modelLoc, model);

        // Dibujar
        if (ShowFloor)
        {
            gl.Uniform1i(_useModelColorLoc, 0);
            BindVboAndDraw(gl, _floorVbo, 6);
        }

        if (OverrideModelColor)
        {
            gl.Uniform1i(_useModelColorLoc, 1);
            SetUniformVec3(gl, _modelColorLoc, ModelColor.R / 255f, ModelColor.G / 255f, ModelColor.B / 255f);
        }
        else
        {
            gl.Uniform1i(_useModelColorLoc, 0);
        }

        BindVboAndDraw(gl, _vbo, _vertexCount);

        // Forzar redibujado continuo para evitar que el motor de UI descarte el único frame
        RequestNextFrameRendering();
    }

    private unsafe void SetUniformMatrix(GlInterface gl, int location, Matrix4x4 matrix)
    {
        gl.UniformMatrix4fv(location, 1, false, &matrix.M11);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void glUniform3f_t(int location, float v0, float v1, float v2);

    private void SetUniformVec3(GlInterface gl, int location, float x, float y, float z)
    {
        if (location == -1) return;
        IntPtr funcPtr = gl.GetProcAddress("glUniform3f");
        if (funcPtr != IntPtr.Zero)
        {
            Marshal.GetDelegateForFunctionPointer<glUniform3f_t>(funcPtr)(location, x, y, z);
        }
    }

    private unsafe int CompileShader(GlInterface gl, int type, string source)
    {
        int shader = gl.CreateShader(type);
        gl.ShaderSourceString(shader, source);
        gl.CompileShader(shader);

        int status;
        gl.GetShaderiv(shader, GlConsts.GL_COMPILE_STATUS, &status);
        if (status == 0)
        {
            byte[] infoLog = new byte[2048];
            int length;
            fixed (byte* pInfoLog = infoLog)
            {
                gl.GetShaderInfoLog(shader, 2048, out length, pInfoLog);
            }
            string error = System.Text.Encoding.UTF8.GetString(infoLog, 0, length);
            Debug.WriteLine($"ERROR COMPILANDO SHADER {(type == GlConsts.GL_VERTEX_SHADER ? "VERTEX" : "FRAGMENT")}: {error}");
        }
        else
        {
            Debug.WriteLine($"Shader {(type == GlConsts.GL_VERTEX_SHADER ? "VERTEX" : "FRAGMENT")} compiled successfully.");
        }
        return shader;
    }

    private unsafe void BindVboAndDraw(GlInterface gl, int vbo, int count)
    {
        gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, vbo);
        int stride = 9 * sizeof(float);
        
        gl.VertexAttribPointer(0, 3, GlConsts.GL_FLOAT, 0, stride, IntPtr.Zero);
        gl.EnableVertexAttribArray(0);
        
        gl.VertexAttribPointer(1, 3, GlConsts.GL_FLOAT, 0, stride, (IntPtr)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 3, GlConsts.GL_FLOAT, 0, stride, (IntPtr)(6 * sizeof(float)));
        gl.EnableVertexAttribArray(2);

        gl.DrawArrays(GlConsts.GL_TRIANGLES, 0, count);
    }
}
