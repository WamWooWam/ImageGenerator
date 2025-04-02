using SixLabors.ImageSharp.Web.DependencyInjection;

namespace ImageGeneratorService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            //builder.Services.AddImageSharp();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //app.UseImageSharp();

            app.MapControllers();

            app.Run();
        }
    }
}
