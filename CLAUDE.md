# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MAGNOR_POS is a comprehensive Point of Sale (POS) system designed for retail stores, restaurants, and service businesses. See `requeriemientos.md` for full functional requirements.

### Key Modules (Planned)
1. **Sales (POS)**: Fast sales registration, barcode scanning, multiple payment types, cash register management
2. **Inventory**: Product management, SKU/barcode, stock alerts, warehouse control
3. **Purchases**: Supplier management, automatic inventory updates
4. **Clients & Suppliers**: Transaction history, customer classification
5. **Users & Roles**: Role-based access (Admin, Cashier, Waiter, Inventory, Supervisor)
6. **Reports**: Sales, profits, best-sellers, exports to PDF/Excel
7. **Restaurant Features** (Optional): Table control, kitchen orders, order modifiers, recipes

### Technical Requirements (from requeriemientos.md)
- **Database**: SQL Server (primary), SQLite (lightweight mode), Entity Framework Core
- **Architecture**: MVVM (Model-View-ViewModel) with dependency injection
- **Performance**: Sales transactions under 3 seconds
- **Hardware Support**: Thermal printers, barcode scanners, cash drawers, touchscreens, kitchen printers
- **Security**: Password encryption, activity auditing, role-based permissions, session timeout

## Technology Stack

- **Framework**: WPF (Windows Presentation Foundation) on .NET 8.0
- **Language**: C# with nullable reference types enabled
- **Platform**: Windows 7.0+ only (net8.0-windows)
- **Project Type**: Desktop GUI application
- **Dependencies**:
  - Entity Framework Core 8.0.11 (SQLite)
  - BCrypt.Net-Next 4.0.3 (password hashing)

## Build and Development Commands

### Building
```bash
# Restore dependencies (if needed)
dotnet restore

# Build the project
dotnet build

# Build for release
dotnet build -c Release
```

### Running
```bash
# Run the application
dotnet run --project MAGNOR_POS

# Or from within the MAGNOR_POS directory
cd MAGNOR_POS
dotnet run
```

### Visual Studio
- Open `MAGNOR_POS.sln` in Visual Studio 2022 (v17.14+)
- Press F5 to build and run
- Press Ctrl+Shift+B to build only

## Architecture Overview

This is a **WPF desktop application** in its initial skeleton stage. The codebase follows the standard WPF structure:

### Application Entry
- **App.xaml/App.xaml.cs**: Application root and startup configuration
- **Splash Screen**: First window shown - fullscreen loading screen with animations (2 seconds)
- **Login Window**: Second window - authenticates user before accessing main application
- **MainWindow.xaml/MainWindow.xaml.cs**: Main application window (shown after successful login, currently placeholder)

### Project Structure
```
MAGNOR_POS/               # Main project directory
├── App.xaml              # WPF application definition (starts with LoginWindow)
├── App.xaml.cs           # Application code-behind
├── MainWindow.xaml       # Main window UI (shown after login)
├── MainWindow.xaml.cs    # Main window logic
├── AssemblyInfo.cs       # Assembly metadata
├── MAGNOR_POS.csproj     # Project file
├── Assets/
│   └── Images/           # Logos and graphics
├── Models/
│   ├── User.cs           # User entity
│   └── Role.cs           # Role entity + RoleType enum
├── Data/
│   ├── AppDbContext.cs   # EF Core DbContext
│   └── DbInitializer.cs  # Database initialization
├── Services/
│   └── AuthenticationService.cs  # Login/logout logic
├── ViewModels/
│   ├── ViewModelBase.cs  # Base class for all ViewModels
│   ├── RelayCommand.cs   # Command implementations
│   └── LoginViewModel.cs # Login screen ViewModel
├── Views/
│   ├── SplashScreen.xaml[.cs]   # Loading screen (fullscreen)
│   └── LoginWindow.xaml[.cs]    # Login window
└── Converters/
    └── BooleanToVisibilityConverter.cs  # XAML value converters
```

### Current State
The application has a functional authentication system with:
- **Splash Screen**: Professional fullscreen loading screen with:
  - Animated logo pulse effect
  - Rotating loading spinner
  - Animated progress bar
  - Loading status messages
  - Gradient background with decorative elements
- **LoginWindow** with modern UI:
  - Logo integration (logo_login.png)
  - Form validation
  - Error message display
- **Authentication**: SQLite database auto-initialized with BCrypt password hashing
- **Default admin user**: username `admin`, password `admin123`
- **MVVM architecture**: Base classes for ViewModels and Commands
- **Database**: Stored in `%LocalAppData%\MAGNOR_POS\magnor_pos.db`
- **Application icon**: favicon.ico integrated
- **MainWindow**: Placeholder shown after successful login

### Planned Architecture (MVVM Pattern)

Following the requirements in `requeriemientos.md`, the application will use MVVM architecture with:

**Service Layer** (per module):
- `AuthenticationService` - Login/logout, password validation, session management
- `SalesService` - POS operations, payment processing, cash register
- `InventoryService` - Product/stock management, alerts
- `PurchaseService` - Supplier orders, cost updates
- `ClientService` - Customer/supplier management
- `UserService` - User CRUD, roles, permissions, activity auditing
- `ReportService` - Sales reports, exports (PDF/Excel)

**Data Layer**:
- Entity Framework Core with SQL Server (primary) or SQLite (lightweight)
- Code-first migrations for database schema
- Repository pattern for data access

**UI Layer** (WPF):
- ViewModels for each module (using INotifyPropertyChanged)
- Views (XAML) with data binding
- Commands for user actions
- Dependency injection for services

**Key Design Considerations**:
- **Authentication Required**: Login window is the entry point - no access without authentication
- Touchscreen-friendly UI (large buttons)
- Fast performance (< 3 seconds per transaction)
- Hardware integration (printers, barcode scanners, cash drawers)
- Role-based access control throughout UI
- Session management with automatic timeout for security

**Application Flow**:
1. App starts → Splash Screen shown (fullscreen, ~2 seconds)
2. Auto-transition → Login Window shown
3. User authenticates (username/password)
4. System validates credentials and loads user role/permissions
5. On success → MainWindow shown with modules accessible per user role
6. On failure → Error message, stay on Login Window
7. Inactive session → Auto-logout, return to Login Window

## File Organization

### XAML Files
- `.xaml` files contain UI markup (XML-based)
- `.xaml.cs` files contain code-behind (event handlers and UI logic)
- Always keep XAML and code-behind pairs together

### Adding New Windows/Views
1. Add new XAML file with corresponding .xaml.cs
2. Reference in App.xaml if needed for application-level resources
3. Navigate using `Window.Show()` or `Window.ShowDialog()`

### Assets and Resources
- **Logos/Images**: Place in `MAGNOR_POS/Assets/Images/`
  - `logo.png` - Main application logo
  - `logo-small.png` - Small logo for headers/title bar
  - `icon.ico` - Application icon
- **Usage in XAML**: `<Image Source="/Assets/Images/logo.png" />`
- **Build Action**: Set to "Resource" in Visual Studio or add to .csproj:
  ```xml
  <ItemGroup>
    <Resource Include="Assets\Images\**\*" />
  </ItemGroup>
  ```

## Code Style Notes

- **Nullable Reference Types**: Enabled project-wide - handle null cases explicitly
- **Implicit Usings**: Common namespaces auto-imported (System, System.Collections.Generic, etc.)
- **WPF Conventions**: Follow x:Name for XAML elements, PascalCase for event handlers
