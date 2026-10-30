using Zyara.Services.Seeders;
using Zyara.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("Cookies").AddCookie(options => { options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied"; options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; options.Cookie.SameSite = SameSiteMode.Lax; options.ExpireTimeSpan = TimeSpan.FromDays(14); options.SlidingExpiration = true; });
builder.Services.AddZyaraServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseMiddleware<Zyara.Web.Middleware.CspMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }
else { app.UseExceptionHandler("/Home/Error"); }
app.UseAuthentication();
using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<Zyara.Services.Data.ZyaraDbContext>(); AdminSeeder.EnsureAdmin(db, builder.Configuration, app.Environment); }
app.UseAuthorization();
app.MapStaticAssets();

// Map Area routes (e.g. /Provider/Dashboard/Index)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

