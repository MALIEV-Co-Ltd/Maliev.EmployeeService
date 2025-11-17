# Multi-stage build for Employee Service
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Maliev.EmployeeService.sln", "./"]
COPY ["Maliev.EmployeeService.Api/Maliev.EmployeeService.Api.csproj", "Maliev.EmployeeService.Api/"]
COPY ["Maliev.EmployeeService.Application/Maliev.EmployeeService.Application.csproj", "Maliev.EmployeeService.Application/"]
COPY ["Maliev.EmployeeService.Domain/Maliev.EmployeeService.Domain.csproj", "Maliev.EmployeeService.Domain/"]
COPY ["Maliev.EmployeeService.Infrastructure/Maliev.EmployeeService.Infrastructure.csproj", "Maliev.EmployeeService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Maliev.EmployeeService.Api/Maliev.EmployeeService.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/Maliev.EmployeeService.Api"
RUN dotnet build "Maliev.EmployeeService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Maliev.EmployeeService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 dotnetuser && \
    adduser --system --uid 1001 --ingroup dotnetuser dotnetuser

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R dotnetuser:dotnetuser /app

# Switch to non-root user
USER dotnetuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/employees/liveness || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Maliev.EmployeeService.Api.dll"]
