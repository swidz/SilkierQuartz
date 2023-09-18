using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using SilkierQuartz.Example.Jobs;
using System.Collections.Generic;
using System.Reflection;

namespace SilkierQuartz.Example
{
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
            services.AddRazorPages();
            services.AddSilkierQuartz(options =>
            {
                options.VirtualPathRoot = "/quartz";
                options.UseLocalTime = true;
                options.DefaultDateFormat = "yyyy-MM-dd";
                options.DefaultTimeFormat = "HH:mm:ss";
                options.CronExpressionOptions = new CronExpressionDescriptor.Options()
                {
                    DayOfWeekStartIndexZero = false //Quartz uses 1-7 as the range
                };
            },
            authenticationOptions =>
            {
                authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous;
            },
            null,
            () => AssemblyHelper.GetAssembliesFromExecutionFolder(Assembly.GetExecutingAssembly())
            );

            services.AddOptions();
            services.Configure<AppSettings>(Configuration);
            services.Configure<InjectProperty>(options => { options.WriteText = "This is inject string"; });
            services.AddQuartzJob<HelloJob>()
                    .AddQuartzJob<InjectSampleJob>()
                    .AddQuartzJob<HelloJobSingle>()
                    .AddQuartzJob<InjectSampleJobSingle>();
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
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSilkierQuartz();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
            //How to compatible old code to SilkierQuartz
            //���ɵ�ԭ���Ĺ滮Job�Ĵ��������ֲ���ݵ�ʾ��
            // app.SchedulerJobs();


            #region  ��ʹ�� SilkierQuartzAttribe ���ԵĽ���ע���ʹ�õ�IJob������ͨ��UseQuartzJob��IJob������  ConfigureServices����AddQuartzJob

            app.UseQuartzJob<HelloJobSingle>(TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever()))
            .UseQuartzJob<InjectSampleJobSingle>(() =>
            {
                return TriggerBuilder.Create()
                   .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever());
            });

            app.UseQuartzJob<HelloJob>(new List<TriggerBuilder>
                {
                    TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever()),
                    TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(2).RepeatForever()),
                     //Add a sample that uses 1-7 for dow
                    TriggerBuilder.Create()
                                  .WithCronSchedule("0 0 2 ? * 7 *"),
                });

            app.UseQuartzJob<InjectSampleJob>(() =>
            {
                var result = new List<TriggerBuilder>();
                result.Add(TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()));
                return result;
            });
            #endregion
        }
    }
}
