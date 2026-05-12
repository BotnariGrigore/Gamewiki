# GameWikiApp (WinForms)

Minimal scaffold of the GameWikiApp WinForms application (NET 6).

Setup:

1. Edit `App.config` to set `GameWikiDb` connection string to your MySQL server.
2. Open a terminal in the project folder and run:

```powershell
dotnet restore
dotnet build
dotnet run
```

Notes:
- This is a minimal skeleton including models, a DB helper, a basic repository, an auth service, a PBKDF2 password hasher, and a simple login/home UI implemented in code (no designer files).
- Add more services, repositories and forms as needed following the provided patterns.
