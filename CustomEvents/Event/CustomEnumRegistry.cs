using System;
using System.Collections.Generic;
using System.Linq;
using ADOFAI;

namespace CustomEvents.Event;

public static class CustomEventTypeRegistry
{
    public const int DefaultValue = 100000;

    private static int _nextValue = DefaultValue;
    
    public static LevelEventType GetNewID()
    {
        return _nextValue >= int.MaxValue ? throw new InvalidOperationException("No more custom event type values available.") : (LevelEventType)_nextValue++;
    }
}

public static class CustomCategoryRegistry
{
    static CustomCategoryRegistry()
    {
        RegisterCustomCategory("Uncategorized");
    }
    
    public const int Uncategorized = 100000;

    private static readonly Dictionary<string, LevelEventCategory> _customCategories = new();
    public static IReadOnlyDictionary<string, LevelEventCategory> CustomCategories => _customCategories;

    private static int _nextValue = Uncategorized;
    
    public static LevelEventCategory RegisterCustomCategory(string name)
    {
        if (_customCategories.TryGetValue(name, out var category))
        {
            return category;
        }
        
        if (_nextValue >= int.MaxValue)
        {
            throw new InvalidOperationException("No more custom category values available.");
        }
        
        var newValue = _nextValue++;
        _customCategories[name] = (LevelEventCategory)newValue;
        return (LevelEventCategory)newValue;
    }
    
    public static LevelEventCategory? GetCustomCategoryValue(string name)
    {
        if (_customCategories.TryGetValue(name, out var value))
        {
            return value;
        }
        
        return null;
    }
    
    public static LevelEventCategory GetOrAddCustomCategoryValue(string name)
    {
        return _customCategories.TryGetValue(name, out var value) ? value : RegisterCustomCategory(name);
    }

    private static LevelEventCategory[] _valuesCache;
    
    public static Array GetValues()
    {
        _valuesCache ??= (LevelEventCategory[])Enum.GetValues(typeof(LevelEventCategory));
        
        return _valuesCache.Concat(CustomCategories.Values.Select(v => v)).ToArray();
    }

    extension(LevelEventCategory category)
    {
        public string ToSafeName()
        {
            var customCategory = CustomCategories.FirstOrDefault(kv => kv.Value == category);
            return customCategory.Key ?? category.ToString();
        }
    }
}