using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using MCto3D.Models;
using MCto3D.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace MCto3D.Controls;

public class MinecraftRenderControl : OpenGlControlBase
{
    // === DEPENDENCY PROPERTIES ===
    public static readonly StyledProperty<List<Triangle>> TrianglesProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, List<Triangle>>(nameof(Triangles));

    public static readonly StyledProperty<Dictionary<System.Drawing.Color, List<Triangle>>> ColoredMeshesProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, Dictionary<System.Drawing.Color, List<Triangle>>>(nameof(ColoredMeshes));

    public static readonly StyledProperty<bool> AutoRotateProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, bool>(nameof(AutoRotate));

    public static readonly StyledProperty<bool> ShowFloorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, bool>(nameof(ShowFloor));

    public static readonly StyledProperty<Avalonia.Media.Color> FloorColorProperty =
        AvaloniaProperty.Register<MinecraftRenderControl, Avalonia.Media.Color>(nameof(FloorColor), Avalonia.Media.Color.Parse("#71B24B"));

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

    public Dictionary<System.Drawing.Color, List<Triangle>> ColoredMeshes
    {
        get => GetValue(ColoredMeshesProperty);
        set => SetValue(ColoredMeshesProperty, value);
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

    // === RENDERING RESOURCES ===
    private GlInterface _gl;
    private ShaderProgram _modelShader;
    private ShaderProgram _floorShader;
    private ShaderProgram _skyShader;

    private GlBuffer _modelBuffer;
    private GlBuffer _floorBuffer;
    private GlBuffer _skyBuffer;

    private Camera _camera;
    private Stopwatch _stopwatch;

    private bool _needsGeometryUpdate;
    private bool _needsFloorUpdate;

    private Point _lastMousePosition;
    private bool _isDragging;
    private Avalonia.Controls.Control? _eventParent;

    private Vector3 _meshCenter;
    private float _meshMinZ;
    private float _meshHeight;
    private float _meshRadius = 1.0f;

    private TaskCompletionSource<List<WriteableBitmap>>? _captureTcs;
    private int _depthBuffer;
    private PixelSize _depthBufferSize;

    public MinecraftRenderControl()
    {
        _camera = new Camera();
        _stopwatch = Stopwatch.StartNew();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TrianglesProperty || change.Property == ColoredMeshesProperty)
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
                _camera.Yaw = FixedCameraAngle.Value * (float)Math.PI / 180f;
                _camera.Pitch = (float)Math.PI / 6f;
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
            var deltaX = (float)(currentPos.X - _lastMousePosition.X);
            var deltaY = (float)(currentPos.Y - _lastMousePosition.Y);

            _camera.HandlePan(deltaX, deltaY, ShowFloor);
            
            _lastMousePosition = currentPos;
            RequestNextFrameRendering();
        }
    }

    private void Parent_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _camera.HandleZoom((float)e.Delta.Y, _meshRadius);
        e.Handled = true; 
        RequestNextFrameRendering();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        _gl = gl;
        Debug.WriteLine("OpenGL Init Started...");

        _modelShader = new ShaderProgram(_gl, Shaders.ModelVertexShader, Shaders.ModelFragmentShader);
        _floorShader = new ShaderProgram(_gl, Shaders.FloorVertexShader, Shaders.FloorFragmentShader);
        _skyShader = new ShaderProgram(_gl, Shaders.SkyVertexShader, Shaders.SkyFragmentShader);

        _modelBuffer = new GlBuffer(_gl);
        _floorBuffer = new GlBuffer(_gl);
        _skyBuffer = new GlBuffer(_gl);

        float[] skyQuad = new float[]
        {
            -1f, -1f,
             1f, -1f,
             1f,  1f,
            -1f, -1f,
             1f,  1f,
            -1f,  1f
        };
        _skyBuffer.SetData(skyQuad, 6);

        _gl.Enable(GlConsts.GL_DEPTH_TEST);
        _gl.Enable(GlConsts.GL_CULL_FACE);

        _needsGeometryUpdate = true;
        _needsFloorUpdate = true;
    }

    protected override unsafe void OnOpenGlDeinit(GlInterface gl)
    {
        _modelShader?.Dispose();
        _floorShader?.Dispose();
        _skyShader?.Dispose();

        _modelBuffer?.Dispose();
        _floorBuffer?.Dispose();
        _skyBuffer?.Dispose();
        
        base.OnOpenGlDeinit(gl);
    }

    private void DoUpdateGeometry()
    {
        if (_gl == null) return;

        var singleMesh = Triangles;
        var multiMesh = ColoredMeshes;
        
        int totalTriangles = 0;
        if (multiMesh != null && multiMesh.Count > 0)
        {
            foreach (var list in multiMesh.Values) totalTriangles += list.Count;
        }
        else if (singleMesh != null)
        {
            totalTriangles = singleMesh.Count;
        }

        if (totalTriangles == 0)
        {
            _modelBuffer.SetData(Array.Empty<float>(), 0);
            RequestNextFrameRendering();
            return;
        }

        float[] vertexData = new float[totalTriangles * 3 * 9];
        int index = 0;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        Action<List<Triangle>, System.Drawing.Color> processList = (list, color) => 
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            
            foreach (var tri in list)
            {
                minX = Math.Min(minX, Math.Min(tri.V1.X, Math.Min(tri.V2.X, tri.V3.X)));
                minY = Math.Min(minY, Math.Min(tri.V1.Y, Math.Min(tri.V2.Y, tri.V3.Y)));
                minZ = Math.Min(minZ, Math.Min(tri.V1.Z, Math.Min(tri.V2.Z, tri.V3.Z)));
                
                maxX = Math.Max(maxX, Math.Max(tri.V1.X, Math.Max(tri.V2.X, tri.V3.X)));
                maxY = Math.Max(maxY, Math.Max(tri.V1.Y, Math.Max(tri.V2.Y, tri.V3.Y)));
                maxZ = Math.Max(maxZ, Math.Max(tri.V1.Z, Math.Max(tri.V2.Z, tri.V3.Z)));

                vertexData[index++] = tri.V1.X; vertexData[index++] = tri.V1.Y; vertexData[index++] = tri.V1.Z;
                vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
                vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;

                vertexData[index++] = tri.V2.X; vertexData[index++] = tri.V2.Y; vertexData[index++] = tri.V2.Z;
                vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
                vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;

                vertexData[index++] = tri.V3.X; vertexData[index++] = tri.V3.Y; vertexData[index++] = tri.V3.Z;
                vertexData[index++] = tri.Normal.X; vertexData[index++] = tri.Normal.Y; vertexData[index++] = tri.Normal.Z;
                vertexData[index++] = r; vertexData[index++] = g; vertexData[index++] = b;
            }
        };

        if (multiMesh != null && multiMesh.Count > 0)
        {
            foreach (var kvp in multiMesh) processList(kvp.Value, kvp.Key);
        }
        else
        {
            processList(singleMesh, System.Drawing.Color.FromArgb(255, 204, 204, 230)); 
        }

        _meshCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
        _meshMinZ = minZ;
        _meshHeight = maxZ - minZ;
        if (_meshHeight < 1.0f) _meshHeight = 1.0f;
        
        float dx = maxX - minX;
        float dy = maxY - minY;
        float dz = maxZ - minZ;
        _meshRadius = (float)Math.Sqrt(dx*dx + dy*dy + dz*dz) / 2f;

        if (_meshRadius > 0)
        {
            _camera.Distance = _meshRadius / (float)Math.Sin(Math.PI / 8f) * 1.2f; 
        }

        _modelBuffer.SetData(vertexData, totalTriangles * 3);
        _needsFloorUpdate = true; 
        RequestNextFrameRendering();
    }

    private void DoUpdateFloor()
    {
        if (_gl == null || !ShowFloor) return;

        float s = Math.Max(500f, _meshRadius * 10f); 
        float cx = _meshCenter.X;
        float cy = _meshCenter.Y;
        float z = (_meshMinZ - _meshCenter.Z); 

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

        _floorBuffer.SetData(floorData, 6);
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

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void glBlendFunc_t(int sfactor, int dfactor);

    private unsafe List<WriteableBitmap> DoGenerateThumbnails(GlInterface gl, int width, int height)
    {
        List<WriteableBitmap> result = new();
        if (_modelBuffer == null || _modelBuffer.VertexCount == 0) return result;

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
        gl.RenderbufferStorage(GlConsts.GL_RENDERBUFFER, 0x81A6, width, height);
        gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, rbo[0]);

        gl.Viewport(0, 0, width, height);
        gl.Enable(GlConsts.GL_DEPTH_TEST);
        
        IntPtr depthMaskPtr = gl.GetProcAddress("glDepthMask");
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(1);

        IntPtr depthFuncPtr = gl.GetProcAddress("glDepthFunc");
        if (depthFuncPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthFunc_t>(depthFuncPtr)(0x0203);

        float[] angles = new[] { 45f, 135f, 225f, 315f };
        float aspect = (float)width / height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, aspect, 1.0f, 4000f);
        Matrix4x4 model = Matrix4x4.CreateTranslation(-_meshCenter);

        byte[] pixels = new byte[width * height * 4];
        byte[] bgraPixels = new byte[width * height * 4];

        foreach (float angle in angles)
        {
            gl.ClearColor(19f/255f, 16f/255f, 30f/255f, 1.0f); 
            gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

            float yaw = angle * (float)Math.PI / 180f;
            float pitch = (float)Math.PI / 6f; 
            Vector3 cameraPos = new Vector3(
                _camera.Distance * (float)Math.Sin(yaw) * (float)Math.Cos(pitch),
                _camera.Distance * (float)Math.Cos(yaw) * (float)Math.Cos(pitch),
                _camera.Distance * (float)Math.Sin(pitch)
            );
            Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitZ);

            if (ShowFloor)
            {
                _floorShader.Use();
                _floorShader.SetUniformMatrix4(_floorShader.GetUniformLocation("projection"), projection);
                _floorShader.SetUniformMatrix4(_floorShader.GetUniformLocation("view"), view);
                _floorShader.SetUniform1f(_floorShader.GetUniformLocation("u_time"), 0f);
                _floorShader.SetUniform1f(_floorShader.GetUniformLocation("fogDistance"), _meshRadius * 10f);
                
                gl.Enable(0x0BE2); // GL_BLEND
                IntPtr blendFuncPtr = gl.GetProcAddress("glBlendFunc");
                if (blendFuncPtr != IntPtr.Zero)
                {
                    Marshal.GetDelegateForFunctionPointer<glBlendFunc_t>(blendFuncPtr)(0x0302, 0x0303); // GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA
                }

                _floorBuffer.BindAndDrawWithAttributes(3, 3, 3, 0);
                gl.Disable(0x0BE2); // GL_BLEND
            }

            _modelShader.Use();
            Matrix4x4 mvp = model * view * projection;
            _modelShader.SetUniformMatrix4(_modelShader.GetUniformLocation("mvp"), mvp);
            _modelShader.SetUniformMatrix4(_modelShader.GetUniformLocation("model"), model);
            _modelShader.SetUniform1f(_modelShader.GetUniformLocation("minZ"), _meshMinZ - _meshCenter.Z);
            _modelShader.SetUniform1f(_modelShader.GetUniformLocation("meshHeight"), _meshHeight);
            _modelShader.SetUniform3f(_modelShader.GetUniformLocation("cameraPos"), cameraPos.X, cameraPos.Y, cameraPos.Z);

            if (OverrideModelColor)
            {
                _modelShader.SetUniform1i(_modelShader.GetUniformLocation("useModelColor"), 1);
                _modelShader.SetUniform3f(_modelShader.GetUniformLocation("modelColor"), ModelColor.R / 255f, ModelColor.G / 255f, ModelColor.B / 255f);
            }
            else
            {
                _modelShader.SetUniform1i(_modelShader.GetUniformLocation("useModelColor"), 0);
            }

            _modelBuffer.BindAndDrawWithAttributes(3, 3, 3, 0);

            fixed (byte* p = pixels)
            {
                readPixelsFunc(0, 0, width, height, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, (IntPtr)p);
            }

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
            gl.RenderbufferStorage(GlConsts.GL_RENDERBUFFER, 0x81A6, size.Width, size.Height);
            _depthBufferSize = size;
        }

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
        gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, _depthBuffer);
        gl.Viewport(0, 0, size.Width, size.Height);
        gl.Enable(GlConsts.GL_DEPTH_TEST);

        IntPtr depthMaskPtr = gl.GetProcAddress("glDepthMask");
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(1);

        IntPtr depthFuncPtr = gl.GetProcAddress("glDepthFunc");
        if (depthFuncPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthFunc_t>(depthFuncPtr)(0x0203);

        gl.ClearColor(11f/255f, 11f/255f, 19f/255f, 1.0f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

        if (_modelBuffer == null || _modelBuffer.VertexCount == 0)
        {
            if (AutoRotate) RequestNextFrameRendering();
            return;
        }

        if (AutoRotate)
        {
            _camera.Yaw -= 0.005f; 
        }

        float time = (float)_stopwatch.Elapsed.TotalSeconds;

        Matrix4x4 projection = _camera.GetProjectionMatrix(width, height);
        Matrix4x4 view = _camera.GetViewMatrix();
        Matrix4x4 model = Matrix4x4.CreateTranslation(-_meshCenter);
        Matrix4x4 invViewProj;
        Matrix4x4.Invert(view * projection, out invViewProj);

        // 1. Draw Sky (Depth writing off)
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(0);
        
        _skyShader.Use();
        _skyShader.SetUniform1f(_skyShader.GetUniformLocation("u_time"), time);
        _skyShader.SetUniformMatrix4(_skyShader.GetUniformLocation("invViewProj"), invViewProj);
        _skyBuffer.BindAndDrawWithAttributes(2, 0, 0, 0); 
        
        if (depthMaskPtr != IntPtr.Zero) Marshal.GetDelegateForFunctionPointer<glDepthMask_t>(depthMaskPtr)(1);

        // 2. Draw Floor
        if (ShowFloor)
        {
            gl.Enable(0x0BE2); // GL_BLEND
            IntPtr blendFuncPtr = gl.GetProcAddress("glBlendFunc");
            if (blendFuncPtr != IntPtr.Zero)
            {
                Marshal.GetDelegateForFunctionPointer<glBlendFunc_t>(blendFuncPtr)(0x0302, 0x0303); // GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA
            }
            
            _floorShader.Use();
            _floorShader.SetUniformMatrix4(_floorShader.GetUniformLocation("projection"), projection);
            _floorShader.SetUniformMatrix4(_floorShader.GetUniformLocation("view"), view);
            _floorShader.SetUniform1f(_floorShader.GetUniformLocation("u_time"), time);
            
            _floorShader.SetUniform1f(_floorShader.GetUniformLocation("fogDistance"), _meshRadius * 10f);

            _floorBuffer.BindAndDrawWithAttributes(3, 3, 3, 0);
            
            gl.Disable(0x0BE2); // GL_BLEND
        }

        // 3. Draw Model
        _modelShader.Use();
        Matrix4x4 mvpModel = model * view * projection;
        _modelShader.SetUniformMatrix4(_modelShader.GetUniformLocation("mvp"), mvpModel);
        _modelShader.SetUniformMatrix4(_modelShader.GetUniformLocation("model"), model);
        _modelShader.SetUniform1f(_modelShader.GetUniformLocation("minZ"), _meshMinZ - _meshCenter.Z);
        _modelShader.SetUniform1f(_modelShader.GetUniformLocation("meshHeight"), _meshHeight);
        
        Vector3 camPos = new Vector3(
            _camera.Distance * (float)Math.Sin(_camera.Yaw) * (float)Math.Cos(_camera.Pitch),
            _camera.Distance * (float)Math.Cos(_camera.Yaw) * (float)Math.Cos(_camera.Pitch),
            _camera.Distance * (float)Math.Sin(_camera.Pitch)
        );
        _modelShader.SetUniform3f(_modelShader.GetUniformLocation("cameraPos"), camPos.X, camPos.Y, camPos.Z);

        if (OverrideModelColor)
        {
            _modelShader.SetUniform1i(_modelShader.GetUniformLocation("useModelColor"), 1);
            _modelShader.SetUniform3f(_modelShader.GetUniformLocation("modelColor"), ModelColor.R / 255f, ModelColor.G / 255f, ModelColor.B / 255f);
        }
        else
        {
            _modelShader.SetUniform1i(_modelShader.GetUniformLocation("useModelColor"), 0);
        }

        _modelBuffer.BindAndDrawWithAttributes(3, 3, 3, 0);

        RequestNextFrameRendering(); 
    }
}
