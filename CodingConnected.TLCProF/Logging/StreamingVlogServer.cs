using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace CodingConnected.TLCProF.Logging
{
    public class StreamingVLOGClient
    {
        private readonly TcpClient _client;
        private readonly bool _binaryStream;
        //private readonly bool _useSsl;
        private readonly ConcurrentQueue<byte> _bytesQueue = new();
        private readonly byte[] _buffer = new byte[4096];
        private readonly NetworkStream _stream;
        private byte _lastByte;

        public bool Connected { get; private set; }

        public event EventHandler Disconnected;

        public void EnqueueByte(byte b)
        {
            if (!Connected) return;

            _bytesQueue.Enqueue(b);
            if (Connected && _binaryStream && b == 0x16 && _lastByte != 0x16)
            {
                StartSend();
            }
            else if (Connected && b == '\n')
            {
                StartSend();
            }

            if (b == 0x16 && _lastByte == 0x16) _lastByte = 0;
            else _lastByte = b;
        }

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
            Connected = false;
        }

        private async void StartSend()
        {
            var i = 0;
            while (_bytesQueue.TryDequeue(out var b))
            {
                _buffer[i++] = b;
            }

            try
            {
                await _stream.WriteAsync(_buffer.AsMemory(0, i)).ConfigureAwait(false);
            }
            catch
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public StreamingVLOGClient(TcpClient client, bool binaryStream) //, bool useSsl)
        {
            Connected = true;
            _client = client;
            _binaryStream = binaryStream;
            //_useSsl = useSsl;
            //if (useSsl)
            //{
            //    try
            //    {
            //        var stream = new SslStream(_client.GetStream(), false) { ReadTimeout = 5000, WriteTimeout = 5000 };
            //        stream.AuthenticateAsServer(StreamingVLOGHost.ServerCertificate, false, SslProtocols.None, true);
            //        _stream = stream;
            //    }
            //    catch (Exception)
            //    {
            //        Disconnected?.Invoke(this, EventArgs.Empty);
            //        Dispose();
            //    }
            //}
            //else
            //{
                _stream = _client.GetStream();
            //}
        }
    }

    public class StreamingVLOGServer(int configurationStreamingPort, bool binaryStream) //, bool useSsl)
    {
        private readonly int _configurationStreamingPort = configurationStreamingPort;
        private readonly bool _binaryStream = binaryStream;
        //private readonly bool _useSsl = useSsl;

        #region Fields

        private readonly List<StreamingVLOGClient> _clients = [];
        private TcpListener _serverSocket;
        private bool _isRunning;
        private bool _cleanClients;

        #endregion // Fields

        #region Public Methods

        public void ForwardString(string text)
        {
            foreach (var client in _clients)
            {
                for (var index = 0; index < text.Length; index++)
                {
                    client.EnqueueByte((byte)text[index]);
                }
            }

            if (_cleanClients)
            {
                var remove = _clients.Where(x => !x.Connected).ToArray();
                foreach (var r in remove)
                {
                    _clients.Remove(r);
                }
            }
        }

        public void ForwardBytes(byte[] bytes, int length)
        {
            foreach (var client in _clients)
            {
                for (var index = 0; index < length; index++)
                {
                    client.EnqueueByte(bytes[index]);
                }
            }

            if (_cleanClients)
            {
                var remove = _clients.Where(x => !x.Connected).ToArray();
                foreach (var r in remove)
                {
                    _clients.Remove(r);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var client in _clients)
            {
                client.Dispose();
            }
            _clients.Clear();
            _serverSocket.Stop();
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            RunStreamingServer(_configurationStreamingPort);
        }

        #endregion // Public Methods

        #region Private Methods

        private void RunStreamingServer(int port)
        {
            _serverSocket = new TcpListener(IPAddress.Any, port);
            _serverSocket.Start();
            Task.Run(() =>
            {
                StreamingVLOGClient c = null;
                try
                {
                    while (_isRunning)
                    {
                        c = new StreamingVLOGClient(_serverSocket.AcceptTcpClient(), _binaryStream); //, _useSsl);
                        c.Disconnected += Client_OnDisconnected;
                        _clients.Add(c);
                    }
                }
                catch (SocketException)
                {
                    if (c != null)
                    {
                        c.Disconnected -= Client_OnDisconnected;
                        _clients.Remove(c);
                    }
                }
            });
        }

        private void Client_OnDisconnected(object sender, EventArgs e)
        {
            var c = (StreamingVLOGClient)sender;
            c.Disconnected -= Client_OnDisconnected;
            c.Dispose();
            _cleanClients = true;
        }

        #endregion // Private Methods
    }
}
