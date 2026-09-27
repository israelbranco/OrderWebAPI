FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OrderWebAPI.slnx", "./"]
COPY ["OrderWebAPI.Domain/OrderWebAPI.Domain.csproj", "OrderWebAPI.Domain/"]
COPY ["OrderWebAPI.Application/OrderWebAPI.Application.csproj", "OrderWebAPI.Application/"]
COPY ["OrderWebAPI.Infrastructure/OrderWebAPI.Infrastructure.csproj", "OrderWebAPI.Infrastructure/"]
COPY ["OrderWebAPI.API/OrderWebAPI.API.csproj", "OrderWebAPI.API/"]
COPY ["OrderWebAPI.Tests/OrderWebAPI.Tests.csproj", "OrderWebAPI.Tests/"]

RUN dotnet restore "OrderWebAPI.slnx"
COPY . .
WORKDIR "/src/OrderWebAPI.API"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderWebAPI.API.dll"]