# Sophie Beauty App — Design and Planning Document

## 1. Scope
This document explains the Sophie Beauty system as a full product, using:
- **Backend repository:** `Fmcivor/sophieBeautyApiPublic` (analyzed directly)
- **Backend API project folder in this repo:** `sophieBeautyApi`
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

### 4.1 Core Modules
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

### 5.4 Explicit Business Rules (Current Implementation)

#### Booking Rules
- A booking request must include customer name, email, appointment date/time, treatment IDs, and payment method choice.
- Customer name is limited to alphabetic words; email and phone number formats are validated.
- Booking price and duration are derived from selected treatments in the backend (not trusted from client input).
- Combined treatment duration is rounded **up** to the next 30-minute boundary.
- Booking end time is computed as `appointmentDate (UK local time) + duration`.
- Public booking starts in `DepositPending` unless the calculated total cost is below £2, in which case it is immediately `Confirmed`.
- Admin-created booking is created as `Confirmed`.

#### Time-Slot and Availability Rules
- Appointment validity is checked against configured availability slots for that date.
- Slot matching uses UK local time conversion for business-hour checks.
- Booking start must fall within an availability slot, and booking must not overlap another non-expired booking (`bookingStart < existingEnd && bookingEnd > existingStart`).
- Availability-time responses are generated in 30-minute increments and exclude overlapping times.
- Overlapping availability-slot definitions for the same date are rejected.
- `availableTimes` uses requested `bookingDuration` to evaluate conflicts; consumer should pass the full rounded service duration.

#### Payment Rules
- Deposit is calculated as 25% of booking total (rounded) and charged in pence through Stripe PaymentIntent.
- Stripe metadata includes booking ID and customer name for reconciliation.
- If Stripe intent creation fails after reservation, the reservation is rolled back (booking removed).
- Webhook `payment_intent.succeeded` confirms the booking and triggers confirmation email.
- Webhook `payment_intent.payment_failed` marks booking retryable only for retryable failure types and only when enough time remains before expiry.
- Webhook `payment_intent.requires_action` sets status to `RequiresAction` and extends a short retry window.

#### Expiration Rules
- New bookings are assigned a short expiry window for deposit completion.
- Deposit-pending/retryable/requires-action bookings become `Expired` when expiry time passes.
- Business logic intentionally subtracts a small buffer when returning expiry time to client-facing flows.
- A background worker enforces expiry transitions every minute.

#### Deletion and Retention Rules
- Manual booking deletion returns success only when the booking exists and delete operation succeeds.
- Manual booking deletion sends a cancellation email to the customer.
- Expired bookings are retained briefly, then auto-deleted after the retention threshold (currently two days).
- Availability slots can be deleted individually or fully cleared via admin flow.

## 6. Frontend Integration Contract (to validate in frontend repo)
The frontend should be structured around these API contracts:
- **Public pages**
  - Treatment browsing
  - Availability-time lookup endpoint integration
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
1. **Test project reference mismatch (workspace-observed)**: in this analysis workspace, the test project references `../SophieBeautyApi/SophieBeautyApi.csproj` while the API project file is `../sophieBeautyApi/sophieBeautyApi.csproj`; verify against CI/main to confirm whether this is branch-specific or a repository-wide issue.
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

## 12. Booking End-Time and Validation Details (Implementation-Level)

### 12.1 End-Time Calculation
- End time is not persisted as its own field; it is derived from:
  - `start = booking.appointmentDate`
  - `duration = booking.duration` (minutes, rounded up to 30-minute boundary)
  - `end = start + duration`
- Overlap and available-time checks convert booking times to UK timezone (`GMT Standard Time`) before comparing time-of-day windows.

### 12.2 Validation Paths
- **Model validation (`[ApiController]` + DataAnnotations)**:
  - `newBookingDTO.customerName` regex: letters and spaces only.
  - `newBookingDTO.email` regex: email format required.
  - `newBookingDTO.phoneNumber` regex (if supplied): UK-style `0` + 10 digits.
  - required fields enforced by `[Required]`.
