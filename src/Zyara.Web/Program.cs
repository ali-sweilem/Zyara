using Zyara.Repositories.Data.Seed;
using Zyara.Services;
using Zyara.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(o => o.Conventions.Add(new AreaAuthorizationConvention()));

// Everything (DbContext, feature services, Identity + its stubs, IPaymentService) is registered here.
builder.Services.AddZyaraServices(builder.Configuration, builder.Environment);
var app = builder.Build();

// Migrate -> seed Roles -> seed Admin (credentials come from user-secrets: Seed:Admin:Email / Seed:Admin:Password)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(
        scope.ServiceProvider,
        builder.Configuration["Seed:Admin:Email"],
        builder.Configuration["Seed:Admin:Password"]);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();

// Map Area routes (e.g. /Provider/Dashboard/Index)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "landing",
    pattern: "",
    defaults: new { controller = "Landing", action = "Index" });

app.MapControllerRoute(
    name: "home",
    pattern: "home",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
