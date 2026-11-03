ARG SDK_VERSION=10.0.302
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY nexolocal.Api/nexolocal.Api.csproj nexolocal.Api/
RUN dotnet restore nexolocal.Api/nexolocal.Api.csproj
COPY nexolocal.Api/ nexolocal.Api/
RUN dotnet publish nexolocal.Api/nexolocal.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /out .
ENTRYPOINT ["dotnet", "nexolocal.Api.dll"]
