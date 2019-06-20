using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if UNITY_EDITOR
using UniRx;
using UnityEngine;

#endif

namespace HiddenSwitch.Networking.Peers.Internal
{
    public class SignalRServer<THub> where THub : Hub
    {
        public static IWebHost Create(string url = "http://localhost:8001/")
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup>()
                .Build();
        }

        public static IWebHost Create<TSingleton1>(string url = "http://localhost:8001/") where TSingleton1 : class
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup<TSingleton1>>()
                .Build();
        }

        public class Startup
        {
            public IConfiguration Configuration { get; }

            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSignalR();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                app.UseSignalR(routes => { routes.MapHub<THub>(HubConnectionExtensions.Path()); });
            }
        }

        public class Startup<TSingleton> where TSingleton : class
        {
            public IConfiguration Configuration { get; }

            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSignalR();
                services.AddSingleton<TSingleton>();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                app.UseSignalR(routes => { routes.MapHub<THub>(HubConnectionExtensions.Path()); });
#if UNITY_EDITOR
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(context =>
                    {
                        var handlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                        // Hoisting us back into the Unity main thread
                        var exception = $"{handlerFeature.Error.Message}\n{handlerFeature.Error.StackTrace}";
                        Observable.Start(() => { Debug.LogError(exception); })
                            .ObserveOnMainThread()
                            .SubscribeOnMainThread();

                        return Task.CompletedTask;
                    });
                });
#endif
            }
        }
    }
}