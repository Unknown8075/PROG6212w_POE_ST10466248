## Part 3 — MVC Application and Cloud Storage

RaceDay includes an ASP.NET Core MVC frontend integrated with a .NET 10 Web API.

### Implemented Features

- Organiser and participant authentication with role-based access
- Event creation, editing, viewing and deletion
- Event categories, enrolments and results
- Participant profile management
- Event banner image uploads
- Participant profile picture uploads
- Image retrieval and display through authenticated API endpoints
- Entity Framework Core with SQL Server
- Automated unit testing using xUnit
- Dockerfiles for the API and MVC applications
- GitHub Actions continuous integration

### Azure Blob Storage and Azurite

Azure Blob Storage functionality was implemented using the Azure Storage SDK.

Deployment of an Azure Storage Account was attempted using an Azure for Students subscription. However, Azure returned the `RequestDisallowedByAzure` policy error, restricting deployment in the selected regions.

Azurite was therefore configured as a local Azure Storage emulator using `UseDevelopmentStorage=true`.

Both participant profile picture uploads and organiser event banner uploads were successfully tested locally.

To start Azurite:

```bash
azurite --location "$HOME/azurite-data" --skipApiVersionCheck
```

The application uses Azurite for local development. A deployed Azure Storage Account would require a valid cloud connection string.

### Testing

The latest local test execution completed successfully:

- Build: Passed
- Unit tests: 12
- Passed: 12
- Failed: 0

### Docker and CI/CD

Dockerfiles are provided for the API and MVC projects.

GitHub Actions is configured to restore dependencies, build the solution, execute unit tests and build Docker images.

Docker image builds and GitHub Actions execution should be verified separately before marking them successful.

### Local Development

Start Azurite, then run:

```bash
dotnet run --project src/RaceDay.Api --launch-profile https
dotnet run --project src/RaceDay.Mvc --launch-profile https
```

API Swagger: `https://localhost:7020/swagger`

MVC application: `https://localhost:7017`