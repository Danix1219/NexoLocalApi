ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY services/trust/src/Evaluation/NexoLocal.Trust.Evaluation.csproj services/trust/src/Evaluation/
COPY services/trust/src/Adapters/NexoLocal.Trust.Adapters.csproj services/trust/src/Adapters/
COPY services/trust/src/Api/NexoLocal.Trust.Api.csproj services/trust/src/Api/
RUN dotnet restore services/trust/src/Api/NexoLocal.Trust.Api.csproj
COPY services/trust/ services/trust/
RUN dotnet publish services/trust/src/Api/NexoLocal.Trust.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NexoLocal.Trust.Api.dll"]
