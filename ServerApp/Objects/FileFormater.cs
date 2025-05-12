using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrukGameObjects.Abilities;
using UrukGameObjects.Characters;

namespace ServerWindow.GameObjects
{
    public static class FileFormatter
    {
        // Преобразование данных персонажа в текст для сохранения в .txt файлы
        internal static string CharacterToText(Character character)
        {
            Console.WriteLine($"Преобразование данных персонажа в текст: {character.Name}");
            var augmentationsText = character.Augmentations != null && character.Augmentations.Any()
                ? string.Join("\n", character.Augmentations.Select(a => a.VisibleText))
                : "Нет аугментаций";

            var skillsText = character.Skills != null && character.Skills.Any()
                ? string.Join("\n", character.Skills.Select(s => s.VisibleText))
                : "Нет навыков";

            var inventorySlots = character.Inventory?.Slots != null
                ? string.Join("\n", character.Inventory.Slots.Select((s, index) => $"Слот {index + 1}: {s.Item.Prototype.Name}"))
                : "Инвентарь пуст";

            return $"Имя: {character.Name}\nРост: {character.Height}\nВозраст: {character.Age}\n\n" +
                   "=====================================================================================================\n" +
                   $"Аугментации:\n\n{augmentationsText}\n\n" +
                   "=====================================================================================================\n" +
                   $"Характеристики:\n\n" +
                   $"Здоровье - {character.Stats.Health}\n" +
                   $"Броня - {character.Stats.Armor}\n" +
                   $"Щиты - {character.Stats.Shields}\n\n" +
                   $"Сила - {character.Stats.Strength}\n" +
                   $"Проворность - {character.Stats.Agility}\n" +
                   $"Интеллект - {character.Stats.Intelligence}\n" +
                   $"Харизма - {character.Stats.Charisma}\n" +
                   $"Управление - {character.Stats.Control}\n" +
                   $"Интуиция - {character.Stats.Intuition}\n\n" +
                   "=====================================================================================================\n" +
                   $"Навыки:\n\n{skillsText}\n\n" +
                   "=====================================================================================================\n" +
                   $"Инвентарь:\n\n{inventorySlots}\n" +
                   "=====================================================================================================";
        }

        // Преобразование данных навыка в текст
        internal static string SkillToText(Skill skill)
        {
            Console.WriteLine($"Преобразование данных навыка в текст: {skill.Name}");
            var levelsText = skill.Levels != null && skill.Levels.Any()
                ? string.Join("\n", skill.Levels.Select(l => $"* Уровень {l.Level}: {l.Effect}"))
                : "Нет уровней";

            return $"Категория: {skill.FrequencyOnMissions}, {skill.Complexity}, {skill.FrequencyOnWorld}, {skill.Type}\n" +
                   $"Нарратив: {skill.Narrative}\n" +
                   $"Требование: {skill.Requirement}\n" +
                   $"{levelsText}";
        }

        // Преобразование данных аугментации в текст
        internal static string AugmentationToText(Augmentation augmentation)
        {
            Console.WriteLine($"Преобразование данных аугментации в текст: {augmentation.Name}");
            var levelsText = augmentation.Levels != null && augmentation.Levels.Any()
                ? string.Join("\n", augmentation.Levels.Select(l => $"* Уровень {l.Level}: {l.Effect}"))
                : "Нет уровней";

            return $"Категория: {augmentation.FrequencyOnMissions}, {augmentation.Complexity}, {augmentation.FrequencyOnWorld}, {augmentation.Type}\n" +
                   $"Нарратив: {augmentation.Narrative}\n" +
                   $"Требование: {augmentation.Requirement}\n" +
                   $"{levelsText}";
        }
    }
}
