# SLIM C# SDK

C# SDK for [SLIM](https://github.com/agntcy/slim) (Secure Low-Latency Interactive Messaging).

This SDK provides an idiomatic C# API built on top of auto-generated bindings from [uniffi-bindgen-cs](https://github.com/NordSecurity/uniffi-bindgen-cs).

## Requirements

- .NET 8.0 or higher
- [GitHub CLI](https://cli.github.com/) (for downloading native libraries)
- [Task](https://taskfile.dev/) (optional, for build automation)

## Quick Start

### Using NuGet Package

```bash
dotnet add package Agntcy.Slim
```

The NuGet package includes native libraries for all supported platforms. No additional setup required!

```csharp
using Agntcy.Slim;

Slim.Initialize();
var service = Slim.GetGlobalService();
// ...
Slim.Shutdown();
```

### Building from Source

1. **Clone the repository:**
   ```bash
   git clone https://github.com/agntcy/slim-bindings-csharp
   cd slim-bindings-csharp
   ```

2. **Download native libraries and build:**
   ```bash
   # Install task if needed: https://taskfile.dev/installation/
   task setup  # Downloads pre-built native libraries
   task build  # Builds the .NET solution
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
task test:smoke
```

**Integration tests** (requires running server):
```bash
# Terminal 1: Start the server
cd ../slim/data-plane
task run:server

# Terminal 2: Run integration tests
task test:integration
```

**All tests:**
```bash
task test
```

## Development

### Available Tasks

```bash
task                  # List all available tasks
task setup            # Download pre-built native libraries
task build            # Build the .NET solution
task test             # Run all tests
task test:smoke       # Run smoke tests (no server required)
task pack             # Create NuGet package
task clean            # Clean all build artifacts
task regenerate       # Regenerate C# bindings (requires slim repo)
```

### Regenerating Bindings

If the SLIM bindings change upstream:

```bash
# Requires slim repo at ../slim
task regenerate
git add Slim/generated/
git commit -m "Regenerate bindings for vX.Y.Z"
```

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
dotnet add package Agntcy.Slim

# Build and run - native library is automatically included
dotnet run
```

## License

Apache-2.0 - See [LICENSE](LICENSE) for details.

## Related Projects

- [SLIM](https://github.com/agntcy/slim) - Core SLIM implementation
- [slim-bindings-go](https://github.com/agntcy/slim/tree/main/data-plane/bindings/go) - Go bindings
- [uniffi-bindgen-cs](https://github.com/NordSecurity/uniffi-bindgen-cs) - C# bindings generator for UniFFI
