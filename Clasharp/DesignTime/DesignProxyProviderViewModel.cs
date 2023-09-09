﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Clasharp.Clash.Models.Providers;
using Clasharp.Clash.Models.Proxies;
using Clasharp.Interfaces;
using Clasharp.Models.Proxies;
using Clasharp.ViewModels;
using ReactiveUI;

namespace Clasharp.DesignTime;

public class DesignProxyProviderViewModel : ViewModelBase, IProxyProviderViewModel
{
    public DesignProxyProviderViewModel()
    {
        Proxies = ProxyProvider.Proxies.Select(pg => new ProxyGroupExt(pg)).ToList();
    }

    public bool IsLoading { get; set; }

    public ProxyProvider ProxyProvider => new ProxyProvider()
    {
        Name = "ssg", Proxies = new List<ProxyGroup>()
        {
            new ProxyGroup()
            {
                All = new List<string>() {"asdf", "asdfasdf", "fgsdfg"},
                History = new List<ProxyHistory>() {new ProxyHistory() {Delay = 123, Time = DateTime.Now}},
                Name = "asdasd",
                Now = "asdfasdf",
                Type = ProxyGroupType.Trojan,
                Udp = true
            }
        },
        Type = "trojan", VehicleType = VehicleType.HTTP, UpdatedAt = DateTime.Now
    };

    public List<ProxyGroupExt> Proxies { get; }
}