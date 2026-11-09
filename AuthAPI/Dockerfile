FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AuthAPI/AuthAPI.csproj AuthAPI/
RUN dotnet restore AuthAPI/AuthAPI.csproj
COPY AuthAPI/ AuthAPI/
RUN dotnet publish AuthAPI/AuthAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthAPI.dll"]
