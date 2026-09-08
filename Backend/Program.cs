using SmartSupermarket.Backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Extension methods for Infrastructure layer
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
