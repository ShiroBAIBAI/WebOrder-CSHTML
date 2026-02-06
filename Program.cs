global using Demo;
global using Demo.Models;
using Demo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

try
{
    var settingsType = global::System.Type.GetType("QuestPDF.Settings, QuestPDF");
    var licenseProp = settingsType?.GetProperty(
        "License",
        global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static
    );
    var enumType = global::System.Type.GetType("QuestPDF.Infrastructure.LicenseType, QuestPDF");

    if (settingsType != null && licenseProp != null && enumType != null)
    {
        var community = global::System.Enum.Parse(enumType, "Community");
        licenseProp.SetValue(null, community);
    }
}
catch
{

}

// Services 
builder.Services.AddControllersWithViews();
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"]; builder.Services.AddHttpClient();
builder.Services.AddScoped<PriceService>();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\CafeDB.mdf;
    Initial Catalog=CafeDB;
    Integrated Security=True;
");

builder.Services.AddScoped<Helper>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddTransient<InvoiceService>();
builder.Services.AddAuthentication()
    .AddCookie(o =>
    {
        o.AccessDeniedPath = "/Account/Denied";
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddSession(o =>        //DELETE This will cause error because cannot get session 
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.Name = ".Cafe.Session";
});

var app = builder.Build();

// Database Migration 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    db.Database.Migrate();
}

// Middleware order
app.UseHttpsRedirection();
app.UseRequestLocalization("en-MY");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();     //cart will not function when deleted
app.UseAuthorization(); //bind with userouting() 
app.MapDefaultControllerRoute();

app.Run();
