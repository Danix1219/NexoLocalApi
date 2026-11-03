ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/relations/src/Projections/NexoLocal.Relations.Projections.csproj services/relations/src/Projections/
COPY services/relations/src/Adapters/NexoLocal.Relations.Adapters.csproj services/relations/src/Adapters/
COPY services/relations/src/Api/NexoLocal.Relations.Api.csproj services/relations/src/Api/
RUN dotnet restore services/relations/src/Api/NexoLocal.Relations.Api.csproj
COPY services/relations/ services/relations/
RUN dotnet publish services/relations/src/Api/NexoLocal.Relations.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Relations.Api.dll"]
