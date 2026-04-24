# Cursus Development Setup Guide

Welcome to the Cursus project! Our team operates across different operating systems (Fedora, Ubuntu, and Windows). This guide provides the exact steps needed to get the Cursus application running locally on your specific machine.

---

## 1. Prerequisites

Before cloning the repository, ensure you have the following installed based on your OS:

### All Platforms
- **.NET SDK:** We are currently targeting **.NET 10**. (Ensure `dotnet --version` outputs a `10.x` version).
- **Git:** Version control.
- **IDE:** 
  - *Windows:* Visual Studio 2022 (Recommended) or VS Code.
  - *Fedora/Ubuntu:* JetBrains Rider or VS Code with the C# Dev Kit extension.

### Database Setup (Crucial OS Difference)

Since we are using **SQL Server** and Entity Framework Core, the setup differs heavily based on your operating system.

#### 🪟 Windows
Windows natively supports SQL Server.
1. Install **SQL Server Express** or ensure **LocalDB** is installed via the Visual Studio Installer (Data storage and processing workload).
2. LocalDB connection strings (default in ASP.NET generated templates) will work out of the box.

#### 🐧 Linux
SQL Server does not run natively on Linux, so you **must use Docker** to run the database.
1. Install **Docker** and **Docker Compose** on your distro.
2. Run the SQL Server 2022 Docker container:
   ```bash
   sudo docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Cursus_DB_Passw0rd!" \
      -p 1433:1433 --name cursus-sql \
      -d mcr.microsoft.com/mssql/server:2022-latest
   ```
3. **Important:** You will need to override the connection string in the codebase so EF Core points to your Docker container instead of Windows LocalDB. 
   - Create a file named `appsettings.Development.json` in `src/` (if it doesn't exist).
   - Add the following connection string:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=localhost,1433;Database=CursusDb;User Id=sa;Password=Cursus_DB_Passw0rd!;TrustServerCertificate=True;"
       }
     }
     ```
   *(Note: Never commit your personal password/connection string to Git. `appsettings.Development.json` is usually ignored or safe for local overrides).*

---

## 2. Cloning the Repository & Initial Setup

Once your database engine is running, set up the project:

```bash
# 1. Clone the repository
git clone https://github.com/3bdo-Yahya/Cursus.git
cd Cursus/src

# 2. Add the EF Core global tool (if you don't have it yet)
dotnet tool install --global dotnet-ef

# 3. Restore all NuGet packages
dotnet restore
```

---

## 3. Database Migrations

You need to apply the database schema so your local SQL Server knows what tables to create.

```bash
cd Cursus/src

# Apply migrations to create the database and schema
dotnet ef database update
```
*If you get an error here, it means your connection string is wrong or your SQL Server/Docker container is not running.*

---

## 4. Running the Application

```bash
cd Cursus/src

# Build the project to ensure there are no compilation errors
dotnet build

# Run the project
dotnet run
```

The terminal will output the local URL (usually `https://localhost:5001` or `http://localhost:5000`). Open this in your browser.

---

## 5. Daily Git Workflow (Gitflow Lite)

As per our `CONTRIBUTING.md`, we use a structured tracking workflow. We track tasks in **ClickUp** and code in **GitHub**.

1. **Never work directly on `master` or `develop`.**
2. When starting a ClickUp task, pull the latest `develop` branch and create a new feature branch:
   ```bash
   git checkout develop
   git pull origin develop
   # Example naming: feature/S1-007-admin-crud
   git checkout -b feature/[clickup-task-id]-short-description
   ```
3. Commit your changes logically.
4. Push your branch to GitHub and **Open a Pull Request** against `develop`.
5. Request a peer review. (Your PR must pass the automated GitHub CI build before Abdelrahman will squash and merge it).

---

## 6. Troubleshooting Common OS Issues

- **Line Endings (CRLF vs LF):** Windows uses CRLF for line breaks, Linux uses LF. To prevent Git from showing every file as "modified" just because of line endings, ensure you have `/home/abdo/Dev/Cursus/.gitattributes` configured properly (it is already included in our repo). If you have issues, run:
  ```bash
  git config --global core.autocrlf input # On Linux
  git config --global core.autocrlf true  # On Windows
  ```
- **HTTPS Certificate Trust (Linux):** ASP.NET Core dev certificates often throw "Not Trusted" warnings on Linux browsers.
  - On Ubuntu/Fedora, run `dotnet dev-certs https --trust`.
  - If your browser still complains, you may need to manually import the cert to Chrome/Firefox, or temporarily bypass it by typing `thisisunsafe` on the error screen (Chrome only).

---
*For architectural guidelines, PR rules, and coding conventions, refer to [CONTRIBUTING.md](../CONTRIBUTING.md).*
