ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/notifications/src/Delivery/NexoLocal.Notifications.Delivery.csproj services/notifications/src/Delivery/
COPY services/notifications/src/Adapters/NexoLocal.Notifications.Adapters.csproj services/notifications/src/Adapters/
COPY services/notifications/src/Api/NexoLocal.Notifications.Api.csproj services/notifications/src/Api/
RUN dotnet restore services/notifications/src/Api/NexoLocal.Notifications.Api.csproj
COPY services/notifications/ services/notifications/
RUN dotnet publish services/notifications/src/Api/NexoLocal.Notifications.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Notifications.Api.dll"]
