FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev
WORKDIR /src

COPY Olve.Template.Api.csproj .
RUN dotnet restore -r linux-x64

COPY . .
RUN dotnet publish -c Release -r linux-x64 -o /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["./Olve.Template.Api"]
