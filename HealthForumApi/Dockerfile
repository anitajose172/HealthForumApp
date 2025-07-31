# Step 1: Use a base image with the .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Step 2: Copy the project file and restore dependencies
COPY HealthForumApi.csproj ./
RUN dotnet restore HealthForumApi.csproj

# Step 3: Copy the rest of the code and build the app
COPY . .
RUN dotnet publish HealthForumApi.csproj -c Release -o /app/publish

# Step 4: Use a smaller base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "HealthForumApi.dll"] 
