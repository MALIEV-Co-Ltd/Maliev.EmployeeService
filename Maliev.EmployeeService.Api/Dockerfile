# Switch to non-root user
USER app

# Copy published application (now owned by app)
COPY --from=publish /app/publish .

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
