var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// G01: host de entrada existente. Sin rutas de negocio ni proxy configurado aun.
app.Run();
