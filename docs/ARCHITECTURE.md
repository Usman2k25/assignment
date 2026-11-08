# Architecture explanation (for the assignment report)

This document explains **how the Campus Services Management System is built and why**.
Every point names the file where you can see it in the code, so you can open it during the viva.

Contents: 1 Overview · 2 Layers · 3 Walk-through · 4 OOP and SOLID · 5 Dependency Injection · 6 Database and EF Core ·
7 REST API · 8 Security · 9 MAUI client · 10 Testing · 11 Limitations

---

## 1. Overview

The system has two programs:

1. **CampusServices.API** – an ASP.NET Core Web API. It contains all rules and all security. It is the only program that talks to PostgreSQL.
2. **CampusServices.MAUI** – a .NET MAUI Blazor Hybrid app (the user interface). It only talks to the API over HTTP + JSON.

```
┌────────────────────────┐   HTTP + JSON + JWT    ┌─────────────────────────────────────────────┐
│  CampusServices.MAUI   │ ─────────────────────▶ │  CampusServices.API                         │
│  Razor pages           │ ◀───────────────────── │   Controllers → Application services        │
│  ApiClient (HttpClient)│                        │   → Repositories (Infrastructure) → EF Core │
└────────────────────────┘                        └───────────────────────────┬─────────────────┘
                                                                              │ SQL
                                                                      ┌───────▼────────┐
                                                                      │   PostgreSQL   │
                                                                      └────────────────┘
```

## 2. Clean Architecture – the layers

Clean Architecture splits the code into layers. The **inner layers contain the important rules** and know nothing about
databases, web or screens. The **outer layers contain technical details** and depend on the inner ones – never the other way round.

```
            ┌──────────────────────────────────────────────────────┐
            │ API   (controllers, middleware, JWT + Swagger setup) │
            │  ┌────────────────────────────────────────────────┐  │
            │  │ Infrastructure (EF Core, Identity, JWT, repos) │  │
            │  │  ┌──────────────────────────────────────────┐  │  │
            │  │  │ Application (use cases, DTOs, interfaces)│  │  │
            │  │  │  ┌────────────────────────────────────┐  │  │  │
            │  │  │  │ Domain (entities, rules, enums)    │  │  │  │
            │  │  │  └────────────────────────────────────┘  │  │  │
            │  │  └──────────────────────────────────────────┘  │  │
            │  └────────────────────────────────────────────────┘  │
            └──────────────────────────────────────────────────────┘
```

| Project | References | Contains | Does NOT know about |
|---|---|---|---|
| **Domain** | nothing of ours (only the Identity base-class package for `ApplicationUser`) | `ServiceRequest`, `Category`, `ApplicationUser`, `RefreshToken`, `RequestStatus`, `StatusRules`, `DomainException` | databases, HTTP, screens |
| **Application** | Domain | DTOs, interfaces (`IServiceRequestService`, `IServiceRequestRepository`, ...), `ServiceRequestService`, `NotFoundException`, `ForbiddenException` | EF Core, PostgreSQL, ASP.NET |
| **Infrastructure** | Application (and so Domain) | `AppDbContext`, entity configurations, migrations, repositories, `AuthService`, `TokenService`, `DbSeeder` | controllers, screens |
| **API** | Application, Infrastructure | controllers, middleware, JWT/Swagger setup, `Program.cs` | screens |
| **MAUI** | Application + Domain (DTOs, roles, status rules only) | pages, `ApiClient`, session handling | the database, EF Core, Infrastructure |
| **Tests** | all of the above | xUnit tests | – |

**Where the dependency rule is enforced:** look at the `<ProjectReference>` lines in each `.csproj`. For example
`CampusServices.Domain.csproj` has none, so Domain *cannot* use EF Core or ASP.NET even by mistake.

**Dependency Inversion in practice:** `ServiceRequestService` (Application) needs to read requests from a database, but
Application must not know EF Core. So Application defines the **interface** `IServiceRequestRepository` and
Infrastructure provides the **class** `ServiceRequestRepository : IServiceRequestRepository` that uses EF Core.
The API connects them in `Infrastructure/DependencyInjection.cs`.

**Why does Domain reference an Identity package?** `ApplicationUser` inherits `IdentityUser` (which gives email, password
hash, lockout ...). That base class lives in the small package `Microsoft.Extensions.Identity.Stores`. It is the only
external package in Domain and it does not make Domain depend on any of *our* outer layers.

