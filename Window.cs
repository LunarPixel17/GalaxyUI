using System;
using System.Drawing;
using System.Runtime.InteropServices;
using GalaxyUI.UIElement;

namespace GalaxyUI.UIElement
{
        /// <summary>
        /// Represents a window in GalaxyUI
        /// </summary>
        public class Window : UIElement
        {
            #region Native Methods and Structures

            [DllImport("user32.dll")]
            private static extern IntPtr CreateWindowEx(
                uint dwExStyle,
                string lpClassName,
                string lpWindowName,
                uint dwStyle,
                int x, int y,
                int nWidth, int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam);

            [DllImport("user32.dll")]
            private static extern bool DestroyWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool RegisterClassEx(ref WNDCLASSEX lpwcx);

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            [StructLayout(LayoutKind.Sequential)]
            private struct WNDCLASSEX
            {
                public uint cbSize;
                public uint style;
                public IntPtr lpfnWndProc;
                public int cbClsExtra;
                public int cbWndExtra;
                public IntPtr hInstance;
                public IntPtr hIcon;
                public IntPtr hCursor;
                public IntPtr hbrBackground;
                public string lpszMenuName;
                public string lpszClassName;
                public IntPtr hIconSm;
            }

            // Window styles
            private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
            private const uint WS_VISIBLE = 0x10000000;

            // Window messages
            private const uint WM_DESTROY = 0x0002;
            private const uint WM_SIZE = 0x0005;
            private const uint WM_PAINT = 0x000F;
            private const uint WM_CLOSE = 0x0010;

            // Show window commands
            private const int SW_SHOW = 5;

            #endregion

            private IntPtr _handle;
            private readonly string _title;
            private RenderContext _renderContext;
            private Panel _rootPanel;
            private bool _acrylicEnabled;
            private float _acrylicOpacity = 0.8f;

            static Window()
            {
                // Register the window class
                RegisterWindowClass();
            }

            public Window(string title = "GalaxyUI Window", int width = 800, int height = 600)
            {
                _title = title;
                Size = new Size(width, height);
                Location = new Point(100, 100);
                _rootPanel = new Panel();

                // Create the actual window
                CreateNativeWindow();

                // Connect with the rendering engine
                _renderContext = HyperDimension.CreateContext(_handle);

                // Register with GalaxyUI
                Galaxy.RegisterWindow(this);
            }

            public IntPtr Handle => _handle;

            public bool AcrylicEnabled
            {
                get => _acrylicEnabled;
                set
                {
                    _acrylicEnabled = value;
                    UpdateAcrylicEffect();
                }
            }

            public float AcrylicOpacity
            {
                get => _acrylicOpacity;
                set
                {
                    _acrylicOpacity = Math.Max(0, Math.Min(1, value));
                    UpdateAcrylicEffect();
                }
            }

        public Panel Content
        {
            get => _rootPanel;
            set
            {
                // Remove the old root panel if it exists
                if (_rootPanel != null && Children is List<UIElement> childrenList1)
                {
                    childrenList1.Remove(_rootPanel);
                }

                // Set the new root panel
                _rootPanel = value;

                // Add the new root panel if it exists
                if (_rootPanel != null && Children is List<UIElement> childrenList2)
                {
                    childrenList2.Add(_rootPanel);
                }
            }
        }

        public event EventHandler Closed;

            public void Show()
            {
                ShowWindow(_handle, SW_SHOW);
            }

            public void Close()
            {
                DestroyWindow(_handle);
            }

            protected override void OnRender(RenderContext context)
            {
                base.OnRender(context);

                if (_acrylicEnabled)
                {
                    // Apply acrylic effect
                    RenderAcrylicEffect(context);
                }
            }

            private void CreateNativeWindow()
            {
                _handle = CreateWindowEx(
                    0,
                    "GalaxyUIWindowClass",
                    _title,
                    WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                    Location.X, Location.Y,
                    Size.Width, Size.Height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    GetModuleHandle(null),
                    IntPtr.Zero);

                if (_handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to create window");
                }
            }

            private void UpdateAcrylicEffect()
            {
                // This would update the window's visual to use the acrylic effect
                // In a real implementation, this would use DWM APIs on Windows
                // or equivalent on other platforms
                Invalidate();
            }

            private void RenderAcrylicEffect(RenderContext context)
            {
                // In a real implementation, this would apply blur and transparency effects
                // This is placeholder for the actual implementation
            }

            private static void RegisterWindowClass()
            {
                WNDCLASSEX wcex = new WNDCLASSEX();
                wcex.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX));
                wcex.style = 0;
                wcex.lpfnWndProc = Marshal.GetFunctionPointerForDelegate<WndProcDelegate>(WndProc);
                wcex.cbClsExtra = 0;
                wcex.cbWndExtra = 0;
                wcex.hInstance = GetModuleHandle(null);
                wcex.hIcon = IntPtr.Zero;
                wcex.hCursor = IntPtr.Zero;
                wcex.hbrBackground = IntPtr.Zero;
                wcex.lpszMenuName = null;
                wcex.lpszClassName = "GalaxyUIWindowClass";
                wcex.hIconSm = IntPtr.Zero;

                RegisterClassEx(ref wcex);
            }

            private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                switch (msg)
                {
                    case WM_CLOSE:
                        // Handle window close
                        DestroyWindow(hWnd);
                        return IntPtr.Zero;

                    case WM_DESTROY:
                        // Window is being destroyed
                        Window window = FindWindowByHandle(hWnd);
                        if (window != null)
                        {
                            window.OnClosed();
                            Galaxy.UnregisterWindow(window);
                        }
                        return IntPtr.Zero;

                    case WM_SIZE:
                        // Window was resized
                        window = FindWindowByHandle(hWnd);
                        if (window != null)
                        {
                            int width = lParam.ToInt32() & 0xFFFF;
                            int height = (lParam.ToInt32() >> 16) & 0xFFFF;
                            window.OnResize(width, height);
                        }
                        return IntPtr.Zero;

                    case WM_PAINT:
                        // Window needs repainting
                        window = FindWindowByHandle(hWnd);
                        if (window != null)
                        {
                            window.Invalidate();
                        }
                        break;
                }

                return DefWindowProc(hWnd, msg, wParam, lParam);
            }

            private static Window FindWindowByHandle(IntPtr handle)
            {
                // This is a simplified version; a real implementation would maintain a dictionary
                // mapping window handles to Window objects
                return null;
            }

            private void OnResize(int width, int height)
            {
                Size = new Size(width, height);
                _renderContext.Resize(width, height);

                if (_rootPanel != null)
                {
                    _rootPanel.Width = width;
                    _rootPanel.Height = height;
                }

                Invalidate();
            }

            private void OnClosed()
            {
                Closed?.Invoke(this, EventArgs.Empty);
                _renderContext.Dispose();
            }

            public void Invalidate()
            {
                _renderContext.BeginDraw();
                OnRender(_renderContext);
                _renderContext.EndDraw();
            }
        }
    }
