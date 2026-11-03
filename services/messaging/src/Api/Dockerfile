ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/messaging/src/Core/NexoLocal.Messaging.Core.csproj services/messaging/src/Core/
COPY services/messaging/src/Adapters/NexoLocal.Messaging.Adapters.csproj services/messaging/src/Adapters/
COPY services/messaging/src/Api/NexoLocal.Messaging.Api.csproj services/messaging/src/Api/
RUN dotnet restore services/messaging/src/Api/NexoLocal.Messaging.Api.csproj
COPY services/messaging/ services/messaging/
RUN dotnet publish services/messaging/src/Api/NexoLocal.Messaging.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Messaging.Api.dll"]
