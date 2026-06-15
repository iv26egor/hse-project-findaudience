FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MySite/MySite.csproj", "MySite/"]
COPY ["CourseProject.Parser/CourseProject.Parser.csproj", "CourseProject.Parser/"]
RUN dotnet restore "MySite/MySite.csproj"

COPY . .
WORKDIR "/src/MySite"
RUN dotnet build "MySite.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MySite.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "MySite.dll"]