# Testing Guide

## 🎯 Overview

This document provides comprehensive testing strategies for the Scheduler WPF application, including unit tests, integration tests, and end-to-end testing scenarios.

## 🧪 Testing Strategy

### 1. Unit Testing (Client-Side)

- **BackupRestoreService**: Core backup/restore logic
- **ViewModels**: MVVM pattern testing
- **Helpers & Utilities**: Utility functions
- **Converters**: WPF value converters

### 2. Integration Testing (Server-Side)

- **API Endpoints**: REST API functionality
- **Authentication**: JWT validation
- **File Operations**: Upload/download workflows

### 3. End-to-End Testing

- **Full Backup Workflow**: Client to server backup process
- **Restore Workflow**: Complete restore operations
- **Error Scenarios**: Network failures, invalid files

## 🔧 Client-Side Testing Setup

### Prerequisites

```bash
cd SchedulerSolution
dotnet add SchedulerWpfApp package Microsoft.NET.Test.Sdk
dotnet add SchedulerWpfApp package NUnit
dotnet add SchedulerWpfApp package NUnit3TestAdapter
dotnet add SchedulerWpfApp package Moq
dotnet add SchedulerWpfApp package FluentAssertions
```

### Test Project Structure

```
SchedulerWpfApp.Tests/
├── Unit/
│   ├── Services/
│   │   ├── BackupRestoreServiceTests.cs
│   │   └── ScheduleServicesTests.cs
│   ├── ViewModels/
│   │   ├── BackupRestoreViewModelTests.cs
│   │   └── MainViewModelTests.cs
│   └── Helpers/
│       └── UtilityTests.cs
├── Integration/
│   ├── DatabaseTests.cs
│   └── ApiIntegrationTests.cs
└── TestData/
    ├── test_backup.db
    └── invalid_file.txt
```

## 📝 Unit Test Examples

### BackupRestoreService Tests

```csharp
using NUnit.Framework;
using Moq;
using FluentAssertions;
using SchedulerWpfApp.ServiceRefactor.BackupRestoreService;
using SchedulerWpfApp.Data;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

[TestFixture]
public class BackupRestoreServiceTests
{
    private BackupRestoreService _service;
    private Mock<IHttpClientFactory> _httpClientFactory;
    private Mock<IConfiguration> _configuration;
    private Mock<DataContext> _dataContext;
    private string _testDatabasePath;

    [SetUp]
    public void Setup()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _configuration = new Mock<IConfiguration>();
        _dataContext = new Mock<DataContext>();

        // Setup test database path
        _testDatabasePath = Path.Combine(Path.GetTempPath(), "test_app.db");
        _configuration.Setup(c => c["DatabasePath"]).Returns(_testDatabasePath);

        _service = new BackupRestoreService(
            _httpClientFactory.Object,
            _configuration.Object,
            _dataContext.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup test files
        if (File.Exists(_testDatabasePath))
        {
            File.Delete(_testDatabasePath);
        }

        _service?.Dispose();
    }

    [Test]
    public async Task CreateBackupAsync_WithValidDatabase_ShouldCreateBackupFile()
    {
        // Arrange
        await CreateTestDatabase();
        var progress = new Mock<IProgress<BackupProgress>>();

        // Act
        var result = await _service.CreateBackupAsync("Test backup", progress.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        File.Exists(result.LocalFilePath).Should().BeTrue();

        // Verify progress was reported
        progress.Verify(p => p.Report(It.IsAny<BackupProgress>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task CreateBackupAsync_WithDatabaseLocked_ShouldRetryAndSucceed()
    {
        // Arrange
        await CreateTestDatabase();
        var progress = new Mock<IProgress<BackupProgress>>();

        // Simulate database lock by keeping connection open
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDatabasePath}");
        await connection.OpenAsync();

        // Act & Assert
        var result = await _service.CreateBackupAsync("Test backup", progress.Object);
        result.Should().NotBeNull();
        // Service should handle lock with retry logic
    }

    [Test]
    public async Task RestoreFromBackupAsync_WithValidBackup_ShouldRestoreDatabase()
    {
        // Arrange
        await CreateTestDatabase();
        var backupPath = await CreateTestBackup();
        var progress = new Mock<IProgress<BackupProgress>>();

        // Act
        var result = await _service.RestoreFromBackupAsync(backupPath, progress.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        File.Exists(_testDatabasePath).Should().BeTrue();
    }

    [Test]
    public async Task RestoreFromBackupAsync_WithInvalidFile_ShouldFail()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "invalid.txt");
        await File.WriteAllTextAsync(invalidPath, "Not a SQLite file");
        var progress = new Mock<IProgress<BackupProgress>>();

        // Act
        var result = await _service.RestoreFromBackupAsync(invalidPath, progress.Object);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid SQLite");

        // Cleanup
        File.Delete(invalidPath);
    }

    [Test]
    public void VerifyBackupFile_WithValidSQLiteFile_ShouldReturnTrue()
    {
        // Arrange
        var validPath = CreateValidSQLiteFile();

        // Act
        var result = _service.VerifyBackupFile(validPath);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        File.Delete(validPath);
    }

    [Test]
    public void VerifyBackupFile_WithInvalidFile_ShouldReturnFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "invalid.txt");
        File.WriteAllText(invalidPath, "Not SQLite");

        // Act
        var result = _service.VerifyBackupFile(invalidPath);

        // Assert
        result.Should().BeFalse();

        // Cleanup
        File.Delete(invalidPath);
    }

    [Test]
    public async Task EnsureDatabaseClosedWithRetry_ShouldCloseAllConnections()
    {
        // Arrange
        await CreateTestDatabase();

        // Act & Assert
        await _service.EnsureDatabaseClosedWithRetry();

        // Verify database can be accessed exclusively
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDatabasePath};Mode=ReadWriteCreate");
        await connection.OpenAsync();
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    private async Task CreateTestDatabase()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS TestTable (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );
            INSERT INTO TestTable (Name) VALUES ('Test Data');
        ";
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> CreateTestBackup()
    {
        var backupPath = Path.Combine(Path.GetTempPath(), "test_backup.db");
        File.Copy(_testDatabasePath, backupPath, true);
        return backupPath;
    }

    private string CreateValidSQLiteFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "valid.db");
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE test (id INTEGER)";
        command.ExecuteNonQuery();

        return path;
    }
}
```

