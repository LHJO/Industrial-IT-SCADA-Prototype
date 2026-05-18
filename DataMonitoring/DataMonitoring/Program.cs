using DataMonitoring.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register OpcReader as a Singleton so the same instance is shared everywhere
builder.Services.AddSingleton<OpcReader>();

// Register AlarmService as a Singleton
builder.Services.AddSingleton<AlarmService>();

// Register OpcReaderService as a background hosted service
builder.Services.AddHostedService<OpcReaderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
