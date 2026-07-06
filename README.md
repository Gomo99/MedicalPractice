# Medical Practice Management System

A secure, role‑based web application built with **ASP.NET Core MVC** and **Entity Framework Core** to digitize doctor visits. The system replaces physical patient folders with a centralised platform that manages patient records, appointment scheduling, visit histories, and user accounts for a medical practice.

---

## Features

### Authentication & Security
- **Role‑Based Access Control** – Four primary roles: **Admin**, **Doctor**, **Assistant**, and **Receptionist** (plus a **Patient** self‑service portal).
- **BCrypt Password Hashing** – All passwords are hashed; legacy plain‑text passwords are automatically upgraded upon successful login.
- **Account Lockout** – After 5 consecutive failed attempts, the account is locked for 15 minutes. The locked‑out user receives an email, and all admins get a security alert notification.
- **Two‑Factor Authentication (TOTP)** – Opt‑in 2FA using authenticator apps (Google Authenticator, etc.).  
  - QR code setup, recovery codes (hashed), ability to regenerate or disable 2FA.
  - Backup recovery codes can be used to bypass 2FA in an emergency.
- **Forgot Password via Email PIN** – Users request a reset PIN to their registered email; the PIN expires after 15 minutes.
- **Automatic Password Generation** – When an admin creates a new staff account, a strong random password is generated and emailed.
- **Must‑Change Password** – First‑time users are forced to change their temporary password.
- **Patient Self‑Registration & Login** – Patients can register directly, log in, and manage their own appointments and visits.

### Admin Module
- **Employee Management**  
  - List all employees (Doctors, Assistants, Receptionists).  
  - **Create** new employee accounts: choose role, set username/email; credentials are emailed automatically.  
  - **Edit** employee details (username, email, full name, role).  
  - **Toggle Status** – Activate / deactivate any employee account (soft lock). Both the affected user and all admins receive in‑app notifications.
- **Notifications** – All critical actions (account creation, status change, security lockouts) generate in‑app and email notifications for the relevant users/roles.

### Assistant Module
- **Patient Management (CRUD)**  
  - **Create** patients with mandatory fields: name, surname, username, gender, home address, email, phone number, medical aid.  
  - **Medical Aid Toggle** – If “Yes”, a dropdown of three providers (Discovery, Momentum, Bonitas) appears; selection is required.  
  - **Edit** patient details with duplicate username validation.  
  - **Delete** patients permanently.  
  - **Search** patients by name, surname, or username.
- **Notifications** – All doctors receive a notification when a new patient is added, updated, or removed.

### Doctor Module
- **Patient List & Search**  
  - View all patients with patient number, name, surname, and username.  
  - Search by patient number, name, surname, or username.
- **Patient Visit History**  
  - View a full timeline of visits for any patient, showing date/time, diagnosis, and treatment notes.
- **Visit Management**  
  - **Add a Visit** – Record the date/time, diagnosis, and treatment.  
  - **Edit a Visit** – Update previously recorded visit details (only the owning doctor can edit).
- **My Appointments**  
  - View own scheduled appointments for a selected date (via the Receptionist module).
- **Notifications** – Self‑confirmation alerts when visits are recorded or updated.

### Receptionist Module
- **Appointment Scheduling**  
  - **Daily Schedule** – View all appointments for a selected date (default today), sorted by time.  
  - **Doctor Schedule** – View a specific doctor’s appointments for a day (used to check availability).  
  - **Book Appointment** – Select patient and doctor, choose date/time and reason.  
  - **Reschedule Appointment** – Change the date/time of a booked appointment.  
  - **Cancel Appointment** – Mark an appointment as cancelled.  
  - **Check‑In** – Mark a patient as “Arrived” when they show up.  
  - **Update Status** – Cycle through appointment statuses (Booked → Arrived → In Progress → Completed).
- **Patient Registration** – Quick patient registration with the same fields as the Assistant module.
- **Appointment Request Management**  
  - View **pending requests** submitted by patients.  
  - **Approve** a request (create an appointment, optionally adjust the date/time).  
  - **Reject** a request.

### Patient Portal
- **Self‑Registration & Login** – Patients can register directly, log in, and manage their own data.
- **Dashboard** – Overview of upcoming appointments and personal information.
- **Appointment Management**  
  - View upcoming and past appointments with date filters.  
  - Request a new appointment (choose preferred doctor and date/time).  
  - Cancel or request reschedule for booked appointments (subject to time constraints).
- **Visit History** – View a list of all past visits with doctor names and details.
- **Profile Management** – Update address, email, phone number, and medical aid information. Change password.
- **Visit Summary** – Print‑friendly view of a single visit (diagnosis and treatment).

### Notifications System
- Real‑time in‑app notifications via **SignalR** (new patient, account changes, lockouts).
- Bell icon in the navbar shows unread count; dropdown displays recent notifications with action links.
- AJAX endpoints allow marking individual or all notifications as read.

---

## User Roles & Permissions

| Role            | Permissions                                                                                     |
|-----------------|-------------------------------------------------------------------------------------------------|
| **Admin**       | Full staff management: create, edit, activate/deactivate employee accounts.                    |
| **Assistant**   | CRUD patients, search patients, manage medical aid details.                                    |
| **Doctor**      | View/search patients, manage visit history, add/edit visits, view own appointments.            |
| **Receptionist**| Manage appointments (book, reschedule, cancel, check‑in), register patients, handle appointment requests. |
| **Patient**     | Self‑service: register/login, view appointments/visits, request appointments, update profile.  |

---

## Technology Stack

- **Backend:** ASP.NET Core MVC (.NET 6/7/8), Entity Framework Core
- **Database:** SQL Server (LocalDB, Express, or full edition)
- **Frontend:** Razor Views with Bootstrap 5
- **Authentication:** ASP.NET Core Cookie Authentication
- **Two‑Factor Authentication:** TOTP (RFC 6238) with SHA‑1
- **Password Hashing:** BCrypt.Net
- **Email:** Custom `IEmailService` (SMTP ready)
- **Notifications:** Custom `INotificationService` + **SignalR** hub for real‑time updates
- **QR Code Generation:** QRCoder library (for 2FA setup)

---

## Setup & Installation

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or later)
- SQL Server (LocalDB or higher)
- Visual Studio 2022 / VS Code / Rider
- An email service configuration (SMTP, SendGrid, etc.)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/medical-practice.git
   cd medical-practice