## 3. Walk-through: "a student creates a request"

1. The student fills the form in `CreateRequest.razor` (MAUI). The form uses the `CreateRequestDto` validation attributes (`[Required]`, `[StringLength]`, `[Range]`).
2. `ApiClient.CreateRequestAsync` sends `POST /api/requests` with the JWT in the `Authorization: Bearer ...` header.
3. ASP.NET Core **authentication** reads the JWT, checks signature, issuer, audience and expiry, and builds `User` with the claims (id, name, email, role).
4. **Authorization**: `RequestsController.Create` has `[Authorize(Roles = Roles.Student)]`, so a Staff token gets **403** here.
5. **Model validation**: `[ApiController]` checks the DTO attributes. Invalid data → automatic **400**.
6. The controller reads the student's id from the token claim (never from the request body) and calls `IServiceRequestService.CreateAsync`.
7. `ServiceRequestService.CreateAsync` (Application) checks the category exists, builds a `ServiceRequest` entity and asks the repository to add and save it.
8. `ServiceRequestRepository` (Infrastructure) uses EF Core to `INSERT` the row in PostgreSQL.
9. The service reads the new request back (with category / student names) and maps it to a `ServiceRequestDto`.
10. The controller returns **201 Created** with the DTO. If anything throws, `ExceptionHandlingMiddleware` turns it into the right status code.

## 4. OOP and SOLID – examples in this project

| Principle | Example in the code |
|---|---|
| **Encapsulation** (OOP) | `ServiceRequest` owns its rules: `Assign()`, `ChangeStatus()` and `Edit()` check the rules before changing the object. Nobody else decides if a status change is allowed. |
| **Abstraction** (OOP) | Controllers use the interface `IServiceRequestService`; they do not know how it works. |
| **Polymorphism** (OOP) | `IServiceRequestRepository` can be `ServiceRequestRepository` (EF Core/PostgreSQL) in production. Tests can plug in other implementations (see `ErrorHandlingTests`, which swaps in a repository that throws). |
| **S – Single Responsibility** | `TokenService` only creates tokens. `AuthService` only handles register/login/refresh/logout. `ExceptionHandlingMiddleware` only converts exceptions. `ServiceRequestRepository` only runs queries. |
| **O – Open/Closed** | To add a new kind of exception → status code mapping you only add one `else if` in the middleware; controllers do not change. |
| **L – Liskov Substitution** | Any class that implements `ICategoryRepository` can replace `CategoryRepository` without the service noticing (proved by the test with `BrokenCategoryRepository`). |
| **I – Interface Segregation** | Small interfaces: `ICategoryRepository` has 2 methods, `IUserRepository` has 2, instead of one giant "IRepository". |
| **D – Dependency Inversion** | `ServiceRequestService` depends on `IServiceRequestRepository` (abstraction owned by Application), not on EF Core. |

## 5. Dependency Injection (DI)

DI means a class does **not create** the objects it needs. It asks for them in its constructor and ASP.NET Core supplies them.

```csharp
public ServiceRequestService(IServiceRequestRepository requests, ICategoryRepository categories, IUserRepository users)
```

The registrations are in two places:

* `Infrastructure/DependencyInjection.cs` – `AddInfrastructure()` registers the DbContext, Identity, `AuthService`, `TokenService` and the 3 repositories (`AddScoped<IServiceRequestRepository, ServiceRequestRepository>()` ...).
* `API/Program.cs` – registers `IServiceRequestService → ServiceRequestService`, JWT authentication and Swagger.

All of them use **Scoped** lifetime: one instance per HTTP request, so every class in one request shares the same `AppDbContext`.
`Program.cs` is the *composition root* – the one place where the layers are connected. It stays short because the details are in extension methods (`AddInfrastructure`, `AddJwtAuthentication`, `AddSwaggerWithJwt`).

The MAUI app has its own DI setup in `MauiProgram.cs`, including the **typed HttpClient** (`AddHttpClient<ApiClient>(...)`).

## 6. Database and EF Core

### 6.1 Entities and relationships

```
 Category 1 ───────< ServiceRequest >─────── 1 ApplicationUser (Student, "CreatedRequests")
                           │
                           └─────── 0..1 ApplicationUser (Staff, "AssignedRequests")

 ApplicationUser 1 ───< RefreshToken
```

