using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace ServerWindow.NetCode
{
    public class ServerHandler
    {
        private GatewayServer _localServer;
        private GatewayServer _externalServer;
        private MainWindow _mainWindow;
        private Dictionary<string, AppSession> _activeSessions;

        public ServerHandler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _localServer = new GatewayServer(mainWindow);
            _externalServer = new GatewayServer(mainWindow);
            _activeSessions = new Dictionary<string, AppSession>();
        }

        public void SetupAndStartServer()
        {
            string hostName = Dns.GetHostName();
            IPAddress externalIp = null;

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    string externalIpString = webClient.DownloadString("http://api.ipify.org");
                    externalIp = IPAddress.Parse(externalIpString);
                    _mainWindow.LogMessage($"Внешний IP-адрес: {externalIp}\r\nПолная запись: {externalIp}:8080\r\n", "ConsoleOfConnectionRequest");
                }
                catch (Exception ex)
                {
                    _mainWindow.LogMessage($"Ошибка получения внешнего IP: {ex.Message}\r\n", "ConsoleOfConnectionRequest");
                    return;
                }
            }
            IPAddress localIp = null;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            localIp = ipInfo.Address;
                            break;
                        }
                    }
                }

                if (localIp != null)
                    break;
            }

            var localConfig = new ServerConfig
            {
                Name = "Локальный",
                Port = 8081,
                Ip = localIp.ToString(),
                MaxConnectionNumber = 10,
                Mode = SocketMode.Tcp,
            };

            var externalConfig = new ServerConfig
            {
                Name = "Внешний",
                Port = 8080,
                Ip = "0.0.0.0",
                MaxConnectionNumber = 10,
                Mode = SocketMode.Tcp,
            };

            StartServer(_externalServer, externalConfig, "внешний сервер");
            StartServer(_localServer, localConfig, "локальный сервер");
        }

        private void StartServer(GatewayServer server, ServerConfig config, string serverName)
        {
            try
            {
                if (!server.Setup(config))
                {
                    _mainWindow.LogMessage($"Не удалось настроить {serverName}: ошибка настройки сокета!\r\n", "ConsoleOfSystem");
                    return;
                }
                else
                {
                    _mainWindow.LogMessage($"{serverName} {server.Config.Ip}:{server.Config.Port}\r\n", "ConsoleOfConnectionRequest");
                }

                if (!server.Start())
                {
                    _mainWindow.LogMessage($"Не удалось запустить {serverName}: ошибка запуска сокета!\r\n", "ConsoleOfSystem");
                    return;
                }

                _mainWindow.LogMessage($"{serverName} успешно запущен.\r\n", "ConsoleOfSystem");
            }
            catch (SocketException ex)
            {
                _mainWindow.LogMessage($"SocketException ({serverName}): {ex.Message}\r\n", "ConsoleOfConnectionRequest");
            }
            catch (Exception ex)
            {
                _mainWindow.LogMessage($"Общая ошибка ({serverName}): {ex.Message}\r\n", "ConsoleOfSystem");
            }
        }
        public void StopServer()
        {
            _localServer.Stop();
            _externalServer.Stop();
        }

        public bool IsSessionAuthenticated(string sessionId)
        {
            return _activeSessions.ContainsKey(sessionId);
        }
    }
}
