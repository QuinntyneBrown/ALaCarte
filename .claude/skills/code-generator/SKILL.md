---
name: code-generator
description: Generate code using CodeGenerator.DotNet and CodeGenerator.Angular
user-invocable: true
---

# CodeGenerator Skill

Use this skill when asked to generate .NET solutions, projects, classes, entities, or Angular workspaces using the CodeGenerator framework.

## Package References

Add these NuGet packages to your project:

```xml
<PackageReference Include="QuinntyneBrown.CodeGenerator.Core" Version="1.0.0" />
<PackageReference Include="QuinntyneBrown.CodeGenerator.DotNet" Version="1.0.0" />
<PackageReference Include="QuinntyneBrown.CodeGenerator.Angular" Version="1.0.0" />
```

## Service Registration

Register services in your DI container:

```csharp
using CodeGenerator.Core;
using Microsoft.Extensions.DependencyInjection;

services.AddCoreServices(typeof(Program).Assembly);  // Core + artifact/syntax generation strategies
services.AddDotNetServices();                         // .NET factories, template processor, syntax services
services.AddAngularServices();                        // Angular file factory and generation strategies
```

## Core Abstractions

### IArtifactGenerator

Generates files from model objects. Resolve from DI and call `GenerateAsync`:

```csharp
using CodeGenerator.Core.Artifacts.Abstractions;

var artifactGenerator = serviceProvider.GetRequiredService<IArtifactGenerator>();
await artifactGenerator.GenerateAsync(model);  // model can be any file/project/solution model
```

### ISyntaxGenerator

Generates source code strings from syntax models:

```csharp
using CodeGenerator.Core.Syntax;

var syntaxGenerator = serviceProvider.GetRequiredService<ISyntaxGenerator>();
string code = await syntaxGenerator.GenerateAsync(classModel);
```

### ICommandService

Runs shell commands (e.g., `dotnet new sln`):

```csharp
using CodeGenerator.Core.Services;

var commandService = serviceProvider.GetRequiredService<ICommandService>();
commandService.Start("dotnet new sln -n MySolution", workingDirectory);
```

### ITemplateProcessor

Processes Liquid templates with token dictionaries:

```csharp
using CodeGenerator.Core.Services;

var templateProcessor = serviceProvider.GetRequiredService<ITemplateProcessor>();
string result = templateProcessor.Process(template, new Dictionary<string, object>
{
    { "name", "MyClass" },
    { "namespace", "MyApp.Models" }
});
// Also supports async: await templateProcessor.ProcessAsync(template, tokens)
// And dynamic models: templateProcessor.Process(template, new { name = "MyClass" })
```

## DotNet Models

### SolutionModel

Top-level model representing a .NET solution:

```csharp
using CodeGenerator.DotNet.Artifacts.Solutions;

var solution = new SolutionModel(name: "MySolution", directory: outputDirectory);
// Key properties:
//   solution.Name              -> "MySolution"
//   solution.SolutionDirectory -> "{outputDirectory}/MySolution"
//   solution.SrcDirectory      -> "{outputDirectory}/MySolution/src"
//   solution.TestDirectory     -> "{outputDirectory}/MySolution/tests"
//   solution.Projects          -> List<ProjectModel>
//   solution.DependOns          -> List<DependsOnModel>
```

### ProjectModel

Represents a .NET project inside a solution:

```csharp
using CodeGenerator.DotNet.Artifacts.Projects;
using CodeGenerator.DotNet.Artifacts.Projects.Enums;

var project = new ProjectModel(DotNetProjectType.WebApi, "MyApp.Api", solution.SrcDirectory);
// Key properties:
//   project.Name             -> "MyApp.Api"
//   project.Directory        -> "{srcDir}/MyApp.Api"
//   project.DotNetProjectType -> DotNetProjectType.WebApi
//   project.TargetFramework  -> "net9.0" (default; set to "net8.0" etc. to change)
//   project.Files            -> List<FileModel>
//   project.Packages         -> List<PackageModel>
//   project.References       -> List<string>
//   project.Namespace        -> "MyApp.Api" (defaults to Name)
//   project.Path             -> "{Directory}/{Name}.csproj"
```

### DotNetProjectType Enum

```csharp
using CodeGenerator.DotNet.Artifacts.Projects.Enums;

// Available values:
// Console, NUnit, XUnit, ClassLib, WebApi, MinimalWebApi, Web, Worker, Angular, TypeScriptStandalone
```

