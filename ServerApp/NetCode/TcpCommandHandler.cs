// Обновлённый обработчик TcpCommandHandler с поддержкой длины пакета
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Windows;
using Newtonsoft.Json;

namespace ServerWindow.NetCode
{
    public class TcpCommandHandler
    {
        private readonly TcpClient client;
        private readonly MainWindow mainWindow;
        private readonly IMongoDatabase database;
        private readonly Dictionary<string, TcpClient> sessions;

        public TcpCommandHandler(TcpClient client, MainWindow mainWindow, IMongoDatabase database, Dictionary<string, TcpClient> sessions)
        {
            this.client = client;
            this.mainWindow = mainWindow;
            this.database = database;
            this.sessions = sessions;
        }

        public async Task HandleAsync()
        {
            using var stream = client.GetStream();
            string sessionId = null;

            while (true)
            {
                try
                {
                    string line = await ReadMessageAsync(stream);
                    if (string.IsNullOrEmpty(line)) break;

                    var doc = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);
                    var command = doc.GetValueOrDefault("triggerCommand")?.ToString();
                    Log($"Получена команда: {command}\r\n", "ConsoleOfConnectionRequest");

                    if (command == "AUTH")
                    {
                        var username = doc.GetValueOrDefault("username")?.ToString();
                        var password = doc.GetValueOrDefault("password")?.ToString();

                        if (username == "user" && password == "3012")
                        {
                            sessionId = Guid.NewGuid().ToString();
                            sessions[sessionId] = client;
                            await SendMessageAsync(stream, $"Авторизация успешна. Ваш sessionId: {sessionId}");
                            Log("Авторизация успешна.\r\n", "ConsoleOfAuthenticationRequest");
                        }
                        else
                        {
                            await SendMessageAsync(stream, "Неверный логин или пароль.");
                            Log("Неверный логин или пароль.\r\n", "ConsoleOfAuthenticationRequest");
                        }
                        continue;
                    }

                    if (!doc.TryGetValue("sessionId", out var sidObj) || sidObj?.ToString() != sessionId)
                    {
                        await SendMessageAsync(stream, "Вы не авторизованы.");
                        Log("Попытка команды без авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                        continue;
                    }

                    switch (command)
                    {
                        case "GET_COLLECTIONS":
                            var collections = database.ListCollectionNames().ToList();
                            await SendMessageAsync(stream, JsonConvert.SerializeObject(collections));
                            break;

                        case "GET_ELEMENTS":
                            var targetCollectionName = doc.GetValueOrDefault("collectionName")?.ToString();
                            if (string.IsNullOrWhiteSpace(targetCollectionName))
                            {
                                await SendMessageAsync(stream, "Ошибка: не указано имя коллекции.");
                                break;
                            }
                            var elements = mainWindow.dataHandler.GetAllElementNames(targetCollectionName);
                            await SendMessageAsync(stream, JsonConvert.SerializeObject(elements));
                            break;

                        case "GET_ELEMENT":
                            var cname = doc.GetValueOrDefault("collectionName")?.ToString();
                            var ename = doc.GetValueOrDefault("elementName")?.ToString();
                            if (string.IsNullOrWhiteSpace(cname) || string.IsNullOrWhiteSpace(ename))
                            {
                                await SendMessageAsync(stream, "Ошибка: не указано имя элемента или коллекции.");
                                break;
                            }
                            var element = mainWindow.dataHandler.GetElementByName(cname, ename);
                            var json = element?.ToJson() ?? "\"Элемент не найден\"";
                            await SendMessageAsync(stream, json);
                            break;

                        case "GET_STRUCTURE":
                            var colName = doc.GetValueOrDefault("collectionName")?.ToString();
                            if (string.IsNullOrWhiteSpace(colName))
                            {
                                await SendMessageAsync(stream, "{\"error\":\"collectionName не указан\"}");
                                break;
                            }
                            var className = mainWindow.dataHandler.GetMappedClassName(colName);
                            var assemblyName = mainWindow.dataHandler.GetMappedAssemblyName(colName);
                            var result = new Dictionary<string, string> { { "class", className }, { "assembly", assemblyName } };
                            await SendMessageAsync(stream, JsonConvert.SerializeObject(result));
                            break;

                        case "ADD_ELEMENT_JSON":
                            var tname = doc.GetValueOrDefault("collectionName")?.ToString();
                            var jsonData = doc.GetValueOrDefault("jsonData")?.ToString();
                            if (tname != null && jsonData != null)
                            {
                                var collection = database.GetCollection<BsonDocument>(tname);
                                var bsonDoc = BsonDocument.Parse(jsonData);
                                await collection.InsertOneAsync(bsonDoc);
                                await SendMessageAsync(stream, $"Новый элемент добавлен в коллекцию '{tname}'.");
                            }
                            break;

                        default:
                            await SendMessageAsync(stream, "Неизвестная команда.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    await SendMessageAsync(stream, $"Ошибка: {ex.Message}");
                    Log($"Ошибка обработки запроса: {ex.Message}\r\n", "ConsoleOfSystem");
                }
            }
        }

        private async Task<string> ReadMessageAsync(NetworkStream stream)
        {
            byte[] lengthBytes = new byte[4];
            int read = await stream.ReadAsync(lengthBytes, 0, 4);
            if (read == 0) return null;

            int length = BitConverter.ToInt32(lengthBytes, 0);
            byte[] buffer = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }
            return Encoding.UTF8.GetString(buffer, 0, totalRead);
        }

        private async Task SendMessageAsync(NetworkStream stream, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private void Log(string message, string console = "ConsoleOfSystem")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.LogMessage(message, console);
            });
        }
    }
}
