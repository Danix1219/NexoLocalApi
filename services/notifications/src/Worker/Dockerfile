ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/notifications/src/Delivery/NexoLocal.Notifications.Delivery.csproj services/notifications/src/Delivery/
COPY services/notifications/src/Adapters/NexoLocal.Notifications.Adapters.csproj services/notifications/src/Adapters/
COPY services/notifications/src/Worker/NexoLocal.Notifications.Worker.csproj services/notifications/src/Worker/
RUN dotnet restore services/notifications/src/Worker/NexoLocal.Notifications.Worker.csproj
COPY services/notifications/ services/notifications/
RUN dotnet publish services/notifications/src/Worker/NexoLocal.Notifications.Worker.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Notifications.Worker.dll"]
