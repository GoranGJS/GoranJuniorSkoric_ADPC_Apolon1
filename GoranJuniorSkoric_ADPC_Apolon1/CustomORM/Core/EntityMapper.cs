using System.Collections.Concurrent;

namespace CustomORM.Core;


// Maps entity types to their database metadata using reflection.
// Caches metadata for performance
public static class EntityMapper
{
    
    private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadataCache = new();


    // Gets or creates metadata for an entity type.
    public static EntityMetadata GetMetadata<T>() => GetMetadata(typeof(T));


    // For when type is Not known at compile time
    public static EntityMetadata GetMetadata(Type entityType)
    {
        return _metadataCache.GetOrAdd(entityType, type => new EntityMetadata(type));
    }


    // Clears metadata cache
    public static void ClearCache()
    {
        _metadataCache.Clear();
    }
}
