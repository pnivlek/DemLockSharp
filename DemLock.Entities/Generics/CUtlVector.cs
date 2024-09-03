﻿using System.Text;
using DemLock.Utils;

namespace DemLock.Entities.Generics;

public class CUtlVector: DGeneric
{
    public int Size { get; set; }
    private Func<DObject> _typeFactory;
    public Dictionary<int, DObject?> Data { get; set; }
    public CUtlVector(string genericTypeName, Func<DObject> typeFactory) : base(genericTypeName)
    {
        GenericTypeName = genericTypeName;
        _typeFactory = typeFactory;
        Data = new();
    }

    public override void SetValue(object value)
    {
        throw new NotImplementedException($"CUtlVector::SetValue(Object) is not implemented for {GenericTypeName}");
    }

    public override void SetValue(ReadOnlySpan<int> path, ref BitBuffer bs)
    {
        if (path.Length == 0)
        {
            Size = (int)bs.ReadVarUInt32();
        }

        if (path.Length >= 1)
        {
            var i = path[0];
            if (!Data.ContainsKey(i)) Data.Add(i, _typeFactory());
            Data[i].SetValue(path[1..], ref bs);
        }

    }

    public override string ToJson()
    {
        StringBuilder sb = new();
        sb.AppendLine("{");
        sb.AppendLine("\"@FieldType\": \"" + "CNetworkUtlVectorBase" + "\",");
        sb.AppendLine("\"@GenericType\": \"" + GenericTypeName + "\",");
        sb.AppendLine($"\"@Size\": \"{Size}\"");
        sb.AppendLine(",\"Values\": [");
        if (Data.Count > 0)
        {
            List<string> il = new List<string>();
            foreach (var kv in Data)
            {
                if (kv.Value != null)
                {
                    il.Add(kv.Value.ToJson());
                }
            }

            sb.AppendLine(string.Join(",", il));
        }

        sb.Append("]");
        sb.AppendLine("}");

        return sb.ToString();
    }
    public override object GetValue()
    {
        return Data;
    }
}