### BackupRestoreViewModel Tests

```csharp
using NUnit.Framework;
using Moq;
using FluentAssertions;
using SchedulerWpfApp.ViewModel;
using SchedulerWpfApp.ServiceRefactor.BackupRestoreService;
using System.Threading.Tasks;

[TestFixture]
public class BackupRestoreViewModelTests
{
    private BackupRestoreViewModel _viewModel;
    private Mock<IBackupRestoreService> _backupService;

    [SetUp]
    public void Setup()
    {
        _backupService = new Mock<IBackupRestoreService>();
        _viewModel = new BackupRestoreViewModel(_backupService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel?.Dispose();
    }

    [Test]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.CreateBackupCommand.Should().NotBeNull();
        _viewModel.RestoreBackupCommand.Should().NotBeNull();
        _viewModel.RefreshBackupsCommand.Should().NotBeNull();
        _viewModel.IsOperationInProgress.Should().BeFalse();
        _viewModel.BackupDescription.Should().BeEmpty();
    }

    [Test]
    public async Task CreateBackupCommand_WhenExecuted_ShouldCallService()
    {
        // Arrange
        _viewModel.BackupDescription = "Test backup";
        var expectedResult = new BackupResult { Success = true, LocalFilePath = "test.db" };

        _backupService
            .Setup(s => s.CreateBackupAsync(It.IsAny<string>(), It.IsAny<IProgress<BackupProgress>>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _viewModel.CreateBackupCommand.ExecuteAsync(null);

        // Assert
        _backupService.Verify(s => s.CreateBackupAsync("Test backup", It.IsAny<IProgress<BackupProgress>>()), Times.Once);
        _viewModel.LastOperationResult.Should().Be("Backup created successfully");
    }

    [Test]
    public async Task CreateBackupCommand_WhenServiceFails_ShouldShowError()
    {
        // Arrange
        var expectedResult = new BackupResult { Success = false, ErrorMessage = "Service error" };

        _backupService
            .Setup(s => s.CreateBackupAsync(It.IsAny<string>(), It.IsAny<IProgress<BackupProgress>>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _viewModel.CreateBackupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastOperationResult.Should().Contain("Service error");
    }

    [Test]
    public void IsOperationInProgress_DuringOperation_ShouldBeTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<BackupResult>();
        _backupService
            .Setup(s => s.CreateBackupAsync(It.IsAny<string>(), It.IsAny<IProgress<BackupProgress>>()))
            .Returns(tcs.Task);

        // Act
        var task = _viewModel.CreateBackupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsOperationInProgress.Should().BeTrue();

        // Complete the operation
        tcs.SetResult(new BackupResult { Success = true });
    }

    [Test]
    public async Task ProgressReporting_ShouldUpdateProgressProperty()
    {
        // Arrange
        var progressValues = new List<int>();
        _viewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(_viewModel.ProgressPercentage))
                progressValues.Add(_viewModel.ProgressPercentage);
        };

        _backupService
            .Setup(s => s.CreateBackupAsync(It.IsAny<string>(), It.IsAny<IProgress<BackupProgress>>()))
            .Callback<string, IProgress<BackupProgress>>((desc, progress) => {
                progress.Report(new BackupProgress { PercentComplete = 50, StatusMessage = "Half done" });
                progress.Report(new BackupProgress { PercentComplete = 100, StatusMessage = "Complete" });
            })
            .ReturnsAsync(new BackupResult { Success = true });

        // Act
        await _viewModel.CreateBackupCommand.ExecuteAsync(null);

        // Assert
        progressValues.Should().Contain(50);
        progressValues.Should().Contain(100);
    }
}
```

