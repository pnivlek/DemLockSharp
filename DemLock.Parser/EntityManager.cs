using DemLock.Entities;
using DemLock.Entities.Generated;
using DemLock.Parser.Models;
using DemLock.Utils;

namespace DemLock.Parser;

public class EntityFieldData
{
    public EntityFieldData(string fieldName, object fieldValue)
    {
        FieldName = fieldName;
        FieldValue = fieldValue;
    }

    public string FieldName { get; set; }
    public object FieldValue { get; set; }

    public override string ToString()
    {
        return $"{FieldName}::{FieldValue}";
    }
}

public record EntityMetaData
{
    public required string ClassName { get; set; }
    public required int ClassId { get; set; }
}

/// <summary>
/// Manager for entities to consolidate all of the logic needed to instantiating, and updating
/// the objects. Entities are different types that appear inside of a frame - each representing
/// the state of something in the world. For example, the CCitadelPlayerPawn entity contains details
/// about each player. They are represented as lists of fields, all of which have a nested path
/// (represented as a ReadOnlySpan) inside of a frame.
/// </summary>
public class EntityManager
{
    private DemoParserContext _context;

    /// <summary>
    /// The entities that are being tracked by the system, a simple array would probably work here,
    /// however I have opted for a dictionary, as this makes it easier to reason about, and deals with
    /// cases where there might end up being gaps, which again could be handled but this is easier
    /// just to get things running.
    ///
    /// The second level of the key is the hash of the field paths.
    /// </summary>
    private Dictionary<int, Dictionary<ulong, EntityFieldData>> _entities;

    /// <summary>
    /// This variable stores entities that have been mapped to specific objects, usually because
    /// they are notable and important in some way. A 
    /// </summary>
    private Dictionary<int, BaseEntity> _mappedEntities;

    /// <summary>
    /// The metadata around each entity index in the frame.
    /// </summary>
    private Dictionary<int, EntityMetaData> _metaData;

    /// <summary>
    /// The map of entity indices to <see cref="EntityDecoder"/>.
    /// </summary>
    private Dictionary<int, EntityDecoder> _entityDecoders;

    /// <summary>
    /// This private member stores a cache of field decoders, ready for use with each entity found.
    /// Therefore, we do not have to allocate a new decoder every time we come across every field,
    /// and only have to when we come across a type that has not been seen before. The key is set 
    /// to be a combination of the entity class id along with the field path, which determines 
    /// where in the frame the field is.
    /// </summary>
    private Dictionary<ulong, FieldDecoder> _fieldDecoders;

    private Dictionary<ulong, string> _witness;

    public EntityManager(DemoParserContext context)
    {
        _context = context;
        _entities = new();
        _mappedEntities = new();
        _metaData = new();
        _entityDecoders = new Dictionary<int, EntityDecoder>();
        _fieldDecoders = new();
        _witness = new();
    }

    public void AddNewEntity(int index, DClass serverClass, uint serial)
    {
        var entity = _context.GetSerializerByClassName(serverClass.ClassName)?.Instantiate(serial);
        _entityDecoders[index] = entity;
        _entities[index] = new Dictionary<ulong, EntityFieldData>();
        if (serverClass.ClassName == "CCitadelPlayerPawn")
        {
            _mappedEntities[index] = new CCitadelPlayerPawn();
        }

        _metaData[index] = new EntityMetaData()
        {
            ClassName = serverClass.ClassName,
            ClassId = serverClass.ClassId,
        };
    }

    public void DeleteEntity(int index)
    {
        _entities[index] = null!;
        _entityDecoders[index] = null!;
        _metaData[index] = null!;
        _mappedEntities[index] = null!;
    }

    public void UpdateAtIndex(int index, byte[] entityData)
    {
        var bb = new BitBuffer(entityData);
        UpdateAtIndex(index, ref bb);
    }

    public BaseEntity UpdateAtIndex(int index, ref BitBuffer entityData)
    {
        List<EntityFieldData> entityDataList = new();
        var metaData = _metaData[index];

        Span<FieldPath> fieldPaths = stackalloc FieldPath[512];
        var fp = FieldPath.Default;
        // Keep reading field paths until we reach an op with a null reader.
        // The null reader signifies `FieldPathEncodeFinish`.
        var fpi = 0;
        while (FieldPathEncoding.ReadFieldPathOp(ref entityData) is { Reader: { } reader })
        {
            if (fpi == fieldPaths.Length)
            {
                var newArray = new FieldPath[fieldPaths.Length * 2];
                fieldPaths.CopyTo(newArray);
                fieldPaths = newArray;
            }

            reader.Invoke(ref entityData, ref fp);
            fieldPaths[fpi++] = fp;
        }

        fieldPaths = fieldPaths[..fpi];

        BaseEntity targetEntity = null;

        if (_mappedEntities.ContainsKey(index))
        {
            targetEntity = _mappedEntities[index];
        }

        for (var idx = 0; idx < fieldPaths.Length; idx++)
        {
            var fieldPath = fieldPaths[idx];
            var fieldHash = fieldPath.GetHash(metaData.ClassId);

            var pathSpan = fieldPath.AsSpan();

            var entityDecoder = _entityDecoders[index];
            FieldDecoder fieldDecoder;
            if (_fieldDecoders.TryGetValue(fieldHash, out fieldDecoder))
            {
            }
            else
            {
                _fieldDecoders[fieldHash] = entityDecoder.GetFieldDecoder(pathSpan);
                fieldDecoder = _fieldDecoders[fieldHash];
            }
            var value = fieldDecoder.ReadValue(ref entityData);
            targetEntity?.UpdateProperty(pathSpan, value);


            continue;
            if (metaData.ClassName == "CCitadelPlayerPawn")
            {
                var hash = fieldPath.GetHash();
                string fieldName = null;
                EntityFieldData fieldData;
                if (_entities[index].TryGetValue(hash, out fieldData))
                {
                    if (string.IsNullOrEmpty(fieldData.FieldName))
                        entityDecoder.ReadFieldName(pathSpan, ref fieldName);
                    fieldData.FieldValue = value;
                }
                else
                {
                    entityDecoder.ReadFieldName(pathSpan, ref fieldName);
                }
            }
        }

        return targetEntity;
    }
}
