﻿using System;
using YamlDotNet.Serialization;

namespace Clasharp.Cli.ClashConfigs;

public class Tun
{
    [YamlMember(Alias = "enable")]
    public bool Enable { get; set; }

    [YamlMember(Alias = "stack")]
    public string Stack { get; set; } = string.Empty;

    [YamlMember(Alias = "dns-hijack")]
    public string[] DnsHijack { get; set; } = Array.Empty<string>();

    [YamlMember(Alias = "auto-route")]
    public bool AutoRoute { get; set; }

    [YamlMember(Alias = "auto-detect-interface")]
    public bool AutoDetectInterface { get; set; }
}