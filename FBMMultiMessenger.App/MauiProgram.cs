using Blazored.LocalStorage;
using FBMMultiMessenger.AuthorizationPolicies.ActiveSubscriptionPolicy;
using FBMMultiMessenger.Database;
using FBMMultiMessenger.Database.Services;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using OneSignalSDK.DotNet;
using System;
using System.Reflection;


namespace FBMMultiMessenger
{
    public static class MauiProgram
    {

        public static MauiApp CreateMauiApp()
        {

            try
            {
                var builder = MauiApp.CreateBuilder();
                builder.UseMauiApp<App>()
                       .ConfigureFonts(fonts =>
                       {
                           fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                       });

                builder.UseSentry(options =>
                {
                    options.SendDefaultPii = true;
                    options.Dsn = "https://22e97d6953cd92ecb6fb6d5e4948b90a@o4510596251385856.ingest.us.sentry.io/4510599401897984";
                    options.MinimumBreadcrumbLevel = LogLevel.Debug;
                    options.MinimumEventLevel = LogLevel.Warning;
                    options.AttachStacktrace = true;
                    options.DiagnosticLevel = SentryLevel.Error;
                    options.TracesSampleRate = 0.2;
                });


                // Load appsettings.json from root
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("FBMMultiMessenger.appsettings.json");

                if (stream != null)
                {
                    var config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();

                    builder.Configuration.AddConfiguration(config);
                }

                builder.Services.AddMauiBlazorWebView();
                builder.Services.AddScoped<IBaseService, BaseService>();

                builder.Services.AddScoped<IAuthService, AuthService>();

                builder.Services.AddScoped<IAccountService, AccountService>();
                builder.Services.AddScoped<ITokenProvider, TokenProvider>();
                builder.Services.AddScoped<IChatMessagesService, ChatMessageService>();
                builder.Services.AddScoped<ILocalServerService, LocalServerService>();
                builder.Services.AddScoped<ISubscriptionSerivce, SubscriptionService>();
                builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddScoped<IDefaultMessageService, DefaultMessageService>();
                builder.Services.AddScoped<IProfileService, ProfileService>();
                builder.Services.AddScoped<IPricingService, PricingService>();
                builder.Services.AddScoped<IPaymentService, PaymentService>();
                builder.Services.AddScoped<IProxyService, ProxyService>();
                builder.Services.AddScoped<IAppService, AppService>();
                builder.Services.AddScoped<IFacebookService, FacebookService>();
                builder.Services.AddScoped<ChatEventDispatcherService, ChatEventDispatcherService>();
                builder.Services.AddScoped<ISyncMessagesService, SyncMessagesService>();
                
                builder.Services.AddSingleton<MessageDbService>();
                builder.Services.AddSingleton<SyncMessagesDbService>();

                builder.Services.AddSingleton<BackButtonService>();
                builder.Services.AddSingleton<SignalRService>();
                builder.Services.AddHttpClient();
                builder.Services.AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                    config.SnackbarConfiguration.PreventDuplicates = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.VisibleStateDuration = 4000;
                    config.SnackbarConfiguration.HideTransitionDuration = 500;
                    config.SnackbarConfiguration.ShowTransitionDuration = 500;
                    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                    config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
                });
                builder.Services.AddBlazoredLocalStorage();

                builder.Services.AddAuthorizationCore(options =>
                {
                    options.AddPolicy("ValidSubscription", policy =>
                    {
                        policy.RequireAuthenticatedUser(); // This will fail if token doesn't exist
                        policy.AddRequirements(new ActiveSubscriptionRequirement());
                    });
                });

                builder.Services.AddScoped<IAuthorizationHandler, ActiveSubscriptionRequirementHandler>();

#if DEBUG
                builder.Services.AddBlazorWebViewDeveloperTools();
                builder.Logging.AddDebug();
#endif

                if (PlatformHelper.IsMobilePlatform)
                {
                    var appId = builder.Configuration.GetValue<string>("OneSignal:AppId")!;
                    OneSignal.Initialize(appId);
                }

                builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
                    options.UseSqlite($"Data Source={DBPathHelper.GetDbPath()}"));

                var app = builder.Build();

                // Run migrations here
                using var db = app.Services
                                  .GetRequiredService<IDbContextFactory<MessengerDbContext>>()
                                  .CreateDbContext();
                db.Database.Migrate();

                return app;
            }
            catch (Exception ex)
            {

                throw;
            }


        }
    }
}
