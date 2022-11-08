﻿using System.Collections.Generic;
using System.Reactive;
using System.ServiceProcess;
using ClashGui.Interfaces;
using ClashGui.Models.ServiceMode;
using ClashGui.Models.Settings;
using ClashGui.ViewModels;
using ReactiveUI;

namespace ClashGui.DesignTime;

public class DesignSettingsViewModel:ViewModelBase, ISettingsViewModel
{
    public override string Name => "Settings";
    public string ClashApiAddress { get; set; }
    public SystemProxyMode SystemProxyMode { get; set; }
    public bool UseServiceMode { get; set; }
    public bool IsUninstalled { get; set; }
    public ServiceStatus CoreServiceStatus { get; }
    public List<SystemProxyMode> SystemProxyModes { get; }
    public ReactiveCommand<Unit, Unit> InstallService { get; }
    public ReactiveCommand<Unit, Unit> UninstallService { get; }
}