using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Quartz;
using SilkierQuartz;
using SilkierQuartz.Example;
using SilkierQuartz.Example.Jobs;
using System.Configuration;
using System.Reflection;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddRazorPages();

var services = builder.Services;
var Configuration =builder.Configuration;


var quartzConfiguration = Configuration.GetSection("Quartz");

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
}
    ,
         authenticationOptions =>
         {
             authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous;
         },
         stdSchedulerFactoryOptions =>
         {
             stdSchedulerFactoryOptions.Add(quartzConfiguration
                 .Get<Dictionary<string, string>>()
                 .ToNameValueCollection());
         },
         () => AssemblyHelper.GetAssembliesFromExecutionFolder(Assembly.GetExecutingAssembly())

            );
services.AddOptions();
services.Configure<AppSettings>(Configuration);
services.Configure<InjectProperty>(options => { options.WriteText = "This is inject string"; });
services.AddQuartzJob<HelloJob>()
        .AddQuartzJob<InjectSampleJob>()
        .AddQuartzJob<HelloJobSingle>()
        .AddQuartzJob<InjectSampleJobSingle>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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

app.UseAuthentication();
app.UseAuthorization();
app.UseSilkierQuartz();
app.MapRazorPages();
#region  不使用 SilkierQuartzAttribe 属性的进行注册和使用的IJob，这里通过UseQuartzJob的IJob必须在  ConfigureServices进行AddQuartzJob

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

using (var scope = app.Services.CreateScope())
{
    var services2 = scope.ServiceProvider;    

    try
    {        
        var context = services2.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {     
        throw;
    }
}

app.Run();
