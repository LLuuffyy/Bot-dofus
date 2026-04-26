using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SnowbotUnityServer.Data;
using System.Net.Sockets;
using System.Text;

namespace SnowbotUnityServer
{
    public class Program
    {
        public static Dictionary<string, List<string>> MapIdsHints = new();
        public static async Task Main(string[] args)
        {
#if DEBUG == false
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ressources", "hints2");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Ajoute le support pour Windows-1252

            foreach (var filePath in Directory.EnumerateFiles(directory, "*.txt"))
            {
                string fileName = Path.GetFileName(filePath);
                var lines = new List<string>(File.ReadAllLines(filePath, Encoding.GetEncoding("Windows-1252")));
                Program.MapIdsHints.Add(fileName.Replace(".txt", ""), lines);
            }
#endif

            TcpListener.DofusListener.Start();
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<ApiKeyStorageService>();
            builder.Services.AddSingleton<CharacterCacheService>();
            var app = builder.Build();
            app.UseWebSockets();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

   

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
            var mainTask = app.RunAsync($"http://*:{5004}");

            await mainTask;
        }
    }
}