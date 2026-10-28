using Aplication.Configuration;
using Aplication.Interfaces;
using Aplication.Services;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios de MVC ────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// ─── MongoDB ─────────────────────────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// ─── Repositorios ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IInformativePageRepository, InformativePageRepository>();
builder.Services.AddScoped<IImageMetadataRepository, ImageMetadataRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();

// ─── Seguridad del contenido editorial ────────────────────────────────────────
builder.Services.Configure<ContentSecurityOptions>(
    builder.Configuration.GetSection(ContentSecurityOptions.SectionName));
builder.Services.AddSingleton<IHtmlSanitizer, GanssHtmlSanitizer>();
builder.Services.AddScoped<ContentBodySanitizer>();

// ─── Servicios de aplicación ──────────────────────────────────────────────────
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ─── Almacenamiento de imágenes ───────────────────────────────────────────────
builder.Services.AddScoped<LocalStorage>();
builder.Services.AddScoped<S3Storage>();
builder.Services.AddScoped<CloudflareR2Storage>();
builder.Services.AddScoped<IImageStorage>(sp => sp.GetRequiredService<S3Storage>());
builder.Services.AddScoped<ImageService>();


// ─── Autenticación por cookies ────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAntiforgery();

// ─── Configuración de appsettings ─────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

var app = builder.Build();

// ─── Inicialización de índices MongoDB (startup asíncrono) ───────────────────
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await MongoDbIndexer.EnsureIndexesAsync(ctx);

    // Migración idempotente: páginas informativas con el campo legado "Content" (HTML) → "Blocks"
    var migratedPages = await MongoDbIndexer.MigrateLegacyPageContentAsync(ctx);
    if (migratedPages > 0)
    {
        app.Logger.LogInformation(
            "Migración de páginas informativas: {Count} documento(s) convertidos del campo legado 'Content' a bloques estructurados.",
            migratedPages);
    }

    await MongoDbIndexer.EnsureSeedDataAsync(ctx);
}

// ─── Pipeline HTTP ────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();

// Si se configuró una ruta física para LocalStorage en VPS fuera de wwwroot, servirla bajo RequestPath
var customUploadsPath = app.Configuration["LocalStorage:BasePath"];
var customUploadsUrl = app.Configuration["LocalStorage:BaseUrl"] ?? "/uploads";
if (!string.IsNullOrWhiteSpace(customUploadsPath))
{
    var resolvedPath = Path.IsPathRooted(customUploadsPath)
        ? customUploadsPath
        : Path.Combine(app.Environment.ContentRootPath, customUploadsPath);

    Directory.CreateDirectory(resolvedPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(resolvedPath),
        RequestPath = new PathString(customUploadsUrl.StartsWith('/') ? customUploadsUrl : "/" + customUploadsUrl)
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// Ruta del área Admin
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" })
    .WithStaticAssets();

// Ruta pública por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
