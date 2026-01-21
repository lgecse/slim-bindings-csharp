# SLIM C# Bindings

C# bindings for [SLIM](https://github.com/agntcy/slim) (Secure Low-Latency Interactive Messaging).

These bindings are auto-generated using [uniffi-bindgen-cs](https://github.com/NordSecurity/uniffi-bindgen-cs) from the same Rust UniFFI definitions used for Go, Python, and other language bindings, with a clean public API wrapper.

## Requirements

- .NET 8.0 or higher

**For building from source only:**
- [Task](https://taskfile.dev/)
- [Rust](https://rustup.rs/)

## Quick Start

### Using NuGet Package

```bash
dotnet add package Agntcy.SlimBindings
```

The NuGet package includes native libraries for all supported platforms. No additional setup required!

### Building from Source

1. **Clone the repository:**
   ```bash
   git clone https://github.com/agntcy/slim-bindings-csharp
   cd slim-bindings-csharp
   ```

2. **Ensure the SLIM repository is available:**
   The build expects the SLIM repo at `../slim`. You can override this:
   ```bash
   # Clone SLIM if needed
   git clone https://github.com/agntcy/slim ../slim
   ```

3. **Generate bindings and build:**
   ```bash
   # Install task if needed: https://taskfile.dev/installation/
   task generate  # Generates C# bindings from Rust library
   task build     # Builds the .NET solution
   ```

## API Overview

### Main Classes

| Class | Description |
|-------|-------------|
| `Slim` | Static entry point for initialization, shutdown, and global access |
| `SlimService` | Service for managing connections and creating apps |
| `SlimApp` | Application for managing sessions, subscriptions, and routing |
| `SlimSession` | Session for sending and receiving messages |
| `SlimName` | Identity in `org/namespace/app` format |
| `SlimMessage` | Received message with `Payload` (bytes) and `Text` (string) |
| `SlimSessionConfig` | Session configuration (type, MLS, retries) |

## Running Tests

**Smoke tests** (no server required):
```bash
task test -- --filter "Category!=Integration"
```

**Integration tests** (requires running server):
```bash
# Terminal 1: Start the server
cd ../slim/data-plane
task data-plane:run:server

# Terminal 2: Run integration tests
task test -- --filter "Category=Integration"
```

**All tests:**
```bash
task test
```

## Development

### Available Tasks

```bash
task              # List all available tasks
task generate     # Generate C# bindings (builds Rust library first)
task build        # Build the .NET solution (runs generate first)
task test         # Run tests
task pack         # Create NuGet package
task clean        # Clean all build artifacts
```

### Regenerating Bindings

If you modify the Rust bindings in the SLIM repository:

```bash
task clean
task generate
```

### uniffi-bindgen-cs Version

This project uses `uniffi-bindgen-cs v0.9.0+v0.28.3`, which matches the UniFFI version used in SLIM (v0.28.3).

## Platform Support

| Platform | Architecture | .NET RID | Status |
|----------|--------------|----------|--------|
| Linux    | x86_64       | linux-x64 | Supported |
| Linux    | aarch64      | linux-arm64 | Supported |
| macOS    | x86_64       | osx-x64 | Supported |
| macOS    | aarch64      | osx-arm64 | Supported |
| Windows  | x86_64       | win-x64 | Supported |

## NuGet Package

The NuGet package includes native libraries for all supported platforms using the standard `runtimes/{rid}/native/` structure. When you install the package, .NET automatically selects the correct native library for your platform.

```bash
# Install the package
dotnet add package Agntcy.SlimBindings

# Build and run - native library is automatically included
dotnet run
```

## License

Apache-2.0 - See [LICENSE](LICENSE) for details.

## Related Projects

- [SLIM](https://github.com/agntcy/slim) - Core SLIM implementation
- [slim-bindings-go](https://github.com/agntcy/slim/tree/main/data-plane/bindings/go) - Go bindings
- [uniffi-bindgen-cs](https://github.com/NordSecurity/uniffi-bindgen-cs) - C# bindings generator for UniFFI
