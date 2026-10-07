# Rezber CLI

Rezber CLI is a command-line client for working with packages registered on a Rezber server. Use it to connect to a server, install a package into the current .NET project, publish a package file, or run the process as a system service.

> The command-line application and assembly are named `Rezber`; the project file is `Nexora.Cli.csproj`.

## Features

- Interactive sign-in and persistent server configuration
- Install a package from a Rezber server into the current .NET project
- Upload a package file to a Rezber server
- Run commands interactively or directly from a terminal
- Keep the process running as a systemd service
- Publish as a single-file application that uses the installed .NET runtime

## Prerequisites

- .NET SDK 10.0 or later
- Network access to a Rezber server
- To install a package: run the command from the destination project directory and ensure `dotnet` is available
- To run the service on Linux: systemd

Check the installed SDK:

```bash
dotnet --version
```

## Clone and build

Clone the repository and enter its directory:

```bash
git clone <repository-url>
cd <repository-directory>
```

Restore dependencies and build the project:

```bash
dotnet restore
dotnet build --configuration Release
```

Run the application from source:

```bash
dotnet run --project Nexora.Cli.csproj
```

Pass command-line arguments after `--`:

```bash
dotnet run --project Nexora.Cli.csproj -- help
```

## Publish

The project targets .NET 10 and is configured to publish as a single-file, framework-dependent application (`SelfContained=false`). For example, publish for Linux x64:

```bash
dotnet publish Nexora.Cli.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false
```

The output is created in a directory similar to:

```text
bin/Release/net10.0/linux-x64/publish/
```

For another operating system or architecture, pass the appropriate runtime identifier to `--runtime`, such as `win-x64` or `osx-arm64`. Since the application is not self-contained, the target machine must have the .NET 10 Runtime installed.

## Usage

### Show help

```bash
dotnet run --project Nexora.Cli.csproj -- help
```

After publishing, replace `dotnet run --project Nexora.Cli.csproj --` with the path to the published executable.

### Interactive mode

Run the application without arguments:

```bash
dotnet run --project Nexora.Cli.csproj
```

Enter commands at the `Rezber>` prompt. Use `exit` or `quit` to leave interactive mode, and `clear` or `cls` to clear the screen. Use `help` to display the general command help.

### Sign in to a server

```bash
dotnet run --project Nexora.Cli.csproj -- login
```

The CLI prompts for the server URL and API token. Enter the full URL, including its scheme, for example `https://rezber.example.com`.

Configuration is stored under the current user's home directory:

```text
~/.Rezber/config.json
```

The configuration file has this structure:

```json
{
  "ServerUrl": "https://rezber.example.com",
  "Token": "<api-token>"
}
```

> **Security note:** The token is stored as plain text, and terminal echo is not disabled while entering it. Restrict access to the configuration file, never commit it or include it in logs, and avoid signing in from shared terminals.

### Install a package

Run the command from the destination .NET project directory:

```bash
dotnet run --project /path/to/Nexora.Cli.csproj -- \
  package install Fundation.Abstractions --version 1.0.0
```

After downloading the package from the server, the CLI runs `dotnet add package` in the current directory and provides the temporary download directory along with NuGet.org as package sources. Keep the following in mind:

- Sign in with `login` before installing.
- Run the command from the directory containing the destination project.
- The server must return a package compatible with the requested name and version.
- Access to NuGet.org is also required to restore the project's other dependencies.

### Push a package

```bash
dotnet run --project /path/to/Nexora.Cli.csproj -- \
  push ./dist/MyPackage.1.0.0.nupkg \
  --name MyPackage \
  --version 1.0.0
```

The specified file must exist. The CLI uploads it to the server using a `PUT` request and sends the API token in the `X-Rezber-ApiKey` header.

## API contract

The CLI uses the following endpoints. The server must implement this contract:

| Operation | Request | Description |
| --- | --- | --- |
| Download package | `GET /api/packages/{name}/{version}/download` | A successful response must return package content usable as a NuGet package source. |
| Push package | `PUT /api/packages/push/{name}/{version}/{filename}` | The request body is a stream with content type `application/octet-stream`; authentication uses the `X-Rezber-ApiKey` header. |

The `name`, `version`, and `filename` path segments are URL-encoded. For unsuccessful responses, the CLI prints the HTTP status code and response details to standard error.

### Run as a service

Start the service manually:

```bash
Rezber service
```

The `daemon` alias is also supported. This command keeps the process alive. To manage it with systemd, use the included `nexora.service` example. Before installing the unit, update its `ExecStart` path to the location of the published executable:

```ini
ExecStart=/opt/Rezber/Rezber service
```

Install the unit and enable the service:

```bash
sudo install -m 644 nexora.service /etc/systemd/system/nexora.service
sudo systemctl daemon-reload
sudo systemctl enable --now nexora.service
sudo systemctl status nexora.service
```

The service runs as the user configured in systemd. That user must be able to access the executable. Configuration is loaded from that user's home directory, so sign in once as the same user. The current service implementation only keeps the process alive; it does not run a worker or perform background processing.

## Behavior and exit codes

- Exit code `0`: command completed successfully or help was displayed
- Non-zero exit code: command error, missing file, unsuccessful server response, or SDK execution failure
- Command names and options are matched without case sensitivity.
- Running without arguments starts interactive mode.

## Project structure

| File | Responsibility |
| --- | --- |
| `Program.cs` | Application entry point and top-level error handling |
| `CliApplication.cs` | Command registration, dispatch, and help output |
| `InteractiveShell.cs` | Interactive terminal loop |
| `ConfigStore.cs` | Persisting and loading user configuration |
| `LoginCommand.cs` | Collecting and saving connection details |
| `PackageInstallCommand.cs` | Downloading and installing a package into the current project |
| `PushCommand.cs` | Uploading a package file to the server |
| `ServiceCommand.cs` | Keeping the service process alive |
| `nexora.service` | Example systemd unit |

## Contributing

To add a command:

1. Create a class that implements `ICliCommand`.
2. Return the command name and any aliases from `Aliases`.
3. Implement the command logic in `ExecuteAsync` and report user-facing errors using the `CliApplication.Fail` pattern.
4. Register an instance of the command in the command array in `CliApplication`.
5. Update the CLI help and this README to document the new behavior.

Before submitting changes, build the project:

```bash
dotnet build --configuration Release
```

In pull requests, describe the purpose of the change, how to reproduce or verify it, and any user-visible behavior changes. Never commit configuration files containing real credentials or API tokens.

## License

No license file is currently included in this repository. Contact the repository maintainer to clarify the project's licensing before using or redistributing it.
