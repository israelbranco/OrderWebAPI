FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrderWebAPI.sln", "./"]
COPY ["OrderWebAPI.Domain/OrderWebAPI.Domain.csproj", "OrderWebAPI.Domain/"]
COPY ["OrderWebAPI.Application/OrderWebAPI.Application.csproj", "OrderWebAPI.Application/"]
COPY ["OrderWebAPI.Infrastructure/OrderWebAPI.Infrastructure.csproj", "OrderWebAPI.Infrastructure/"]
COPY ["OrderWebAPI.API/OrderWebAPI.API.csproj", "OrderWebAPI.API/"]
COPY ["OrderWebAPI.Tests/OrderWebAPI.Tests.csproj", "OrderWebAPI.Tests/"]

RUN dotnet restore "OrderWebAPI.sln"
COPY . .
WORKDIR "/src/OrderWebAPI.API"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderWebAPI.API.dll"]