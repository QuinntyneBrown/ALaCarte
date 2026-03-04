# ALaCarte

A CLI tool to scaffold project workspaces and create composite solutions from multiple git repositories, integrating both .NET and Angular projects.

## Overview

ALaCarte is a command-line tool built with System.CommandLine that helps you:

- **Scaffold new project workspaces** with a build solution, console application, and Claude Code skill
- **Compose multi-repo solutions** by combining multiple git repositories using git submodules

It automatically:

- Scaffolds a workspace with a build solution and ALaCarte.Core package reference
- Installs a Claude Code skill for AI-assisted development
- Initializes a new git repository
- Adds git repositories as submodules (supports GitHub, GitLab, and self-hosted git servers)
- Discovers and integrates .NET projects
- Discovers and integrates Angular projects/libraries
- Creates a unified .NET solution
- Creates a unified Angular workspace
- Resolves project dependencies by replacing NuGet references with project references

## Installation

```bash
dotnet tool install --global QuinntyneBrown.ALaCarte.Cli
```

## Features

### Project Scaffolding
- Scaffold a new project workspace with `a-la-carte --name <project-name>`
- Creates a `build` solution with a console application project
- Automatically adds a `PackageReference` to `QuinntyneBrown.ALaCarte.Core`
- Installs a Claude Code skill (`.claude/skills/a-la-carte/SKILL.md`) in the build folder for AI-assisted development

### Skill Installation
- Install the ALaCarte Claude Code skill into any directory with `a-la-carte install --skills`
- Documents the full ALaCarte.Core API: service registration, core abstractions, options, exceptions, and command handler patterns

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

### Scaffold a New Project

```bash
a-la-carte --name MyProject
a-la-carte -n MyProject
```

This creates:
```
MyProject/
├── build/
│   ├── build.csproj          # Console app referencing ALaCarte.Core
│   └── .claude/
│       └── skills/
│           └── a-la-carte/
│               └── SKILL.md  # Claude Code skill for AI-assisted development
└── build.sln
```

### Install Claude Code Skill

```bash
a-la-carte install --skills
```

Installs the ALaCarte.Core skill into the current directory at `.claude/skills/a-la-carte/SKILL.md`.

### Compose Multi-Repo Solutions

```bash
a-la-carte init --repos <repo-url1> <repo-url2> --branch <branch-name> --folder <folder-name>
```

### Options

#### Root Command (Scaffold)
- `--name`, `-n` (Required): Name of the project workspace to scaffold

#### Init Subcommand
- `--repos`, `-r` (Required): Git repository URLs to include (can specify multiple)
- `--branch`, `-b` (Optional): Git branch to use (default: `main`)
- `--folder`, `-f` (Optional): Folder name for the new solution (auto-generated if not specified)
- `--projects`, `-p` (Optional): Filter which projects to include

#### Install Subcommand
- `--skills`, `-s`: Install the Claude Code skill

### Examples

#### Scaffold a new workspace

```bash
a-la-carte --name MyApp
```

#### Create solution from multiple repositories

```bash
a-la-carte init \
  --repos https://github.com/user/repo1.git https://github.com/user/repo2.git
```

#### Create solution with specific branch and folder name

```bash
a-la-carte init \
  --repos https://github.com/user/repo1.git https://github.com/user/repo2.git \
  --branch develop \
  --folder my-solution
```

### Help

```bash
a-la-carte --help
a-la-carte init --help
a-la-carte install --help
```

## Output Structure

### Scaffold (`--name`)

```
<project-name>/
├── build/
│   ├── build.csproj          # Console app with ALaCarte.Core reference
│   └── .claude/skills/a-la-carte/SKILL.md
└── build.sln
```

### Init (multi-repo composition)

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

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the detailed roadmap to full-featured implementation, including:
- Enhanced project selection and filtering
- Advanced Angular library transformation
- Bidirectional sync and push-back to source repositories
- Workspace management commands
- IDE integration and advanced features

## License

See [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
