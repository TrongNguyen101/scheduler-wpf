# Scheduler WPF Application

A comprehensive scheduling application with SQLite backup/restore functionality.

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Node.js 18+ (for backup server)
- SQLite

### Client Application

```bash
cd SchedulerSolution
dotnet build
dotnet run --project SchedulerWpfApp
```

### Backup Server Setup

See [SERVER_SETUP.md](SERVER_SETUP.md) for detailed instructions.

## 📁 Project Structure

```
scheduler-wpf/
├── SchedulerSolution/           # WPF Client Application
│   └── SchedulerWpfApp/
├── SERVER_SETUP.md             # Server implementation guide
├── TESTING_GUIDE.md            # Testing documentation
└── API_DOCUMENTATION.md        # API specifications
```

## 🔧 Features

- **Schedule Management**: Create, edit, and manage class schedules
- **Data Import/Export**: Excel file support for bulk operations
- **Backup & Restore**: Secure SQLite database backup with cloud storage
- **User Authentication**: JWT-based authentication system
- **Real-time Progress**: Progress tracking for long operations