* `ServiceRequest` has the foreign keys `CategoryId`, `StudentId`, `AssignedToId` (nullable) and the navigation properties `Category`, `Student`, `AssignedTo`.
* `Category` and `ApplicationUser` have collection navigations back to their requests.
* Roles (`Student`, `Staff`) are Identity roles, not a column – they live in the Identity tables.

### 6.2 Code-First, Fluent API and migrations

**Code-First** = we write the C# classes first and EF Core creates the database from them.

* `Data/AppDbContext.cs` – the `DbSet`s (tables). It inherits `IdentityDbContext<ApplicationUser>`, which adds the Identity tables.
* `Data/Configurations/*.cs` – **Fluent API** (max lengths, relationships, delete behaviour, status stored as text, unique index, the 3 seeded categories with `HasData`).
* `Migrations/` – generated by `dotnet ef migrations add InitialCreate`. `dotnet ef database update` runs it against PostgreSQL.

Delete behaviours (`ServiceRequestConfiguration.cs`): deleting a category or a student that has requests is **blocked** (`Restrict`);
deleting a staff user sets `AssignedToId` to `NULL` (`SetNull`).

No raw SQL is used anywhere – only LINQ-to-Entities.

### 6.3 `AsNoTracking()` – one example

`Infrastructure/Repositories/ServiceRequestRepository.cs`:

```csharp
private IQueryable<ServiceRequest> ReadOnlyQuery()
{
    return _db.ServiceRequests
        .AsNoTracking()
        .Include(r => r.Category)
        .Include(r => r.Student)
        .Include(r => r.AssignedTo);
}
```

Normally EF Core **tracks** every object it loads (it keeps a copy of the original values so it can detect changes).
For screens that only **display** data (the request lists, the details page) we do not change anything, so tracking is a waste.
`AsNoTracking()` switches it off: faster and less memory.

### 6.4 Change tracking – one example

`ServiceRequestService.UpdateAsync` (Application) with `GetByIdForUpdateAsync` (Infrastructure):

```csharp
var request = await _requests.GetByIdForUpdateAsync(id);   // tracked: EF Core remembers the original values
request.Edit(dto.Title, dto.Description, dto.CategoryId);   // we change the object in memory
await _requests.SaveChangesAsync();                         // EF Core compares and writes: UPDATE ... SET Title=..., Description=..., ...
```

We never write an `UPDATE` statement. EF Core notices which properties changed and updates only those columns.
That is why this query is **not** `AsNoTracking`.

### 6.5 The N+1 problem – basic example and fix

**The problem (this code is NOT in the project, it is only for explanation).** Show 20 requests with their category name:

```csharp
var requests = await _db.ServiceRequests.ToListAsync();          // 1 query: get the 20 requests

foreach (var request in requests)
{
    var category = await _db.Categories                           // +1 query for EVERY request
        .FirstAsync(c => c.Id == request.CategoryId);
    Console.WriteLine(request.Title + " - " + category.Name);
}
// total = 1 + 20 = 21 queries.  With 1000 requests = 1001 queries. This is slow.
```

**The solution used in this project:** load the related data in the *same* query with `Include` (SQL `JOIN`):

```csharp
var requests = await _db.ServiceRequests
    .Include(r => r.Category)          // JOIN Categories
    .Include(r => r.Student)           // JOIN AspNetUsers
    .Include(r => r.AssignedTo)        // LEFT JOIN AspNetUsers
    .ToListAsync();                    // total = 1 query, whatever the number of requests
```

That is exactly what `ReadOnlyQuery()` does for the lists and the details page.
(Another valid fix is a projection with `.Select(r => new { r.Title, CategoryName = r.Category.Name })`, which loads only the needed columns.)

## 7. REST API

| Method and URL | Who | Success | Errors |
|---|---|---|---|
| `POST /api/auth/register` | anyone | 200 | 400 |
| `POST /api/auth/login` | anyone | 200 | 400, 401 |
| `POST /api/auth/refresh` | anyone with a refresh token | 200 | 401 |
| `POST /api/auth/logout` | anyone with a refresh token | 204 | – |
| `GET /api/categories` | any logged-in user | 200 | 401 |
| `GET /api/staff` | **Staff** | 200 | 401, 403 |
| `POST /api/requests` | **Student** | **201** | 400, 401, 403 |
| `GET /api/requests` | logged in: student → own, staff → all | 200 | 401 |
| `GET /api/requests/{id}` | the owner or staff | 200 | 401, 403, 404 |
| `PUT /api/requests/{id}` | **Student**, own request, while *Pending* | 200 | 400, 401, 403, 404 |
| `PUT /api/requests/{id}/assign` | **Staff** | 200 | 400, 401, 403, 404 |
| `PUT /api/requests/{id}/status` | **Staff** | 200 | 400, 401, 403, 404 |

