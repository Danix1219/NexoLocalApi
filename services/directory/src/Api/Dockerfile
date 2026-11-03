ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/directory/src/Features/NexoLocal.Directory.Features.csproj services/directory/src/Features/
COPY services/directory/src/Adapters/NexoLocal.Directory.Adapters.csproj services/directory/src/Adapters/
COPY services/directory/src/Api/NexoLocal.Directory.Api.csproj services/directory/src/Api/
RUN dotnet restore services/directory/src/Api/NexoLocal.Directory.Api.csproj
COPY services/directory/ services/directory/
RUN dotnet publish services/directory/src/Api/NexoLocal.Directory.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Directory.Api.dll"]
