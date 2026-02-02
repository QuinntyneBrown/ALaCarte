# ALaCarte Roadmap - Full Featured Implementation

## Vision

ALaCarte enables teams to maintain separate repositories for CI/CD purposes while working locally as if everything were in a monorepo. Developers can selectively pull specific projects (Angular, .NET, or other) from multiple repos, transform and reorganize them into a cohesive local workspace, make changes across projects, and easily push changes back to their respective source repositories.

## Current State (v0.1)

### Implemented Features
- ✅ Initialize git repository with submodules from multiple repos
- ✅ Discover .NET projects (`.csproj` files) across submodules
- ✅ Discover Angular projects (`angular.json` files) across submodules
- ✅ Create unified .NET solution with project reference resolution
- ✅ Create unified Angular workspace
- ✅ Copy projects to local structure (`src/` for .NET)
- ✅ Replace NuGet references with project references when both projects are present
- ✅ Strip relative file references to ensure clean builds

### Current Limitations
- Only supports adding entire repositories (no selective project picking)
- All repos must use the same branch
- No transformation/reorganization of split Angular libraries
- No bidirectional sync (changes stay local)
- No push-back mechanism to source repositories
- Limited configuration options
- No support for project filters or exclusions

---

## Roadmap to Full Implementation

### Phase 1: Enhanced Project Selection and Filtering
**Goal**: Enable fine-grained control over which projects to include from each repository.

#### 1.1 Project-Level Selection (High Priority)
- **Feature**: Allow users to specify specific projects to include from each repo
- **Implementation**:
  - Add `--include-projects` CLI option accepting project name patterns
  - Add `--exclude-projects` CLI option for exclusion patterns
  - Support wildcards (e.g., `*.Api`, `MyCompany.*`)
  - Create configuration file support (`.alacarterc` or `alacarte.json`)
- **Example**:
  ```bash
  alacarte init \
    --repo https://github.com/org/backend.git --include-projects "MyApi" "MyCore" \
    --repo https://github.com/org/frontend.git --include-projects "angular:my-app" \
    --folder my-workspace
  ```

#### 1.2 Per-Repository Branch Support (High Priority)
- **Feature**: Allow different branches for different repositories
- **Implementation**:
  - Extend CLI to accept branch per repository
  - Format: `--repo <url>:<branch>` or separate `--branches` option
- **Example**:
  ```bash
  alacarte init \
    --repo https://github.com/org/repo1.git:main \
    --repo https://github.com/org/repo2.git:develop \
    --folder my-workspace
  ```

#### 1.3 Configuration File Support (Medium Priority)
- **Feature**: Define project selection in a configuration file
- **Implementation**:
  - Support YAML or JSON configuration format
  - Schema validation for configuration
  - Allow overrides via CLI options
- **Example** (`alacarte.config.json`):
  ```json
  {
    "repositories": [
      {
        "url": "https://github.com/org/backend.git",
        "branch": "main",
        "projects": {
          "include": ["*.Api", "*.Core"],
          "exclude": ["*.Tests"]
        }
      },
      {
        "url": "https://github.com/org/frontend.git",
        "branch": "develop",
        "projects": {
          "include": ["angular:my-app", "angular:shared-lib"]
        }
      }
    ],
    "workspace": {
      "name": "my-workspace",
      "structure": "standard"
    }
  }
  ```
  ```bash
  alacarte init --config alacarte.config.json
  ```

#### 1.4 Project Type Filters (Medium Priority)
- **Feature**: Filter by project type (Angular app, Angular lib, .NET API, .NET lib)
- **Implementation**:
  - Detect project types automatically
  - Add `--types` filter (e.g., `--types angular-app dotnet-lib`)
  - Exclude test projects by default with `--include-tests` to opt-in

---

### Phase 2: Advanced Angular Library Transformation
**Goal**: Support reorganization and transformation of split Angular libraries.

#### 2.1 Split Library Detection (High Priority)
- **Feature**: Detect Angular libraries split across multiple folders/repos
- **Implementation**:
  - Scan for library markers (package.json, public-api.ts, ng-package.json)
  - Identify related library parts by naming conventions or metadata
  - Create library mapping configuration

