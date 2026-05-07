# Sophie Beauty App — Design and Planning Document

## 1. Scope
This document explains the Sophie Beauty system as a full product, using:
- **Backend repository:** `Fmcivor/sophieBeautyApiPublic` (analyzed directly)
- **Frontend repository:** Sophie Beauty frontend (not present in this workspace; frontend details below are based on backend integration points and should be validated against frontend code)

## 2. Product Overview
Sophie Beauty is an appointment booking platform with:
- Public customer booking flow
- Admin authentication and management area
- Availability management
- Stripe deposit/payment handling
- Automated booking expiry handling
- Customer/admin email notifications

## 3. High-Level Architecture

### 3.1 Runtime Architecture
- **Frontend**: web UI (local dev origin in API CORS includes `http://localhost:3000`; production domains include `https://shapedbysophiee.netlify.app` and `https://www.shapedbysophiee.com`)
- **Backend API**: ASP.NET Core 8 Web API
- **Database**: MongoDB (`SophieBeauty` database)
- **Payments**: Stripe PaymentIntent
- **Email**: Azure Communication Services Email
- **Secrets/config**: Azure Key Vault + app configuration
- **Hosting/deploy**: Azure Web App + GitHub Actions workflow

### 3.2 Backend Layering
- **Controllers**: HTTP entry points and response mapping
- **Services**: business logic (booking, availability, payment, admin auth, email)
- **Repositories**: MongoDB persistence implementation
- **Models/DTOs**: domain entities and request payloads

## 4. Backend Codebase Design (`sophieBeautyApiPublic`)

## 4.1 Core Modules
- **Booking**
  - Booking creation (customer/admin)
  - Time-slot conflict validation
  - Revenue summaries
  - Expiry status checks
- **Availability slots**
  - Daily slot setup and overlap prevention
  - Available-time generation in 30-minute intervals
- **Treatments & categories**
  - CRUD for treatment catalog
  - Category list/create/delete
- **Admin auth**
  - Login + JWT generation
  - JWT verification endpoint
- **Payments**
  - Stripe PaymentIntent creation
  - Stripe webhook processing for success/failure/requires action
- **Email**
  - Confirmation, cancellation, reminder, and internal notification templates
- **Background processing**
  - Hosted service runs every minute
  - Marks expired bookings and removes old expired records

### 4.2 Data Model Summary
Primary collections in MongoDB (`SophieBeauty`):
- `bookings`
- `availabilitySlots`
- `services` (treatments)
- `categories`
- `admins`

Important booking states:
- `Confirmed`
- `Completed`
- `DepositPending`
- `Expired`
- `RequiresAction`
- `FailedRetryable`
- `Processing`

## 5. End-to-End Functional Flows

### 5.1 Customer Booking Flow
1. Frontend submits booking request (`/booking/Create`)
2. API computes total price and duration from treatment IDs
3. API verifies availability window and overlap
4. If no deposit needed, booking becomes confirmed and confirmation email is sent
5. If deposit needed, Stripe PaymentIntent is created and `clientSecret` is returned
6. Frontend confirms payment via Stripe client
7. Stripe webhook updates booking status and triggers confirmation email on success

### 5.2 Admin Flow
1. Admin logs in (`/admin/login`) and receives JWT
2. Frontend sends JWT in protected requests
3. Admin creates/updates/deletes bookings, treatment data, and availability slots
4. Admin can trigger reminders (`/admin/remindBookings`)

### 5.3 Booking Expiry Flow
1. New deposit-pending bookings receive expiry timestamp
2. Background service checks every minute
3. Expired pending/retryable bookings are marked `Expired`
4. Old expired bookings are deleted after retention window

## 6. Frontend Integration Contract (to validate in frontend repo)
The frontend should be structured around these API contracts:
- **Public pages**
  - Treatment browsing
  - Availability-time lookup endpoint integration (current API route: `/availablilitySlot/availableTimes`)
  - Booking creation and payment completion UX
- **Admin pages**
  - Auth (login, JWT storage/refresh strategy)
  - Booking management dashboard
  - Revenue widgets (weekly/monthly)
  - Availability and treatment management

Required frontend concerns:
- Consistent UTC/local time handling for booking times (API converts using UK timezone)
- Stripe Elements/payment UX with retry handling
- Protected route guard for admin pages using JWT
- Error-state mapping for API error strings (e.g., `TAKEN`, `SERVER_ERROR`)

## 7. Environment and Configuration Design
Current backend dependencies require:
- Mongo connection string (`mongoDB-conn`)
- JWT secret (`jwtSecret`)
- Stripe secret key (`StripeSecretKey`)
- Azure Email connection string (`AzureEmailConnString`)
- Azure Key Vault access from runtime identity

Recommendation:
- Keep secrets only in Key Vault / secure host settings
- Maintain separate dev/staging/prod configuration sets

## 8. Delivery and Operations
- Backend CI/CD is configured through GitHub Actions (`.github/workflows/main_sophiebeautyapi.yml`)
- Swagger is enabled for API exploration
- CORS policy currently permits local dev + production frontend domains

## 9. Current Gaps / Technical Debt Observed
1. **Test project reference mismatch**: test project currently references `../SophieBeautyApi/SophieBeautyApi.csproj`, but the actual API project file is `../sophieBeautyApi/sophieBeautyApi.csproj`, causing test failures in this workspace.
2. **Naming consistency**: class/type naming is inconsistent (lowercase class names etc.), reducing maintainability.
3. **Webhook hardening opportunity**: webhook parsing exists, but endpoint should be reviewed for signature verification strategy and stricter failure handling.
4. **API consistency opportunities**: route naming and casing can be standardized across controllers.

## 10. Planning Roadmap (Backend + Frontend)

### Phase 1 — Stabilize Contracts
- Finalize and document API request/response contracts for booking and payment lifecycle
- Align frontend API client models with backend DTOs
- Normalize timezone rules and shared date-handling conventions

### Phase 2 — Reliability and Security
- Fix test project path issue and restore passing automated tests
- Add/strengthen integration tests for booking/payment status transitions
- Harden webhook verification and payment failure/retry edge cases
- Review JWT lifetime/session behavior in frontend and backend

### Phase 3 — UX and Admin Improvements
- Improve booking/payment retry UX and status messaging
- Expand admin analytics views (appointment + revenue trends)
- Add operational monitoring for failed email/payment/webhook paths

### Phase 4 — Scalability and Maintainability
- Standardize naming conventions and API route patterns
- Add API versioning strategy if major frontend changes are planned
- Introduce observability dashboarding for booking funnel metrics

## 11. Next Action Needed to Complete Full Cross-Repo Design
To fully ground this as a strict **two-repo** design document, the frontend repository code should be reviewed next and this document updated with:
- Actual frontend architecture (framework, folder structure, state management)
- Real page/component map
- Actual API client layer and auth/token storage strategy
- Build/deploy pipeline details for frontend
