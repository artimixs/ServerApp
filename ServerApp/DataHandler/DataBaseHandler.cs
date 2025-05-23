using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ServerWindow.DataHandler
{
    public partial class DataBaseHandler
    {
        MainWindow _mainWindow;
        public readonly IMongoDatabase Database;
        public readonly string TargetDirectory;
        public readonly string ConfigFilePath;
        private Dictionary<string, CollectionClassInfo> collectionTypeMap = new Dictionary<string, CollectionClassInfo>();
        private JObject jsonStructure;

        public DataBaseHandler(IMongoDatabase database, MainWindow _mainWindow, string configFilePath = "DataHandler/DataBaseObjects.json")
        {
            this._mainWindow = _mainWindow;
            _mainWindow.LogMessage($"Инициализация DataHandler базы данных '{database.DatabaseNamespace}'\r\n", "ConsoleOfDatabaseRequest");
            Database = database;
            TargetDirectory = "DataHandler";
            ConfigFilePath = configFilePath;

            _mainWindow.LogMessage($"Корневая папка '{TargetDirectory}'\r\n", "ConsoleOfDatabaseRequest");
            _mainWindow.LogMessage($"Поиск баз данных '{ConfigFilePath}'\r\n", "ConsoleOfDatabaseRequest");
            _mainWindow.LogMessage($"Начата проверка конфигураций из файла '{ConfigFilePath}'\r\n", "ConsoleOfDatabaseRequest");

            // Загрузка конфигурации и маппинга типов
            LoadJsonStructure();
            LoadCollectionTypeMap(jsonStructure);

            InitializationConfiguration();
        }

        private void LoadJsonStructure()
        {
            try
            {
                string jsonText = File.ReadAllText(ConfigFilePath);
                jsonStructure = JObject.Parse(jsonText);
            }
            catch (Exception ex)
            {
                _mainWindow.LogMessage($"Ошибка загрузки JSON-конфигурации: {ex.Message}\r\n", "ConsoleOfDatabaseRequest");
                jsonStructure = new JObject();
            }
        }

        private void LoadCollectionTypeMap(JObject structure)
        {
            var main = structure["main"] as JObject;
            if (main == null) return;

            foreach (var prop in main.Properties())
            {
                var className = prop.Value["class"]?.ToString();
                var assemblyName = prop.Value["assembly"]?.ToString();

                if (!string.IsNullOrEmpty(className))
                {
                    collectionTypeMap[prop.Name] = new CollectionClassInfo
                    {
                        @class = className,
                        assembly = string.IsNullOrEmpty(assemblyName) ? "UrukGameObjects" : assemblyName
                    };
                }
            }

            _mainWindow.LogMessage($"Типы коллекций загружены: {string.Join(", ", collectionTypeMap.Select(p => $"{p.Key} → {p.Value}"))}\r\n", "ConsoleOfDatabaseRequest");
        }
        public string GetMappedClassName(string collectionName)
        {
            return collectionTypeMap.TryGetValue(collectionName, out var info)
                ? info.@class
                : null;
        }

        public string GetMappedAssemblyName(string collectionName)
        {
            return collectionTypeMap.TryGetValue(collectionName, out var info)
                ? info.assembly
                : "System";
        }

        public BsonDocument CreateTemplateFromClassName(string className, string assemblyName, string name)
        {
            try
            {
                string fullTypeName = $"{className}, {assemblyName}";
                Type type = Type.GetType(fullTypeName);

                if (type != null)
                {
                    object instance = BuildInteractiveObject(type);

                    // Устанавливаем имя вручную, если поле есть
                    var prop = type.GetProperty("Name");
                    if (prop != null && prop.CanWrite)
                        prop.SetValue(instance, name);

                    return instance.ToBsonDocument();
                }

                _mainWindow.LogMessage($"Класс '{className}' не найден в сборке '{assemblyName}'\r\n.", "ConsoleOfDatabaseRequest");
            }
            catch (Exception ex)
            {
                _mainWindow.LogMessage($"Ошибка при создании шаблона: {ex.Message}\r\n", "ConsoleOfDatabaseRequest");
            }

            return new BsonDocument
            {
                { "name", name },
                { "createdAt", DateTime.UtcNow }
            };
        }

        private object BuildInteractiveObject(Type type)
        {
            object instance = Activator.CreateInstance(type);

            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanWrite || !prop.CanRead)
                    continue;

                Type propType = prop.PropertyType;

                // Примитивы и string
                if (propType == typeof(string) || propType.IsPrimitive)
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox(
                        $"Введите значение для {prop.Name} ({propType.Name}):",
                        $"Заполнение {prop.Name}");

                    try
                    {
                        object value = Convert.ChangeType(input, propType);
                        prop.SetValue(instance, value);
                    }
                    catch
                    {
                        _mainWindow.LogMessage($"Ошибка установки значения поля '{prop.Name}'", "ConsoleOfDatabaseRequest");
                    }
                }
                // Вложенный класс
                else if (propType.GetConstructor(Type.EmptyTypes) != null)
                {
                    var nested = BuildInteractiveObject(propType);
                    prop.SetValue(instance, nested);
                }
                // Пропускаем списки и сложные типы пока
            }

            return instance;
        }

        private void InitializationConfiguration()
        {
            try
            {
                if (TargetDirectory != null)
                {
                    _mainWindow.LogMessage($"Создание структуры папок из JSON...\r\n", "ConsoleOfDatabaseRequest");
                    CheckAndSyncMongoStructure();
                }
                else
                {
                    _mainWindow.LogMessage($"Не обнаружена корневая папка.\r\n", "ConsoleOfDatabaseRequest");
                }

                GenerateFiles();
            }
            catch (Exception ex)
            {
                _mainWindow.LogMessage($"Ошибка при чтении конфигурации: {ex.Message}\r\n", "ConsoleOfDatabaseRequest");
            }
        }

        private void CheckAndCreateDatabases(string dataBaseName)
        {
            var client = new MongoClient(Database.Client.Settings);
            var dataBaseNames = client.ListDatabaseNames().ToList();

            string mainDatabaseName = dataBaseName;
            string archiveDatabaseName = "archive";

            if (!dataBaseNames.Contains(mainDatabaseName))
            {
                _mainWindow.LogMessage($"База данных '{mainDatabaseName}' не найдена. Создание новой...\r\n", "ConsoleOfDatabaseRequest");
                client.GetDatabase(mainDatabaseName).CreateCollection("Core");
            }
            else
            {
                _mainWindow.LogMessage($"База данных '{mainDatabaseName}' найдена.\r\n", "ConsoleOfDatabaseRequest");
            }

            if (!dataBaseNames.Contains(archiveDatabaseName))
            {
                _mainWindow.LogMessage($"Архивная база данных '{archiveDatabaseName}' не найдена. Создание новой...\r\n", "ConsoleOfDatabaseRequest");
                client.GetDatabase(archiveDatabaseName).CreateCollection("Core");
            }
            else
            {
                _mainWindow.LogMessage($"Архивная база данных '{archiveDatabaseName}' найдена.\r\n", "ConsoleOfDatabaseRequest");
            }
        }


        private void CheckAndSyncMongoStructure()
        {
            try
            {
                // Проверяем и создаем базы данных, если их нет
                CheckAndCreateDatabases("TestBD");

                var client = new MongoClient(Database.Client.Settings);
                var db = client.GetDatabase("TestBD");

                var comparer = new MongoStructureComparer(db, ConfigFilePath);


                if (comparer.HasStructureDifferences(out var collectionsOnlyInMongo, out var collectionsOnlyInJson))
                {
                    var message = $"Обнаружены различия структуры в базе '{Database.DatabaseNamespace.DatabaseName}':\r\n";

                    if (collectionsOnlyInMongo.Any())
                    {
                        message += "Лишние коллекции в MongoDB (будут перемещены в 'archive'):\r\n";
                        collectionsOnlyInMongo.ForEach(c => message += $" - {c}\r\n");
                    }

                    if (collectionsOnlyInJson.Any())
                    {
                        message += "\nОтсутствующие коллекции в MongoDB (будут созданы):\r\n";
                        collectionsOnlyInJson.ForEach(c => message += $" + {c}\r\n");
                    }

                    message += "\nВыберите действие:";

                    var result = MessageBox.Show(message, "Несоответствие структур", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            ApplyMongoChanges(collectionsOnlyInJson, db);
                            MoveCollectionsToArchive(collectionsOnlyInMongo, db);
                            break;

                        case MessageBoxResult.No:
                            ApplyMongoChanges(collectionsOnlyInJson, db);
                            break;

                        case MessageBoxResult.Cancel:
                            _mainWindow.LogMessage("Синхронизация структуры отменена пользователем.\r\n", "ConsoleOfDatabaseRequest");
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Структуры соответствуют! Изменения не требуются.", "Проверка структуры");
                }
            }
            catch (Exception ex)
            {
                _mainWindow.LogMessage($"Ошибка при обработке базы данных: {ex.Message}\r\n", "ConsoleOfDatabaseRequest");
            }
        }

        private void ApplyMongoChanges(List<string> collectionsOnlyInJson, IMongoDatabase db)
        {
            foreach (var col in collectionsOnlyInJson)
            {
                db.CreateCollection(col);
            }

            MessageBox.Show($"Изменения успешно применены в базе '{db.DatabaseNamespace.DatabaseName}'!", "Готово");
        }


        private void MoveCollectionsToArchive(List<string> collectionsOnlyInMongo, IMongoDatabase db)
        {
            var client = new MongoClient(Database.Client.Settings); // Используем тот же сервер
            var archiveDb = client.GetDatabase("archive"); // Всегда архивируем в "archive"

            foreach (var col in collectionsOnlyInMongo)
            {
                var sourceCollection = db.GetCollection<BsonDocument>(col);
                var archiveCollection = archiveDb.GetCollection<BsonDocument>(col);

                // Копируем документы в архив
                var documents = sourceCollection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                if (documents.Any())
                {
                    archiveCollection.InsertMany(documents);
                }

                // Удаляем коллекцию из основной базы
                db.DropCollection(col);
            }

            MessageBox.Show($"Коллекции успешно перенесены в архивную базу 'archive'!", "Готово");
        }


        private void GenerateFiles()
        {
            Console.WriteLine("Начало генерации файлов...");
            // Здесь можно вызвать методы для генерации файлов (например, персонажи, навыки и аугментации)
            // GenerateCharacterFiles();
            // GenerateSkillFiles();
            // GenerateAugmentationFiles();
            Console.WriteLine("Генерация файлов завершена.");
        }

        public BsonDocument GetElementByName(string collectionName, string elementName)
        {
            var client = new MongoClient(Database.Client.Settings);
            var db = client.GetDatabase("TestBD");
            var collection = db.GetCollection<BsonDocument>(collectionName);

            var filter = Builders<BsonDocument>.Filter.Eq("Name", elementName);


            var result = collection.Find(filter).FirstOrDefault();

            if (result != null)
            {
                _mainWindow.LogMessage($"Элемент '{elementName}' найден в коллекции '{collectionName}' базы '{"TestBD"}'.\r\n", "ConsoleOfDatabaseRequest");
            }
            else
            {
                _mainWindow.LogMessage($"Элемент '{elementName}' не найден в коллекции '{collectionName}' базы '{"TestBD"}'.\r\n", "ConsoleOfDatabaseRequest");
            }

            return result;
        }
        public List<string> GetAllElementNames(string collection)
        {
            var result = new List<string>();
            var documents = Database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToList();

            foreach (var doc in documents)
            {
                if (doc.Contains("Name") && doc["Name"].IsString)
                    result.Add(doc["Name"].AsString);
            }

            return result;
        }
        /*public List<string> GetAllElementNames(string collectionName)
        {
            var client = new MongoClient(Database.Client.Settings);
            var db = client.GetDatabase("gameBD");
            var collection = db.GetCollection<BsonDocument>(collectionName);

            var documents = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();

            var names = new List<string>();

            foreach (var doc in documents)
            {
                if (doc.Contains("name"))
                {
                    names.Add(doc["name"].AsString);
                }
            }

            _mainWindow.LogMessage($"Из коллекции '{collectionName}' получены имена элементов: {string.Join(", ", names)}\r\n", "ConsoleOfDatabaseRequest");

            return names;
        }*/
        public List<string> GetAllCollections()
        {
            var client = new MongoClient(Database.Client.Settings);
            var db = client.GetDatabase("TestBD");

            var collections = db.ListCollectionNames().ToList();

            _mainWindow.LogMessage($"Найдены коллекции в базе данных '{"TestBD"}': {string.Join(", ", collections)}\r\n", "ConsoleOfDatabaseRequest");

            return collections;
        }

    }
}
