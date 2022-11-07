using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ClashGui.Cli;
using ClashGui.Common;
using ClashGui.Interfaces;
using ClashGui.Services;
using ClashGui.Utils;
using ClashGui.ViewModels;
using ClashGui.Views;
using ReactiveUI;
using Refit;
using Splat;
using Splat.Autofac;

namespace ClashGui
{
    public partial class App : Application
    {
        public override void Initialize()
        {
           
            
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            SetupSuspensionHost();
            SetupAutofac();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = Locator.Current.GetService<IMainWindowViewModel>(),
                    ClashCli =  Locator.Current.GetService<IClashCli>()
                };
                desktop.ShutdownRequested += (sender, args) =>
                {
                    RxApp.SuspensionHost.AppState = Locator.Current.GetService<ISettingsViewModel>();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void SetupAutofac()
        {
            // https://www.reactiveui.net/docs/handbook/dependency-inversion/custom-dependency-inversion
            var builder = new ContainerBuilder();
            builder.RegisterType<ClashCli>().As<IClashCli>().Keyed<bool>(false).SingleInstance();
            builder.RegisterType<ClashRemoteCli>().As<IClashCli>().Keyed<bool>(true).SingleInstance();
            builder.RegisterType<ClashCliFactory>().As<IClashCliFactory>().SingleInstance();
            builder.RegisterType<ClashApiFactory>().As<IClashApiFactory>().SingleInstance();
            builder.RegisterType<MainWindowViewModel>().As<IMainWindowViewModel>().SingleInstance();
            builder.RegisterType<ProxiesViewModel>().As<IProxiesViewModel>().SingleInstance();
            builder.RegisterType<ClashLogsViewModel>().As<IClashLogsViewModel>().SingleInstance();
            builder.RegisterType<ProxyRulesListViewModel>().As<IProxyRulesListViewModel>().SingleInstance();
            builder.RegisterType<ConnectionsViewModel>().As<IConnectionsViewModel>().SingleInstance();
            builder.RegisterType<ClashInfoViewModel>().As<IClashInfoViewModel>().SingleInstance();
            builder.RegisterType<ProxyGroupListViewModel>().As<IProxyGroupListViewModel>().SingleInstance();
            builder.RegisterType<ProxyProviderListViewModel>().As<IProxyProviderListViewModel>().SingleInstance();
            builder.RegisterType<DashboardViewModel>().As<IDashboardViewModel>().SingleInstance();
            builder.RegisterInstance(RxApp.SuspensionHost.GetAppState<SettingsViewModel>()).As<ISettingsViewModel>().SingleInstance();
            builder.RegisterInstance(RestService.For<IRemoteClash>($"http://localhost:{GlobalConfigs.ClashServicePort}/")).SingleInstance();

            builder.RegisterType<ProxyGroupService>().As<IProxyGroupService>().SingleInstance();
            builder.RegisterType<ProxyProviderService>().As<IProxyProviderService>().SingleInstance();
            builder.RegisterType<ProxyRuleService>().As<IProxyRuleService>().SingleInstance();
            builder.RegisterType<ProxyRuleProviderService>().As<IProxyRuleProviderService>().SingleInstance();
            builder.RegisterType<ConnectionService>().As<IConnectionService>().SingleInstance();
            builder.RegisterType<RealtimeTrafficService>().As<IRealtimeTrafficService>().SingleInstance();
            builder.RegisterType<VersionService>().As<IVersionService>().SingleInstance();

            var autofacDependencyResolver = builder.UseAutofacDependencyResolver();
            builder.RegisterInstance(autofacDependencyResolver);
            autofacDependencyResolver.InitializeReactiveUI();

            // https://github.com/reactiveui/splat/issues/882
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));

            var container = builder.Build();
            var autofacResolver = container.Resolve<AutofacDependencyResolver>();
            autofacResolver.SetLifetimeScope(container);
        }

        private void SetupSuspensionHost()
        {
            // Create the AutoSuspendHelper.
            var suspension = new AutoSuspendHelper(ApplicationLifetime);
            RxApp.SuspensionHost.CreateNewAppState = () => new SettingsViewModel();
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new NewtonsoftJsonSuspensionDriver(GlobalConfigs.MainConfig));
            suspension.OnFrameworkInitializationCompleted();
        }
    }
}