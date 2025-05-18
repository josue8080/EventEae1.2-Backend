
# EventEase Backup & Restore Setup

This package includes:

1. BackupController.cs - ASP.NET Core API controller for downloading and restoring database backups.
2. backup.sql - SQL script to create a database backup.
3. restore.sql - SQL script to restore the database from backup.
4. Instructions for setting up Windows Task Scheduler to automate backups.

---

## Setup Instructions

### 1. Add BackupController.cs

- Place `BackupController.cs` in your ASP.NET Core project's `Controllers` folder.
- Replace the namespace `YourNamespace` with your project's actual namespace.
- Ensure your project references `Microsoft.Data.SqlClient` package.
- Register authentication and authorization with an Admin role in your `Program.cs` or `Startup.cs`.

### 2. Connection String

Make sure your `appsettings.json` has a connection string named `DefaultConnection`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=eventEase1.2;Trusted_Connection=True;"
  }
}
```

---

### 3. SQL Scripts

- Place `backup.sql` and `restore.sql` in a folder, for example: `C:\db-scripts`
- Modify paths if necessary to match your environment.

---

### 4. Create Backup Folder

Create folder for backups:

```
C:\backups
```

Make sure SQL Server and your ASP.NET app have write permissions here.

---

### 5. Schedule Automatic Backups (Windows Task Scheduler)

- Open Task Scheduler
- Create a Basic Task called "EventEase DB Backup"
- Trigger: Daily (or your preferred schedule)
- Action: Start a program
- Program/script: `sqlcmd`
- Arguments:

```
-S YOUR_SERVER -E -i "C:\db-scripts\backup.sql"
```

- Finish and save.

---

### 6. Usage

- Use API endpoints (requires Admin role auth):
  - GET `/api/backup/download` to download current backup.
  - POST `/api/backup/restore` to upload and restore a backup file.

---

If you have any questions or need further help, just ask!
