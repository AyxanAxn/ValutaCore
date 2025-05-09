# ValutaCore: A Modern Currency Conversion Service

ValutaCore is a high-performance, secure, and extensible API for currency conversion tasks. Designed with Clean Architecture in C# and ASP.NET Core, it emphasizes maintainability, testability, and resilience.

## Features

- **Latest Exchange Rates**: Fetch the latest exchange rates for a specified base currency
- **Currency Conversion**: Convert amounts between different currencies with precision
- **Historical Exchange Rates**: Retrieve paged historical rates over custom date ranges
- **Resilience & Performance**
    - In-memory caching to minimize external calls
    - Retry policies with exponential backoff
    - Circuit breaker pattern for graceful degradation
- **Security & Access Control**
    - JWT authentication
    - Role-based authorization (User / Admin)
    - Request throttling to prevent abuse
- **Logging & Monitoring**
    - Structured logging via Serilog
    - Request/response correlation IDs
    - Health checks and metrics endpoints

## Architecture

ValutaCore follows Clean Architecture with these layers:

- **API Layer**: Controllers, middleware, DTOs and Swagger setup
- **Application Layer**: Use-case orchestration, validation, and mapping
- **Domain Layer**: Core models, business rules, and custom exceptions
- **Infrastructure Layer**: HTTP clients, caching, configuration, and persistence

### Why This Architecture?

1. **Separation of Concerns**  
   Each layer has a single responsibility, simplifying maintenance and onboarding.
2. **Dependency Rule**  
   Inner layers (Domain/Application) never depend on outer layers (Infrastructure/API).
3. **Testability**  
   Use interfaces and DI to mock external dependencies in unit tests.
4. **Flexibility**  
   Swap out rate providers (Frankfurter, Fixer.io, etc.) without touching core logic.
5. **Scalability**  
   Add new features (crypto support, alerts) with minimal refactoring.
6. **Resilience**  
   Encapsulate retry, circuit-breaker, and fallback policies in Infrastructure.
7. **Security**  
   Enforce authentication and authorization at the API boundary.

### Key Design Patterns

- **Repository Pattern**: Abstracts data access
- **Factory Pattern**: Creates the appropriate currency provider
- **Dependency Injection**: Decouples implementations from interfaces
- **Options Pattern**: Binds configuration sections to strongly-typed classes

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/) or later
- Docker (optional, for containerized deployment)

### Installation

1. **Clone the repo**
   ```bash
   git clone https://github.com/yourusername/ValutaCore.git
   cd ValutaCore