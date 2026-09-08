using SmartSupermarket.Backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Extension method registering Infrastructure layer (DbContext, Security, Options)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
