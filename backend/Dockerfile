FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/LibraryManagement.Api/LibraryManagement.Api.csproj src/LibraryManagement.Api/
COPY src/LibraryManagement.Application/LibraryManagement.Application.csproj src/LibraryManagement.Application/
COPY src/LibraryManagement.Domain/LibraryManagement.Domain.csproj src/LibraryManagement.Domain/
COPY src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj src/LibraryManagement.Infrastructure/
RUN dotnet restore src/LibraryManagement.Api/LibraryManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/LibraryManagement.Api/LibraryManagement.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=5 \
    CMD ["dotnet", "LibraryManagement.Api.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "LibraryManagement.Api.dll"]
