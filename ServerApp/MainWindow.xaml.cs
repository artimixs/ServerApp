using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using ServerWindow.DataHandler;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine.Configuration;
using MongoDB.Driver;
using System.Net;
using System.Net.NetworkInformation;
using MongoDB.Bson;
using ServerWindow.NetCode;

namespace ServerWindow
{
    public partial class MainWindow : Window
    {
        private ServerHandler _serverHandler;
        private Thread _serverThread;
        internal DataBaseHandler dataHandler;

        public MainWindow()
        {
            InitializeComponent();
            // Запуск сервера в отдельном потоке
            _serverThread = new Thread(StartServer);
            _serverThread.IsBackground = true; // Поток завершится, когда закрывается приложение
            _serverThread.Start();

            var mongoConnection = new MongoDBConnection();

            // Получите базу данных
            var database = mongoConnection.GetDatabase();

            dataHandler = new DataBaseHandler(database, this);
        }

        private void StartServer()
        {
            _serverHandler = new ServerHandler(this);

            Dispatcher.Invoke(() =>
            {
                ConsoleOfSystem.Text += "Запуск сервера...\n";
            });

            _serverHandler.SetupAndStartServer();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Остановка сервера при закрытии окна
            if (_serverHandler != null)
            {
                _serverHandler.StopServer();
                _serverThread.Join(); // Дожидаемся завершения потока
            }
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
