using ApiPeliculas.Data;
using ApiPeliculas.PeliculasMapper;
using AutoMapper;
using ApiPeliculas.Repository;
using ApiPeliculas.Repository.IRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Reflection;
using System.IO;
using Microsoft.OpenApi.Models;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using ApiPeliculas.Helpers;

namespace ApiPeliculas
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(Options => Options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ICategoriaRepository, CategoriaRepository>();

            services.AddScoped<IPeliculaRepository, PeliculaRepository>();

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // Agregar dependencia del token
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => 
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddAutoMapper(typeof(PeliculasMappers));

            //De aqui en adelante configuracion de documentacion de nuestra API
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("ApiPeliculasCategorias", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Title = "API Categor�as Pel�culas",
                    Version = "1",
                    Description = "Backend Pel�culas",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {
                        Email = "matiasdigiovanni15@gmail.com",
                        Name = "Matias",
                        Url = new Uri("https://render2web.com")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://en.wikipedia.org/wiki/Mit_License")
                    }
                });

                options.SwaggerDoc("ApiPeliculas", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Title = "API Peliculas",
                    Version = "1",
                    Description = "Backend Peliculas",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {
                        Email = "matiasdigiovanni15@gmail.com",
                        Name = "Matias",
                        Url = new Uri("https://render2web.com")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://en.wikipedia.org/wiki/Mit_License")
                    }
                });

                options.SwaggerDoc("ApiPeliculasUsuarios", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Title = "API Usuarios Peliculas",
                    Version = "1",
                    Description = "Backend Peliculas",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {
                        Email = "matiasdigiovanni15@gmail.com",
                        Name = "Matias",
                        Url = new Uri("https://render2web.com")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://en.wikipedia.org/wiki/Mit_License")
                    }
                });

                var archivoXmlComentarios = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var rutaApiComentarios = Path.Combine(AppContext.BaseDirectory, archivoXmlComentarios);
                options.IncludeXmlComments(rutaApiComentarios);

                // Primero definir el esquema de seguridad
                options.AddSecurityDefinition("Bearer",
                   new OpenApiSecurityScheme
                   {
                       Description = "Autenticaci�n JWT (Bearer)",
                       Type = SecuritySchemeType.Http,
                       Scheme = "bearer"
                   });

                   options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                   {
                       new OpenApiSecurityScheme
                       {
                           Reference = new OpenApiReference
                           {
                               Id = "Bearer",
                               Type = ReferenceType.SecurityScheme
                           }
                       }, new List<string>()
                   }
                 });
            
            });

            services.AddControllers();

            //Damos soporte para CORS
            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>{
                    builder.Run(async context =>{
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();

                        if (error != null)
                        {
                            context.Response.AddAplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            app.UseHttpsRedirection();
            //Linea para documentacion api
            app.UseSwagger();
            app.UseSwaggerUI(options => 
            {
                options.SwaggerEndpoint("/swagger/ApiPeliculasCategorias/swagger.json", "API Categor�as Pel�culas");
                options.SwaggerEndpoint("/swagger/ApiPeliculas/swagger.json", "API Pel�culas");
                options.SwaggerEndpoint("/swagger/ApiPeliculasUsuarios/swagger.json", "API Usuarios Pel�culas");

                // Estos Endpoint se utilizan en caso de subir proyecto en IIS, se deben de comentar los Endpoint de arriba.
                /*
                options.SwaggerEndpoint("/apiPeliculas/swagger/ApiPeliculasCategorias/swagger.json", "API Categor�as Pel�culas");
                options.SwaggerEndpoint("/apiPeliculas/swagger/ApiPeliculas/swagger.json", "API Pel�culas");
                options.SwaggerEndpoint("/apiPeliculas/swagger/ApiPeliculasUsuarios/swagger.json", "API Usuarios Pel�culas");
                */
                options.RoutePrefix = "";
            });

            app.UseRouting();

            //Estos dos son para la autenticaci�n y autorizaci�n
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //Damos soporte para CORS
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyOrigin().AllowAnyHeader());
        }
    }
}
