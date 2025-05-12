using ServerWindow.GameObjects;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ServerWindow
{
    public class GatewayServer : AppServer
    {
        private MainWindow _mainWindow;
        private Dictionary<string, AppSession> _activeSessions;

        public GatewayServer(MainWindow mainWindow) : base(new TerminatorReceiveFilterFactory("\r\n"))
        {
            _mainWindow = mainWindow;
            _activeSessions = new Dictionary<string, AppSession>();
        }

        protected override void ExecuteCommand(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                // Попытка десериализации данных как JSON
                var commandData = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestInfo.Key);
                var command = commandData.ContainsKey("triggerCommand") ? commandData["triggerCommand"] : null;

                if (command != null)
                {
                    // Логируем получение команды от клиента
                    _mainWindow.LogMessage($"Получена команда: {command}\r\n", "ConsoleOfConnectionRequest");

                    switch (command)
                    {
                        case "AUTH":
                            // Обработка авторизации
                            HandleAuth(session, commandData);
                            break;

                        case "GET_COLLECTIONS":
                            if (IsSessionAuthenticated(session))
                            {
                                var collections = _mainWindow.dataHandler.GetAllCollections();

                                // Сериализуем в JSON с UTF-8
                                string jsonResponse = JsonConvert.SerializeObject(collections);
                                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse + "\r\n");

                                // Отправляем данные с корректной кодировкой
                                session.Send(responseBytes, 0, responseBytes.Length);

                                _mainWindow.LogMessage("Список коллекций отправлен клиенту.\r\n", "ConsoleOfConnectionRequest");
                            }
                            else
                            {
                                string errorMessage = "Вы не авторизованы.\r\n";
                                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                session.Send(errorBytes, 0, errorBytes.Length);

                                _mainWindow.LogMessage("Попытка получить коллекции без авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                            }
                            break;

                        case "GET_ELEMENTS":
                            if (IsSessionAuthenticated(session))
                            {
                                string collectionName = commandData.ContainsKey("collectionName") ? commandData["collectionName"] : null;
                                if (string.IsNullOrEmpty(collectionName))
                                {
                                    string errorMessage = "Название коллекции не указано.\r\n";
                                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                    session.Send(errorBytes, 0, errorBytes.Length);
                                }
                                else
                                {
                                    var elements = _mainWindow.dataHandler.GetAllElementNames(collectionName);

                                    // Сериализуем в JSON с UTF-8
                                    string jsonResponse = JsonConvert.SerializeObject(elements);
                                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse + "\r\n");

                                    // Отправляем данные с корректной кодировкой
                                    session.Send(responseBytes, 0, responseBytes.Length);

                                    _mainWindow.LogMessage($"Список элементов коллекции '{collectionName}' отправлен клиенту.\r\n", "ConsoleOfConnectionRequest");
                                }
                            }
                            else
                            {
                                string errorMessage = "Вы не авторизованы.\r\n";
                                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                session.Send(errorBytes, 0, errorBytes.Length);

                                _mainWindow.LogMessage("Попытка получить элементы без авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                            }
                            break;

                        case "GET_ELEMENT":
                            if (IsSessionAuthenticated(session))
                            {
                                string collectionName = commandData.ContainsKey("collectionName") ? commandData["collectionName"] : null;
                                string elementName = commandData.ContainsKey("elementName") ? commandData["elementName"] : null;

                                if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(elementName))
                                {
                                    string errorMessage = "Название коллекции или элемента не указано.\r\n";
                                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                    session.Send(errorBytes, 0, errorBytes.Length);
                                }
                                else
                                {
                                    var element = _mainWindow.dataHandler.GetElementByName(collectionName, elementName);

                                    // Сериализуем элемент или отправляем сообщение об отсутствии
                                    string jsonResponse = element?.ToJson() ?? "Элемент не найден.\r\n";
                                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse + "\r\n");

                                    // Отправляем данные с корректной кодировкой
                                    session.Send(responseBytes, 0, responseBytes.Length);

                                    _mainWindow.LogMessage($"Элемент '{elementName}' из коллекции '{collectionName}' отправлен клиенту.\r\n", "ConsoleOfConnectionRequest");
                                }
                            }
                            else
                            {
                                string errorMessage = "Вы не авторизованы.\r\n";
                                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                session.Send(errorBytes, 0, errorBytes.Length);

                                _mainWindow.LogMessage("Попытка получить элемент без авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                            }
                            break;

                        case "ADD_ELEMENT": // УСТАРЕЛО
                            if (IsSessionAuthenticated(session))
                            {
                                string collectionName = commandData.ContainsKey("collectionName") ? commandData["collectionName"] : null;
                                string elementName = commandData.ContainsKey("elementName") ? commandData["elementName"] : null;

                                if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(elementName))
                                {
                                    string errorMessage = "Название коллекции или имя элемента не указано.\r\n";
                                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                    session.Send(errorBytes, 0, errorBytes.Length);
                                    _mainWindow.LogMessage("Некорректные данные при попытке добавить элемент.\r\n", "ConsoleOfConnectionRequest");
                                }
                                else
                                {
                                    try
                                    {
                                        string className = _mainWindow.dataHandler.GetMappedClassName(collectionName);
                                        string assemblyName = _mainWindow.dataHandler.GetMappedAssemblyName(collectionName);
                                        var newDoc = _mainWindow.dataHandler.CreateTemplateFromClassName(className, assemblyName, elementName);


                                        var collection = _mainWindow.dataHandler.Database.GetCollection<BsonDocument>(collectionName);
                                        collection.InsertOne(newDoc);

                                        string okMessage = $"Элемент '{elementName}' добавлен в коллекцию '{collectionName}'.\r\n";
                                        byte[] okBytes = Encoding.UTF8.GetBytes(okMessage);
                                        session.Send(okBytes, 0, okBytes.Length);

                                        _mainWindow.LogMessage(okMessage, "ConsoleOfConnectionRequest");
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = $"Ошибка при добавлении элемента: {ex.Message}\r\n";
                                        byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                        session.Send(errorBytes, 0, errorBytes.Length);

                                        _mainWindow.LogMessage(errorMessage, "ConsoleOfConnectionRequest");
                                    }
                                }
                            }
                            else
                            {
                                string errorMessage = "Вы не авторизованы.\r\n";
                                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                session.Send(errorBytes, 0, errorBytes.Length);

                                _mainWindow.LogMessage("Попытка добавить элемент без авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                            }
                            break;

                        case "ADD_ELEMENT_JSON":
                            if (IsSessionAuthenticated(session))
                            {
                                string collectionName = commandData.GetValueOrDefault("collectionName");
                                string jsonData = commandData.GetValueOrDefault("jsonData");

                                if (!string.IsNullOrEmpty(collectionName) && !string.IsNullOrEmpty(jsonData))
                                {
                                    try
                                    {
                                        var doc = BsonDocument.Parse(jsonData);
                                        _mainWindow.dataHandler.Database.GetCollection<BsonDocument>(collectionName).InsertOne(doc);

                                        string msg = $"Новый элемент добавлен в коллекцию '{collectionName}'.\r\n";
                                        session.Send(Encoding.UTF8.GetBytes(msg));
                                        _mainWindow.LogMessage(msg, "ConsoleOfDatabaseRequest");
                                    }
                                    catch (Exception ex)
                                    {
                                        string error = $"Ошибка при добавлении элемента: {ex.Message}\r\n";
                                        session.Send(Encoding.UTF8.GetBytes(error));
                                        _mainWindow.LogMessage(error, "ConsoleOfDatabaseRequest");
                                    }
                                }
                            }
                            else
                            {
                                session.Send(Encoding.UTF8.GetBytes("Вы не авторизованы.\r\n"));
                            }
                            break;
                        case "GET_STRUCTURE":
                            if (IsSessionAuthenticated(session))
                            {
                                string collectionName = commandData.ContainsKey("collectionName") ? commandData["collectionName"] : null;

                                if (!string.IsNullOrEmpty(collectionName))
                                {
                                    var className = _mainWindow.dataHandler.GetMappedClassName(collectionName);
                                    var assemblyName = _mainWindow.dataHandler.GetMappedAssemblyName(collectionName);

                                    var structureInfo = new Dictionary<string, string>
                                    {
                                        { "class", className },
                                        { "assembly", assemblyName }
                                    };

                                    string response = JsonConvert.SerializeObject(structureInfo);
                                    byte[] responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                                    session.Send(responseBytes, 0, responseBytes.Length);

                                    _mainWindow.LogMessage($"Структура коллекции '{collectionName}' отправлена клиенту.\r\n", "ConsoleOfConnectionRequest");
                                }
                                else
                                {
                                    string errorMessage = "collectionName не указан.\r\n";
                                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                    session.Send(errorBytes, 0, errorBytes.Length);
                                }
                            }
                            else
                            {
                                string errorMessage = "Вы не авторизованы.\r\n";
                                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                                session.Send(errorBytes, 0, errorBytes.Length);
                            }
                            break;

                        default:
                            string unknownCommandMessage = "Неизвестная команда.\r\n";
                            byte[] unknownCommandBytes = Encoding.UTF8.GetBytes(unknownCommandMessage);
                            session.Send(unknownCommandBytes, 0, unknownCommandBytes.Length);

                            _mainWindow.LogMessage($"Получена неизвестная команда: {command}.\r\n", "ConsoleOfConnectionRequest");
                            break;
                    }
                }
                else
                {
                    session.Send("Не удалось распознать команду.\r\n");
                    _mainWindow.LogMessage("Ошибка распознавания команды.\r\n", "ConsoleOfConnectionRequest");
                }
            }
            catch (JsonException ex)
            {
                session.Send("Ошибка обработки данных JSON.\r\n");
                _mainWindow.LogMessage($"Ошибка обработки данных JSON: {ex.Message}\r\n", "ConsoleOfConnectionRequest");
            }
            catch (Exception ex)
            {
                session.Send("Произошла ошибка на сервере.\r\n");
                _mainWindow.LogMessage($"Произошла ошибка: {ex.Message}\r\n", "ConsoleOfConnectionRequest");
            }
        }


        private void HandleAuth(AppSession session, Dictionary<string, string> requestData)
        {
            // Логируем попытку авторизации
            _mainWindow.LogMessage($"Попытка авторизации с данными: {requestData["username"]}:{requestData["password"]}\r\n", "ConsoleOfAuthenticationRequest");

            try
            {

                // Проверка на наличие ключей username и password
                if (requestData != null && requestData.ContainsKey("username") && requestData.ContainsKey("password"))
                {
                    if (requestData["username"] == "user" && requestData["password"] == "3012")
                    {
                        string sessionId = Guid.NewGuid().ToString();
                        _activeSessions[sessionId] = session;
                        session.Items["sessionId"] = sessionId;
                        var message = $"Авторизация успешна. Ваш sessionId: {sessionId}\r\n";
                        session.Send(Encoding.UTF8.GetBytes(message), 0, message.Length);

                        _mainWindow.LogMessage("Авторизация успешна.\r\n", "ConsoleOfAuthenticationRequest");
                    }
                    else
                    {
                        session.Send("Неверный логин или пароль.\r\n");
                        _mainWindow.LogMessage("Неверный логин или пароль.\r\n", "ConsoleOfAuthenticationRequest");
                    }
                }
                else
                {
                    session.Send("Неполные данные для авторизации.\r\n");
                    _mainWindow.LogMessage("Неполные данные для авторизации.\r\n", "ConsoleOfAuthenticationRequest");
                }
            }
            catch (JsonException ex)
            {
                session.Send("Ошибка обработки данных авторизации.\r\n");
                _mainWindow.LogMessage($"Ошибка обработки данных авторизации: {ex.Message}\r\n", "ConsoleOfAuthenticationRequest");
            }
            catch (Exception ex)
            {
                session.Send("Произошла ошибка при авторизации.\r\n");
                _mainWindow.LogMessage($"Произошла ошибка при авторизации: {ex.Message}\r\n", "ConsoleOfAuthenticationRequest");
            }
        }

        private bool IsSessionAuthenticated(AppSession session)
        {
            return session.Items.ContainsKey("sessionId") && _activeSessions.ContainsKey(session.Items["sessionId"].ToString());
        }
    }
}
