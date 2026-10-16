# RaceDay — RESTful API

## Project Overview

RaceDay is a RESTful API developed using ASP.NET Core and C# for managing sporting events, participant registrations and race results.

The system supports two user roles:

- **Organiser:** Creates and manages events, categories, event-category assignments and race results.
- **Participant:** Registers for events, views enrolments, cancels registrations and retrieves personal race results.

The project was developed for the PROG6212w Programming 2A Portfolio of Evidence.

## Technologies Used

- C# and .NET 10
- ASP.NET Core Web API
- Entity Framework Core (Code First)
- Microsoft SQL Server / SQL Server LocalDB
- ASP.NET Core Cookie Authentication
- xUnit automated testing
- Swagger / OpenAPI
- Git and GitHub
- GitHub Actions continuous integration

## Project Structure

```text
RaceDay/
├── .github/
│   └── workflows/
│       └── dotnet.yml
├── src/
│   └── RaceDay.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Migrations/
│       ├── Models/
│       └── Program.cs
├── tests/
│   └── RaceDay.Api.Tests/
├── RaceDay.slnx
└── README.md
```

## Main Features

### Authentication and User Profiles

- User registration and login
- Password hashing
- Cookie-based authentication
- Role-based access control
- View and update the authenticated user's profile
- Logout functionality

### Event Management

- Create sporting events
- View available events
- Update existing events
- Delete events
- Restrict event management to the owning Organiser
- Support Run, Walk and Cycle events

### Category Management

- Create, retrieve, update and delete categories
- Prevent duplicate category names
- Restrict category management to Organisers

### Event Category Management

- Assign categories to events
- Configure entry fees
- Define maximum participant capacity
- Prevent duplicate event-category assignments

### Participant Enrolments

- Register for an event category
- View personal enrolments
- Cancel an enrolment
- Prevent duplicate registrations
- Check participant capacity
- Restrict enrolment creation to Participants

### Race Results

- Record finishing times and positions
- Update recorded results
- Support Finished, DNF, DNS and DQ statuses
- Allow Participants to view their own results
- Restrict result management to the owning Organiser

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a user |
| POST | `/api/auth/login` | Log in |
| POST | `/api/auth/logout` | Log out |
| GET | `/api/auth/me` | View authenticated user |
| GET | `/api/profile` | View own profile |
| PUT | `/api/profile` | Update own profile |
| GET | `/api/events` | List events |
| GET | `/api/events/{id}` | Get event details |
| POST | `/api/events` | Create an event |
| PUT | `/api/events/{id}` | Update an event |
| DELETE | `/api/events/{id}` | Delete an event |
| GET | `/api/categories` | List categories |
| POST | `/api/categories` | Create a category |
| PUT | `/api/categories/{id}` | Update a category |
| DELETE | `/api/categories/{id}` | Delete a category |
| GET | `/api/events/{eventId}/categories` | View event categories |
| POST | `/api/events/{eventId}/categories` | Assign an event category |
| PUT | `/api/events/{eventId}/categories/{id}` | Update an assignment |
| DELETE | `/api/events/{eventId}/categories/{id}` | Remove an assignment |
| POST | `/api/enrolments` | Create enrolment |
| GET | `/api/enrolments/my` | View personal enrolments |
| GET | `/api/enrolments/{id}` | View enrolment |
| PUT | `/api/enrolments/{id}/cancel` | Cancel enrolment |
| POST | `/api/results` | Record a result |
| GET | `/api/results/my` | View personal results |
| GET | `/api/results/{id}` | View a result |
| PUT | `/api/results/{id}` | Update a result |
| GET | `/api/results/event/{eventId}` | View event results |

## Database Setup

The project uses Entity Framework Core Code First with SQL Server.

1. Ensure SQL Server LocalDB or another compatible SQL Server instance is installed.
2. Configure the database connection string in the application's configuration.
3. Restore NuGet dependencies.
4. Apply Entity Framework Core migrations.

```bash
dotnet restore RaceDay.slnx
dotnet ef database update --project src/RaceDay.Api
```

The database includes the following entities:

- User
- Event
- Category
- EventCategory
- Enrolment
- Result

Entity Framework Core migrations manage database schema changes.

## Running the Application

From the repository root:

```bash
dotnet build RaceDay.slnx
dotnet run --project src/RaceDay.Api --launch-profile https
```

The API uses HTTPS for local development.

The Swagger UI is available at the application's HTTPS address followed by `/swagger` when running in Development mode.

## Automated Testing

The project uses xUnit to test DTO validation, default model values and Entity Framework Core database configuration.

Run the test suite using:

```bash
dotnet test RaceDay.slnx
```

At the time of documentation, the automated suite contains **12 passing tests**.

Additional manual HTTP testing has been performed for authentication, event management, category management, enrolment creation, duplicate prevention and result retrieval.

## Continuous Integration

GitHub Actions is configured in:

`.github/workflows/dotnet.yml`

The workflow automatically:

1. Checks out the repository.
2. Installs .NET 10.
3. Restores NuGet dependencies.
4. Builds the solution.
5. Runs automated tests.

The workflow runs on pushes and pull requests.

## Security and Authorization

The API implements:

- Hashed user passwords
- Authenticated API endpoints
- Organiser and Participant roles
- Ownership checks for protected resources
- Validation of submitted data
- Prevention of duplicate enrolments and results

This project is an academic implementation. Further security hardening would be required before production deployment, including stronger Organiser registration controls and CSRF protection for cookie-authenticated state-changing endpoints.

## Version Control

The project is maintained using Git and GitHub.

Repository:

https://github.com/Unknown8075/PROG6212w_POE_ST10466248

Development branch:

`part2-api-development`

## Demonstration

A demonstration video should show:

- API startup and Swagger documentation
- Registration and login
- Organiser event and category management
- Participant enrolment
- Race result management
- Role-based authorization
- Automated test execution
- Successful GitHub Actions workflow

## Conclusion

RaceDay demonstrates the development of a role-based RESTful API using ASP.NET Core, Entity Framework Core and SQL Server.

The project incorporates authentication, CRUD operations, database relationships, validation, automated testing, Git version control and continuous integration.