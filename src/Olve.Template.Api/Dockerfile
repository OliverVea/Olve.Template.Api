FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Olve.Template.Api/Olve.Template.Api.csproj src/Olve.Template.Api/
RUN dotnet restore src/Olve.Template.Api -r linux-x64

COPY src/Olve.Template.Api/ src/Olve.Template.Api/
RUN dotnet publish src/Olve.Template.Api -c Release -r linux-x64 -o /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["./Olve.Template.Api"]
