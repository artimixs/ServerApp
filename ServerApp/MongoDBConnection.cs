using MongoDB.Driver;

namespace ServerWindow
{
    public class MongoDBConnection
    {
        private readonly IMongoDatabase database;

        public MongoDBConnection()
        {
            // Установите соединение с локальной MongoDB
            var client = new MongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("gameBD"); // Название базы данных
        }

        public IMongoDatabase GetDatabase()
        {
            return database;
        }
    }
}
