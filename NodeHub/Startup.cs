using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Node.Web.Services;
using Node.Web.Services.Interfaces;
using NodeHub.Core;
using NodeHub.Hubs;
using NodeHub.Services;
using NodeHub.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MILogger = Microsoft.Extensions.Logging.ILogger;

namespace NodeHub
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }
        public BigInteger[] IDS = new BigInteger[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSignalR();
            services.AddSingleton(typeof(MILogger), LoggerFactory.Create(b => b.AddSerilog()).CreateLogger<Startup>());

            services.AddTransient<IStorageService, StorageService>();
            services.AddTransient<IStatesService, StatesService>();
            services.AddTransient<IOverlayService, OverlayService>();
            services.AddTransient<IConnectionService, ConnectionService>();
            services.AddTransient<IReplicationService, ReplicationService>();
            services.AddSingleton<INetworkingService, NetworkingService>();

            foreach(var id in IDS)
            {
                services.AddSingleton<INodeService, NodeService>(p =>
                {
                    var node = new NodeService(
                        p.GetRequiredService<INetworkingService>(),
                        p.GetRequiredService<IStatesService>(),
                        p.GetRequiredService<IOverlayService>(),
                        p.GetRequiredService<IConnectionService>(),
                        p.GetRequiredService<IStorageService>(),
                        p.GetRequiredService<IReplicationService>(),
                        p.GetRequiredService<IHubContext<CommonHub, ICommonHub>>(),
                        p.GetRequiredService<MILogger>())
                    { ID = id };
                    
                    return node;
                });
                services.AddSingleton<IHostedService>(p => p.GetRequiredService<IEnumerable<INodeService>>().First(x => x.ID == id));
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:80")
                        .AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<CommonHub>("/commonhub");
            });
        }
    }
}
