using System;
using System.Collections.Generic;
using System.Linq;
using ADOFAI;

namespace CustomEvents.Event;

/// <summary>Allocates and manages unique runtime arithmetic type identifiers for dynamic custom event definitions.</summary>
/// <remarks>Pushes identifiers out into a high-index memory safe zone starting at <c>100,000</c> to eliminate structural overlap risks against vanilla engine enum maps.</remarks>
public static class CustomEventTypeRegistry
{
    /// <summary>Gets the starting lower boundary point parameter index used for out-of-band custom event tracking types.</summary>
    public const int DefaultValue = 100000;

    private static int _nextValue = DefaultValue;
    
    /// <summary>Increments and issues the next sequential unassigned <see cref="LevelEventType"/> handle block.</summary>
    /// <returns>A unique, newly assigned custom event identification casting index wrapper value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tracking counter exceeds the execution threshold limits of <see cref="int.MaxValue"/>.</exception>
    public static LevelEventType GetNewID()
    {
        return _nextValue >= int.MaxValue ? throw new InvalidOperationException("No more custom event type values available.") : (LevelEventType)_nextValue++;
    }
}

/// <summary>Allocates, manages, and looks up customized UI workspace categories and sorting containers parameters mapping rules.</summary>
public static class CustomCategoryRegistry
{
    static CustomCategoryRegistry()
    {
        RegisterCustomCategory("Uncategorized");
    }
    
    /// <summary>Gets the starting lower boundary point parameter index used for customized category types.</summary>
    public const int Uncategorized = 100000;

    private static readonly Dictionary<string, LevelEventCategory> _customCategories = new();

    /// <summary>Gets the collection containing actively registered categories indexed by their unique descriptive header labels.</summary>
    public static IReadOnlyDictionary<string, LevelEventCategory> CustomCategories => _customCategories;

    private static int _nextValue = Uncategorized;
    
    /// <summary>Validates and stores a unique category classification name string inside the system organization mapping trackers.</summary>
    /// <param name="name">The text literal assigned to display as the grouping segment reference handle key.</param>
    /// <returns>The newly generated or previously associated <see cref="LevelEventCategory"/> value identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when allocation requests surpass the technical limits of <see cref="int.MaxValue"/>.</exception>
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
    
    /// <summary>Performs a standard string key lookup check across active category registries profiles rows lines.</summary>
    /// <param name="name">The exact label string key whose identity mask undergoes matching confirmation evaluations.</param>
    /// <returns>The corresponding <see cref="LevelEventCategory"/> descriptor if found; otherwise, returns <see langword="null"/>.</returns>
    public static LevelEventCategory? GetCustomCategoryValue(string name)
    {
        if (_customCategories.TryGetValue(name, out var value))
        {
            return value;
        }
        
        return null;
    }
    
    /// <summary>Retrieves an existing category identifier or initializes a new one if the text key is unmapped.</summary>
    /// <param name="name">The target structural tag name undergoing identity lookups or registration instantiation pipelines.</param>
    /// <returns>The verified or newly registered category identification cast profile.</returns>
    public static LevelEventCategory GetOrAddCustomCategoryValue(string name)
    {
        return _customCategories.TryGetValue(name, out var value) ? value : RegisterCustomCategory(name);
    }

    private static LevelEventCategory[] _valuesCache;
    
    /// <summary>Synthesizes vanilla built-in categories alongside actively loaded custom category registrations into a unified composite array.</summary>
    /// <remarks>Caches the initial vanilla metadata tracking slice to minimize repetitive array creation operations across active frames loops rendering states.</remarks>
    /// <returns>An array block mapping out every recognized vanilla and extended <see cref="LevelEventCategory"/> instance structural values.</returns>
    public static Array GetValues()
    {
        _valuesCache ??= (LevelEventCategory[])Enum.GetValues(typeof(LevelEventCategory));
        
        return _valuesCache.Concat(CustomCategories.Values.Select(v => v)).ToArray();
    }

    extension(LevelEventCategory category)
    {
        /// <summary>Resolves a legible textual name for the specified category segment identifier.</summary>
        /// <returns>The matching text tag string registered to the category value, falling back on standard enum stringification if missing.</returns>
        public string ToSafeName()
        {
            var customCategory = CustomCategories.FirstOrDefault(kv => kv.Value == category);
            return customCategory.Key ?? category.ToString();
        }
    }
}