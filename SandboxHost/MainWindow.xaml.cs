using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SandboxHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int LWA_ALPHA = 0x2;

        public const int LWA_COLORKEY = 0x1;
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern IntPtr SetWindowLongW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommands ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;

            /// <summary>
            /// Gets the default (empty) value.
            /// </summary>
            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        public struct POINT
        {
            public FIXED x;
            public FIXED y;
        }

        public struct FIXED
        {
            public short fract;
            public short value;

        }
        public static long WS_BORDER = 0x800000;
        public static long WS_POPUP = 0x80000000;
        public static long WS_CAPTION = 0xC00000;
        public static long WS_DISABLED = 0x8000000;
        public static long WS_DLGFRAME = 0x400000;
        public static long WS_GROUP = 0x20000;
        public static long WS_HSCROLL = 0x100000;
        public static long WS_MAXIMIZE = 0x1000000;
        public static long WS_MAXIMIZEBOX = 0x10000;
        public static long WS_MINIMIZE = 0x20000000;
        public static long WS_MINIMIZEBOX = 0x20000;
        public static long WS_OVERLAPPED = 0;
        public static long WS_OVERLAPPEDWINDOW = 0xCF0000;
        public static long WS_POPUPWINDOW = 0x80880000;
        public static long WS_SIZEBOX = 0x40000;
        public static long WS_SYSMENU = 0x80000;
        public static long WS_TABSTOP = 0x10000;
        public static long WS_THICKFRAME = 0x40000;
        public static long WS_VSCROLL = 0x200000;
        public static long WS_VISIBLE = 0x10000000;
        public static long WS_CHILD = 0x40000000;

        private static string GetLongValues(long val)
        {
            string vals = "Values for " + val.ToString() + " ";

            if ((val & WS_BORDER) != 0)
                vals += "WS_BORDER ";

            if ((val & WS_POPUP) != 0)
                vals += "WS_POPUP ";

            if ((val & WS_CAPTION) != 0)
                vals += "WS_CAPTION ";

            if ((val & WS_DISABLED) != 0)
                vals += "WS_DISABLED ";

            if ((val & WS_DLGFRAME) != 0)
                vals += "WS_DLGFRAME ";

            if ((val & WS_GROUP) != 0)
                vals += "WS_GROUP ";

            if ((val & WS_HSCROLL) != 0)
                vals += "WS_HSCROLL ";

            if ((val & WS_MAXIMIZE) != 0)
                vals += "WS_MAXIMIZE ";

            if ((val & WS_MAXIMIZEBOX) != 0)
                vals += "WS_MAXIMIZEBOX ";

            if ((val & WS_MINIMIZE) != 0)
                vals += "WS_MINIMIZE ";

            if ((val & WS_MINIMIZEBOX) != 0)
                vals += "WS_MINIMIZEBOX ";

            if ((val & WS_OVERLAPPED) != 0)
                vals += "WS_OVERLAPPED ";

            if ((val & WS_OVERLAPPEDWINDOW) != 0)
                vals += "WS_OVERLAPPEDWINDOW ";

            if ((val & WS_POPUPWINDOW) != 0)
                vals += "WS_POPUPWINDOW ";

            if ((val & WS_SIZEBOX) != 0)
                vals += "WS_SIZEBOX ";

            if ((val & WS_SYSMENU) != 0)
                vals += "WS_SYSMENU ";

            if ((val & WS_TABSTOP) != 0)
                vals += "WS_TABSTOP ";

            if ((val & WS_THICKFRAME) != 0)
                vals += "WS_THICKFRAME ";

            if ((val & WS_VSCROLL) != 0)
                vals += "WS_VSCROLL ";

            if ((val & WS_VISIBLE) != 0)
                vals += "WS_VISIBLE ";

            if ((val & WS_CHILD) != 0)
                vals += "WS_CHILD ";

            return vals;
        }

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        enum WindowLongFlags : int
        {
            GWL_EXSTYLE = -20,
            GWLP_HINSTANCE = -6,
            GWLP_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4,
            DWLP_USER = 0x8,
            DWLP_MSGRESULT = 0x0,
            DWLP_DLGPROC = 0x4
        }

        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
            /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
            /// blocking its execution while other threads process the request.</summary>
            /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
            AsynchronousWindowPosition = 0x4000,
            /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
            /// <remarks>SWP_DEFERERASE</remarks>
            DeferErase = 0x2000,
            /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
            /// <remarks>SWP_DRAWFRAME</remarks>
            DrawFrame = 0x0020,
            /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
            /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
            /// is sent only when the window's size is being changed.</summary>
            /// <remarks>SWP_FRAMECHANGED</remarks>
            FrameChanged = 0x0020,
            /// <summary>Hides the window.</summary>
            /// <remarks>SWP_HIDEWINDOW</remarks>
            HideWindow = 0x0080,
            /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
            /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
            /// parameter).</summary>
            /// <remarks>SWP_NOACTIVATE</remarks>
            DoNotActivate = 0x0010,
            /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
            /// contents of the client area are saved and copied back into the client area after the window is sized or 
            /// repositioned.</summary>
            /// <remarks>SWP_NOCOPYBITS</remarks>
            DoNotCopyBits = 0x0100,
            /// <summary>Retains the current position (ignores X and Y parameters).</summary>
            /// <remarks>SWP_NOMOVE</remarks>
            IgnoreMove = 0x0002,
            /// <summary>Does not change the owner window's position in the Z order.</summary>
            /// <remarks>SWP_NOOWNERZORDER</remarks>
            DoNotChangeOwnerZOrder = 0x0200,
            /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
            /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
            /// window uncovered as a result of the window being moved. When this flag is set, the application must 
            /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
            /// <remarks>SWP_NOREDRAW</remarks>
            DoNotRedraw = 0x0008,
            /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
            /// <remarks>SWP_NOREPOSITION</remarks>
            DoNotReposition = 0x0200,
            /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
            /// <remarks>SWP_NOSENDCHANGING</remarks>
            DoNotSendChangingEvent = 0x0400,
            /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
            /// <remarks>SWP_NOSIZE</remarks>
            IgnoreResize = 0x0001,
            /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
            /// <remarks>SWP_NOZORDER</remarks>
            IgnoreZOrder = 0x0004,
            /// <summary>Displays the window.</summary>
            /// <remarks>SWP_SHOWWINDOW</remarks>
            ShowWindow = 0x0040,
        }

        public enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window 
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
                          /// <summary>
                          /// Activates the window and displays it as a maximized window.
                          /// </summary>       
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value 
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level 
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is 
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position. 
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
            /// that owns the window is not responding. This flag should only be 
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }


        private EnumWindowsProc _winProc;
        private List<IntPtr> _children = new List<IntPtr>();
        private bool _childrenLoaded = false;
        private IntPtr _hostHWnd;
        private Dispatcher _dispather = null;
        private NPComms _comms = null;

        public MainWindow()
        {
            string[] args;
            int val;

            InitializeComponent();
            _dispather = Dispatcher.CurrentDispatcher;
            _comms = new NPComms("Host");

            args = Environment.GetCommandLineArgs();

            args
                .ToList()
                .ForEach((a) =>
                {
                    if (int.TryParse(a, out val))
                        _children.Add(new IntPtr(val));

                });


            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RECT rect = new RECT();
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            if (App._children.Count > 0)
            {
                //GetWindowRect(App._children.Last(), out rect);
                //Debug.WriteLine(rect);
                //SetWindowPos(App._children.Last(), HWND_TOP, 0,20, (int)ContentGrid.ActualWidth, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20, SetWindowPosFlags.DoNotActivate | SetWindowPosFlags.IgnoreMove);
                MoveChildWindow();
                //placement.ShowCmd = ShowWindowCommands.Show;
                //placement.NormalPosition = new RECT(0, 20, (int)ContentGrid.ActualWidth, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20);

                //SetWindowPlacement(App._children.Last(), ref placement);
                GetWindowRect(App._children.Last(), out rect);
                Debug.WriteLine(rect);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
           

            if (!_childrenLoaded)
            {
                _hostHWnd = (new WindowInteropHelper(this)).Handle;
                _children.ForEach((c) =>
                {
                    SetParent(c, _hostHWnd);
                    //MessageBox.Show("Setting parent for " + c.ToString() + " = " +.ToString());
                });
                _childrenLoaded = true;
            }

            base.OnActivated(e);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {

            base.OnRenderSizeChanged(sizeInfo);
        }

        //protected override void OnLocationChanged(EventArgs e)
        //{
        //    RECT rect = new RECT();
        //    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
        //    if (App._children.Count > 0)
        //    {
        //        //GetWindowRect(App._children.Last(), out rect);
        //        //Debug.WriteLine(rect);
        //        //SetWindowPos(App._children.Last(), HWND_TOP, 0,20, (int)ContentGrid.ActualWidth, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20, SetWindowPosFlags.DoNotActivate | SetWindowPosFlags.IgnoreMove);
        //        MoveChildWindow();
        //        //placement.ShowCmd = ShowWindowCommands.Show;
        //        //placement.NormalPosition = new RECT(0, 20, (int)ContentGrid.ActualWidth, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20);

        //        //SetWindowPlacement(App._children.Last(), ref placement);
        //        GetWindowRect(App._children.Last(), out rect);
        //        Debug.WriteLine(rect);
        //    }
        //    base.OnLocationChanged(e);
        //}

        private void StartClients()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.UseShellExecute = false;

#if DEBUG
            startInfo.FileName = Path.GetFullPath(@"..\..\..\SandboxClient\bin\Debug\SandboxClient.exe");
#else
            startInfo.FileName = Path.GetFullPath(@"..\..\..\SandboxClient\bin\Release\SandboxClient.exe");          
#endif

            StartClient(startInfo);
            //StartClient(startInfo);
            //StartClient(startInfo);

            //if (_children.Count == 0)
            //    throw new Exception();
        }

        private void StartClient(ProcessStartInfo startInfo)
        {
            long lVal;
            long lExVal;
            bool status;

            Task.Run(() =>
            {
                Process proc = Process.Start(startInfo);

                while (proc.MainWindowHandle == IntPtr.Zero)
                { }

                lVal = GetWindowLong(proc.MainWindowHandle, (int)WindowLongFlags.GWL_STYLE);
                lExVal = GetWindowLong(proc.MainWindowHandle, (int)WindowLongFlags.GWL_EXSTYLE);

                Debug.WriteLine(GetLongValues(lVal));

                //lVal &= ~WS_POPUP;
                //lVal &= ~WS_POPUPWINDOW;
                //lVal |= WS_CHILD;
                lVal = WS_CHILD | WS_VISIBLE;
                Debug.WriteLine(GetLongValues(lVal));

                lExVal |= WS_EX_LAYERED;

                SetWindowLongW(proc.MainWindowHandle, (int)WindowLongFlags.GWL_STYLE, new IntPtr(lVal));
                //SetWindowLongW(proc.MainWindowHandle, GWL_EXSTYLE, new IntPtr(lExVal));
                //status = SetLayeredWindowAttributes(proc.MainWindowHandle, 0xff00ff, 128, LWA_ALPHA);

                //Console.WriteLine("SetLayered=" + status.ToString());
                //if (status != true)
                //    Console.WriteLine("ErrorCode=" + GetLastError().ToString());

                lVal = GetWindowLong(proc.MainWindowHandle, (int)WindowLongFlags.GWL_STYLE);

                Debug.WriteLine(GetLongValues(lVal));

                SetParent(proc.MainWindowHandle, _hostHWnd);
                App._children.Add(proc.MainWindowHandle);
                App._childrenProc.Add(proc.Id);
                MoveChildWindow();

            });

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartClients();
        }

        private void MoveChildWindow()
        {
            //_comms.SendMessage("Client_" + App._childrenProc.Last().ToString(), "0,20," + ((int)ContentGrid.ActualWidth - 0).ToString() + "," + ((int)ContentGrid.RowDefinitions[0].ActualHeight - 20).ToString());
            _dispather.Invoke(() => SetWindowPos(App._children.Last(), HWND_TOP, 0, 20, (int)ContentGrid.ActualWidth - 0, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20, SetWindowPosFlags.DoNotActivate));

            //MoveWindow(App._children.Last(), 0, 20, (int)ContentGrid.ActualWidth, (int)ContentGrid.RowDefinitions[0].ActualHeight - 20, true);
            //_dispather.Invoke(() => SetWindowPos(App._children.Last(), HWND_TOPMOST, (int)Left + 3, (int)Top + 3, (int)ContentGrid.ActualWidth - 6, (int)ContentGrid.RowDefinitions[0].ActualHeight - 6, SetWindowPosFlags.DoNotActivate));
            //_dispather.Invoke(() => SetWindowPos(App._children.Last(), HWND_NOTOPMOST, (int)Left + 3, (int)Top + 3, (int)ContentGrid.ActualWidth - 6, (int)ContentGrid.RowDefinitions[0].ActualHeight - 6, SetWindowPosFlags.DoNotActivate));
        }

    }


    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs()
        {

        }


        public MessageEventArgs(string message)
        {
            Message = message;

        }

        public string Message { get; private set; }
    }

    public class NPComms : IDisposable
    {
        private const int MAX_PIPE_CONNECTIONS = 32;
        private const int NAMED_PIPE_CONNECT_TIMEOUT = 3000;
        private NamedPipeServerStream _pipeServer = null;
        private NamedPipeClientStream _pipeClient = null;
        private string _pipeServerName = string.Empty;

        public event EventHandler<MessageEventArgs> MessageReceived;

        public NPComms(string pipeServerName)
        {
            if (string.IsNullOrEmpty(pipeServerName))
                throw new ArgumentNullException(nameof(pipeServerName));

            _pipeServerName = pipeServerName;

            StartNamedPipeServer();

        }

        #region Named Pipe Client

        public void SendMessage(string serverName, string message)
        {
            IDictionary<string, string> payload = new Dictionary<string, string>();
            StreamReader reader;
            StreamWriter writer;

            try
            {

                if (string.IsNullOrEmpty(message))
                    return;

                if (_pipeClient != null)
                    _pipeClient.Dispose();

                _pipeClient = new NamedPipeClientStream(".", serverName, PipeDirection.InOut, PipeOptions.Asynchronous);

                // The connect function will indefinitely wait for the pipe to become available
                // If that is not acceptable specify a maximum waiting time (in ms)
                _pipeClient.Connect(NAMED_PIPE_CONNECT_TIMEOUT);
                Console.WriteLine("Connected to server.");

                reader = new StreamReader(_pipeClient);
                writer = new StreamWriter(_pipeClient);


                writer.WriteLine(message);



                writer.Flush();

                _pipeClient.WaitForPipeDrain();

                Console.WriteLine("Sent");
                message = reader.ReadLine();

                reader.Close();
                reader.Dispose();
                //writer.Close();

                //if (_pipeClient.IsConnected)
                //    writer.Dispose();


                Console.WriteLine("Received: " + message);


                Console.WriteLine("Done");



            }
            catch (Exception exc)
            {

                Console.WriteLine(exc);
            }
            finally
            {
                _pipeClient.Close();
            }
        }

        #endregion

        #region Named Pipe Server

        protected virtual NamedPipeServerStream CreateNamedPipeServer()
        {
            return new NamedPipeServerStream(_pipeServerName,
                    PipeDirection.InOut,
                    MAX_PIPE_CONNECTIONS,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);
        }

        protected virtual void StartNamedPipeServer()
        {

            try
            {
                _pipeServer = CreateNamedPipeServer();
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, _pipeServer);
            }
            catch (Exception exc)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exc);
#endif
                throw;
            }

        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);

                using (StreamReader reader = new StreamReader(_pipeServer))
                {
                    HandleMessageReceived(reader.ReadLine());
                }

                _pipeServer.Close();
                StartNamedPipeServer();

            }
            catch (Exception exc)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exc);
#endif
                //throw;
            }
        }

        private void HandleMessageReceived(string message)
        {
            //string[] parts;

            if (string.IsNullOrEmpty(message))
                return;

            //parts = message.Split(new char[] { MESSAGE_DELIM }, StringSplitOptions.RemoveEmptyEntries);

            //Task.Run(() =>
            //{


            try
            {

                RaiseMessageReceived(message);

            }
            catch (Exception exc)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(exc);
#endif
                throw;
            }
            //});
        }


        #endregion

        protected void RaiseMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new MessageEventArgs(message));
        }

        public void Dispose()
        {
            _pipeServer?.Close();
            _pipeServer?.Dispose();
        }
    }


}
