using UniversityEnrolment.Models;
using UniversityEnrolment.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Azure Storage configuration (Azurite connection strings by default)
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("AzureStorage"));

builder.Services.AddSingleton<TableStorageService>();
builder.Services.AddSingleton<EnrollmentQueueService>();

// Background worker that processes the CourseEnrollmentQueue
builder.Services.AddSingleton<EnrollmentProcessor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EnrollmentProcessor>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
