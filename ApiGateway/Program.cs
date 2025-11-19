using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Newtonsoft.Json.Linq;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var proxyConfig = builder.Configuration.GetSection("ReverseProxy");
builder.Services.AddReverseProxy()
    .LoadFromConfig(proxyConfig);

// Registrar el servicio de rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        }));
});

/*builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.Cookie.Name = "Auth";
    // Expira después de 30 minutos de inactividad
    options.ExpireTimeSpan = TimeSpan.FromMinutes(2);

    // Renueva la cookie si el usuario está activo
    options.SlidingExpiration = true;

    // Evento para manejar acceso denegado
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {   Console.WriteLine("Validando principal de la cookie...");
            var expiresUtc = context.Properties.ExpiresUtc;
            var now = DateTimeOffset.UtcNow;
            Console.WriteLine($"hola..");
            if (expiresUtc.HasValue && expiresUtc.Value < now)
            {
                Console.WriteLine("La cookie ha expirado.");
                // La cookie expiró: cerrar sesión
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine("La sesion ha expirado.");
            }
        }
    };
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
}).AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.CallbackPath = builder.Configuration["Keycloak:CallbackPath"];

    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters= new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = ClaimTypes.Role

    };
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async ctx =>
        {
            var identity = (ClaimsIdentity)ctx.Principal.Identity;
            var realmAccessClaim = identity.FindFirst("realm_access");

            if (realmAccessClaim != null)
            {
                var roles = JObject.Parse(realmAccessClaim.Value)["roles"];
                if (roles != null)
                {
                    var existingRoles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();

                    foreach (var role in roles)
                    {
                        var roleStr = role.ToString();
                        if (!existingRoles.Contains(roleStr))
                        {
                            Console.WriteLine($"Agregando rol: {roleStr}");
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleStr));
                        }
                    }
                }
            }
        }
    };

});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedOnlyAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrador");
    });
    options.AddPolicy("AuthenticatedUsersOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});
*/
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/*app.Use(async (context, next) =>
{
    Console.WriteLine("Acceso denegado. Redirigiendo al usuario4...");
    var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (result.Succeeded && result.Properties?.ExpiresUtc < DateTimeOffset.UtcNow)
    {
        Console.WriteLine("Acceso denegado. Redirigiendo al usuario3...");
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        context.Response.Redirect("/Account/Login?expired=true");
        return;
    }

    await next();
});



app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        Console.WriteLine("Claims del usuario autenticado:");
        foreach (var claim in context.User.Claims)
        {
            Console.WriteLine($" - {claim.Type}: {claim.Value}");
        }
    }
    else
    {
        Console.WriteLine("Usuario no autenticado.");
    }

    await next();
});*/

//app.UseAuthorization();

app.UseRateLimiter();
app.MapReverseProxy();

app.Run();
