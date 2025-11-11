# TicketSalesApp UI - Rust/Slint Linux Client

Native Linux GUI client for the Ticket Sales/Bus Fleet Management System, built with **Rust** and **Slint UI**.

## 🦀 Technology Stack

- **Rust 1.92.0** (2024 edition)
- **Slint 1.14** - Declarative UI framework
- **Tokio** - Async runtime
- **Reqwest** - HTTP client for REST API
- **Serde** - JSON serialization/deserialization
- **Chrono** - Date/time handling

## 📁 Project Structure

```
TicketSalesApp_UI_Rust_Linux/
├── src/
│   ├── main.rs                 # Application entry point & UI logic
│   ├── api/
│   │   ├── mod.rs             # Base API client
│   │   ├── auth.rs            # Authentication endpoints
│   │   ├── employees.rs       # Employee management
│   │   └── departments.rs     # Department management
│   └── models/
│       ├── mod.rs             # Model exports
│       ├── employee.rs        # Employee data model
│       ├── department.rs      # Department model
│       ├── user.rs            # User & authentication
│       ├── vacation_request.rs
│       └── training.rs
├── ui/
│   └── app-window.slint       # Slint UI definitions
├── Cargo.toml                 # Dependencies
└── build.rs                   # Slint build script
```

## 🚀 Features

### Implemented
- ✅ **Login system** with C# backend authentication
- ✅ **Employee management** (view, list)
- ✅ **Department browsing**
- ✅ **JWT token authentication**
- ✅ **Async API calls** with Tokio
- ✅ **Modern UI** with Slint
- ✅ **Multi-tab interface** (Employees, Departments, Vacations, Training)

### Planned
- ⏳ Add/Edit/Delete employees
- ⏳ Vacation request management
- ⏳ Training & certification tracking
- ⏳ Emergency contact management
- ⏳ Document management

## 🔧 Prerequisites

1. **Rust toolchain** (1.92+)
   ```bash
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   ```

2. **Backend API** running on `http://localhost:5000`
   - Start the C# ASP.NET Core API server
   - See main project README for backend setup

3. **Linux dependencies** (for Slint):
   ```bash
   # Ubuntu/Debian
   sudo apt install libfontconfig1-dev libxcb-render0-dev libxcb-shape0-dev libxcb-xfixes0-dev
   
   # Fedora
   sudo dnf install fontconfig-devel libxcb-devel
   
   # Arch
   sudo pacman -S fontconfig libxcb
   ```

## 📦 Building

```bash
# Clone the repository (if not already)
cd d:\code\bru-year1coursework\TicketSalesApp_UI_Rust_Linux

# Build debug version
cargo build

# Build release version (optimized)
cargo build --release
```

## ▶️ Running

```bash
# Run in debug mode
cargo run

# Run release version
cargo run --release

# Or run the compiled binary directly
./target/release/TicketSalesApp_UI_Rust_Linux
```

## 🔑 Default Login Credentials

Use the same credentials as the C# apps:
- **Username:** `admin`
- **Password:** `admin`

## 🌐 API Configuration

The API base URL is configured in `src/main.rs`:
```rust
const API_BASE_URL: &str = "http://localhost:5000";
```

Change this if your backend runs on a different host/port.

## 🎨 UI Architecture

### Slint Components

1. **LoginWindow** (`ui/app-window.slint`)
   - Username/password fields
   - Loading state
   - Error messages

2. **AppWindow** (`ui/app-window.slint`)
   - Header with user info & logout
   - TabWidget with:
     - **Сотрудники** (Employees) tab
     - **Отделы** (Departments) tab
     - **Отпуска** (Vacations) tab
     - **Обучение** (Training) tab
   - Status bar

### Rust Application Flow

```
main() 
  └─> show_login_window()
       └─> on login success
            └─> show_main_window()
                 ├─> load_employees()
                 ├─> on_refresh_employees()
                 ├─> on_add_employee_clicked()
                 └─> on_logout_clicked()
```

## 🔗 API Integration

All API calls use the same REST endpoints as the C#/Avalonia apps:

```rust
// Authentication
POST /api/auth/login

// Employees
GET  /api/employees
GET  /api/employees/{id}
POST /api/employees
PUT  /api/employees/{id}
DELETE /api/employees/{id}

// Departments
GET  /api/departments
GET  /api/departments/{id}
```

## 📝 Development Notes

### Adding New API Endpoints

1. Add method to appropriate `src/api/*.rs` file
2. Create/update model in `src/models/*.rs`
3. Call from UI event handlers in `src/main.rs`

### Updating UI

1. Edit `ui/app-window.slint`
2. Add callbacks in Slint components
3. Implement callback handlers in `src/main.rs`
4. Rebuild with `cargo build`

### Debugging

```bash
# Run with full logging
RUST_LOG=debug cargo run

# Check for errors
cargo check

# Run tests
cargo test
```

## 🐧 Why Rust for Linux?

1. **Performance** - Compiled, zero-cost abstractions
2. **Memory safety** - No segfaults or data races
3. **Native binaries** - No runtime dependencies
4. **Ecosystem** - Cargo, crates.io, excellent tooling
5. **Cross-platform** - Same codebase works on Linux, macOS, Windows

## 🆚 Comparison with Other Clients

| Feature | WinForms | Avalonia | **Rust/Slint** |
|---------|----------|----------|----------------|
| Platform | Windows only | Cross-platform | **Linux native** |
| UI Framework | WinForms (2002) | XAML/Avalonia | **Slint (modern)** |
| Language | C# | C# | **Rust** |
| Performance | Moderate | Good | **Excellent** |
| Memory | GC | GC | **Manual (safe)** |
| Binary Size | ~15MB + .NET | ~50MB | **~5MB** |
| Native Look | Windows only | Custom everywhere | **Custom** |

## 🔐 Security

- ✅ JWT tokens stored in memory only (not persisted)
- ✅ HTTPS support via rustls
- ✅ No credential storage
- ✅ Memory-safe Rust (no buffer overflows)

## 🏗️ Future Improvements

- [ ] Add local caching with SQLite
- [ ] Implement offline mode
- [ ] Add GTK/Qt theme integration
- [ ] Support for multiple backend servers
- [ ] i18n/l10n support
- [ ] Dark mode
- [ ] Keyboard shortcuts
- [ ] Advanced filtering/search

## 📚 Resources

- [Slint Documentation](https://slint.dev/docs)
- [Rust Book](https://doc.rust-lang.org/book/)
- [Tokio Tutorial](https://tokio.rs/tokio/tutorial)
- [Reqwest Docs](https://docs.rs/reqwest/)

## 📜 License

Same as parent project - MIT License

## 🤝 Contributing

This is a university coursework project. See main repository for contribution guidelines.

---

**Built with 🦀 Rust + ⚡ Slint for native Linux performance**