* **DTOs:** the API never sends EF Core entities. `ServiceRequestService.ToDto` converts a `ServiceRequest` into a `ServiceRequestDto`.
  This prevents leaking fields (such as password hashes through navigation properties) and cyclic JSON.
* **Validation:** DataAnnotations attributes on the DTOs (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]`, `[EnumDataType]`). `[ApiController]` returns **400** automatically.
  Business validation (category exists, assignee is staff, valid status change) is done in the service/domain and reported as `DomainException` → **400**.
* **Centralised error handling:** `Middleware/ExceptionHandlingMiddleware.cs` is the only place with `try/catch`.

  | Exception | Status |
  |---|---|
  | `NotFoundException` | 404 |
  | `ForbiddenException` | 403 |
  | `DomainException` | 400 |
  | anything else | 500 with a generic message; the real error is only written to the log |

* **Swagger:** `Extensions/SwaggerExtensions.cs` adds the *Authorize* button so you can paste a JWT and test protected endpoints.

## 8. Security

### 8.1 Passwords – ASP.NET Core Identity

`UserManager.CreateAsync(user, password)` hashes the password (PBKDF2 with a salt). Only the **hash** is stored (`AspNetUsers.PasswordHash`).
Identity also enforces the password rules (6+ characters, a capital letter, a digit and a symbol) and **locks an account after 5 wrong passwords**
(`AuthService.LoginAsync` calls `AccessFailedAsync` / `IsLockedOutAsync`; covered by a test).

### 8.2 Two roles, created safely

Roles `Student` and `Staff` are created at start-up by `Seed/DbSeeder.cs`.
`RegisterDto` has **no role field**, and `AuthService.RegisterAsync` always adds the `Student` role, so nobody can register as staff
(there is a test that sends `"role": "Staff"` and checks that it is ignored). The staff account comes from the `SeedStaff` user secrets.

### 8.3 JWT access token (beginner explanation)

After login the API gives the client an **access token**. Think of it as a **signed ID card with an expiry time**.
It contains *claims*: user id, name, email and **role**. The API signs it with the secret `Jwt:Key`.
The client sends it with every request: `Authorization: Bearer <token>`.

The API checks (`Extensions/AuthenticationExtensions.cs`): the signature is valid (nobody changed the claims), the **issuer** and **audience** match, and it is **not expired**
(30 minutes, `ClockSkew = 0`). A tampered token gives **401** (tested live).
Created in `Auth/TokenService.cs`.

### 8.4 Refresh token – rotation and revocation (beginner explanation)

A 30-minute access token is safe (a stolen one stops working soon) but it would be annoying to type the password every 30 minutes.
So login also returns a **refresh token** – a long random text valid for **7 days**. When the access token is about to expire, the app sends the refresh token to
`POST /api/auth/refresh` and receives a **new access token and a new refresh token**.

| Idea | How it is done here |
|---|---|
| Stored safely | Only a **SHA-256 hash** of the refresh token is saved in the `RefreshTokens` table (`TokenService.Hash`), so a database leak does not reveal usable tokens. |
| **Rotation** | Every refresh **uses up** the old refresh token (`RevokedAt` is set) and issues a new one (`AuthService.RefreshAsync`). Using the old one again gives **401** (test: `Refresh_GivesNewTokens_AndTheOldRefreshTokenStopsWorking`). |
| **Revocation** | `POST /api/auth/logout` sets `RevokedAt`, so the token can never be used again (test: `Logout_RevokesTheRefreshToken`). A token also stops working at `ExpiresAt`. |
| In the app | `ApiClient.GetValidAccessTokenAsync` refreshes when less than 1 minute is left. If the refresh token is rejected, the session is deleted and the user is sent to the login page. |

### 8.5 Authorization is on the SERVER (three layers)

Hiding a button in the app is **not** security – anyone can call the API with Swagger, curl or Postman. So the API checks everything itself:

1. **Role check** – `[Authorize(Roles = Roles.Staff)]` on `assign`, `status` and `GET /api/staff`; `[Authorize(Roles = Roles.Student)]` on create/edit. Wrong role → **403**. No/invalid token → **401**.
2. **Ownership check** – `ServiceRequestService.GetByIdAsync` / `UpdateAsync` compare the request's `StudentId` with the id in the token. A student touching another student's request → **403**.
   The user id always comes from the **token**, never from the request body, so it cannot be faked.
3. **Business rules** – the Domain refuses invalid status changes (`ServiceRequest.ChangeStatus`) and assigning to a non-staff user.

The MAUI app also hides links by role (`NavMenu.razor`, `[Authorize(Roles = ...)]` on pages) only for convenience.
These checks were demonstrated against the live API with raw HTTP calls (student token on staff endpoints → 403, other student's request → 403).

### 8.6 Secrets

| Secret | Where it lives | In Git? |
|---|---|---|
| Database password | `dotnet user-secrets` (or environment variable) | **No** |
| `Jwt:Key` | `dotnet user-secrets` (or environment variable) | **No** |
| Staff seed password | `dotnet user-secrets` | **No** |
| User's access/refresh token on the device | MAUI **`SecureStorage`** (encrypted by Windows / Android) – see `SessionStore.cs` | n/a |

The app does **not** use browser `localStorage` for tokens. If `SecureStorage` is not available on a device, `SessionStore` falls back to keeping the session in memory only (the user logs in again after restarting the app).
`appsettings.json` contains no password and no key. `.gitignore` also blocks `secrets.json`, `.env` and key files.

## 9. MAUI Blazor Hybrid client

* **Pages** (`Components/Pages`): Login, Register, Student Dashboard, Create Request, My Requests, Request Details, Staff Dashboard, Manage Requests.
* **API communication is separate from the pages:** pages call `ApiClient` (a typed `HttpClient`). They never use `HttpClient` directly.
  `ApiClient` turns error answers into `ApiException` with a friendly message, so each page just does `catch (ApiException ex) { errorMessage = ex.Message; }`.
* **Authentication state:** `SessionStore` keeps the tokens; `AppAuthStateProvider` turns the stored session into a `ClaimsPrincipal` for Blazor.
  `<AuthorizeView Roles="...">` and `[Authorize]` then work as in any Blazor app. When the session changes (login / logout / expired) `SessionStore.Changed` makes Blazor re-check, so the app redirects to the login page automatically.
* **Reuse:** the app reuses the DTOs from `Application` (including their validation attributes, which drive the form messages) and `StatusRules` from `Domain`
  (so the status drop-down only offers valid changes; the API still enforces the rules).
* **Targets:** Windows and Android. The API address is in `Services/ApiSettings.cs`.

## 10. Testing

`CampusServices.Tests` (xUnit):

* **API tests** (`ServiceRequestApiTests`, `AuthApiTests`, `ErrorHandlingTests`) start the *real* API in memory with `WebApplicationFactory<Program>` and call it with `HttpClient`.
  Only the database is replaced (EF Core InMemory instead of PostgreSQL), so controllers, JWT, authorization and middleware are the real code.
* **Domain tests** (`ServiceRequestDomainTests`) test the status rules without any API or database.

Not covered by automated tests: the PostgreSQL provider itself (the SQL was reviewed with `dotnet ef migrations script`), and the MAUI pages
(these were checked by hand against the running API).

## 11. Limitations and ideas for improvement

* **Refresh-token reuse detection:** if a *used* refresh token is presented again we just answer 401. A stricter design would also revoke every token of that user (possible theft).
* **Cleanup:** expired refresh tokens stay in the table; a background job could delete them.
* **HTTPS:** during development the app uses plain `http://localhost` (and Android cleartext traffic). A real deployment must use HTTPS only.
* **No email confirmation / password reset** (not required by the assignment).
* **No rate limiting** beyond the Identity account lockout.
* **Staff accounts** are created by seeding only; there is no admin screen to add more.
* **The app has no screen to edit a request** (the API supports it: `PUT /api/requests/{id}`).
* **Two simultaneous requests** at the exact moment the access token expires could both try to refresh; the second would fail and the user would log in again. The pages load data one call after another, so this does not happen in normal use.
* iOS and Mac Catalyst were removed from the MAUI project (they need a Mac).
