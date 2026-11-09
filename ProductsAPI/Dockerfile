FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ProductsAPI/ProductsAPI.csproj ProductsAPI/
RUN dotnet restore ProductsAPI/ProductsAPI.csproj
COPY ProductsAPI/ ProductsAPI/
RUN dotnet publish ProductsAPI/ProductsAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/images && chown -R 1654:1654 /app/wwwroot
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductsAPI.dll"]
