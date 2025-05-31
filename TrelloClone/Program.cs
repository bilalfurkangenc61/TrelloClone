using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Data;
using TrelloClone.Models;
using TrelloClone.Hubs; // BoardHub için gerekli


var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlantýsýný ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity servislerini ekle (kullanýcý yönetimi için)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Þifre gereksinimleri
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    // Email onayý gereksinimleri
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>() // Roller için
.AddEntityFrameworkStores<ApplicationDbContext>();

// MVC servislerini ekle
builder.Services.AddControllersWithViews();

// SignalR servisini ekle (gerçek zamanlý güncellemeler için)
builder.Services.AddSignalR();

// Session desteði ekle
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



var app = builder.Build();

// HTTP request pipeline yapýlandýrmasý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session'ý aktif et
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// MVC routing - önemli kýsým
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API routing için ek route
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");

// Özel routing'ler
app.MapControllerRoute(
    name: "card-details",
    pattern: "Card/GetCardDetails/{cardId:int}",
    defaults: new { controller = "Card", action = "GetCardDetails" });

app.MapControllerRoute(
    name: "card-comments",
    pattern: "Card/GetComments/{cardId:int}",
    defaults: new { controller = "Card", action = "GetComments" });

// SignalR Hub'ýný map et
app.MapHub<BoardHub>("/boardHub");

app.Run();