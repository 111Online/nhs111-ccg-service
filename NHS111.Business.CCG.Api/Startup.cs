﻿namespace NHS111.Business.CCG.Api
{
    using Domain.CCG;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NHS111.Business.CCG.Services;
    using NHS111.Business.CCG.Api.Services;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<IAzureAccountSettings, AzureAccountSettings>(p => new AzureAccountSettings(Configuration["connection"], Configuration["ccgtable"], Configuration["stptable"], Configuration["nationalwhitelistblobname"]));
            services.AddSingleton<IMonitorService>(new MonitorService());
            services.AddSingleton<ICCGRepository, CCGRepository>();
            services.AddSingleton<ISTPRepository, STPRepository>();
            services.AddSingleton<ICCGService, CCGService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}