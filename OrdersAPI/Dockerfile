FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OrdersAPI/OrdersAPI.csproj OrdersAPI/
RUN dotnet restore OrdersAPI/OrdersAPI.csproj
COPY OrdersAPI/ OrdersAPI/
RUN dotnet publish OrdersAPI/OrdersAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrdersAPI.dll"]
