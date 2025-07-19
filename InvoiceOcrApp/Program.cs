using InvoiceOcr.Data;
using InvoiceOcr.Repositories.InvoiceDetailRepository;
using InvoiceOcr.Repositories.InvoiceRepository;
using InvoiceOcr.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace InvoiceOcrApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region Application Builder Setup
            var builder = WebApplication.CreateBuilder(args);

            // CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            #endregion

            #region Database Configuration
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=invoiceocr.db").UseLazyLoadingProxies());
            #endregion

            #region Dependency Injection
            // Repositories
            builder.Services.AddScoped<InvoiceRepository>();
            builder.Services.AddScoped<InvoiceDetailRepository>();
            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<IInvoiceDetailRepository, InvoiceDetailRepository>();

            // Services
            builder.Services.AddScoped<PdfConverter>();
            builder.Services.AddScoped<OcrService>();
            builder.Services.AddScoped<InvoiceService>();
            #endregion



            #region Swagger Configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Invoice OCR API",
                    Version = "v1",
                    Description = "API for extracting and managing invoice data from images or PDFs"
                });
            });
            #endregion

            #region Logging Configuration
            builder.Services.AddLogging(logging => logging.AddConsole());
            #endregion

            #region Application Pipeline Configuration
            var app = builder.Build();

            // بعد builder.Build()
            app.UseCors("AllowAngular");

            // Other middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice OCR API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
            #endregion
        }
    }
}