### ContentFileModel

Simple text-based file — use for raw content files:

```csharp
using CodeGenerator.DotNet.Artifacts.Files;

var file = new ContentFileModel(
    content: "file contents here",
    name: "MyFile",
    directory: outputDirectory,
    extension: ".cs"
);
await artifactGenerator.GenerateAsync(file);
// Writes to: {outputDirectory}/MyFile.cs
```

### CodeFileModel<T>

Typed file model for syntax-based code generation:

```csharp
using CodeGenerator.DotNet.Artifacts.Files;

var file = new CodeFileModel<ClassModel>(classModel, "MyClass", directory, ".cs");
// Contains: Usings, Namespace, syntax object of type T
await artifactGenerator.GenerateAsync(file);
```

### ClassModel

Represents a C# class:

```csharp
using CodeGenerator.DotNet.Syntax.Classes;

var classModel = new ClassModel("MyService")
{
    AccessModifier = AccessModifier.Public,
    BaseClass = "BaseService",
    Static = false,
    Sealed = false
};
// Add members:
classModel.Properties.Add(new PropertyModel { Name = "Name", Type = new TypeModel("string") });
classModel.Fields.Add(new FieldModel { Name = "_logger", Type = new TypeModel("ILogger"), ReadOnly = true });
classModel.Constructors.Add(new ConstructorModel { Name = "MyService" });
classModel.AddMethod();  // Adds a method with Interface = false
classModel.CreateDto();  // Creates a DTO version of this class
```

### EntityModel

Extends ClassModel for domain entities:

```csharp
using CodeGenerator.DotNet.Syntax.Entities;

var entity = new EntityModel("Order")
{
    AggregateRootName = "OrderAggregate"
};
entity.Properties.Add(new PropertyModel { Name = "Total", Type = new TypeModel("decimal") });
entity.CreateDto();  // Creates a DTO from this entity
```

### InterfaceModel

Represents a C# interface:

```csharp
using CodeGenerator.DotNet.Syntax.Interfaces;

var iface = new InterfaceModel("IOrderService");
iface.Implements.Add(new TypeModel("IDisposable"));
iface.AddMethod();  // Adds a method with Interface = true
```

### MethodModel

Represents a method signature and body:

```csharp
using CodeGenerator.DotNet.Syntax.Methods;

var method = new MethodModel
{
    Name = "GetOrderAsync",
    ReturnType = new TypeModel("Task<Order>"),
    AccessModifier = AccessModifier.Public,
    Async = true,
    Static = false,
    Override = false,
    Interface = false
};
method.Params.Add(new ParamModel { Name = "id", Type = new TypeModel("Guid") });
```

### PropertyModel

Represents a C# property:

```csharp
using CodeGenerator.DotNet.Syntax.Properties;

var prop = new PropertyModel
{
    Name = "OrderId",
    Type = new TypeModel("Guid"),
    AccessModifier = AccessModifier.Public,
    Required = false,
    Id = false
};
```

### FieldModel

Represents a C# field:

```csharp
using CodeGenerator.DotNet.Syntax.Fields;

var field = new FieldModel
{
    Name = "_logger",
    Type = new TypeModel("ILogger<MyService>"),
    ReadOnly = true,
    Static = false,
    AccessModifier = AccessModifier.Private
};
// Static helpers:
var mediatorField = FieldModel.Mediator;                 // IMediator _mediator
var loggerField = FieldModel.LoggerOf("MyService");   // ILogger<MyService> _logger
```

### ConstructorModel

Represents a constructor:

```csharp
using CodeGenerator.DotNet.Syntax.Constructors;

var ctor = new ConstructorModel
{
    Name = "MyService",
    AccessModifier = AccessModifier.Public
};
ctor.Params.Add(new ParamModel { Name = "logger", Type = new TypeModel("ILogger<MyService>") });
ctor.BaseParams.Add("logger");  // Passes to base constructor
```

### TypeModel

Represents a type reference:

```csharp
using CodeGenerator.DotNet.Syntax.Types;

var type = new TypeModel("string");
var genericType = new TypeModel("List<Order>");
var taskType = new TypeModel("Task<IActionResult>");
```

## DotNet Factories

Resolve from DI to create pre-configured models:

