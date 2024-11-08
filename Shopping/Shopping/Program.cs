using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shopping.Data;
using Shopping.Data.Entities;
using Shopping.Helpers;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        builder.Services.AddDbContext<DataContext>(o =>
        {
            o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });


        builder.Services.AddIdentity<User, IdentityRole>(cfg =>
        {
            cfg.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
            cfg.SignIn.RequireConfirmedEmail = true;
            cfg.User.RequireUniqueEmail = true;
            cfg.Password.RequireDigit = false;
            cfg.Password.RequiredUniqueChars = 0;
            cfg.Password.RequireLowercase = false;
            cfg.Password.RequireNonAlphanumeric = false;
            cfg.Password.RequireUppercase = false;
            cfg.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            cfg.Lockout.MaxFailedAccessAttempts = 3;
            cfg.Lockout.AllowedForNewUsers = true;

        })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<DataContext>();

        //builder.Services.ConfigureApplicationCookie(options =>
        //{
        //    options.LoginPath = "/Account/NotAuthorized";
        //    options.AccessDeniedPath = "/Account/NotAuthorized";
        //});


        //3 maneras de inyectar
        builder.Services.AddTransient<SeedDb>(); //La voy a usar una vez y lo destruye cuando lo utilzan
                                                 //builder.Services.AddScoped<SeeDb>(); //Es que la inyecta cada que necesita y la destruye cuando se deja de ocupar.
                                                 //builder.Services.AddSingleton<SeeDb>(); //Lo inyecta una vez y no lo destruye.

        builder.Services.AddScoped<IUserHelper, UserHelper>();
        builder.Services.AddScoped<ICombosHelper, CombosHelper>();
        builder.Services.AddScoped<IBlogHelper, BlobHelper>();
        builder.Services.AddScoped<IMailHelper, MailHelper>();
        builder.Services.AddScoped<IOrdersHelper, OrdersHelper>();
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation(); //ACTUALIZA LAS VISTAS SIN TENER QUE DEJAR DE CORRER EL PROGRAMA

        var app = builder.Build();
        SeedData();

        void SeedData()
        {
            IServiceScopeFactory scopedFactory = app.Services.GetService<IServiceScopeFactory>();

            using (IServiceScope scope = scopedFactory.CreateScope())
            {
                SeedDb service = scope.ServiceProvider.GetService<SeedDb>();
                service.SeedAsync()
                    .Wait();
            }
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");

            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.UseAuthentication();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
//}
//}
//add-migration
//add-migration AddIndexToCountry
//update-database

