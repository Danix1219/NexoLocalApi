ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/identity/src/Domain/NexoLocal.Identity.Domain.csproj services/identity/src/Domain/
COPY services/identity/src/Application/NexoLocal.Identity.Application.csproj services/identity/src/Application/
COPY services/identity/src/Infrastructure/NexoLocal.Identity.Infrastructure.csproj services/identity/src/Infrastructure/
COPY services/identity/src/Api/NexoLocal.Identity.Api.csproj services/identity/src/Api/
RUN dotnet restore services/identity/src/Api/NexoLocal.Identity.Api.csproj
COPY services/identity/ services/identity/
RUN dotnet publish services/identity/src/Api/NexoLocal.Identity.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Identity.Api.dll"]
