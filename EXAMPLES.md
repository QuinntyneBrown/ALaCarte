# ALaCarte Usage Examples

This document provides practical examples of how to use the ALaCarte CLI tool with various git providers.

## Git Provider Support

ALaCarte uses git command line tools and supports multiple git providers:
- **GitHub** (HTTPS and SSH)
- **GitLab** (HTTPS and SSH, including nested groups)
- **Self-hosted Git servers** (any URL format)
- **Multiple owners/organizations** (works with complex repository structures)

## Example 1: Creating a Solution from Public GitHub Repositories (HTTPS)

Suppose you have two .NET library repositories on GitHub that you want to combine into a single solution:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/library-a.git https://github.com/your-org/library-b.git \
  --branch main \
  --folder my-combined-solution
```

This will:
1. Create a folder called `my-combined-solution`
2. Initialize it as a git repository
3. Add both repositories as submodules (using the `main` branch)
4. Discover all .NET projects in both repositories
5. Create a unified solution in `my-combined-solution/Solution.sln`
6. Copy projects to `my-combined-solution/src/`
7. Resolve dependencies between projects

## Example 1a: Using SSH URLs with GitHub

You can also use SSH URLs if you have SSH keys configured:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos git@github.com:your-org/library-a.git git@github.com:your-org/library-b.git \
  --branch main \
  --folder my-combined-solution
```

## Example 1b: Using GitLab Repositories

ALaCarte works seamlessly with GitLab repositories, including those with nested groups:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://gitlab.com/group/subgroup/project-a.git https://gitlab.com/group/project-b.git \
  --branch main \
  --folder gitlab-solution
```

## Example 1c: Using Self-Hosted Git Repositories

For self-hosted git servers (like GitHub Enterprise, GitLab self-hosted, or Gitea):

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://git.company.com/team/backend.git https://git.company.com/team/frontend.git \
  --branch develop \
  --folder company-solution
```

Or with SSH:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos git@git.company.com:team/backend.git git@git.company.com:team/frontend.git \
  --branch develop \
  --folder company-solution
```

## Example 2: Working with Multiple Branches

If your repositories use different branch names:

```bash
# For the first iteration, use the same branch for all repos
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/repo1.git https://github.com/your-org/repo2.git \
  --branch develop \
  --folder dev-solution
```

Note: Currently, all repositories must use the same branch name.

## Example 3: Angular Projects Integration

If your repositories contain Angular projects:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/angular-app.git https://github.com/your-org/angular-lib.git \
  --branch main \
  --folder angular-workspace
```

This will:
1. Create the folder structure
2. Detect Angular projects by finding `angular.json` files
3. Create a new Angular workspace at `angular-workspace/angular-workspace/`
4. Copy and integrate the Angular projects into the workspace

## Example 4: Mixed .NET and Angular Projects

When repositories contain both .NET and Angular projects:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/backend-api.git https://github.com/your-org/frontend-app.git \
  --branch main \
  --folder full-stack-solution
```

This will create:
- `full-stack-solution/Solution.sln` - .NET solution
- `full-stack-solution/src/` - .NET projects
- `full-stack-solution/angular-workspace/` - Angular workspace

## Example 5: Auto-generated Folder Name

If you don't specify a folder name, one will be automatically generated:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/repo1.git https://github.com/your-org/repo2.git \
  --branch main
```

This will create a folder with a timestamp, like `alacarte-20260202-141530`.

## Example 6: Dependency Resolution

Consider two repositories:
- `LibraryA` (Package ID: "MyCompany.LibraryA")
- `LibraryB` which depends on the NuGet package "MyCompany.LibraryA"

When you run:

```bash
dotnet run --project src/ALaCarte.Cli/ALaCarte.Cli.csproj -- init \
  --repos https://github.com/your-org/library-a.git https://github.com/your-org/library-b.git \
  --branch main \
  --folder integrated-solution
```

The tool will:
1. Detect that `LibraryB` has a PackageReference to "MyCompany.LibraryA"
2. Detect that `LibraryA` has PackageId "MyCompany.LibraryA"
3. Replace the PackageReference in `LibraryB` with a ProjectReference to `LibraryA`

This allows the projects to work together seamlessly in the same solution without needing to publish NuGet packages.

## Verifying the Result

After running the tool, you can verify the solution:

```bash
cd my-combined-solution

# Check the git repository
git status

# Check the submodules
git submodule status

# Build the .NET solution
dotnet build Solution.sln

# If Angular workspace was created
cd angular-workspace
npm install
ng build
```

## Troubleshooting

### Git Submodule Issues

If you encounter errors adding submodules, ensure:
- The repository URLs are correct and accessible
- The specified branch exists in all repositories
- You have appropriate permissions to clone the repositories

### .NET Build Issues

If projects don't build after integration:
- Check that all necessary NuGet packages are referenced
- Verify project references were correctly updated
- Look for any remaining relative path references

### Angular Workspace Issues

If Angular workspace creation fails:
- Ensure Angular CLI is installed: `npm install -g @angular/cli`
- Check that the source repositories have valid `angular.json` files
- Verify node_modules are properly installed after workspace creation

## Tips

1. **Test with small repositories first**: Before combining large projects, test with smaller repositories to ensure the tool works as expected.

2. **Review generated solution**: After creation, review the solution structure and project references to ensure they match your expectations.

3. **Commit early and often**: After generating the solution, make an initial commit before making any manual changes.

4. **Use version control**: Always work with repositories under version control to track changes and facilitate collaboration.
