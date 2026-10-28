# 🐾 PetHope — Pet Welfare & Adoption Management System (PetWAMS)

A web-based pet adoption and shelter management platform built with **ASP.NET Core MVC** and **SQL Server**. PetHope connects adopters with animal shelters, letting users browse pets, submit adoption requests, and track their applications, while shelters and administrators manage listings and requests from dedicated dashboards.

---

## ✨ Features

### For Adopters
- Browse available pets with filtering by category (Dog, Cat, Bird)
- View detailed pet profiles: breed, age, gender, color, vaccination status, and description
- Register, sign in, and submit adoption requests
- Personal dashboard showing adoption requests and their status
- Upload verification documents for adoption approval

### For Shelters / Staff
- Shelter dashboard with live counts of total pets and adoptions
- Full pet management (Create, Read, Update, Delete)
- Shelter profile management (contact email, opening hours)

### For Administrators
- Secure admin login with server-side session validation
- Admin dashboard for platform oversight

### Platform
- Role-based access control (Adopter, Shelter Staff, Admin) using ASP.NET Core Areas
- Real-time notifications via SignalR (`NotificationHub`)
- Machine learning features powered by ML.NET
- Responsive UI converted from the original HTML design into Razor views

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 8) |
| Language | C# |
| ORM | Entity Framework Core 8 |
| Database | Microsoft SQL Server |
| Real-time | SignalR |
| Machine Learning | ML.NET (FastTree) |
| Frontend | Razor Views, HTML5, CSS3, JavaScript |
| IDE | Visual Studio 2022 / 2026 |

---

## 📁 Project Structure

```
PetWAMS.Web/
├── Areas/
│   ├── Admin/            # Admin dashboard
│   ├── Adopter/          # Adopter dashboard
│   └── Shelter/          # Shelter dashboard, pet management (CRUD), profile
├── Controllers/          # Public controllers (Pets, Account)
├── Data/                 # EF Core DbContext (PetWamsDbContext)
├── Helpers/              # Utility classes
├── MLModels/             # ML.NET models
├── Migrations/           # EF Core migrations
├── Models/               # Entity classes mapped to database tables
├── Services/             # Business logic layer
├── ViewModels/           # View-specific data models
├── Views/                # Razor views
├── wwwroot/              # Static assets (CSS, JS, images)
├── App_Data/             # Uploaded adopter verification documents
├── NotificationHub.cs    # SignalR hub for real-time notifications
├── Program.cs            # Application entry point and service configuration
├── appsettings.json      # Configuration and connection strings
├── PetWAMSDB_WEB_SETUP.sql   # Full database setup script
└── additional_schema.sql     # Supplementary schema changes
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (Express or Developer edition)
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms)
- Visual Studio 2022 or later, with the **ASP.NET and web development** workload

### 1. Clone the repository
```bash
git clone https://github.com/tahrimmaria/PetWAMS.Web.git
cd PetWAMS.Web
```

### 2. Set up the database
Open SSMS and execute `PetWAMSDB_WEB_SETUP.sql`. This creates the `PetWAMSDB` database with all required tables and demo data.

### 3. Configure the connection string
Update `appsettings.json` so that `Server` matches your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=PetWAMSDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> Use `Server=.\\SQLEXPRESS` if you are running SQL Server Express.

### 4. Run the application
Open `PetWAMS.Web.slnx` in Visual Studio and press **F5**, or run from the terminal:

```bash
dotnet run
```

---

## 🔐 Demo Accounts

| Role | Email | Password |
|---|---|---|
| Adopter | `rahim@gmail.com` | Any non-empty value (demo mode) |
| Shelter Staff | `tamim@gmail.com` | Any non-empty value (demo mode) |
| Admin | `admin@gmail.com` | `admin123` |

> The admin panel is accessible at `/Account/AdminLogin`.

---

## 🧭 Key Routes

| Route | Description |
|---|---|
| `/Pets` | Browse all pets (supports category filtering) |
| `/Pets/Details/{id}` | Pet detail page |
| `/Account/SignIn` | Sign in with role selection |
| `/Account/SignUp` | Adopter registration |
| `/Account/AdminLogin` | Administrator login |
| `/Adopter/Dashboard` | Adopter dashboard |
| `/Shelter/Pets` | Manage shelter pets |
| `/Shelter/Profile` | Shelter profile |

---

## ⚠️ Known Limitations

This project is an academic prototype. The following are planned improvements before any production use:

- **Password verification:** Authentication currently checks email and role only. A `PasswordHash` column with BCrypt hashing should be added.
- **Shelter–staff mapping:** Users are not yet linked to a specific shelter, so shelter dashboards display the first shelter record. Adding a `ShelterID` foreign key to `Users` will resolve this.
- **Shelter registration:** The shelter sign-up form is currently UI-only.
- **Pet form fields:** The add/edit pet form covers core fields only. Microchip ID and category selection are planned.

---

## 🗺️ Roadmap

- [ ] Secure password hashing and ASP.NET Core Identity integration
- [ ] Shelter–staff relationship in the database
- [ ] Image upload for pet profiles
- [ ] Email notifications for adoption status changes
- [ ] Deployment to Azure App Service

---

## 👩‍💻 Author

**Tahrim Maria**
GitHub: [@tahrimmaria](https://github.com/tahrimmaria)

---

## 📄 License

This project was developed for academic purposes. All rights reserved.