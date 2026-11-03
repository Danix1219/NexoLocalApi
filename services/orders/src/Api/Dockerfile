ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/orders/src/Domain/NexoLocal.Orders.Domain.csproj services/orders/src/Domain/
COPY services/orders/src/Application/NexoLocal.Orders.Application.csproj services/orders/src/Application/
COPY services/orders/src/Infrastructure/NexoLocal.Orders.Infrastructure.csproj services/orders/src/Infrastructure/
COPY services/orders/src/Api/NexoLocal.Orders.Api.csproj services/orders/src/Api/
RUN dotnet restore services/orders/src/Api/NexoLocal.Orders.Api.csproj
COPY services/orders/ services/orders/
RUN dotnet publish services/orders/src/Api/NexoLocal.Orders.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Orders.Api.dll"]