## 🌐 Server-Side Testing

### API Integration Tests (Express.js)

```javascript
// tests/integration/backup.test.js
const request = require("supertest");
const fs = require("fs");
const path = require("path");
const app = require("../../server");

describe("Backup API Integration Tests", () => {
  let authToken;
  const testUserId = "test-user-123";

  beforeAll(async () => {
    // Create test JWT token
    const jwt = require("jsonwebtoken");
    const config = require("../../config/config");

    authToken = jwt.sign(
      {
        sub: testUserId,
        username: "testuser",
        email: "test@example.com",
      },
      config.jwt.secret,
      { expiresIn: "1h" }
    );
  });

  afterAll(async () => {
    // Cleanup test files
    const testDir = path.join("./backups/users", testUserId);
    if (fs.existsSync(testDir)) {
      fs.rmSync(testDir, { recursive: true, force: true });
    }
  });

  describe("POST /api/backups/upload", () => {
    it("should upload a valid SQLite backup", async () => {
      // Create test SQLite file
      const testDbPath = path.join(__dirname, "../fixtures/test.db");
      createTestSQLiteFile(testDbPath);

      const response = await request(app)
        .post("/api/backups/upload")
        .set("Authorization", `Bearer ${authToken}`)
        .attach("backup", testDbPath)
        .field("description", "Test backup upload");

      expect(response.status).toBe(201);
      expect(response.body.message).toBe("Backup uploaded successfully");
      expect(response.body.backup).toHaveProperty("filename");

      // Cleanup
      fs.unlinkSync(testDbPath);
    });

    it("should reject invalid file types", async () => {
      const testFilePath = path.join(__dirname, "../fixtures/invalid.txt");
      fs.writeFileSync(testFilePath, "Not a SQLite file");

      const response = await request(app)
        .post("/api/backups/upload")
        .set("Authorization", `Bearer ${authToken}`)
        .attach("backup", testFilePath);

      expect(response.status).toBe(400);
      expect(response.body.error).toContain("Invalid file type");

      // Cleanup
      fs.unlinkSync(testFilePath);
    });

    it("should require authentication", async () => {
      const response = await request(app).post("/api/backups/upload");

      expect(response.status).toBe(401);
      expect(response.body.error).toContain("Authentication token required");
    });
  });

  describe("GET /api/backups/list", () => {
    beforeEach(async () => {
      // Upload a test backup
      const testDbPath = path.join(__dirname, "../fixtures/test.db");
      createTestSQLiteFile(testDbPath);

      await request(app)
        .post("/api/backups/upload")
        .set("Authorization", `Bearer ${authToken}`)
        .attach("backup", testDbPath)
        .field("description", "Test backup for listing");

      fs.unlinkSync(testDbPath);
    });

    it("should list user backups", async () => {
      const response = await request(app)
        .get("/api/backups/list")
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(200);
      expect(response.body.backups).toBeInstanceOf(Array);
      expect(response.body.backups.length).toBeGreaterThan(0);
      expect(response.body.backups[0]).toHaveProperty("filename");
      expect(response.body.backups[0]).toHaveProperty("uploadDate");
      expect(response.body.backups[0]).toHaveProperty("size");
    });

    it("should return empty array for new user", async () => {
      const newUserToken = jwt.sign(
        { sub: "new-user-456", username: "newuser" },
        require("../../config/config").jwt.secret
      );

      const response = await request(app)
        .get("/api/backups/list")
        .set("Authorization", `Bearer ${newUserToken}`);

      expect(response.status).toBe(200);
      expect(response.body.backups).toEqual([]);
    });
  });

  describe("GET /api/backups/download/:filename", () => {
    let uploadedFilename;

    beforeEach(async () => {
      // Upload a test backup
      const testDbPath = path.join(__dirname, "../fixtures/test.db");
      createTestSQLiteFile(testDbPath);

      const uploadResponse = await request(app)
        .post("/api/backups/upload")
        .set("Authorization", `Bearer ${authToken}`)
        .attach("backup", testDbPath);

      uploadedFilename = uploadResponse.body.backup.filename;
      fs.unlinkSync(testDbPath);
    });

    it("should download existing backup", async () => {
      const response = await request(app)
        .get(`/api/backups/download/${uploadedFilename}`)
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(200);
      expect(response.headers["content-type"]).toContain(
        "application/octet-stream"
      );
    });

    it("should return 404 for non-existent backup", async () => {
      const response = await request(app)
        .get("/api/backups/download/non-existent.db")
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(404);
      expect(response.body.error).toBe("Backup file not found");
    });

    it("should prevent path traversal attacks", async () => {
      const response = await request(app)
        .get("/api/backups/download/../../../etc/passwd")
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(400);
      expect(response.body.error).toBe("Invalid filename");
    });
  });

  describe("DELETE /api/backups/delete/:filename", () => {
    let uploadedFilename;

    beforeEach(async () => {
      // Upload a test backup
      const testDbPath = path.join(__dirname, "../fixtures/test.db");
      createTestSQLiteFile(testDbPath);

      const uploadResponse = await request(app)
        .post("/api/backups/upload")
        .set("Authorization", `Bearer ${authToken}`)
        .attach("backup", testDbPath);

      uploadedFilename = uploadResponse.body.backup.filename;
      fs.unlinkSync(testDbPath);
    });

    it("should delete existing backup", async () => {
      const response = await request(app)
        .delete(`/api/backups/delete/${uploadedFilename}`)
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(200);
      expect(response.body.message).toBe("Backup deleted successfully");

      // Verify file is deleted
      const listResponse = await request(app)
        .get("/api/backups/list")
        .set("Authorization", `Bearer ${authToken}`);

      const remainingBackups = listResponse.body.backups.filter(
        (backup) => backup.filename === uploadedFilename
      );
      expect(remainingBackups).toHaveLength(0);
    });

    it("should return 404 for non-existent backup", async () => {
      const response = await request(app)
        .delete("/api/backups/delete/non-existent.db")
        .set("Authorization", `Bearer ${authToken}`);

      expect(response.status).toBe(404);
      expect(response.body.error).toBe("Backup file not found");
    });
  });
});

// Helper function to create test SQLite file
function createTestSQLiteFile(filePath) {
  const sqlite3 = require("sqlite3");
  const db = new sqlite3.Database(filePath);

  db.serialize(() => {
    db.run("CREATE TABLE test (id INTEGER PRIMARY KEY, name TEXT)");
    db.run("INSERT INTO test (name) VALUES ('Test Data')");
  });

  db.close();
}
```

