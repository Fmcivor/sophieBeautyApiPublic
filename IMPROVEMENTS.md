# Shaped by Sophie API — Future Improvements

This document catalogues suggested improvements for the Sophie Beauty API, grouped by priority.  
No existing code has been modified; this file is purely a planning reference.

---

## Table of Contents

1. [Test Coverage](#1-test-coverage)
2. [Security Fixes](#2-security-fixes)
3. [Bug Fixes](#3-bug-fixes)
4. [Code Quality & Maintainability](#4-code-quality--maintainability)
5. [New Features](#5-new-features)
6. [Logging & Observability](#6-logging--observability)
7. [Deployment & DevOps](#7-deployment--devops)
8. [Nice-to-Have Polish](#8-nice-to-have-polish)

---

## 1. Test Coverage

The test project (`SophieBeautyApi.Tests`) exists and is wired up with **xUnit + coverlet** but contains **zero test files**.  
The goal should be at least **80 % line coverage** on the service and controller layers.

### 1.1 Unit Tests — Services

| Class | What to test |
|---|---|
| `bookingService` | `CreateBooking`, `GetAllBookings`, `GetBookingsByDate`, `DeleteBooking`, `UpdateBooking`, revenue calculations with edge-case date ranges |
| `treatmentService` | CRUD operations, `GetManyByIds` with empty / partial ID lists |
| `categoryService` | CRUD, duplicate-name guard |
| `availabilitySlotService` | Overlap detection, `GetAvailableTimes` time-range arithmetic, empty-slot edge cases |
| `adminService` | Password hashing and verification (PBKDF2), login success / failure paths |
| `jwtTokenHandler` | Token generated with correct claims, issuer, audience, and 1-hour expiry |
| `emailService` | Mock Azure `EmailClient`; verify correct recipient, subject, and body substitutions for all four email types (confirmation, cancellation, reminder, new-booking notification) |

### 1.2 Unit Tests — Controllers

Each controller action should be tested in isolation by mocking the underlying service:

- `bookingController` — all eight endpoints; check status codes (201, 200, 204, 400, 404, 409)
- `adminController` — login success/failure (returns JWT vs. 401), verify endpoint, remindBookings trigger
- `treatmentController` — CRUD endpoints and ModelState validation
- `categoryController` — CRUD endpoints including duplicate-name conflict (409)
- `availabilitySlotController` — create/delete/overlap conflict, available-times calculation

### 1.3 Integration Tests

- Spin up an in-memory MongoDB instance (e.g. **Mongo2Go** or **Testcontainers-dotnet**) and exercise the full request → service → database path for the most critical flows:
  - Create booking → confirm slot is marked taken
  - Delete booking → slot freed
  - Revenue endpoint returns correct totals

### 1.4 CI Test Step

Add a `dotnet test --collect:"XPlat Code Coverage"` step to the GitHub Actions workflow so tests run on every push and a coverage report is published as a workflow artifact.

---

## 2. Security Fixes

### 2.1 Category Controller Missing `[Authorize]`

The `POST /category` and `DELETE /category` endpoints are publicly accessible — anyone can create or delete categories.  
Add `[Authorize]` to both actions (or to the controller class) the same way the booking and treatment controllers are protected.

### 2.2 Hardcoded Issuer / Audience URL

`jwtTokenHandler.cs` hardcodes the production Azure URL as the JWT issuer and audience.  
Move this value to configuration (`appsettings.json` / Azure Key Vault key `jwtIssuer`) so it resolves correctly in development and staging environments.

### 2.3 Hardcoded CORS Origins

The CORS policy in `Program.cs` lists specific localhost IPs and ports.  
Read allowed origins from configuration so the list can be updated without redeployment and so dev/staging/prod can differ.

### 2.4 No Refresh Token Mechanism

JWT tokens expire after 1 hour with no refresh path.  
Implement a `/admin/refresh` endpoint that issues a new short-lived access token in exchange for a longer-lived refresh token, rather than forcing the admin to re-login.

### 2.5 No Rate Limiting

The booking creation endpoint (`POST /booking/Create`) has no throttling.  
Add ASP.NET Core 7+ `RateLimiter` middleware (or the `AspNetCoreRateLimit` package) to limit requests per IP and prevent spam bookings or brute-force attempts on the login endpoint.

### 2.6 Email / Notification Addresses Hardcoded

`emailService.cs` hardcodes `DoNotReply@shapedbysophiee.com` and `info@beautybysophieee.com`.  
Move these to Azure Key Vault or `appsettings.json` so they can be changed without a redeploy.

---

## 3. Bug Fixes

### 3.1 Wrong Status Code on `deleteAll` Success

`availabilitySlotController.DeleteAll()` returns `NotFound()` on the success path.  
It should return `NoContent()` (204) when all slots are deleted successfully.

### 3.2 Inconsistent Revenue Booking-Status Filter

`lastWeeksRevenue` filters by `BookingStatus.Completed` (correct) but `lastMonthsRevenue` filters by `BookingStatus.Confirmed`.  
Both endpoints should use the same status (`Completed`) so revenue figures are consistent.

### 3.3 No Validation of Past Booking Dates

`newBookingDTO` does not prevent customers from booking dates in the past.  
Add a custom validation attribute or a service-layer check that rejects any booking where the requested date is before today.

### 3.4 Treatment Duration / Price Not Validated

A negative duration or zero/negative price can be saved without error.  
Add `[Range(1, int.MaxValue)]` (or appropriate bounds) to `treatment.cs` for both `price` and `duration`.

---

## 4. Code Quality & Maintainability

### 4.1 Introduce Repository / Service Interfaces

Services (`bookingService`, `treatmentService`, etc.) are registered as concrete classes.  
Extract `IBookingService`, `ITreatmentService`, etc. interfaces and inject those.  This makes unit-testing with mocks straightforward and decouples controllers from implementation details.

### 4.2 Centralise Timezone Conversion

GMT Standard Time conversion is duplicated across `bookingController` and `adminController`.  
Extract a small `DateTimeHelper` or `IDateTimeProvider` class that encapsulates all UTC ↔ local-time logic.

### 4.3 Add Timestamps to Entities

No entity records when it was created or last modified.  
Add `CreatedAt` and `UpdatedAt` (UTC `DateTime`) fields to `booking`, `treatment`, and `category` so there is an audit trail.

### 4.4 Extend `BookingStatus` Enum

The current enum only has `Confirmed` and `Completed`.  
Adding `Cancelled` and `NoShow` would allow cancelled bookings to be retained in the database for reporting rather than deleted, and would make revenue logic more accurate.

### 4.5 Remove or Document Dead / Commented-Out Code

Several large code blocks are commented out:
- Admin registration endpoint (`adminController.cs`)
- Special booking endpoint (`bookingController.cs` lines ~115–148)
- Old available-times endpoint (`bookingController.cs`)

Either delete them (git history preserves them) or add a `// TODO:` comment explaining why they are disabled and what needs to happen before they can be re-enabled.

### 4.6 Correct the Typo in `availablilitySlot`

The folder name, class names, and routes all contain the misspelling `availablility` (extra `l`).  
Rename to `availabilitySlot` for consistency.  This is a breaking API change so it should be coordinated with any consuming front-end code.

### 4.7 Fix Swagger Title

`Program.cs` registers the Swagger document with the placeholder title `"Your API"`.  
Change it to `"Shaped by Sophie API"` and fill in the version, description, and contact fields.

### 4.8 Add FluentValidation (Optional Alternative)

Consider replacing DataAnnotation validation with **FluentValidation** for richer, testable validation rules that live outside the model classes.

### 4.9 Cascading Deletes / Referential Integrity

Deleting a treatment does not clean up bookings that reference that treatment.  
Before deleting a treatment, either reject the delete if active bookings exist, or cascade-update those bookings to flag the treatment as removed.

---

## 5. New Features

### 5.1 Customer-Facing Booking Cancellation

Currently only admins can cancel bookings via the authenticated DELETE endpoint.  
Add a public `/booking/cancel/{token}` endpoint that accepts a time-limited signed token (sent to the customer in their confirmation email) so customers can self-cancel without admin involvement.

### 5.2 Payment Integration (Stripe)

A `stripeId` field already exists on `booking.cs` but is unused, and a Stripe using statement is commented out in `Program.cs`.  
Integrate Stripe Checkout or Payment Intents so the `payByCard` flag triggers real payment collection at booking time.

### 5.3 Recurring / Bulk Availability Slot Generation

Admins currently create one availability slot at a time.  
Add a `POST /availabilityslot/createRange` endpoint that accepts a date range and a weekly schedule (e.g., Monday–Friday 09:00–17:00) and generates all slots in bulk.

### 5.4 Booking Waitlist

When a requested time slot is already taken, add the customer to a waitlist.  
If the original booking is cancelled, automatically notify the first waitlisted customer.

### 5.5 Admin Dashboard Metrics Endpoint

Consolidate the four separate revenue / appointment analytics endpoints into a single `GET /admin/dashboard` response with today's appointments, upcoming count, last-week and last-month revenue, and a treatment popularity breakdown.

### 5.6 Treatment / Category Soft Delete

Instead of permanently deleting treatments and categories, add a boolean `isArchived` flag.  
Archived treatments are hidden from the public list but can be restored by an admin, and historical bookings remain valid.

### 5.7 Multi-Admin Support

Admin registration is commented out; only one admin appears to exist in the database.  
Implement a proper admin management system: a super-admin can create, activate, and deactivate other admin accounts, each with an `isActive` flag and a last-login timestamp.

### 5.8 Customer Booking History / Lookup

Add a public `GET /booking/lookup` endpoint where a customer can enter their email address and receive a list of their upcoming bookings (no sensitive data exposed) so they can self-service reschedule or cancel.

### 5.9 API Versioning

Add route-based versioning (`/v1/`, `/v2/`) using `Asp.Versioning.Mvc` so breaking changes can be introduced in a new version without affecting existing front-end consumers.

### 5.10 Webhook / Event Notifications

Publish booking-lifecycle events (created, cancelled, completed) to an Azure Service Bus topic so other systems (loyalty points, CRM, analytics) can subscribe without tightly coupling to the API.

---

## 6. Logging & Observability

### 6.1 Replace Console.WriteLine with Structured Logging

`emailService.cs` logs errors with `Console.WriteLine`.  
Integrate **Serilog** (or `Microsoft.Extensions.Logging` sinks) with sinks for:
- Console (development)
- Azure Application Insights (production)

### 6.2 Request / Response Logging Middleware

Add a middleware that records the HTTP method, path, status code, and duration for every request.  
This makes it easy to spot slow endpoints and unexpected errors in production.

### 6.3 Health-Check Endpoint

Register `services.AddHealthChecks()` with checks for MongoDB connectivity and Azure Email service reachability.  
Expose `GET /health` so Azure App Service and external monitoring tools can verify the API is operational.

### 6.4 Application Insights Integration

Wire `Microsoft.ApplicationInsights.AspNetCore` into `Program.cs` for automatic dependency tracking (MongoDB calls, outbound HTTP), exception telemetry, and performance counters, visible in the Azure portal.

---

## 7. Deployment & DevOps

### 7.1 Add Test Stage to CI Pipeline

The current GitHub Actions workflow builds and deploys but **never runs tests**.  
Insert a `dotnet test` step between build and publish so a failing test blocks deployment.

### 7.2 Staging Slot Deployment

Add a staging deployment slot in Azure App Service and deploy to staging first, with a manual approval gate before swapping to production.

### 7.3 Separate `appsettings` per Environment

Create `appsettings.Development.json` and `appsettings.Production.json` (both git-ignored) to manage environment-specific settings (CORS origins, log levels) without relying solely on Key Vault.

### 7.4 Docker Support

Add a `Dockerfile` and `docker-compose.yml` so the API can be run locally with a local MongoDB container, removing the dependency on Azure Key Vault during development.

### 7.5 Dependency Vulnerability Scanning

Add a `dotnet list package --vulnerable` step to the CI pipeline (or use Dependabot) to flag known CVEs in NuGet packages automatically.

---

## 8. Nice-to-Have Polish

### 8.1 OpenAPI / Swagger Enhancements

- Set a meaningful API title, description, and contact info in `Program.cs`.
- Add `[ProducesResponseType]` attributes to every controller action so Swagger shows all possible response schemas.
- Enable JWT auth in the Swagger UI so endpoints can be tested directly from the browser.

### 8.2 Remove the `TestavailableTimes` Endpoint from Production

`POST /availablilityslot/TestavailableTimes` is labelled as a test endpoint.  
Either rename it to `availableTimes` (the preferred implementation) and remove the older `availableTimes` endpoint, or gate it behind `[Authorize]`/environment checks so it is not publicly callable in production.

### 8.3 Localisation / Internationalisation

Email templates are in English only.  
If the business expands, consider a resource-file approach for email content so templates can be translated without code changes.

### 8.4 Pagination on List Endpoints

`GET /booking/Allbookings` and `GET /treatment/AllTreatments` return every document in the collection.  
Add optional `page` and `pageSize` query parameters so these endpoints remain fast as the database grows.

### 8.5 Customer-Friendly Error Messages

API errors currently return raw `ModelState` dictionaries or bare status codes.  
Introduce a consistent error response envelope (`{ "status": 400, "errors": [...] }`) using a problem-details formatter (`AddProblemDetails()`) so consuming front-ends can display user-friendly messages.

---

*Last updated: March 2026*
