# Valuta Core API

Currency conversion & foreign-exchange rates service written in **.NET 9**  
with **Clean Architecture**, **MediatR**, and **Serilog**.

---

## ✨ Features
- **JWT authentication** with role-based authorization (`Admin`, `User`)
- **Latest**, **historical**, and **conversion** endpoints
- Modular layers  
  `Host → Api → Application → Core → Infrastructure`
- OpenAPI / Swagger documentation out of the box
- Telemetry via OpenTelemetry (`AspNetCore`, `HttpClient`)
- Fully unit-tested controllers & handlers (MediatR mocked)

---

## 📦 Project Structure
src/
│
├─ ValutaCore.Host ← entry point / composition root
│
├─ ValutaCore.Api ← controllers, middleware, Swagger
│
├─ ValutaCore.Application ← Mediator commands, queries, validators
│
├─ ValutaCore.Core ← domain models & abstractions
│
└─ ValutaCore.Infrastructure← EF DbContexts, HTTP clients, token service

yaml
Copy
Edit

---

## 🚀 Getting Started

```bash
# clone & restore
git clone https://github.com/your-org/valuta-core.git
cd valuta-core
dotnet restore

# run locally (HTTPS 5001)
dotnet run --project src/ValutaCore.Host
Open https://localhost:5001/swagger to explore the API.

🧪 Tests
bash
Copy
Edit
dotnet test
Controllers are unit-tested with Moq + xUnit

Application handlers are tested in isolation (mocks only)

Integration tests spin up the full Host with WebApplicationFactory

⚙️ Configuration
File	Purpose
appsettings.json	default settings (Serilog, JWT, external FX API)
appsettings.Development.json	local overrides
Properties/launchSettings.json	debug profile (HTTPS 5001 / HTTP 5000)

Set sensitive values (JWT secret, external API key) via environment variables
or User Secrets.

📈 API Reference
Method	Route	Roles
POST	/api/v1/authentication/login	anonymous
GET	/api/v1/currency/rates?base=USD	User, Admin
GET	/api/v1/currency/convert?value=100&source=USD&target=EUR	User, Admin
GET	/api/v1/currency/historical?...	Admin

Full request/response examples are shown in Swagger.

🛠️ Tech Stack
.NET 9 Preview

MediatR 11

FluentValidation 12

Serilog

OpenTelemetry

Docker (optional containerization)

🤝 Contributing
Fork & branch from main

Follow the code style (dotnet format)

Write / update tests

PR with a clear description