- **Service validation (booking creation)**:
  - Treatment IDs are resolved server-side; total cost and duration are recalculated in backend.
  - Duration is normalized to 30-minute increments.
  - Slot validation checks:
    - there is a slot for that date/time,
    - start time is inside a configured slot,
    - computed booking window does not overlap existing non-expired bookings.
- **Controller-level response mapping**:
  - invalid model -> `400`
  - slot conflict -> `400` with `TAKEN`
  - persistence/internal failure -> `500`

### 12.3 Expiry/Retry Validation
- New deposit bookings default to short reservation expiry.
- Client-facing expiry endpoints subtract 25 seconds as a display buffer.
- Retry is allowed only when booking is not confirmed/expired and expiry has not passed.
- Stripe failed payments become:
  - `FailedRetryable` for retryable declines with enough remaining time (>= 60 seconds),
  - otherwise `Expired`.

## 13. Models and DTO Field Reference

### 13.1 `booking` (collection: `bookings`)
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `Id` | `string?` | No | Mongo ObjectId representation | Primary key |
| `customerName` | `string` | Yes | Regex: letters/spaces | Customer full name |
| `email` | `string` | Yes | Email regex | Contact and Stripe receipt email |
| `phoneNumber` | `string?` | No | Regex: `^0\\d{10}$` when provided | Optional contact |
| `appointmentDate` | `DateTime` | Yes | Required | Stored UTC in DB; often converted to UK local for responses |
| `treatmentNames` | `List<string>` | Yes | Required | Resolved from treatment IDs at creation |
| `duration` | `int` | Yes | Required | Minutes; rounded up to next 30-minute block |
| `cost` | `int` | Yes | Required | Total treatment cost (GBP units) |
| `payByCard` | `bool` | Yes | Required | Booking payment preference |
| `paid` | `bool` | Yes | Required | Set true when pay-by-card flow is used |
| `bookingStatus` | `booking.status` | Yes | Required | Lifecycle state |
| `stripeId` | `string?` | No | None | Optional legacy/external stripe reference field |
| `reminderSent` | `bool` | Yes | Required | Admin reminder tracking |
| `remainingPayment` | `int` | Yes | Required | Initialized as `cost - round(cost*0.25)` |
| `expiryDate` | `DateTime` | Yes | Required | Defaults to now + 5m25s; used for reservation expiry |
| `stripePaymentId` | `string?` | No | None | Stripe PaymentIntent ID |

`booking.status` enum values:
- `Confirmed`, `Completed`, `DepositPending`, `Expired`, `RequiresAction`, `FailedRetryable`, `Processing`

### 13.2 `newBookingDTO`
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `customerName` | `string` | Yes | Name regex | Booking requester |
| `appointmentDate` | `DateTime` | Yes | Required | Requested start time |
| `treatmentIds` | `List<string>` | Yes | Required, ObjectId representation | Used to calculate cost/duration |
| `email` | `string` | Yes | Email regex | Customer email |
| `payByCard` | `bool` | Yes | Required | Drives paid/deposit path |
| `phoneNumber` | `string?` | No | UK phone regex when provided | Optional |

### 13.3 `availabilitySlot` (code class: `availablilitySlot`, collection: `availabilitySlots`)
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `Id` | `string?` | No | Mongo ObjectId representation | Primary key |
| `date` | `string` | Yes | Expected `yyyy-MM-dd` in service parsing | Slot day |
| `startTime` | `TimeSpan` | Yes | Required | Slot start (local business time) |
| `endTime` | `TimeSpan` | Yes | Required | Slot end (local business time) |

### 13.4 `availableTimesRequest`
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `date` | `DateTime` | Yes | Required | Date queried for availability |
| `bookingDuration` | `int` | Yes | Required | Duration (minutes) used for overlap checks |

### 13.5 `treatment` (collection: `services`)
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `Id` | `string?` | No | Mongo ObjectId representation | Primary key |
| `name` | `string` | Yes | Required | Treatment name |
| `price` | `int` | Yes | Required | GBP units |
| `duration` | `int` | Yes | Required | Minutes |
| `type` | `string` | Yes | Required | Category/type label |
| `description` | `string` | Yes | Min 20 / Max 150 chars | Public-facing description |

