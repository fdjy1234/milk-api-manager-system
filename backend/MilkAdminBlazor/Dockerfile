FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy both projects to allow project references to resolve
COPY ["MilkAdminBlazor/MilkAdminBlazor.csproj", "MilkAdminBlazor/"]
COPY ["MilkApiManager/MilkApiManager.csproj", "MilkApiManager/"]
RUN dotnet restore "MilkAdminBlazor/MilkAdminBlazor.csproj"

COPY . .
WORKDIR "/src/MilkAdminBlazor"
RUN dotnet publish "MilkAdminBlazor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MilkAdminBlazor.dll"]
