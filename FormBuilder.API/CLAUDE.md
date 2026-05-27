# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Overview

Full-stack dynamic form builder:
- **Backend**: `FormBuilder.API` — ASP.NET Core 8 Web API (.NET 8.0), SQL Server, EF Core 8
- **Frontend**: `form-builder-ui` — Angular 18 standalone components (lives at `c:\Users\anand\Downloads\form-builder-ui`)

## Commands

### Backend
```powershell
# From FormBuilder.API/
dotnet run                  # Start API on http://localhost:5282
dotnet watch run            # Hot reload dev mode
dotnet build                # Build only
```

Swagger UI: `http://localhost:5282/swagger`
Health check: `http://localhost:5282/health`

### Frontend (from form-builder-ui/)
```bash
npm install
npm start                   # Dev server on http://localhost:4200
npm run build:prod          # Production build
```

## Database Setup

- SQL Server required. Connection string in `appsettings.json`:
  `Server=GOPALPASUPULETI\SQLEXPRESS;Database=FormBuilderDB;Trusted_Connection=True;TrustServerCertificate=True;`
- **No EF migrations exist.** Schema is auto-created via `EnsureCreated` (or enable the commented `MigrateAsync` block in `Program.cs` lines 94–100).
- Seed master data by running scripts in `SQL/` against FormBuilderDB in order:
  1. `MasterData_Seed.sql` — initial ControlTypes, DataTypes, ValidationRules, DataSources
  2. `MasterData_Fix_Controls.sql` — renames wrong control names to match Angular frontend expectations

## Architecture

### Control Type Name Contract

The Angular frontend matches controls by exact `controlTypeName` string. The authoritative list of valid names lives in `form-builder-ui/src/app/components/control-preview.component.ts` (`knownTypes` Set). Controls not in that set render as a yellow "unknown" fallback. **Always keep `ControlTypes.Name` values in sync with the frontend `knownTypes`.**

Valid names: `TextBox`, `EmailBox`, `PasswordBox`, `NumberBox`, `MultiTextBox`, `DropdownList`, `MultiSelect`, `RadioButton`, `Checkbox`, `CheckboxGroup`, `ToggleSwitch`, `DatePicker`, `DateTimePicker`, `TimePicker`, `Slider`, `FileUpload`, `ImageUpload`, `ColorPicker`, `RichTextEditor`, `Label`, `Heading`, `Divider`, `HtmlContent`, `Button`, `LinkButton`, `IconButton`, `Section`, `Spacer`

### Backend Layers

```
Controllers/        HTTP endpoints — thin, delegate to services
Services/           Business logic (IFormService, IMasterDataService, etc.)
Data/               FormBuilderDbContext (EF Core, SQL Server)
Models/             Domain entities in DomainModels.cs (21 entities)
DTOs/               Request/response shapes separate from domain models
Middleware/         GlobalExceptionMiddleware — catches all unhandled exceptions
```

All controllers return `ApiResponse<T>` envelope (`{ success, message, data, statusCode }`). Paginated endpoints return `PagedResult<T>` (`{ items, totalCount, page, pageSize }`).

JSON is configured camelCase + null-omitting + string enums (`Program.cs` lines 39–45).

### Key Domain Relationships

- **Forms** → **FormControls** (cascade delete) → **FormControlValidations** (cascade delete)
- **DataSources** → **DataSourceItems** (cascade delete); DataSource can be Static, API, or Database type
- **ExternalConnections** → Dashboards, Reports, DataSources (Database type)
- **ApiCollections** → ApiFolders (recursive) → ApiRequests

### Master Data Tables (read-only at runtime)

`ControlTypes`, `DataTypes`, `ValidationRules`, `DataSources`, `DataSourceItems` — seeded via SQL scripts, not modified by normal API usage. All have unique indexes on `Name`.

### CORS

Allowed origins configured in `appsettings.json` under `AllowedOrigins`. Default includes `http://localhost:4200`. Policy name is `"Angular"`.
