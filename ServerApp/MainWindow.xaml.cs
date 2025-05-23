using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using ServerWindow.DataHandler;
using MongoDB.Driver;
using System.Net;
using System.Net.NetworkInformation;
using MongoDB.Bson;
using ServerWindow.NetCode;
using System.Threading.Tasks;

namespace ServerWindow
{
    public partial class MainWindow : Window
    {
        //private ServerHandler _serverHandler;
        TcpServer _tcpServer;
        private Thread _serverThread;
        internal DataBaseHandler dataHandler;

        public MainWindow()
        {
            InitializeComponent();

            var mongoConnection = new MongoDBConnection();
            var database = mongoConnection.GetDatabase();

            dataHandler = new DataBaseHandler(database, this);

            StartServer(); // только после dataHandler!
        }

        private void StartServer()
        {
            Task.Run(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        ConsoleOfSystem.Text += "Запуск TCP JSON-сервера...\n";
                    });

                    _tcpServer = new TcpServer(8080, this);
                    await _tcpServer.StartAsync();
                }
                catch (Exception e)
                {
                    LogMessage(e.Message.ToString(), "ConsoleOfSystem");
                }
            });
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Остановка TCP JSON-сервера при закрытии окна
            if (_tcpServer != null)
            {
                _tcpServer.Stop();
            }

            _serverThread?.Join(); // Дожидаемся завершения фонового потока
        }


        public void LogMessage(string message, string consoleName)
        {
            Dispatcher.Invoke(() =>
            {
                switch (consoleName)
                {
                    case "ConsoleOfConnectionRequest":
                        ConsoleOfConnectionRequest.Text += message;
                        break;
                    case "ConsoleOfAuthenticationRequest":
                        ConsoleOfAuthenticationRequest.Text += message;
                        break;
                    case "ConsoleOfDatabaseRequest":
                        ConsoleOfDatabaseRequest.Text += message;
                        break;
                    case "ConsoleOfSystem":
                        ConsoleOfSystem.Text += message;
                        break;
                    default:
                        Console.Write(message);
                        break;
                }
            });
        }
    }
}
