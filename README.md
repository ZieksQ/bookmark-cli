# BookmarkCli

A lightweight terminal-based bookmark manager built with **C# and .NET 10**.

Organize, manage, and quickly access your website bookmarks directly from your terminal. BookmarkCli uses JSON file storage instead of a database and integrates with [fzf](https://github.com/junegunn/fzf) for fuzzy searching and [Gum](https://github.com/charmbracelet/gum) for interactive terminal interfaces.

## Features

- **Bookmark Management** – Add, rename, move, and delete bookmarks.
- **Folder Management** – Create, organize, and manage bookmark folders.
- **Interactive Prompts** – Use Gum to add bookmarks and manage folders without manually entering CLI arguments.
- **Manual Commands** – Supports command-line arguments for scripting and automation.
- **Fuzzy Search** – Quickly find bookmarks using fzf.
- **Browser Integration** – Open bookmarked websites directly from your terminal.
- **JSON Storage** – Store bookmarks locally without requiring a database.
- **Lightweight** – Minimal dependencies and a simple terminal-based workflow.

## Requirements

BookmarkCli relies on external terminal utilities.

| Dependency | Purpose | Required |
|---|---|---|
| [fzf](https://github.com/junegunn/fzf) | Fuzzy searching | Yes |
| [Gum](https://github.com/charmbracelet/gum) | Interactive terminal interfaces | Yes for interactive features |
| Web Browser | Open bookmarked websites | Yes for opening bookmarks |

**Note:** Regular users do not need to install .NET. The distributed application includes its own .NET runtime.

## Installation

### Option 1: Download a Release (Recommended)

1. Visit the [GitHub Releases](../../releases) page.
2. Download the archive matching your system architecture.
3. Extract the archive.
4. Install the executable.

**Linux x64 example:**

```bash
tar -xzf BookmarkCli-linux-x64.tar.gz

mkdir -p ~/.local/bin

install -m 755 BookmarkCli ~/.local/bin/bookmark-cli
```

Ensure `~/.local/bin` is included in your PATH:

```bash
export PATH="$HOME/.local/bin:$PATH"
```

Add the PATH configuration to your shell configuration file if it is not already present.

Verify the installation:

```bash
bookmark-cli
```

### Install Dependencies

**Fedora Linux**

```bash
sudo dnf install fzf gum
```

**Arch Linux**

```bash
sudo pacman -S fzf gum
```

For other distributions:

- [fzf Installation](https://github.com/junegunn/fzf#installation)
- [Gum Installation](https://github.com/charmbracelet/gum#installation)

### Option 2: Build from Source

For developers who want to modify the application.

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), clone the repository, and build:

```bash
git clone <repository-url>
cd BookmarkCli

dotnet restore
dotnet build -c Release
```

Run the application:

```bash
dotnet run
```

## Usage

### Add Bookmarks

Add a bookmark manually:

```bash
bookmark-cli add "GitHub" "https://github.com"
```

Add a bookmark to a specific folder:

```bash
bookmark-cli add "Microsoft" "https://microsoft.com" --folder "Development"
```

Use interactive prompts:

```bash
bookmark-cli add
```

### List Bookmarks

```bash
bookmark-cli list
```

### Folder Management

Create and manage folders using the folder commands.

Interactive folder creation is also supported.

Refer to the application's command usage for the available folder operations.

## Data Storage

BookmarkCli stores bookmarks locally in JSON format.

**Default storage location (Linux):**

```text
~/.local/share/bookmark-cli/bookmarks.json
```

The application automatically creates the required directory and JSON file when needed.

Your bookmarks are stored locally. No database or internet connection is required for bookmark storage.

## Updating

To update BookmarkCli:

1. Visit the [GitHub Releases](../../releases) page.
2. Download the latest version.
3. Replace the existing executable.

Your existing bookmarks are preserved because they are stored separately from the application.

**Important:** Do not delete the application data directory when updating.

## Building and Publishing

BookmarkCli is distributed as a self-contained executable, meaning users do not need to install the .NET runtime.

Developers can generate a distributable Linux x64 executable using:

```bash
dotnet publish -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

For ARM64 Linux:

```bash
dotnet publish -c Release \
  -r linux-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The published files are generated under:

```text
bin/Release/net10.0/linux-x64/publish/
```

External dependencies such as fzf and Gum must still be installed separately.

## Project Structure

```text
BookmarkCli/
├── Commands/
├── Models/
├── Services/
├── Program.cs
└── BookmarkCli.csproj
```

## Development

Contributions and improvements are welcome.

To get started:

1. Fork the repository.
2. Clone your fork.
3. Install the development dependencies.
4. Make your changes.
5. Build and test the application.
6. Submit a pull request.

## License

See the [LICENSE](LICENSE) file for licensing information.
