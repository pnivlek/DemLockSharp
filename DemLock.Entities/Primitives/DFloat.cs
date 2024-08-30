﻿using DemLock.Utils;

namespace DemLock.Entities.Primitives;

/// <summary>
/// Represents a float32 in the entity space
/// </summary>
public class DFloat: DPrimitive
{
    /// <summary>
    /// The network name for the field type for checking what serializer to use
    /// </summary>
    public const string NetworkName = "float32";
    public float Value { get; set; }
    private FieldEncodingInfo _encodingInfo;

    public DFloat(FieldEncodingInfo encodingInfo)
    {
        _encodingInfo = encodingInfo;
    }

    public override void SetValue(object value)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(ReadOnlySpan<int> path, ref BitBuffer bs)
    {
        if(_encodingInfo != null)
        {
            switch (_encodingInfo.VarEncoder)
            {
                case "coord":
                    Value = bs.ReadCoord();
                    return;
                case "simtime":
                    Value = (DecodeSimulationTime(ref bs));
                    return;
                case "runetime":
                    Value = DecodeRuneTime(ref bs);
                    return;
                case null:
                    break;
                default:
                    throw new Exception($"Unknown float encoder: {_encodingInfo.VarEncoder}");
            }
        }

        if (_encodingInfo.BitCount <= 0 || _encodingInfo.BitCount >= 32)
        {
            Value =  bs.ReadFloat();
            return;
        }
        
        var encoding = QuantizedFloatEncoding.Create(_encodingInfo);
        Value = encoding.Decode(ref bs);
    }
 internal static float DecodeSimulationTime(ref BitBuffer buffer)
    {
        // Assume a 64 tick server... this will need to be set somehow
        var ticks = buffer.ReadVarUInt32();
        return ticks / 64.0f;
    }

    private static float DecodeRuneTime(ref BitBuffer buffer)
    {
        var bits = buffer.ReadUInt(4);
        unsafe
        {
            return *(float*)&bits;
        }
    }
    public override object GetValue() => Value;

}