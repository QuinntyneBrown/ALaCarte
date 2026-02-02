# ALaCarte

A CLI tool to create a new solution from git repositories, integrating both .NET and Angular projects.

## Overview

ALaCarte is a command-line tool built with System.CommandLine that helps you create a new solution by combining multiple git repositories using **git command line tools**. It automatically:

- Initializes a new git repository
- Adds git repositories as submodules (supports GitHub, GitLab, and self-hosted git servers)
- Discovers and integrates .NET projects
- Discovers and integrates Angular projects/libraries
- Creates a unified .NET solution
- Creates a unified Angular workspace
- Resolves project dependencies by replacing NuGet references with project references

## Features

### Git Integration
- Creates a new folder and initializes it as a git repository using git command line tools
- Adds all provided git repositories as submodules with specified branch
- **Supports multiple git providers:**
  - GitHub (https://github.com or git@github.com)
  - GitLab (https://gitlab.com or git@gitlab.com, including nested groups)
  - Self-hosted Git servers (any URL format)
  - Both HTTPS and SSH URL formats
  - Repositories with multiple owners/organizations

### .NET Project Integration
- Discovers all .NET projects (`.csproj` files) in the submodules
- Creates a new .NET solution
- Copies projects to the `src` folder
- Strips out relative file references that may cause build issues
- Analyzes project dependencies
- Replaces NuGet package references with project references when both projects are included
  - Example: If project `Foo` depends on NuGet package `Bar`, and both repos are referenced, the NuGet reference is replaced with a direct project reference

### Angular Integration
- Discovers Angular applications and libraries (by finding `angular.json` files)
- Creates a new Angular workspace
- Copies and integrates Angular projects into the workspace

## Installation

### Prerequisites
- .NET 10.0 or later
- Git
- (Optional) Angular CLI for Angular workspace creation

### Build from Source

```bash
git clone https://github.com/QuinntyneBrown/ALaCarte.git
cd ALaCarte
dotnet build
```

## Usage

### Basic Command

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init --repos <repo-url1> <repo-url2> --branch <branch-name> --folder <folder-name>
```

### Options

- `--repos`, `-r` (Required): Git repository URLs to include (can specify multiple)
- `--branch`, `-b` (Optional): Git branch to use (default: `main`)
- `--folder`, `-f` (Optional): Folder name for the new solution (auto-generated if not specified)

### Examples

#### Create solution from multiple repositories using main branch

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/user/repo1.git https://github.com/user/repo2.git
```

#### Create solution with specific branch and folder name

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/user/repo1.git https://github.com/user/repo2.git \
  --branch develop \
  --folder my-solution
```

#### Short form with multiple repos

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  -r https://github.com/user/repo1.git https://github.com/user/repo2.git \
  -b main \
  -f my-solution
```

### Help

```bash
# General help
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- --help

# Help for init command
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init --help
```

## Output Structure

After running the command, the following structure is created:

```
<solution-folder>/
├── .git/                    # Git repository
├── submodules/              # Git submodules
│   ├── repo1/
│   └── repo2/
├── src/                     # .NET projects (if any .NET projects found)
│   ├── Project1/
│   └── Project2/
├── angular-workspace/       # Angular workspace (if any Angular projects found)
│   └── projects/
│       ├── app1/
│       └── lib1/
└── Solution.sln            # .NET solution file
```

## Development

### Running Tests

```bash
dotnet test
```

### Building

```bash
dotnet build
```

## How It Works

1. **Initialization**: Creates a new folder and initializes it as a git repository
2. **Submodule Addition**: Clones all specified repositories as git submodules
3. **Project Discovery**: Scans submodules for .NET projects (`.csproj`) and Angular projects (`angular.json`)
4. **Project Integration**:
   - For .NET: Copies projects to `src/`, strips relative references, replaces NuGet references with project references where applicable
   - For Angular: Creates a workspace and copies Angular projects/libraries
5. **Solution Creation**: Creates .NET solution and/or Angular workspace configuration

## Technical Details

### Dependency Resolution

The tool analyzes .NET projects and:
- Reads `PackageId` from project files
- Identifies NuGet `PackageReference` elements
- When a NuGet package matches a project's `PackageId`, replaces the NuGet reference with a `ProjectReference`

### File Reference Cleanup

The tool removes:
- Relative file references starting with `..`
- Relative `Compile` items
- Relative `Content` items
- Relative `None` items

This ensures projects build cleanly in their new location without external dependencies.

## License

See [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
