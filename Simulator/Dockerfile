FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["Simulator/EstaparParkingChallenge.Simulator.csproj", "Simulator/"]
RUN dotnet restore "Simulator/EstaparParkingChallenge.Simulator.csproj"
COPY . .
WORKDIR "/src/Simulator"
RUN dotnet build "EstaparParkingChallenge.Simulator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EstaparParkingChallenge.Simulator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EstaparParkingChallenge.Simulator.dll"]
