using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pokemonTrainer.Data;
using pokemonTrainer.Models;
using pokemonTrainer.Services;
using System.Text;
using pokemonTrainer.Infrastructure;
using pokemonTrainer.Workers;
namespace pokemonTrainer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddControllers();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient<PokeApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            


            builder.Services

            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })

            .AddEntityFrameworkStores<ApplicationDbContext>();

         
            var jwtSection = builder.Configuration.GetSection("Jwt");

            var jwtKey = jwtSection["Key"]
                         ?? throw new InvalidOperationException("JWT Key is missing.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtKey))
                    };
                });
            builder.Services.AddScoped<JwtTokenService>();
            builder.Services.AddSingleton<PokemonImportStatusService>();
            builder.Services.AddScoped<PokemonImportService>();
            builder.Services.AddHostedService<PokemonStartupImportWorker>();
            builder.Services.AddScoped<DreamTeamService>();
            builder.Services.AddScoped<PokemonService>();
            builder.Services.AddScoped<AuthService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
