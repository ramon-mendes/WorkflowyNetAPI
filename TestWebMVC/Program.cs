using Microsoft.OpenApi;
using WorkflowyNetAPI;

namespace TestWebMVC
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// demo account
			Environment.SetEnvironmentVariable("workflowy_apikey", "00303119042bf0cc996c030991064b5d6d646939");

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews()
				.AddRazorRuntimeCompilation()
				.AddApplicationPart(typeof(WFAPIController).Assembly)
				.AddControllersAsServices();

			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo { Title = "WorkflowyNetAPI" });
			});


			var app = builder.Build();

			app.UseSwagger();
			app.UseSwaggerUI(options =>
			{
				options.SwaggerEndpoint("v1/swagger.json", "WorkflowyNetAPI");
			});

			// Configure the HTTP request pipeline.
			if(!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseRouting();

			app.UseAuthorization();

			app.MapStaticAssets();
			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}")
				.WithStaticAssets();

			app.Run();
		}
	}
}
