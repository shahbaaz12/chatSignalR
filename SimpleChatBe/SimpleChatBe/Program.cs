
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleChatBe.Hubs;
using SimpleChatBe.Repositories;

namespace SimpleChatBe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // OpenAPI (from .NET template)
            builder.Services.AddOpenApi();

            // Add SignalR for real-time chat
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IChatRepository, InMemoryChatRepository>();

            // Add CORS so React can call the API + Hub
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                });
            });

            var app = builder.Build();

            // Configure OpenAPI only in development
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors();

            // Map controllers
            app.MapControllers();

            // Map hubs — we will add ChatHub.cs later
            app.MapHub<ChatHub>("/hubs/chat");

            app.Run();
        }
    }
}
