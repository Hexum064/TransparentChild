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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SandboxClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NPComms _comms = null;
        private Dispatcher _dispatcher = null;

        public MainWindow()
        {
            //ShowInTaskbar = false;
            InitializeComponent();
            _dispatcher = Dispatcher.CurrentDispatcher;

            _comms = new NPComms("Client_" + Process.GetCurrentProcess().Id.ToString());
            _comms.MessageReceived += _comms_MessageReceived;
        }

        private void _comms_MessageReceived(object sender, MessageEventArgs e)
        {
            string[] parts;
            int x, y, w, h;

            if (string.IsNullOrEmpty(e.Message))
                return;

            parts = e.Message.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
                return;

            if (!int.TryParse(parts[0], out x)
                || !int.TryParse(parts[1], out y)
                || !int.TryParse(parts[2], out w)
                || !int.TryParse(parts[3], out h))
                return;


            Dispatcher.Invoke(() =>
            {
                Top = y;
                Left = x;
                Width = w;
                Height = h;
            });

            //MessageBox.Show("Resized");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int val = 1;
            int val2 = 0;

            val /= val2;

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
