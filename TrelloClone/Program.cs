using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.Hubs; // BoardHub i�in gerekli


var builder = WebApplication.CreateBuilder(args);

// Veritaban� ba�lant�s�n� ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity servislerini ekle (kullan�c� y�netimi i�in)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // �ifre gereksinimleri
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    // Email onay� gereksinimleri
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>() // Roller i�in
.AddEntityFrameworkStores<ApplicationDbContext>();

// MVC servislerini ekle
builder.Services.AddControllersWithViews();

// SignalR servisini ekle (ger�ek zamanl� g�ncellemeler i�in)
builder.Services.AddSignalR();

// Session deste�i ekle
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



var app = builder.Build();

// HTTP request pipeline yap�land�rmas�
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session'� aktif et
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// MVC routing - �nemli k�s�m
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API routing i�in ek route
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");

// �zel routing'ler
app.MapControllerRoute(
    name: "card-details",
    pattern: "Card/GetCardDetails/{cardId:int}",
    defaults: new { controller = "Card", action = "GetCardDetails" });

app.MapControllerRoute(
    name: "card-comments",
    pattern: "Card/GetComments/{cardId:int}",
    defaults: new { controller = "Card", action = "GetComments" });

// SignalR Hub'�n� map et
app.MapHub<BoardHub>("/boardHub");

app.Run();