#### 2.2 Library Assembly/Transformation (High Priority)
- **Feature**: Transform split libraries into complete, cohesive libraries
- **Implementation**:
  - Define transformation rules (merge strategies)
  - Support custom transformation scripts/hooks
  - Combine source files, tests, and assets
  - Generate proper Angular library structure with:
    - Consolidated public-api.ts
    - Merged package.json dependencies
    - Combined ng-package.json configuration
- **Configuration Example**:
  ```json
  {
    "transformations": [
      {
        "type": "angular-library-merge",
        "name": "my-shared-lib",
        "sources": [
          "repo1/libs/shared/core",
          "repo2/libs/shared/utils"
        ],
        "output": "projects/shared-lib",
        "merge": {
          "strategy": "deep",
          "publicApi": "combine",
          "dependencies": "union"
        }
      }
    ]
  }
  ```

#### 2.3 Custom Transformation Pipeline (Medium Priority)
- **Feature**: Support custom transformations for any project type
- **Implementation**:
  - Plugin system for transformation steps
  - Pre/post transformation hooks
  - Support for custom scripts (Node.js, PowerShell, bash)
- **Example**:
  ```json
  {
    "transformations": [
      {
        "name": "prepare-library",
        "steps": [
          {
            "type": "script",
            "command": "node scripts/transform-library.js"
          },
          {
            "type": "file-copy",
            "from": "temp/output",
            "to": "projects/my-lib"
          }
        ]
      }
    ]
  }
  ```

#### 2.4 Dependency Resolution for Transformed Projects (Medium Priority)
- **Feature**: Update references after transformation
- **Implementation**:
  - Update import statements in TypeScript files
  - Update angular.json path mappings
  - Update package.json dependencies
  - Regenerate tsconfig.json paths

---

### Phase 3: Bidirectional Sync and Push-back
**Goal**: Enable seamless synchronization between local workspace and source repositories.

#### 3.1 Change Tracking (High Priority)
- **Feature**: Track which files belong to which source repository
- **Implementation**:
  - Maintain metadata mapping files to source repos
  - Store in `.alacarte/` directory (not committed to workspace repo)
  - Track file provenance through git history
- **Metadata Structure**:
  ```json
  {
    "fileMapping": {
      "src/MyApi/Controller.cs": {
        "sourceRepo": "https://github.com/org/backend.git",
        "sourcePath": "src/MyApi/Controller.cs",
        "branch": "main",
        "lastSync": "2026-02-02T10:30:00Z"
      }
    }
  }
  ```

#### 3.2 Push Command (High Priority)
- **Feature**: Push local changes back to source repositories
- **Implementation**:
  - Analyze changed files and group by source repository
  - Create commits in source repository submodules
  - Push changes to remote branches
  - Support creating pull requests automatically (via GitHub API)
- **Example**:
  ```bash
  # Push all changes back
  alacarte push
  
  # Push specific projects
  alacarte push --projects MyApi MyCore
  
  # Push with PR creation
  alacarte push --create-pr --pr-title "Fix: Updated API endpoints"
  
  # Push to specific branch
  alacarte push --target-branch feature/my-changes
  ```

#### 3.3 Sync Command (High Priority)
- **Feature**: Pull latest changes from source repositories
- **Implementation**:
  - Update submodules to latest commits
  - Re-run discovery and integration
  - Detect and handle local changes (merge or warn)
  - Support selective sync (specific repos or projects)
- **Example**:
  ```bash
  # Sync all repositories
  alacarte sync
  
  # Sync specific repository
  alacarte sync --repo backend
  
  # Sync with automatic merge
  alacarte sync --auto-merge
  ```

#### 3.4 Conflict Resolution (Medium Priority)
- **Feature**: Handle conflicts between local changes and upstream updates
- **Implementation**:
  - Detect conflicts during sync
  - Interactive conflict resolution UI
  - Support for merge strategies (ours, theirs, manual)
  - Create backup before sync operations
  - Integration with git merge tools

