﻿using DemLock.Utils;

namespace DemLock.Entities.Generics;

/// <summary>
/// Base class that marks an object as a generic,
/// For now there really is not to mcuh need for this besides
/// flagging it for debugging, but easy to group it until it becomes a problem
/// </summary>
public abstract class DGeneric: FieldDecoder
{
    public string GenericTypeName { get; set; }
    // TODO: This needs to somehow handle applying binding so that it knows how to handle the generic type
    public DGeneric(string genericTypeName)
    {
        GenericTypeName = genericTypeName;
    }
}