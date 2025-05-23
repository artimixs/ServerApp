// TcpJsonServer: отвечает только за запуск, остановку и управление клиентами
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Windows;
using ServerWindow.Objects;

namespace ServerWindow.NetCode
{
    public class TcpServer
    {
        private readonly int port;
        private TcpListener listener;
        private WebClient webClient;
        private readonly Dictionary<string, TcpClient> authenticatedClients = new();
        private readonly MainWindow _mainWindow;
        private bool _isRunning = true;
        private readonly IMongoDatabase database;

        public TcpServer(int port, MainWindow mainWindow)
        {
            this.port = port;
            this._mainWindow = mainWindow;
            this.database = _mainWindow.dataHandler?.Database ?? throw new Exception("dataHandler.Database не инициализирован");
        }

        public async Task StartAsync()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            var localIp = GetLocalIPAddress();
            Log($"TCP сервер запущен на локальном адресе {localIp}:{port}\r\n");
            try
            {
                webClient = new WebClient();
                using var wc = webClient;
                var globalIp = wc.DownloadString("http://api.ipify.org");
                Log($"TCP сервер запущен на глобальном IP {globalIp}:{port}\r\n");
            }
            catch (Exception ex)
            {
                Log($"Не удалось определить внешний IP: {ex.Message}\r\n");
            }


            while (_isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => new TcpCommandHandler(client, _mainWindow, database, authenticatedClients).HandleAsync());
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            listener?.Stop();
            Log("TCP сервер остановлен.\r\n", "ConsoleOfSystem");
        }

        private void Log(string message, string console = "ConsoleOfSystem")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.LogMessage(message, console);
            });
        }

        private static string GetLocalIPAddress()
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                            return ua.Address.ToString();
                    }
                }
            }
            return "127.0.0.1";
        }
    }
}
