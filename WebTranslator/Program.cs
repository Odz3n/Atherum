using Azure.Core.Extensions;
using WebTranslator.Hubs;
using WebTranslator.Models.TranslationDTOs.Common;
using WebTranslator.Services.AIVisionService;
using WebTranslator.Services.Api;
using WebTranslator.Services.Azure;
using WebTranslator.Services.ErrorHandling;
using WebTranslator.Services.Routing;
using WebTranslator.Services.Status;
using WebTranslator.Services.TranslationService;
using WebTranslator.Services.Validation;

namespace WebTranslator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400;
            });

            builder.Services.AddScoped<TranslationConfig>();
            builder.Services.AddScoped<TranslationService>();

            builder.Services.AddScoped<IVisionService, AzureVisionService>();
            builder.Services.AddScoped<ITextValidator, TextValidator>();
            builder.Services.AddScoped<IImageValidator, ImageValidator>();

            // xD
            builder.Services.AddScoped<IApiConfiguration<TranslationApiConfig>, AzureTranslatorApiConfiguration>();
            builder.Services.AddScoped<IApiConfiguration<AzureVisionApiConfig>, AzureVisionApiConfiguration>();
            
            builder.Services.AddScoped<IErrorHandler, ErrorHandler>();
            builder.Services.AddScoped<Services.Routing.IRouteBuilder, Services.Routing.AzureRouteBuilder>();
            builder.Services.AddScoped<IStatusSender, StatusSender>();
            builder.Services.AddScoped<IAzureTranslationClient, AzureTranslationClient>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapHub<TranslationHub>("/translationHub");

            app.MapStaticAssets();
            app.Run();
        }
    }
}
