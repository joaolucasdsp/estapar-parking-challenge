#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install cultures (same approach as Alpine SDK image)
RUN apk add --no-cache icu-libs

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A}
ENV CORECLR_NEWRELIC_HOME=/app/newrelic
ENV CORECLR_PROFILER_PATH=/app/newrelic/libNewRelicProfiler.so
ENV NEW_RELIC_APP_NAME=AspNetBoilerplate
ENV NEW_RELIC_LICENSE_KEY=REPLACEKEY

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["Site/AspNetBoilerplate.Site.csproj", "Site/"]
COPY ["Api/AspNetBoilerplate.Api.csproj", "Api/"]
RUN dotnet restore "Site/AspNetBoilerplate.Site.csproj"
COPY . .
WORKDIR "/src/Site"
RUN dotnet build "AspNetBoilerplate.Site.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AspNetBoilerplate.Site.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AspNetBoilerplate.Site.dll"]