#### 3.5 Transformation Reverse Mapping (Medium Priority)
- **Feature**: Map transformed files back to their original locations
- **Implementation**:
  - For split libraries, determine which changes go to which source
  - Support manual mapping overrides
  - Validate that reverse transformation is possible
- **Challenge**: This is the most complex feature as it requires intelligent splitting of changes back to multiple source locations.

---

### Phase 4: Workspace Management
**Goal**: Provide tools for managing the local workspace lifecycle.

#### 4.1 Status Command (High Priority)
- **Feature**: Show workspace status and sync state
- **Implementation**:
  - Show which repositories are included
  - Display last sync time for each repo
  - Show uncommitted local changes
  - Show available upstream changes
- **Example**:
  ```bash
  alacarte status
  
  # Output:
  # Workspace: my-workspace
  # 
  # Repositories:
  #   ✓ backend (main) - synced 2 hours ago - 3 local changes
  #   ⚠ frontend (develop) - synced 5 days ago - upstream changes available
  # 
  # Projects:
  #   .NET: 5 projects
  #   Angular: 3 projects (1 transformed)
  ```

#### 4.2 Update Command (Medium Priority)
- **Feature**: Update workspace configuration without recreating
- **Implementation**:
  - Add new repositories
  - Remove repositories (with safety checks)
  - Change project selections
  - Update transformations
- **Example**:
  ```bash
  # Add a new repository
  alacarte add-repo https://github.com/org/new-repo.git
  
  # Remove a repository
  alacarte remove-repo backend --preserve-local-changes
  
  # Update project selection
  alacarte update-projects --repo backend --include "*.Api"
  ```

#### 4.3 Clean Command (Medium Priority)
- **Feature**: Clean and reset workspace
- **Implementation**:
  - Remove generated files
  - Reset to last sync state
  - Clear transformation cache
- **Example**:
  ```bash
  # Clean all generated files
  alacarte clean
  
  # Reset to last sync (discard local changes)
  alacarte reset --hard
  ```

#### 4.4 Validate Command (Medium Priority)
- **Feature**: Validate workspace integrity
- **Implementation**:
  - Check that all submodules are accessible
  - Verify all project references are valid
  - Check for broken transformations
  - Validate configuration file

---

### Phase 5: Advanced Features and Developer Experience
**Goal**: Enhance usability and support advanced workflows.

#### 5.1 Watch Mode (Medium Priority)
- **Feature**: Automatically sync changes in real-time
- **Implementation**:
  - File system watcher for local changes
  - Optional automatic commit and push
  - Configurable debouncing
- **Example**:
  ```bash
  alacarte watch --auto-push
  ```

#### 5.2 Templates and Presets (Medium Priority)
- **Feature**: Save and reuse workspace configurations
- **Implementation**:
  - Export current workspace as template
  - Library of common templates
  - Template variables for customization
- **Example**:
  ```bash
  # Save current config as template
  alacarte export-template my-template
  
  # Create workspace from template
  alacarte init --template my-template --var project-name=NewProject
  ```

#### 5.3 Multi-Workspace Support (Low Priority)
- **Feature**: Manage multiple workspaces simultaneously
- **Implementation**:
  - Workspace registry
  - Quick switching between workspaces
  - Shared configuration across workspaces

#### 5.4 IDE Integration (Low Priority)
- **Feature**: VS Code extension and Visual Studio integration
- **Implementation**:
  - Status bar integration
  - Command palette commands
  - Inline sync indicators
  - Push/pull from editor UI

#### 5.5 CI/CD Integration (Low Priority)
- **Feature**: Support for automated workspace creation in CI/CD
- **Implementation**:
  - Headless mode for CI/CD pipelines
  - Docker image with ALaCarte pre-installed
  - GitHub Actions integration
  - Azure DevOps tasks

#### 5.6 Analytics and Reporting (Low Priority)
- **Feature**: Insights into workspace usage
- **Implementation**:
  - Track sync frequency
  - Report on change velocity
  - Identify frequently modified projects
  - Export reports for team insights

---

## Implementation Priorities

