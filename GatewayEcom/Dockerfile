FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GatewayEcom/GatewayEcom.csproj GatewayEcom/
RUN dotnet restore GatewayEcom/GatewayEcom.csproj
COPY GatewayEcom/ GatewayEcom/
RUN dotnet publish GatewayEcom/GatewayEcom.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY GatewayEcom/ocelot.docker.json ./ocelot.json
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GatewayEcom.dll"]
