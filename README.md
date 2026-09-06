# Training Management System (TMS)

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Angular 22](https://img.shields.io/badge/Angular-22-DD0031)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6)
![License](https://img.shields.io/badge/License-Not%20licensed-gray)

> **Training Management System** is a full-stack platform that lets **students**, **instructors**, and **administrators** run a complete academic program in one place — course catalog and enrollment, schedules and calendars, assignments and assessments, automated gradebook, attendance, digital certificates, and transcript generation.

---

## Project Summary

| | |
|---|---|
| **Purpose** | Manage the end-to-end lifecycle of training courses — from instructor authoring and admin approval, through student enrollment and completion, to certificates and transcripts. |
| **Backend** | ASP.NET Core Web API (.NET 10), Onion architecture, MediatR/CQRS, EF Core + PostgreSQL, ASP.NET Core Identity + JWT, SignalR, OpenTelemetry, Scalar API docs. |
| **Frontend** | Angular 22 SPA with Angular Material, signals (@ngrx/signals), RxJS, SignalR client, role-based routing, and light/dark themes. |
| **Database** | PostgreSQL, managed with EF Core migrations and an automated development seeder. |
| **Data model** | Real, database-backed data end to end — there is no mock/frontend-only functionality. Every screen reads from and writes to the API. |

---

## Table of Contents

- [Introduction](#introduction)
- [Features](#features)
  - [Student Features](#student-features)
  - [Instructor Features](#instructor-features)
  - [Administrator Features](#administrator-features)
- [Screenshots](#screenshots)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Database](#database)
- [Authentication & Authorization](#authentication--authorization)
- [Real-Time Features](#real-time-features)
- [API Documentation](#api-documentation)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [How to Use the System](#how-to-use-the-system)
- [Key Business Rules](#key-business-rules)
- [Validation & Error Handling](#validation--error-handling)
- [UI & Design](#ui--design)
- [Available User Roles](#available-user-roles)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Security](#security)
- [Future Improvements](#future-improvements)
- [Contributing](#contributing)
- [License](#license)
- [Contact / Support](#contact--support)

---

## Introduction

The **Training Management System (TMS)** is a full-stack web application built to replace paper spreadsheets, manual emails, and separate tools with a single, real-time platform for running a training center or academic program.

### What it does

- Replaces paper spreadsheets, emails, and separate tools with one **real-time platform**: an approved course catalog students can browse and enroll in; tracking through enrollment → attendance → assignments → grades → completion; tools for instructors to author and run courses; and administrator oversight of people, courses, and calendars.
- Preserves **academic history** — grades feed a live cumulative **GPA** (4.00 scale), and completed work produces **certificates** and **official transcripts**.
- Eliminates **stale information** — SignalR pushes course approvals, grades, notifications, capacity alerts, and transcript status without a page refresh.
- Removes **role confusion** — three clearly separated areas (Student, Instructor, Admin), each protected by role-based authorization on routes and API endpoints.

### How the frontend and backend work together

The **Angular SPA** talks to the **ASP.NET Core Web API** purely over HTTP (JSON) and SignalR (WebSockets):

```text
Angular Services ── HTTP ──▶ Versioned Web API (api/v1/...) ──▶ CQRS Pipeline ──▶ Business Services ──▶ EF Core ──▶ PostgreSQL
       ▲
       └──────── SignalR /hubs/tms ◀── Hub pushes (grades, approvals, notifications, transcript status)
```

The frontend never stores domain logic — it renders data returned by the API and broadcasts live updates received from the hub. All persistence is real: every feature is backed by a database table and a working endpoint.

---

## Features

Every item below reflects functionality that is actually implemented in the codebase. Features are grouped by the role that uses them.

### Student Features

- **Secure authentication and role-based access** — Sign in or register; access tokens, refresh tokens, and a proactive session-expiry timer keep the student area protected.
- **Personalized dashboard** — Academic status, cumulative GPA, course/class counts, today's class with its attendance status, and the next upcoming classes (date, time, location).
- **Course catalog & search** — Browse all active courses (category, level, ratings, capacity, instructor) with debounced live search, filtering, and sorting.
- **Course details** — Description, about, features, prerequisites, schedule, modules/lessons, resources, and enrolled-student count.
- **Course enrollment** — Enroll with one action; prerequisites, deadlines, course status, and capacity ("Course is full") are validated server-side.
- **My Courses** — Personal list with status (Upcoming / Active / Completed / Withdrawn), progress, grade, and statistics.
- **Course withdrawal** — Students can withdraw from an active course where the active-enrollment rule permits it.
- **Class schedule / calendar** — Weekly timetable derived from the course's recurring schedule configuration.
- **Assignments & submissions** — View assignments per course, submit text and/or a file attachment, and follow submission status.
- **Grades & feedback** — Published grades per course/assessment, letter grades, and instructor feedback.
- **Progress tracking** — Progress overview across courses plus a per-course view showing each assessment's *pending / submitted / graded* state.
- **Attendance information** — Per-course attendance summary (present / late / absent, attendance rate) and session history, shown on course progress.
- **Profile management** — Name, contact, profile picture upload, and password change.
- **Notifications** — Bell with unread count, live push notification toasts, and a list of read/unread messages.
- **Certificates** — View issued certificates and download them; certificates are generated automatically when a student meets the completion criteria.
- **Transcript generation** — Preview and request an official transcript; the PDF is generated in the background and delivered over SignalR when ready.

### Instructor Features

- **Instructor dashboard** — Today's classes with attendance status, upcoming sessions, teaching load, pending grading, and student questions awaiting a reply.
- **Today's classes** — Direct roll-call entry points from the dashboard.
- **Upcoming sessions** — Next sessions with course, date, time, and room.
- **Master schedule / calendar** — The instructor's lesson timetable.
- **Course creation** — Create a course with code, title, category, level, description, about, features, prerequisites, credits, capacity, and dates.
- **Course content builder** — Add modules and lessons, reorder them, upload a thumbnail, manage resources (title + file upload) with a responsive, dark-mode-ready builder UI.
- **Course resources** — Upload and manage downloadable resources per course.
- **Weekly class schedule configuration** — Configure recurring weekly slots (day of week, start/end time, location, room, schedule type) per course.
- **Course assessment configuration** — Configure the expected structure: number of assignments, max points, midterm and final exams and their max points.
- **Course submission for admin review** — Submit the finished course; it enters the admin review queue.
- **Course management** — List and edit your own courses, see approval/rejection status and the admin's rejection reason.
- **Assignment management** — Create, edit, delete, publish assignments per course and attach reference files.
- **Gradebook & submissions** — Course-wide gradebook populated from the approved assessment structure (bulk or per-cell editing, persisted to the database); list submissions, view detail (text/file), and leave grade + feedback.
- **Automatic assessment structure** — On course approval the system auto-creates one assessment record per student for every assessment in the course configuration (and again when a new student enrolls).
- **Attendance / digital roll call** — Mark attendance per student in bulk for a session (present / late / absent / excused + remarks) against the recurring schedule, with per-session history and rate-by-status stats.
- **Instructor profile** — Personal details, department, employee number, bio, avatar upload, and password change.
- **Notifications** — Real-time notifications for course approval/rejection and other system events.

### Administrator Features

- **Admin dashboard** — Overview widgets: students, instructors, courses, enrollments, capacity utilization, recent activity, and upcoming schedule items.
- **Student management** — Paginated/searchable list, detail with academic record, create/update flows, and account status (active / suspension / probation).
- **Instructor management** — List instructors, view details (courses taught, attendance rate), manage status, create accounts (profile + linked login), and suspend/reactivate with a reason/duration (including course reassignment).
- **Course review and approval** — Review submitted courses; **approve** (requires course content + scheduled classes, then activates it in the catalog) or **reject** with a reason that is sent back to the instructor.
- **Course editing / management** — Edit and archive/delete courses; full course detail view.
- **Enrollment management** — Browse all enrollments with status filters, create enrollments, and update enrollment status.
- **Calendar & targeted notifications** — Institutional calendar (meetings, workshops, events) with attendees and linked courses, plus scheduled agenda items with **targeted-audience** support that produce notifications.
- **System activity monitoring** — Recent sign-ins and activity surfaced on the dashboard.
- **Role-based administration** — All admin endpoints and routes are locked behind the `Admin` role.

---

## Screenshots

Captured from the live application against real seeded data (dark theme). Full-size PNGs live in [`docs/screenshots/`](./docs/screenshots).

### Authentication

![Login / Register](./docs/screenshots/login-placeholder.png)

> The sign-in / registration screen with role-aware redirect.

---

### Student

![Student Dashboard](./docs/screenshots/student-dashboard-placeholder.png)

> Academic status, cumulative GPA, today's class, upcoming classes, and the week's schedule.

![Course Catalog](./docs/screenshots/course-catalog-placeholder.png)

> Searchable course catalog with capacity, level, and instructor cards.

![Course Details](./docs/screenshots/course-details-placeholder.png)

> Course overview with schedule, modules/lessons, resources, and prerequisites.

![Student Calendar / Schedule](./docs/screenshots/student-calendar-placeholder.png)

> Weekly timetable derived from the course's recurring schedule configuration.

![Student Progress](./docs/screenshots/student-progress-placeholder.png)

> Progress across courses and assessment-by-assessment completion.

![Transcripts & Certificates](./docs/screenshots/transcript-certificate-placeholder.png)

> Issued certificates and transcript request/download.

---

### Instructor

![Instructor Dashboard](./docs/screenshots/instructor-dashboard-placeholder.png)

> Today's classes, upcoming sessions, pending grading, and student questions.

![Course Builder](./docs/screenshots/course-builder-placeholder.png)

> Content builder with modules, lessons, resources, weekly schedule, and assessment configuration.

![Instructor Gradebook](./docs/screenshots/gradebook-placeholder.png)

> Course gradebook populated from the approved assessment structure.

![Attendance / Roll Call](./docs/screenshots/attendance-placeholder.png)

> Digital roll call per session with status and remarks.

![Instructor Schedule](./docs/screenshots/instructor-schedule-placeholder.png)

> The instructor's teaching timetable.

---

### Admin

![Admin Dashboard](./docs/screenshots/admin-dashboard-placeholder.png)

> Institution-wide overview: people, courses, enrollments, and activity.

![Student Management](./docs/screenshots/student-management-placeholder.png)

> Student list, search, and management.

![Instructor Management](./docs/screenshots/instructor-management-placeholder.png)

> Instructor list, details, suspension, and course reassignment.

![Course Review & Approval](./docs/screenshots/course-review-placeholder.png)

> Course review queue with approve/reject actions.

---

## Technology Stack

### Backend

| Technology | Why it is used |
|---|---|
| **.NET 10 / ASP.NET Core Web API** | Cross-platform, high-performance foundation for the REST API (`net10.0`). |
| **C#** | Primary language for backend, `Nullable` + `ImplicitUsings` enabled. |
| **Entity Framework Core (10)** | ORM for all persistence; model is configured in code and schema is managed with migrations. |
| **PostgreSQL (Npgsql provider)** | Production-grade relational database used by the API. |
| **ASP.NET Core Identity** | Handles users (`TmsUser`), roles, password hashing, and EF Core stores on `TmsDbContext`. |
| **JWT Authentication** | Stateless bearer tokens for API calls; validation of issuer, audience, lifetime, and signing key. |
| **Role-Based Authorization** | Endpoints protected with `[Authorize(Roles = "...")]` — `Student`, `Instructor`, `Admin`. |
| **MediatR / CQRS** | Commands/queries are dispatched through `IMediator` to dedicated handlers (MediatR 12.4.1). |
| **FluentValidation** | Declarative request validation registered from the Application assembly and run by a pipeline behavior (11.11). |
| **SignalR** | Real-time push to the browser (`/hubs/tms`) for grades, approvals, notifications, capacity, transcript status, attendance, and assignments. |
| **Scalar / OpenAPI** | Interactive API reference (Scalar 2.0) over the OpenAPI document; every endpoint carries summaries and descriptions. |
| **AutoMapper** | Object-to-object mapping between entities and DTOs (13.0.1). |
| **API Versioning** | `Asp.Versioning` (8.1) supports `api/v1/...` and an experimental `v2` Courses controller. |
| **HybridCache** | In-process caching layer for a cached course read path (`AddHybridCache`). |
| **Polly** | Resilience utility included in the infrastructure layer (8.6.6). |
| **OpenTelemetry** | Tracing and metrics for the API (console exporter), attributed to the API service. |
| **Dependency Injection** | All services, handlers, validators, and behaviors are registered through the built-in DI container. |

### Frontend

| Technology | Why it is used |
|---|---|
| **Angular 22** | Component-based SPA framework with lazy-loaded routes. |
| **TypeScript (6)** | Typed JavaScript for the entire application. |
| **Angular Material (22)** | Ready-made, accessible UI components (tables, menus, dialogs, inputs, icons). |
| **SCSS** | Styled with SCSS, including a custom design system and theme variables. |
| **RxJS (7.8)** | Reactive streams for HTTP, event handling, and inter-service communication. |
| **Angular Routing** | Lazy-loaded, guarded routes per role (`authGuard`, `roleGuard`). |
| **HTTP Client** | Typed calls to the API with functional interceptors (auth + global error handling). |
| **Signals / @ngrx/signals** | Reactive state (auth, course catalog, enrollments) built on Angular signals. |
| **SignalR client** | Connects to `/hubs/tms` with auto-reconnect to receive live events. |
| **Light / Dark Mode** | Theme toggle persisted to `localStorage` (dark is the default). |

---

## Architecture

### Onion Architecture

The backend follows **Onion Architecture**. Dependencies point **inward** — the Domain knows nothing about the outside world, and the API only depends on abstractions.

```text
        TmsApi.Domain
              ↓
      TmsApi.Application
              ↓
    TmsApi.Infrastructure
              ↓
        TmsApi.Api  (hosts the API)
```

| Layer | Responsibility |
|---|---|
| **Domain** (`TmsApi.Domain`) | Entities (`Student`, `Instructor`, `Course`, `Enrollment`, `Assessment`, …) and enums. No dependencies. |
| **Application** (`TmsApi.Application`) | DTOs, commands/queries, handlers, validators, pipeline behaviors, service interfaces, AutoMapper profiles, and pure logic such as `GradingService`. Depends only on Domain. |
| **Infrastructure** (`TmsApi.Infrastructure`) | EF Core `TmsDbContext` + migrations, Identity + token services, business service implementations (`CourseService`, `GradeService`, `EnrollmentService`, …), the SignalR hub, data seeder, caching, transcript background worker, and external services. |
| **API** (`TmsApi.Api`) | Versioned controllers, `Program.cs` composition root, middleware, authorization (incl. custom policies), OpenAPI/Scalar, CORS, and OpenTelemetry. |

### CQRS Flow

The API is built on a **CQRS-style** flow: controllers stay thin, and every request is dispatched to a dedicated handler.

```text
Controller
   ↓
Request DTO
   ↓
Command / Query
   ↓
IMediator.Send()
   ↓
Dedicated Handler
   ↓
Service / Business Logic
   ↓
Database (PostgreSQL via EF Core)
```

How it works in practice:

- **Controllers are thin** — they validate the request shape, read the user from claims, and relay outcomes; they contain no business logic.
- **Commands and Queries** — records describing *operations* (e.g., *enroll a student*, *approve a course*) or *read intents*. They are dispatched with `IMediator.Send()`.
- **Dedicated Handlers** — each command/query has its own handler class that orchestrates the operation.
- **Validators** — FluentValidation validators run per request through the `ValidationBehavior` pipeline.
- **Behaviors** — cross-cutting concerns run as MediatR pipeline behaviors (logging, validation).
- **Services** — real business logic lives in reusable services (e.g., `EnrollmentService`, `GradingService`) and is called by handlers or — for the current read paths — directly by controllers.

### Feature-Folder Command Pattern

Handlers are organized in feature folders so each slice of the application is self-contained:

```text
Feature/
    CreateSomething/
        CreateSomethingCommand.cs
        CreateSomethingCommandHandler.cs
        CreateSomethingCommandValidator.cs
```

---

## Project Structure

```
Training Managment System/
│
├── Advanced Web API Development/          # Backend solution (TmsApi.slnx)
│   ├── TmsApi.Domain/                     # Entities + enums (no dependencies)
│   ├── TmsApi.Application/                # DTOs, CQRS, validators, interfaces, mappings
│   ├── TmsApi.Infrastructure/             # EF Core, migrations, services, hub, seeder, workers
│   ├── TmsApi.Api/                        # Controllers, Program.cs, middleware, OpenAPI/Scalar
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── V1/                         # All v1 controllers (Courses, Enrollments, Grades, …)
│   │   │   └── V2/                         # Experimental v2 courses controller
│   │   ├── Middleware/                     # Global exception handling
│   │   ├── Authorization/                  # Custom authorization requirements
│   │   └── Properties/launchSettings.json  # Dev ports & profiles
│   └── TmsApi.Tests/                       # Unit + Integration tests (xUnit)
│
├── tms-clients/                           # Angular 22 frontend
│   ├── src/app/
│   │   ├── features/                      # Route components: login, student-*, instructor-*, admin-*
│   │   ├── services/                      # Typed API services + auth, theme, live-sync (SignalR)
│   │   ├── store/                         # @ngrx/signals stores (course, enrollment)
│   │   ├── models/                        # Typed request/response models
│   │   ├── guards/                        # Route guards (auth, role, redirects)
│   │   ├── interceptors/                  # HTTP interceptors (auth token, error)
│   │   └── ui/                            # Shared layout: header, sidebar, footer, course-card
│   ├── proxy.conf.json                    # Dev proxy: /api + /hubs → http://localhost:5211
│   ├── angular.json
│   └── package.json
│
├── docs/screenshots/                     # README screenshots (captured)
└── README.md
```

---

## Database

### Database engine & access

- **PostgreSQL** is the database, accessed through EF Core with the **Npgsql** provider.
- **EF Core migrations** live in `TmsApi.Infrastructure/Migrations` and are **applied automatically on application start** in relational environments (`MigrateAsync`).
- In the `Testing` environment the app switches to the **InMemory** provider so integration tests run without PostgreSQL.
- A **development seeder** runs on startup for relational databases: it creates roles, users, an instructor pool, students, courses, recurring schedules, modules/lessons, resources, and sample enrollments.

### Users and role-specific entities

`TmsUser` derives from the ASP.NET Core Identity user class and carries optional links to the role-specific profile entities:

```text
TmsUser (IdentityUser)
   ├── StudentId ──▶ Student          (academic profile: registration #, GPA, status)
   └── InstructorId ──▶ Instructor    (employee profile: department, employee #, status)
```

These links allow controllers to resolve *"the logged-in user"* to the correct `Student` or `Instructor` record.

### Core entities

```text
Course
   ├── Instructor        (the owning instructor)
   ├── CourseSchedule    (recurring weekly class slots)
   ├── CourseModule ── CourseLesson
   ├── CourseResource
   ├── Assignment ── Assessment     (per-student assignment submissions)
   └── Enrollment ──▶ Student

Student
   ├── Enrollments ──▶ Course       (status: Active / Pending / Waitlisted / Completed / Dropped / Rejected)
   ├── Assessments                 (per-assignment, per-course work items)
   ├── Attendances                 (session roll-call records)
   ├── Certificates                (issued on completion)
   └── Notifications               (in-app + realtime)

Instructor
   ├── Courses
   ├── Schedules / CourseSchedules
   └── InstructorSuspensionHistory (suspension audit trail)

Admin support
   ├── Schedule                    (institutional calendar: meetings, workshops, events)
   ├── Notification                (targeted agendas / notices)
   ├── RefreshToken                (auth refresh tokens)
   └── StudentQuestion             (student questions for instructors/admins)
```

---

## Authentication & Authorization

### ASP.NET Core Identity + `TmsUser`

- Every person in the system is an **Identity user** (`TmsUser : IdentityUser`), with first/last name, profile picture, and optional links to a `Student` or `Instructor` record.
- Passwords are hashed by Identity (never stored in plain text).
- Three roles are seeded: `Admin`, `Instructor`, `Student`.

### Authentication flow

```text
frontend ─POST /api/v1/auth/login──▶ API ─validate password──▶ Identity
frontend ◀ token + refreshToken + user── API
frontend stores tokens in localStorage and attaches the access token to every request
frontend proactively refreshes ~60s before expiry, with a mutex to avoid duplicate refreshes
```

- **JWT access tokens** carry claims for the user id, name, roles, and student/instructor id.
- **Refresh tokens** are stored in the database (`RefreshToken` entity), are **single-use**, and are **revoked on logout / rotation**.
- The client uses an **automatic session-expiry timer**: if a token expires it refreshes; if refresh fails, the user is signed out and redirected to `/login?sessionExpired=true`.
- `POST /api/v1/auth/logout` revokes the refresh token server-side while the client clears all stored state.

### Role-based authorization

| Mechanism | Where |
|---|---|
| Backend endpoints | `[Authorize]` and `[Authorize(Roles = "Student,Admin")]` on controllers/actions. |
| Frontend routes | `authGuard` + `roleGuard` with `data: { roles: [...] }` per route; unknown roles are redirected via `rootRedirectGuard`. |
| Admin-only | Enrollments, student/instructor management, course approval/rejection, instructor suspension. |
| Instructor/Admin | Course creation, builder, grading, attendance, schedules, submissions. |
| Student/Admin | Enrolled course views, assignments, progress, transcripts, certificates. |

---

## Real-Time Features

The API exposes a **SignalR hub** at `/hubs/tms` (`TmsHub`) and the Angular client connects through `LiveSyncService` with automatic reconnect and private group membership (`User_<id>`, `Student_<id>`, `Course_<id>`, `Admins`).

Clients receive live updates **without manually refreshing the page**:

| Event | Triggered when |
|---|---|
| `ReceiveNotification` | A notification is created for a user (e.g., course approval/rejection). |
| `ReceiveGradePublished` | A grade is published to a student. |
| `ReceiveCapacityAlert` | A course gets near/over its capacity threshold. |
| `ReceiveTranscriptStatus` | A requested transcript PDF finishes generating in the background. |
| `ReceiveScheduleUpdate` | A course is approved (broadcast) or scheduling changes. |
| `ReceiveAssignmentUpdate` | Assignments are created/updated for enrolled students. |
| `ReceiveAttendanceUpdate` | Attendance is recorded for a session. |
| `ReceiveEnrollmentNotification` | An enrollment is created or changed. |

---

## API Documentation

The API uses **Scalar** (over OpenAPI) for interactive, human-readable API documentation. Every controller and endpoint carries a **tag, summary, description, and `ProducesResponseType` metadata**, so the generated reference is self-explanatory.

### Where to find it

From the API defaults (see `Properties/launchSettings.json` — port is a placeholder to avoid guessing):

```text
http://localhost:<API_PORT>/scalar          → Scalar UI (interactive)
http://localhost:<API_PORT>/openapi/v1.json → Raw OpenAPI document
```

The frontend dev proxy expects the API on `http://localhost:5211` (HTTP profile):

```text
http://localhost:5211/scalar
```

> Scalar and the OpenAPI document are only mapped in the `Development` environment.

### What you can do

- Browse every endpoint grouped by tag (Authentication, Students, Instructors, Courses, Enrollments, Assignments, Gradebook, Attendance, Certificates, Transcripts, Notifications, Dashboard, Schedules).
- Read each endpoint's summary and description.
- **Authorize** with a JWT (paste a token) and **test requests** directly from the browser.
- Check each endpoint's documented responses and DTO shapes.

---

## Getting Started

### Prerequisites

| Tool | Required for | Notes |
|---|---|---|
| **.NET SDK 10** | Backend build/run | Target framework is `net10.0`. |
| **Node.js + npm** | Frontend build/run | Angular 22 toolchain. |
| **Angular CLI** | Serving/building the SPA | Version 22 (`npm install -g @angular/cli`) or use `npx ng`. |
| **PostgreSQL** | Database | Any recent PostgreSQL; the Npgsql EF provider requires `Host`/`Database`/`Username`/`Password`. |
| **Git** | Cloning and version control | — |

> Exact SDK/patch versions are not committed by design; anything satisfying the `net10.0` / Angular 22 requirements works.

### Clone the Repository

```bash
git clone <YOUR_REPOSITORY_URL>
cd <PROJECT_FOLDER>
```

### Backend Setup

The backend is the `Advanced Web API Development` folder (solution `TmsApi.slnx`).

```bash
cd "Advanced Web API Development"
```

1. **Configure the database connection** — see [Configuration](#configuration). Create a PostgreSQL database (e.g., `tms_db`).
2. **Configure secrets safely** — never commit real credentials. Use environment variables or `dotnet user-secrets` for `ConnectionStrings__DefaultConnection`, `Jwt__Key`, etc.
3. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

4. **Apply EF Core migrations:**

   ```bash
   dotnet ef database update --project TmsApi.Infrastructure --startup-project TmsApi.Api
   ```

   > Prerequisite: install the EF Core tools once (`dotnet tool install --global dotnet-ef`). Migrations are **also applied automatically on startup** by the seeder when the environment is relational — you can skip this step and just run the API.

5. **Run the API:**

   ```bash
   dotnet run --project TmsApi.Api
   ```

   The HTTP profile binds to `http://localhost:5211` (the HTTPS profile also offers `https://localhost:7090`). On startup, the seeder creates roles, demo users, courses, schedules, and enrollments.

### Frontend Setup

The frontend is the `tms-clients` folder.

```bash
cd tms-clients
```

1. **Install dependencies:**

   ```bash
   npm install
   ```

2. **API URL / proxy** — in development nothing is required: `proxy.conf.json` already forwards `/api` and `/hubs` to `http://localhost:5211`, and the CORS policy allows `http://localhost:4200`. If you move the API, update the proxy target.

3. **Run the application:**

   ```bash
   ng serve
   ```

   Open `http://localhost:4200` and sign in.

### Demo accounts (development seed only)

The seeder creates development-only accounts so the app is usable immediately. These credentials exist only in the local seed data — **change them (or the seeder) before any real deployment**:

| Email | Password | Role |
|---|---|---|
| `admin@tms.local` | `Admin123!` | Admin |
| `gina.alexander@eduva.com` | `Instructor123!` | Instructor |
| `grasiella.lopez@eduva.com` | `Student123!` | Student |

Registration is also available from the Login page (`/api/v1/auth/register`) for self-service student accounts.

---

## Configuration

Configuration is standard ASP.NET Core (JSON + environment variables + user-secrets). **Never commit real credentials.** The defaults below are placeholders.

### PostgreSQL connection string

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<HOST>;Port=5432;Database=tms_db;Username=<USER>;Password=<PASSWORD>"
  }
}
```

### JWT

```json
{
  "Jwt": {
    "Key": "<AT_LEAST_32_BYTE_SECRET>",
    "Issuer": "TmsApi",
    "Audience": "TmsClients",
    "DurationInMinutes": 60
  }
}
```

### Environment variables

In local development, prefer environment variables or `dotnet user-secrets` so nothing sensitive is committed:

```bash
# Example (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=tms_db;Username=postgres;Password=<PASSWORD>"
$env:Jwt__Key = "<AT_LEAST_32_BYTE_SECRET>"
```

### CORS / allowed origins

The API policy `AllowClient` allows `http://localhost:4200` and `https://localhost:4200` (the Angular dev server), with `AllowCredentials` for the SignalR connection:

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200", "https://localhost:4200"]
  }
}
```

> The current policy is defined in `Program.cs`; adjust `AllowedOrigins` there if you host the frontend elsewhere.

### Other settings

`Logging:LogLevel` (logging levels), `AllowedHosts` (host filtering), and `ASPNETCORE_ENVIRONMENT` — `Development` maps Scalar/OpenAPI; `Testing` uses the EF InMemory provider for tests.

---

## How to Use the System

### Student Workflow

1. **Register or sign in** (a new account gets the `Student` role) and review the **Dashboard** — GPA, today's class, and the week ahead.
2. Browse and **search the Course Catalog**, open a course for its details (modules/lessons, resources, schedule, prerequisites), and **enroll**.
3. Track the course under **My Courses**; submit **Assignments** (text and/or file) and follow **Progress & Grades** as assessments are evaluated.
4. When a course is completed, collect the **Certificate** and request a **Transcript** (generated in the background, delivered over SignalR). Manage **Profile** and **Notifications** as needed.

### Instructor Workflow

1. **Sign in** and open the **Instructor Dashboard** — today's classes, upcoming sessions, pending grading, student questions.
2. **Create a course**; in the **Course Builder** add weekly **schedule configuration**, **modules/lessons**, **resources**, and the **assessment structure** (assignments + midterm/final with points).
3. **Submit the course for review**; once approved, publish **assignments**, grade the **Gradebook** and **Submissions**, and record **Attendance** per session.

### Administrator Workflow

1. **Sign in** as Admin and open the **Admin Dashboard** for an institution-wide overview.
2. Manage **Students** and **Instructors** (accounts, status, suspension/reactivation with course reassignment).
3. Review courses in the **Course Review** queue — **approve** (requires content + scheduled classes; activates the course and auto-generates its assessment structure) or **reject** with a reason.
4. Administer **Enrollments**, manage the institutional **Calendar**, and send **targeted agendas/notifications**.

---

## Key Business Rules

Implemented rules that shape how the system behaves:

- **Capacity limits** — courses declare `MaxCapacity`; enrollments count active + pending seats, and a full course rejects further enrollment ("Course is full").
- **No duplicate enrollments** — already active/pending/waitlisted enrollment blocks a second enrollment; a completed course cannot be taken again.
- **Dropped-enrollment reactivation** — if a student re-enrolls, an existing dropped/rejected/deleted record is **reactivated** (status reset, timestamp refreshed) instead of creating a duplicate row.
- **Approval gating** — students can only enroll in `Active` courses; instructor-authored courses must be approved by an admin before they appear in the catalog.
- **Instructor ownership** — a course is always owned by an instructor (required to create/build a course); approval notifies the owning instructor.
- **Assessment structure per course** — assignments, max points, midterm, and final are configured on the course; on **course approval** (and on new enrollment into a course with published assignments) the system auto-creates one assessment record per student for every required assessment.
- **100% progress requires full evaluation** — course progress reaches 100% only when **all required assessments** are evaluated; the gradebook surfaces already/not-yet/required evaluation counts.
- **GPA & transcripts from real data** — the cumulative GPA is recalculated from graded enrollments and course credits on a standard **4.00 scale** whenever grades change; dropped/rejected enrollments are excluded from transcripts and GPA, while completed enrollments carry a grade into them.
- **Certificates only when eligible** — a certificate is issued automatically only when: the course offers a completion certificate, the enrollment is not dropped/rejected, at least one published assignment exists, **every published assessment is graded**, and the average score is **≥ 60%** (or stored grade ≥ 60). Eligibility also marks the enrollment `Completed`.
- **Prerequisites & deadlines** — enrollments validate declared prerequisites (a completed course or passing grade on the prereq) and the enrollment deadline.
- **Role-based access controls** — routes (guards) and endpoints (`[Authorize(Roles=...)]`) enforce roles at every level.

---

## Validation & Error Handling

- **Backend validators** (FluentValidation) are registered from the Application assembly and run by a MediatR **`ValidationBehavior`** pipeline, returning field-specific errors; ASP.NET Core also validates model binding and DTO shape.
- **Expected business errors** (e.g., "Course is full", "Prerequisite not completed", "already enrolled") are raised as `InvalidOperationException` and returned as controlled responses by the global **`ExceptionMiddleware`**; unexpected exceptions are logged and return a generic 500 without leaking stack traces.
- **Frontend** — the `errorInterceptor` surfaces friendly toasts globally, the `authInterceptor` attaches/refreshes the access token, and forms use Angular/Reactive validation with inline field messages.

---

## UI & Design

- **Angular Material** supplies the component palette; the app adds a custom SCSS design system on top (sidebar layout, cards, tables, dashboard widgets).
- **Responsive layout** — a header/sidebar shell adapts to smaller screens for the main app areas.
- **Light and dark mode** — defaults to dark, persists the choice, and applies theme classes globally.
- **Role-specific areas** — separate Student, Instructor, and Admin zones with their own sidebars and route trees; role guards block cross-access.
- **Consistent, professional styling** — clinically clean, information-dense dashboards, course cards, gradebook tables, and calendar views, all styled to be readable in both themes.

---

## Available User Roles

| Role | Description | Main Capabilities |
|---|---|---|
| **Student** | A learner with an academic profile (registration number, GPA). | Dashboard, catalog & search, course details, enrollment/withdrawal, my courses, schedule/calendar, assignments & submissions, grades & feedback, progress & assessment tracking, attendance view, profile, notifications, certificates, transcripts. |
| **Instructor** | A teacher with an employee profile who authors and runs courses. | Dashboard, master schedule, course creation, course builder (modules/lessons/resources/schedules), assessment configuration, submission for review, course management, assignments, grading & submissions, gradebook, attendance/roll call, profile, notifications. |
| **Administrator** | Platform operator overseeing all people and courses. | Dashboard, student management, instructor management & account creation, suspension/reactivation with course reassignment, course review/approval/rejection, course management, enrollment administration, calendar/schedule management, targeted agendas/notifications, activity monitoring. |

---

## Development Workflow

A feature should travel through the whole stack, always backed by a real database model and API surface:

```text
Domain → Application (CQRS) → Infrastructure → API endpoint → Angular service → UI → tests
```

Rules of thumb used in this codebase:

- Add the **entity/migration** first, then **service logic**, then the **endpoint**, then the **Angular service + UI** — no mock/frontend-only functionality ships.
- Keep **controllers thin**; put business rules in services and validation in FluentValidation validators, and document every endpoint (tag, summary, description, `ProducesResponseType`) so Scalar stays accurate.

---

## Testing

### Backend tests (xUnit)

The `TmsApi.Tests` solution project covers both layers:

- **Unit tests** — `GradingService`, transcripts, student & instructor scheduling, course scheduling, course approval workflow, the enroll-student handler, financial/dashboard audits, and student features.
- **Integration tests** — a `CustomWebApplicationFactory` over the real composition root (InMemory provider in the `Testing` environment) together with `CoursesApiTests`.

Run them with:

```bash
cd "Advanced Web API Development"
dotnet test
```

### Frontend tests (Vitest)

The Angular app uses **Vitest + jsdom** for unit tests (e.g., `auth.service.spec.ts`, `course.store.spec.ts`):

```bash
cd tms-clients
npm test        # runs "vitest run"
```

### Manual API testing

- **Scalar UI** at `http://localhost:5211/scalar` (Development) — authorize with a JWT and exercise endpoints interactively.
- The repository includes `TmsApi.Api.http` (VS Code REST Client / JetBrains HTTP client) with ready-to-run request samples.

> **Note:** automated coverage is currently limited to the areas listed above. Broader end-to-end coverage is a planned improvement (see [Future Improvements](#future-improvements)).

---

## Security

Implemented security practices:

- **ASP.NET Core Identity** for user management, roles, and **password hashing** (no plaintext storage).
- **JWT bearer authentication** with issuer/audience/lifetime/signing-key validation, **zero clock skew**, and **refresh-token rotation** (DB-stored, single-use, **revoked on logout**) plus a client-side proactive expiry/refresh timer.
- **Role-based authorization** enforced on API endpoints and client routes.
- **SignalR hub** requires authentication (`[Authorize]`); group access is scoped by user/student/course/admin groups.
- **Input validation** backend (FluentValidation + model binding) and frontend (form validation); **CORS** restricted to the Angular dev origin with credentials support.
- **Global exception middleware** — expected business errors return clean messages; unexpected failures are logged and never leak internals.
- **Configuration hardened by convention** — secrets live in environment variables/user-secrets; the JSON config contains only weakly-typed development defaults and must be overridden before any real deployment.

---

## Future Improvements

Planned and clearly separated from what is already implemented:

- **Automated tests & CI/CD** — broader unit coverage, Angular component tests, and a build/test/deploy pipeline.
- **Containerization** — Docker images for the API (with migrations), the Angular site, and PostgreSQL via `docker-compose`.
- **File storage & secrets** — move uploads from the filesystem to object storage (with virus scanning) and adopt vault-based secrets.
- **Out-of-band notifications** — email for enrollment confirmations, approvals, and transcripts.
- **Observability & analytics** — centralize structured logs, ship OpenTelemetry traces/metrics to a backend, and add richer historical reports.
- **Performance** — caching reads, paging/query tuning, and hub scaling.

---

## Contributing

Contributions are welcome — keep changes focused and consistent with the architecture. **Fork**, make one logical change per branch (Domain → Application → Infrastructure → API → Frontend), run `dotnet test` and `npm test`, and open a pull request describing what changed and how it was verified.

### Commit style

This repository uses **Conventional Commits**-style messages. Examples:

```text
feat(courses): add course approval workflow
fix(enrollment): reactivate dropped enrollment
refactor(api): move business logic from controllers
docs(readme): update setup instructions
style(course-builder): improve responsive and dark mode support
test(gradebook): load course assessment structure from backend
```

---

## License

This project is **currently not licensed for public use**.

Add a `LICENSE` file to define usage and distribution terms before publishing.

---

## Contact / Support

Project Maintainer: `<YOUR_NAME>` — `<YOUR_EMAIL>`

For issues and feature requests, open an issue in this repository.

---

_This README was generated from the actual repository contents — API controllers, routes, entities, migrations, configuration, and frontend features. Screenshots are captured from the running application._