### Authentication Tests

```javascript
// tests/unit/auth.test.js
const authMiddleware = require("../../middleware/auth");
const jwt = require("jsonwebtoken");
const config = require("../../config/config");

describe("Authentication Middleware", () => {
  let req, res, next;

  beforeEach(() => {
    req = {
      headers: {},
    };
    res = {
      status: jest.fn().mockReturnThis(),
      json: jest.fn().mockReturnThis(),
    };
    next = jest.fn();
  });

  it("should authenticate valid JWT token", () => {
    const token = jwt.sign(
      { sub: "user123", username: "testuser" },
      config.jwt.secret
    );

    req.headers.authorization = `Bearer ${token}`;

    authMiddleware(req, res, next);

    expect(req.user).toBeDefined();
    expect(req.user.id).toBe("user123");
    expect(req.user.username).toBe("testuser");
    expect(next).toHaveBeenCalled();
  });

  it("should reject missing authorization header", () => {
    authMiddleware(req, res, next);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({
      error: "Authentication token required",
    });
    expect(next).not.toHaveBeenCalled();
  });

  it("should reject invalid token format", () => {
    req.headers.authorization = "InvalidFormat token123";

    authMiddleware(req, res, next);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({
      error: "Authentication token required",
    });
  });

  it("should reject expired token", () => {
    const expiredToken = jwt.sign(
      { sub: "user123", exp: Math.floor(Date.now() / 1000) - 3600 },
      config.jwt.secret
    );

    req.headers.authorization = `Bearer ${expiredToken}`;

    authMiddleware(req, res, next);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({
      error: "Invalid or expired token",
    });
  });
});
```

