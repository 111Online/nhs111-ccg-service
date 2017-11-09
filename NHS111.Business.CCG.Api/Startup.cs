

namespace NHS111.Business.CCG.Api {
    using Autofac;
    using Domain.CCG;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Services;

    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureContainer(ContainerBuilder builder) {
            builder.Register(c => new AzureAccountSettings(Configuration["connection"], Configuration["table"]))
                .As<AzureAccountSettings>()
                .InstancePerDependency();
            builder.RegisterType<CCGRepository>().As<ICCGRepository>();
            builder.RegisterType<CCGService>().As<ICCGService>();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}