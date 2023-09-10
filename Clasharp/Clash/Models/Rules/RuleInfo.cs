﻿using System;

namespace Clasharp.Clash.Models.Rules;

public sealed class RuleInfo
{
    public string Type { get; set; } = string.Empty;


    public string Payload { get; set; } = string.Empty;


    public string Proxy { get; set; } = string.Empty;

    private bool Equals(RuleInfo other)
    {
        return Type == other.Type && Payload == other.Payload && Proxy == other.Proxy;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RuleInfo) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Payload, Proxy);
    }
}