### Short Term (3-6 months)
1. **Phase 1.1**: Project-level selection - Essential for usability
2. **Phase 1.2**: Per-repository branch support - Critical for real-world usage
3. **Phase 3.1**: Change tracking - Foundation for push-back
4. **Phase 3.2**: Push command - Core feature for two-way sync
5. **Phase 4.1**: Status command - Visibility into workspace state

### Medium Term (6-12 months)
1. **Phase 2.1-2.2**: Angular library transformation - Key differentiator
2. **Phase 3.3**: Sync command - Complete the bidirectional workflow
3. **Phase 1.3**: Configuration file support - Better developer experience
4. **Phase 4.2**: Update command - Workspace lifecycle management
5. **Phase 3.4**: Conflict resolution - Handle real-world scenarios

### Long Term (12+ months)
1. **Phase 2.3-2.4**: Custom transformation pipeline - Maximum flexibility
2. **Phase 3.5**: Transformation reverse mapping - Complete transformation support
3. **Phase 5.1**: Watch mode - Enhanced developer experience
4. **Phase 5.2**: Templates - Ease of adoption
5. **Phase 5.4**: IDE integration - Seamless workflow

---

## Technical Architecture Considerations

### Change Tracking System
- Use SQLite database or JSON files in `.alacarte/` directory
- Store file provenance, transformation metadata, sync state
- Git-ignored to avoid conflicts

### Transformation Engine
- Plugin-based architecture for extensibility
- Support for JavaScript/TypeScript plugins
- Transformation validation before and after

### Sync Engine
- Git-based change detection
- Intelligent merge strategies
- Rollback support for failed operations

### CLI Architecture
- Command pattern for extensibility
- Dependency injection for testing
- Progress reporting with rich CLI output
- Async operations with cancellation support

### Configuration Management
- JSON Schema validation
- Version migration support
- Sensible defaults with overrides

---

## Success Metrics

1. **Adoption**: Number of teams using ALaCarte in production
2. **Efficiency**: Time saved vs. manual repository management
3. **Reliability**: Success rate of push/sync operations
4. **Coverage**: Percentage of projects successfully integrated
5. **Transformation Success**: Accuracy of library transformations

---

## Known Challenges and Risks

### Technical Challenges
1. **Complex Transformations**: Reverse mapping transformed libraries back to source
2. **Conflict Resolution**: Handling divergent changes in multiple repos
3. **Performance**: Managing large repositories and workspaces
4. **Cross-Platform**: Ensuring consistent behavior across Windows, macOS, Linux

### Process Challenges
1. **Learning Curve**: Users need to understand the tool's concepts
2. **Git Complexity**: Users must understand git submodules
3. **Team Coordination**: Multiple developers working in same workspace
4. **CI/CD Integration**: Adapting existing pipelines

### Risk Mitigation
- Comprehensive documentation and examples
- Interactive tutorials and getting started guides
- Robust error handling and helpful error messages
- Extensive testing including integration tests
- Community feedback loops and beta testing program

---

## Community and Contribution

### Open Source Strategy
- Open issue for each roadmap item
- Community voting on priorities
- External contribution guidelines
- Regular roadmap updates based on feedback

### Documentation
- User guide for each feature
- Architecture decision records (ADRs)
- Video tutorials and demos
- Migration guides for version updates

### Support
- GitHub Discussions for Q&A
- Stack Overflow tag
- Discord community channel
- Regular office hours or community calls

---

## Conclusion

This roadmap outlines the path from the current basic implementation to a full-featured tool that enables true monorepo-style development across multiple repositories. The phased approach ensures that foundational features are built first, with each phase delivering tangible value to users.

The key differentiators of ALaCarte are:
1. **Selective Integration**: Choose exactly which projects you need
2. **Intelligent Transformation**: Reorganize split projects automatically
3. **Bidirectional Sync**: Seamlessly push changes back to source repos
4. **Flexibility**: Work with .NET, Angular, and extensible to other platforms

By following this roadmap, ALaCarte will become an indispensable tool for teams that need the benefits of both separate repositories (CI/CD, independence) and monorepos (unified development, easy refactoring).
