﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clasharp.Clash.Models.Logs;
using Clasharp.Cli.ClashConfigs;
using Clasharp.Models.Settings;
using Clasharp.Services;
using Clasharp.Common;
using Clasharp.Utils;
using Clasharp.Utils.PlatformOperations;
using YamlDotNet.Serialization;
using LogLevel = Clasharp.Clash.Models.Logs.LogLevel;

namespace Clasharp.Cli;

public abstract class ClashCliBase : IClashCli
{
    public IObservable<RawConfig> Config => _config;
    public IObservable<Generated.RunningState> RunningState => _runningState;
    public IObservable<LogEntry> ConsoleLog => _consoleLog;

    protected ReplaySubject<Generated.RunningState> _runningState = new(1);
    protected ReplaySubject<LogEntry> _consoleLog = new(1);
    protected ReplaySubject<RawConfig> _config = new(1);


    protected Regex _logRegex = new(@"\d{2}\:\d{2}\:\d{2}\s+(?<level>\S+)\s*(?<module>\[.+?\])?\s*(?<payload>.+)");
    protected Regex _logMetaRegex = new(@"time=""(.+?) level=(?<level>.+?) msg=""(?<payload>.+)""");

    protected IClashApiFactory ClashApiFactory;
    protected GetClashExePath GetClashExePath = new();
    private readonly IProfilesService _profilesService;
    private readonly AppSettings _appSettings;

    protected Dictionary<string, LogLevel> _levelsMap = new()
    {
        ["DBG"] = LogLevel.DEBUG,
        ["INF"] = LogLevel.INFO,
        ["WRN"] = LogLevel.WARNING,
        ["ERR"] = LogLevel.ERROR,
        ["SLT"] = LogLevel.SILENT,
        ["debug"] = LogLevel.DEBUG,
        ["info"] = LogLevel.INFO,
        ["warning"] = LogLevel.WARNING,
        ["error"] = LogLevel.ERROR,
        ["silent"] = LogLevel.SILENT,
    };

    protected ClashCliBase(IClashApiFactory clashApiFactory, IProfilesService profilesService, AppSettings appSettings)
    {
        ClashApiFactory = clashApiFactory;
        _profilesService = profilesService;
        _appSettings = appSettings;
        _runningState.OnNext(Generated.RunningState.Stopped);
    }

    public async Task Start()
    {
        _runningState.OnNext(Generated.RunningState.Starting);
        try
        {
            var rawConfig = await GenerateConfig();

            await DoStart(GlobalConfigs.RuntimeClashConfig, _appSettings.UseSystemCore);

            var port = (rawConfig.ExternalController ?? "9090").Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Last();
            ClashApiFactory.SetPort(int.Parse(port));
            _config.OnNext(rawConfig);
            _runningState.OnNext(Generated.RunningState.Started);
        }
        catch (Exception)
        {
            _runningState.OnNext(Generated.RunningState.Stopped);
            throw;
        }
    }

    public async Task<RawConfig> GenerateConfig()
    {
        var configYaml = await File.ReadAllTextAsync(await EnsureConfig());
        var configDic =
            new DeserializerBuilder().Build().Deserialize(new StringReader(configYaml)) as
                Dictionary<object, object>;
        MergeManagedFields(configDic);

        var mergedYaml = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build().Serialize(configDic);
        await File.WriteAllTextAsync(GlobalConfigs.RuntimeClashConfig, mergedYaml);
        var rawConfig = new DeserializerBuilder().Build().Deserialize<RawConfig>(mergedYaml);

        var clashWrapper = new ClashWrapper(new ClashLaunchInfo
        {
            ConfigPath = GlobalConfigs.RuntimeClashConfig, ExecutablePath = await GetClashExePath.Exec(_appSettings.UseSystemCore),
            WorkDir = GlobalConfigs.ProgramHome
        });
        clashWrapper.Test();

        return rawConfig;
    }

    private void MergeManagedFields(Dictionary<object, object> rawConfig)
    {
        void CheckAndMergeTrans<T, TV>(ManagedConfigValue<T> managedConfigValue, Func<T, TV> transform)
        {
            if (!managedConfigValue.Enabled || managedConfigValue.Value == null) return;
            rawConfig.Patch(managedConfigValue.Path, transform.Invoke(managedConfigValue.Value));
        }

        void CheckAndMerge<T>(ManagedConfigValue<T> managedConfigValue)
        {
            CheckAndMergeTrans(managedConfigValue, v => v);
        }

        var fields = _appSettings.ManagedFields;
        CheckAndMergeTrans(fields.ExternalControllerPort, i => $"127.0.0.1:{i}");
        CheckAndMerge(fields.Tun);
        CheckAndMerge(fields.AllowLan);
        CheckAndMerge(fields.Ipv6);
        CheckAndMerge(fields.MixedPort);
    }

    protected void CliLogProcessor(string log)
    {
        if (string.IsNullOrEmpty(log)) return;
        var match = _logRegex.Match(log);
        if (!match.Success) match = _logMetaRegex.Match(log);
        _consoleLog.OnNext(match.Success
            ? new LogEntry(_levelsMap[match.Groups["level"].Value], match.Groups["payload"].Value)
            : new LogEntry(LogLevel.INFO, log));
    }

    protected abstract Task DoStart(string configPath, bool useSystemCore);

    public async Task Stop()
    {
        _runningState.OnNext(Generated.RunningState.Stopping);
        await DoStop();
        _runningState.OnNext(Generated.RunningState.Stopped);
    }

    protected abstract Task DoStop();

    private Task<string> EnsureConfig()
    {
        Directory.CreateDirectory(GlobalConfigs.ProgramHome);
        var filename = _profilesService.GetActiveProfile();
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new InvalidDataException("No selected profile");
        }

        var fulPath = Path.Combine(GlobalConfigs.ProfilesDir, filename);
        if (!File.Exists(fulPath))
        {
            throw new InvalidDataException("Selected profile file not exist");
        }

        return Task.FromResult(fulPath);
    }
}