### 13.6 `category` (collection: `categories`)
| Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|
| `Id` | `string?` | No | Mongo ObjectId representation | Primary key |
| `name` | `string` | Yes | Required | Category label |

### 13.7 `admin` and `adminDTO` (collection: `admins`)
| Model | Field | Type | Required | Validation/Rule | Notes |
|---|---|---|---|---|---|
| `admin` | `Id` | `string?` | No | Mongo ObjectId representation | Primary key |
| `admin` | `username` | `string` | Yes | Required | Login identifier |
| `admin` | `password` | `string` | Yes | Required | Stored hashed password |
| `admin` | `salt` | `byte[]` | Yes | Required | Password hashing salt |
| `adminDTO` | `username` | `string` | Yes | Required | Login input |
| `adminDTO` | `password` | `string` | Yes | Required | Login input |

### 13.8 `BookingResult`
| Field | Type | Required | Notes |
|---|---|---|---|
| `Booking` | `booking?` | No | Populated on success |
| `Error` | `string?` | No | Populated on failure |
| `IsSuccess` | `bool` | Yes | Success/failure discriminator |

## 14. Endpoint Reference Table

> Note: availability routes are currently exposed under legacy controller spelling `/availablilitySlot`.

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/booking/Allbookings` | Yes | Returns all bookings (admin view). |
| POST | `/booking/Create` | No | Creates a customer booking, validates slot, may create Stripe intent, returns booking + optional `clientSecret`. |
| POST | `/booking/Create-Admin` | Yes | Creates an admin booking (confirmed path). |
| PUT | `/booking/Update` | Yes | Updates an existing booking object. |
| DELETE | `/booking/Delete/{id}` | Yes | Deletes booking by id and sends cancellation email. |
| POST | `/booking/todaysAppts` | Yes | Returns appointments for provided day. |
| POST | `/booking/upcomingAppts` | Yes | Returns upcoming appointments from provided day. |
| POST | `/booking/lastWeeksRevenue` | Yes | Returns previous week revenue (confirmed bookings). |
| POST | `/booking/lastMonthsRevenue` | Yes | Returns previous month revenue (confirmed bookings). |
| POST | `/booking/expired` | No | Checks and updates whether a pending booking is expired. |
| GET | `/booking/{bookingId}/bookingStatus` | No | Returns simplified status (`Confirmed`, `Expired`, `Pending`). |
| GET | `/booking/{bookingId}/canRetryPayment` | No | Indicates whether payment retry is currently allowed. |
| PUT | `/booking/{bookingId}/markExpired` | No | Triggers expired-booking processing pass. |
| GET | `/booking/{bookingId}/expiryTime` | No | Returns booking expiry time with 25-second display buffer. |
| GET | `/availablilitySlot` | No | Returns all availability slots. |
| POST | `/availablilitySlot/create` | Yes | Creates availability slot; rejects overlap for same date. |
| DELETE | `/availablilitySlot` | Yes | Deletes a specific availability slot from request body. |
| DELETE | `/availablilitySlot/deleteAll` | Yes | Deletes all availability slots (dev/admin utility). |
| POST | `/availablilitySlot/availableTimes` | No | Returns free start times for `date` + `bookingDuration`. |
| GET | `/treatment/AllTreatments` | No | Lists all treatments. |
| POST | `/treatment/Create` | Yes | Creates treatment. |
| PUT | `/treatment/Update` | Yes | Updates treatment. |
| DELETE | `/treatment/Delete/{id}` | Yes | Deletes treatment by id. |
| GET | `/category` | No | Lists all categories. |
| POST | `/category` | No (current implementation; recommended admin-only) | Creates category; returns conflict if duplicate. |
| DELETE | `/category` | No (current implementation; recommended admin-only) | Deletes category from request body. |
| POST | `/admin/login` | No | Authenticates admin and returns JWT. |
| GET | `/admin/verify` | Yes | Verifies JWT validity. |
| POST | `/admin/remindBookings` | Yes | Sends reminders for relevant bookings. |
| POST | `/api/paymentWebHook` | No (Stripe webhook) | Processes Stripe payment events and updates booking status. |