## 🔄 End-to-End Testing

### E2E Test Scenarios

```csharp
// E2ETests/BackupRestoreWorkflowTests.cs
[TestFixture]
public class BackupRestoreWorkflowTests
{
    private TestServer _server;
    private BackupRestoreService _service;
    private string _testDatabasePath;

    [SetUp]
    public async Task Setup()
    {
        // Start test server
        _server = new TestServerBuilder().Build();
        await _server.StartAsync();

        // Configure service to use test server
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["ApiSettings:BaseUrl"]).Returns(_server.BaseUrl);

        _testDatabasePath = Path.Combine(Path.GetTempPath(), "e2e_test.db");
        config.Setup(c => c["DatabasePath"]).Returns(_testDatabasePath);

        _service = new BackupRestoreService(
            _server.HttpClientFactory,
            config.Object,
            new Mock<DataContext>().Object
        );
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server?.StopAsync();
        _service?.Dispose();

        if (File.Exists(_testDatabasePath))
            File.Delete(_testDatabasePath);
    }

    [Test]
    public async Task FullBackupWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        await CreateTestDatabase();
        var progress = new TestProgressReporter<BackupProgress>();

        // Act - Create backup
        var backupResult = await _service.CreateBackupAsync("E2E Test Backup", progress);

        // Assert - Backup created
        backupResult.Success.Should().BeTrue();
        File.Exists(backupResult.LocalFilePath).Should().BeTrue();
        progress.Reports.Should().NotBeEmpty();
        progress.Reports.Last().PercentComplete.Should().Be(100);

        // Act - List backups (verify upload)
        var listResult = await _service.ListBackupsAsync();

        // Assert - Backup listed
        listResult.Success.Should().BeTrue();
        listResult.Backups.Should().Contain(b => b.Description == "E2E Test Backup");

        // Act - Download backup
        var downloadedPath = Path.Combine(Path.GetTempPath(), "downloaded_backup.db");
        var downloadResult = await _service.DownloadBackupAsync(
            listResult.Backups.First().Filename,
            downloadedPath,
            progress
        );

        // Assert - Download successful
        downloadResult.Success.Should().BeTrue();
        File.Exists(downloadedPath).Should().BeTrue();

        // Cleanup
        File.Delete(downloadedPath);
    }

    [Test]
    public async Task NetworkFailureScenario_ShouldHandleGracefully()
    {
        // Arrange
        await CreateTestDatabase();
        var progress = new TestProgressReporter<BackupProgress>();

        // Simulate network failure
        await _server.StopAsync();

        // Act
        var result = await _service.CreateBackupAsync("Network Test", progress);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("network");

        // Verify local backup still created
        File.Exists(result.LocalFilePath).Should().BeTrue();
    }

    private async Task CreateTestDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_testDatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE Schedules (
                Id INTEGER PRIMARY KEY,
                SubjectCode TEXT,
                RoomName TEXT,
                TimeSlot INTEGER
            );
            INSERT INTO Schedules (SubjectCode, RoomName, TimeSlot)
            VALUES ('CS101', 'Room A', 1), ('MATH201', 'Room B', 2);
        ";
        await command.ExecuteNonQueryAsync();
    }
}

public class TestProgressReporter<T> : IProgress<T>
{
    public List<T> Reports { get; } = new();

    public void Report(T value)
    {
        Reports.Add(value);
    }
}
```

## 🚀 Running Tests

### Client-Side Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Server-Side Tests

```bash
# Run all tests
npm test

# Run with coverage
npm run test:coverage

# Run specific test file
npm test -- --testPathPattern=backup.test.js

# Watch mode
npm run test:watch
```

## 📊 Test Coverage Goals

- **Unit Tests**: >90% code coverage
- **Integration Tests**: All API endpoints
- **E2E Tests**: Critical user workflows
- **Error Scenarios**: Network failures, invalid data, authentication errors

## 🔧 Continuous Integration

### GitHub Actions Example

```yaml
# .github/workflows/test.yml
name: Test Suite

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test-client:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Restore dependencies
        run: dotnet restore SchedulerSolution/

      - name: Build
        run: dotnet build SchedulerSolution/ --no-restore

      - name: Test
        run: dotnet test SchedulerSolution/ --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v3

  test-server:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: 18

      - name: Install dependencies
        run: npm ci
        working-directory: backup-server

      - name: Run tests
        run: npm test
        working-directory: backup-server
```

This comprehensive testing guide ensures robust quality assurance for both client and server components of the backup/restore system.
