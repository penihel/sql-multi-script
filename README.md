# Free SQL Multi Script

**Free SQL Multi Script** is a free, open-source desktop application for executing SQL scripts across multiple SQL Server databases simultaneously. Built with .NET 8 and Windows Forms.

---

## Features

### Project Management
- Create, open, save, and close projects (`.smsjsonproj` files)
- Add existing `.sql` files or create new scripts directly in the app
- Reorder scripts (move up/down) and remove them from the project
- Unsaved changes detection before closing or switching projects

### SQL Editor
- Built-in SQL editor with syntax highlighting (powered by **ScintillaNET**)
- SQL keyword highlighting, line numbers, and auto-indent
- Edit and save scripts individually

### Database Distribution Lists
- Create and manage **Database Distribution Lists** - named groups of target databases
- Each list can include databases from multiple SQL Server connections
- Select/deselect individual databases before execution using checkbox columns with select all support

### Connection Management
- Register multiple SQL Server connections with support for:
  - **Windows Authentication**
  - **SQL Server Authentication**
  - **Microsoft Entra MFA** (Interactive)
  - **Microsoft Entra Integrated**
  - **Microsoft Entra Password**
- Auto-discover databases from a server connection
- Test connections before saving

### Script Execution
- Execute selected scripts against all selected databases in parallel (up to 10 concurrent connections)
- Pre-authentication phase: validates credentials on each server before execution begins
- Each script runs inside a transaction with automatic rollback on error
- Supports `GO` batch separators
- Cancel running executions at any time

### Results and Output
- **Execution Tree**: browse execution history with per-script and per-database status (Queued, Executing, Success, Error, Cancelled)
- **Messages Panel**: view info and error messages per database, filterable by selected databases
- **Result Grids**: query results displayed in tabbed data grids with database name and connection columns
- **Real-time streaming**: results and messages appear live during execution
- Filter results and messages by toggling database checkboxes in the results grid

### Excel Export
- Export result grids to **Excel (.xlsx)** files using **ClosedXML**
- One worksheet per result set, with headers and auto-sized columns
- Exports only visible/filtered rows

### Logging
- Dedicated log panel with timestamped info and error messages
- All events logged with `[INFO]` / `[ERRO]` prefixes

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 (Windows) |
| UI | Windows Forms |
| SQL Editor | ScintillaNET |
| Database | Microsoft.Data.SqlClient |
| Excel Export | ClosedXML |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging |

## Project Structure

```
 SQLMultiScript.Core/         # Domain models, interfaces, constants
 SQLMultiScript.Services/     # Business logic (execution, connections, projects)
 SQLMultiScript.Resources/    # Localized strings and images
 SQLMultiScript.UI/           # Windows Forms application (forms, controls, factories)
```

## Getting Started

1. Clone the repository
2. Open `SQLMultiScript.sln` in Visual Studio 2022+
3. Build and run the `SQLMultiScript.UI` project
4. Create a connection, set up a database distribution list, add scripts, and execute

## License

This project is free and open source.
