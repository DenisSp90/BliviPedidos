using BliviPedidos.Data;
using BliviPedidos.Middleware;
using BliviPedidos.Models;
using BliviPedidos.Seguranca;
using BliviPedidos.Services.Implementations;
using BliviPedidos.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Licensing;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// O arquivo é opcional: quando existir na conta que executa o processo, fornece
// os segredos também fora de Development. Em produção, variáveis de ambiente
// continuam sendo a fonte recomendada e têm prioridade por serem adicionadas depois.
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var syncfusionLicenseKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(syncfusionLicenseKey))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
}

// Add services to the container.

// MYSQL
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<ReservaEstoqueOptions>(builder.Configuration.GetSection(ReservaEstoqueOptions.Secao));
builder.Services.Configure<ConfirmacaoConsumidorOptions>(builder.Configuration.GetSection(ConfirmacaoConsumidorOptions.Secao));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var confirmacaoConsumidor = builder.Configuration
    .GetSection(ConfirmacaoConsumidorOptions.Secao)
    .Get<ConfirmacaoConsumidorOptions>() ?? new ConfirmacaoConsumidorOptions();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedEmail = confirmacaoConsumidor.ExigirEmail;
        options.SignIn.RequireConfirmedPhoneNumber = confirmacaoConsumidor.ExigirTelefone;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AdicionarPoliticasAutorizacao();

builder.Services.AddControllersWithViews();

builder.Services.
    AddControllersWithViews().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.
    AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddTransient<IProdutoService, ProdutoService>();
builder.Services.AddTransient<IPedidoService, PedidoService>();
builder.Services.AddTransient<IItemPedidoService, ItemPedidoService>();
builder.Services.AddTransient<ICadastroService, CadastroService>();
builder.Services.AddTransient<IEmailEnviarService, EmailEnviarService>();
builder.Services.AddScoped<INotificacaoPedidoService, NotificacaoPedidoService>();
builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();
builder.Services.AddHttpClient<IEnvioSmsService, EnvioSmsService>();
builder.Services.AddTransient<IClienteService, ClienteService>();

builder.Services.AddTransient<IRelatorioService, RelatorioService>();
builder.Services.AddTransient<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IBackupLojaService, BackupLojaService>();
builder.Services.AddScoped<IRestauracaoBackupLojaService, RestauracaoBackupLojaService>();
builder.Services.AddScoped<IResetLojaService, ResetLojaService>();
builder.Services.AddScoped<ILojaAtualService, LojaAtualService>();
builder.Services.AddScoped<ICarrinhoPublicoService, CarrinhoPublicoService>();
builder.Services.AddScoped<ICalculadorCarrinhoPublicoService, CalculadorCarrinhoPublicoService>();
builder.Services.AddScoped<IDadosConsumidorCheckoutService, DadosConsumidorCheckoutService>();
builder.Services.AddScoped<IConfirmacaoCheckoutService, ConfirmacaoCheckoutService>();
builder.Services.AddSingleton<IConfiguracaoReservaLojaService, ConfiguracaoReservaLojaService>();
builder.Services.AddScoped<IPagamentoService, PagamentoPixService>();
builder.Services.AddScoped<IReciboPedidoService, ReciboPedidoWordService>();
builder.Services.AddHostedService<ExpiracaoReservaService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.Name = "Blivi.Session";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    opt.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Muitas solicitações. Aguarde alguns instantes e tente novamente.",
            cancellationToken);
    };

    options.AddPolicy(PoliticasRateLimit.CarrinhoPublico, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            ObterChaveRateLimit(httpContext),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy(PoliticasRateLimit.CheckoutPublico, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            ObterChaveRateLimit(httpContext),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 10,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy(PoliticasRateLimit.ConfirmacaoCheckoutPublico, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ObterChaveRateLimit(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy(PoliticasRateLimit.ContaConsumidor, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ObterChaveRateLimit(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5174") // URL do seu frontend Vue.js
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Configurar suporte ? cultura
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseCors("AllowVueApp");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<LojaAtualMiddleware>();
app.UseAuthorization();
app.UseSession();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers(); 

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        // O projeto usa migrations. EnsureCreated não grava o histórico das
        // migrations e faz a migration inicial falhar com "table already exists".
        await dbContext.Database.MigrateAsync();
        await InicializadorSistema.InicializarAsync(services, app.Configuration);
    }
    catch (Exception ex)
    {
        // Log ou tratamento de erro
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Não foi possível inicializar o banco e o administrador do sistema.");
        throw;
    }
}

app.Run();

static string ObterChaveRateLimit(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "endereco-desconhecido";