### ISolutionFactory

```csharp
using CodeGenerator.DotNet.Artifacts.Solutions.Factories;

var solutionFactory = serviceProvider.GetRequiredService<ISolutionFactory>();
var solution = await solutionFactory.Create("MySolution");
```

### IProjectFactory

```csharp
using CodeGenerator.DotNet.Artifacts.Projects.Factories;

var projectFactory = serviceProvider.GetRequiredService<IProjectFactory>();
var apiProject = await projectFactory.CreateWebApi("MyApp.Api", srcDirectory);
var libProject = await projectFactory.CreateLibrary("MyApp.Core", srcDirectory);
var coreProject = await projectFactory.CreateCore("MyApp.Core", srcDirectory);
var infraProject = await projectFactory.CreateInfrastructure("MyApp.Infrastructure", srcDirectory);
```

### IClassFactory

```csharp
using CodeGenerator.DotNet.Syntax.Classes.Factories;

var classFactory = serviceProvider.GetRequiredService<IClassFactory>();
var controller = classFactory.CreateController(entityModel, directory);
var dbContext = classFactory.CreateDbContext("AppDbContext", entities, directory);
```

### ICqrsFactory

```csharp
using CodeGenerator.DotNet.Syntax.Units.Factories;

var cqrsFactory = serviceProvider.GetRequiredService<ICqrsFactory>();
var query = await cqrsFactory.CreateQueryAsync("GetById", "Order", "Guid OrderId");
var command = await cqrsFactory.CreateCommandAsync("CreateOrder", "string Name, decimal Total");
```

## Angular Models

### WorkspaceModel

Represents an Angular workspace (monorepo):

```csharp
using CodeGenerator.Angular.Artifacts;

var workspace = new WorkspaceModel("my-workspace", "18.0.0", rootDirectory);
// Key properties:
//   workspace.Name          -> "my-workspace"
//   workspace.Version       -> "18.0.0"
//   workspace.Directory     -> "{rootDirectory}/my-workspace"
//   workspace.Projects      -> List<ProjectModel>
```

### ProjectModel (Angular)

Represents an Angular project within a workspace:

```csharp
using CodeGenerator.Angular.Artifacts;

var project = new ProjectModel("my-app", "app", rootDirectory);
// Key properties:
//   project.Name            -> "my-app"
//   project.Prefix          -> "app"
//   project.ProjectType     -> "application"
//   project.Directory       -> computed from root + name
// Supports scoped packages: new ProjectModel("@scope/my-lib", "lib", rootDirectory)
```

### FunctionModel

Represents a TypeScript/Angular function:

```csharp
using CodeGenerator.Angular.Syntax;

var func = new FunctionModel
{
    Name = "fetchOrders",
    Body = "return this.http.get<Order[]>('/api/orders');"
};
func.Imports.Add(new ImportModel("HttpClient", "@angular/common/http"));
```

### ImportModel

Represents a TypeScript import statement:

```csharp
using CodeGenerator.Angular.Syntax;

var import = new ImportModel("Component", "@angular/core");
// import.Types  -> List<TypeModel> containing "Component"
// import.Module -> "@angular/core"
```

## Generation Pattern

The typical workflow is:

1. **Create models** — build a hierarchy of solution/project/file/syntax models
2. **Generate** — pass models to `IArtifactGenerator.GenerateAsync(model)`

```csharp
// Example: Generate a complete solution
var solution = new SolutionModel("MyApp", outputDir);
var project = new ProjectModel(DotNetProjectType.WebApi, "MyApp.Api", solution.SrcDirectory)
{
    TargetFramework = "net8.0"  // defaults to "net9.0" if omitted
};
project.Files.Add(new ContentFileModel(content, "Program", project.Directory, ".cs"));
solution.Projects.Add(project);

// Create directories
Directory.CreateDirectory(solution.SolutionDirectory);
Directory.CreateDirectory(solution.SrcDirectory);
Directory.CreateDirectory(project.Directory);

// Generate files
commandService.Start($"dotnet new sln -n {solution.Name}", solution.SolutionDirectory);
await artifactGenerator.GenerateAsync(project);
foreach (var file in project.Files)
{
    await artifactGenerator.GenerateAsync(file);
}
commandService.Start($"dotnet sln add {project.Path}", solution.SolutionDirectory);
```
