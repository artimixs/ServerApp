using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServerWindow.DataHandler
{

    public class MongoStructureComparer
    {
        private IMongoDatabase database;
        private JObject jsonStructure;

        public MongoStructureComparer(IMongoDatabase db, string jsonFilePath)
        {
            database = db;
            jsonStructure = JObject.Parse(File.ReadAllText(jsonFilePath));
        }

        public bool HasStructureDifferences(out List<string> collectionsOnlyInMongo, out List<string> collectionsOnlyInJson)
        {
            var mongoCollections = database.ListCollectionNames().ToEnumerable().ToList();
            var jsonCollections = GetJsonCollections(jsonStructure["main"]);

            collectionsOnlyInMongo = Enumerable.Except(mongoCollections, jsonCollections).ToList();
            collectionsOnlyInJson = Enumerable.Except(jsonCollections, mongoCollections).ToList();

            return collectionsOnlyInMongo.Any() || collectionsOnlyInJson.Any();
        }

        private List<string> GetJsonCollections(JToken jsonNode)
        {
            var collections = new List<string>();

            if (jsonNode is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    collections.Add(prop.Name);
                }
            }

            return collections;
        }
    }
}
