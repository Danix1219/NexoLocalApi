ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/community/src/Modules/NexoLocal.Community.Modules.csproj services/community/src/Modules/
COPY services/community/src/Adapters/NexoLocal.Community.Adapters.csproj services/community/src/Adapters/
COPY services/community/src/Api/NexoLocal.Community.Api.csproj services/community/src/Api/
RUN dotnet restore services/community/src/Api/NexoLocal.Community.Api.csproj
COPY services/community/ services/community/
RUN dotnet publish services/community/src/Api/NexoLocal.Community.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Community.Api.dll"]
