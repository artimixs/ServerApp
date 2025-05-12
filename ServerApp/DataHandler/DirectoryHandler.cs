using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServerWindow.DataHandler
{
    internal static class DirectoryHandler
    {
        /// <summary>
        /// Инициализирует дерево директорий по структуре из JSON
        /// </summary>
        public static void InitializingANewTree(string baseFolder, string configPath)
        {
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
                Console.WriteLine($"Создана базовая папка: {baseFolder}");
            }

            var structure = ReadDirectoryConfig(configPath);
            if (structure != null)
            {
                CheckAndCreateFolders(baseFolder, structure);
            }
            else
            {
                Console.WriteLine("Конфигурация не найдена или повреждена.");
            }
        }

        /// <summary>
        /// Рекурсивно проверяет и создаёт папки по структуре
        /// </summary>
        public static void CheckAndCreateFolders(string currentPath, Dictionary<string, object> structure)
        {
            foreach (var item in structure)
            {
                string folderName = item.Key;
                string folderPath = Path.Combine(currentPath, folderName);

                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine($"Создаю подкаталог: {folderPath}");
                    Directory.CreateDirectory(folderPath);
                }
                else
                {
                    Console.WriteLine($"Подкаталог уже существует: {folderPath}");
                }

                if (item.Value is JObject nestedObj)
                {
                    var nestedDict = nestedObj.ToObject<Dictionary<string, object>>();
                    CheckAndCreateFolders(folderPath, nestedDict);
                }
            }
        }

        /// <summary>
        /// Читает JSON файл конфигурации директории и возвращает структуру
        /// </summary>
        private static Dictionary<string, object> ReadDirectoryConfig(string configFilePath)
        {
            try
            {
                if (!File.Exists(configFilePath)) return null;

                var jsonContent = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения JSON: {ex.Message}");
                return null;
            }
        }
    }
}
