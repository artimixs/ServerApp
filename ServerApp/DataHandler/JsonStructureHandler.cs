using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ServerWindow.DataHandler
{
    public class JsonStructureHandler
    {
        public dynamic Structure { get; private set; }

        public JsonStructureHandler(string filePath)
        {
            Structure = JObject.Parse(File.ReadAllText(filePath));
        }

        // Метод для чтения конфигурации из JSON файла
        private static Dictionary<string, object> ReadDirectoryConfig(string configFilePath)
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    Console.WriteLine($"Чтение конфигурации из файла: {configFilePath}");

                    // Считываем JSON как строку
                    var jsonContent = File.ReadAllText(configFilePath);

                    // Десериализуем строку JSON в Dictionary
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
                }
                else
                {
                    Console.WriteLine($"Файл конфигурации не найден: {configFilePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
                return null;
            }
        }
    }
}
