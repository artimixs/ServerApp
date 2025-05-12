using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrukGameObjects.Abilities;
using UrukGameObjects.Characters;
using UrukGameObjects.Items;

namespace ServerWindow.GameObjects
{
    internal abstract class MainGameObjects
    {
        // Свойства для имени и описания
        public ObjectId Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        // Абстрактный метод, который должен быть реализован в дочерних классах
        public abstract void DisplayInfo();

        private readonly Dictionary<string, Type> collectionClassMap = new Dictionary<string, Type>
        {
            { "Items", typeof(ItemInstance) },
            { "Characters", typeof(Character) },
            { "Skills", typeof(Skill) },
        };
    }
}
