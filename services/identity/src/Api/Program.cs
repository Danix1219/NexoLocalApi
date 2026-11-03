var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Base autorizada: sin endpoints ni logica de negocio.
app.Run();
