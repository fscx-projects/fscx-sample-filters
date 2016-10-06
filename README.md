# Expandable F# compiler (fscx) sample filters

![fscx-projects](https://raw.githubusercontent.com/fscx-projects/fscx/master/docs/files/img/fscx_128.png)

[Expandable F# compiler (fscx)](https://github.com/fscx-projects/fscx/) is an alternative F# compiler which enables to replace F#'s AST at compile time.
This repository contains sample filter source codes.

* TODO: This project is still work in progress, and need more documents.

## Status

| NuGet (functional) | [![NuGet fscx-sample-functional-filter](https://img.shields.io/nuget/v/fscx-sample-functional-filter.svg?style=flat)](https://www.nuget.org/packages/fscx-sample-functional-filter) |
|:----|:----:|
| NuGet (inheritable) | [![NuGet fscx-sample-inheritable-filter](https://img.shields.io/nuget/v/fscx-sample-inheritable-filter.svg?style=flat)](https://www.nuget.org/packages/fscx-sample-inheritable-filter) |
| Issue status | [![Issue Stats](http://issuestats.com/github/fscx-projects/fscx-sample-filters/badge/issue)](http://issuestats.com/github/fscx-projects/fscx-sample-filters) |
| Pull req | [![PR Stats](http://issuestats.com/github/fscx-projects/fscx-sample-filters/badge/pr)](http://issuestats.com/github/fscx-projects/fscx-sample-filters) |

## What's "filter" ?

* "fscx filter" is attaching for F# build process by fscx.
* This repository contains "sample_functional_filter" and "sample_inheritable_filter" projects, both equals applying results for insert code "function entry-exit logging out (System.Console.WriteLine)" AUTOMATED. Thats mean AOP (aspect oriented paradigm).

## Try to use sample filter:

* Add NuGet package "fscx-sample-functional-filter" or "fscx-sample-inheritable-filter" for your F# project.
* Build, and try execute your code or disassemble (by ILSpy and like).

## Guide for sample filter projects:

* "sample_functional_filter" project are contains how to compose F#'s AST for using "Functional visitor pattern".
* "sample_inheritable_filter" project are contains how to compose F#'s AST for using "Traditional tree visitor pattern".
* "fscx-enabled" project are containing sample target codes with appling for "fscx-sample-functional-filter" NuGet package.
* "fscx-enabled-main" is main executable project referencing for "fscx-enabled".
* "no-fscx-enabled*" are different for not applied filter package.

## Guide for filter background

* TODO: Need more informations...

### Functional visitor implementation:

* "Functional filter" is using for functional visitor patterns with F#'s AST ([FSharp.Compiler.Services](http://fsharp.github.io/FSharp.Compiler.Service/) untyped AST).
  * If you are implemented visitor functions, try declare class with inherit from "DeclareAstFunctionalVisitor" class.
  * "DeclareAstFunctionalVisitor" are abstract classes, generic version and non-generic version. Generic argument is "Context type". Context is depending any information holds for your implementations. Implicit applied "NoContext" type for non-generic version.
  
```fsharp
// Functional visitor pattern (Not use custom context):
let outerVisitor
   (defaultVisitor: (FSharpCheckFileResults * NoContext * SynExpr -> SynExpr),
    symbolInformation: FSharpCheckFileResults,
    context: NoContext,  // (Non custom context type)
    expr: SynExpr) : SynExpr option =

  match expr with
  | SynExpr.Quote(operator, _, _, _, _) ->
    // DEBUG
    printfn "%A" operator
    None  // (None is default visiting)

  | SynExpr.App(exprAtomicFlag, isInfix, funcExpr, argExpr, range) ->
    match funcExpr with
      // ...

  | _ ->
    None  // (None is default visiting)
 
// Declare your own functional visitor.
// Type name for free.
type InsertLoggingVisitor() =
  inherit DeclareAstFunctionalVisitor(outerVisitor)
```
  
### Inheritable visitor implementation:

* "Inheritable filter" is using for traditional visitor patterns with F#'s AST.
  * If you are implemented visitor class inherit from "AstInheritableVisitor" class.
  * "AstInheritableVisitor" are abstract classes, generic version and non-generic version. Generic argument is "Context type" likely DeclareAstFunctionalVisitor.
  * This class likely ["System.Linq.Expressions.ExpressionVisitor" class](https://msdn.microsoft.com/en-us/library/system.linq.expressions.expressionvisitor(v=vs.110).aspx).
  
```fsharp
// Inheritable visitor pattern (Not use custom context):
// Type name for free.
type InsertLoggingVisitor() =
  inherit AstInheritableVisitor()

  override __.VisitExpr_Quote(context, operator, isRaw, quoteSynExpr, isFromQueryExpression, range) =
    // DEBUG
    printfn "%A" operator
    base.VisitExpr_Quote(context, operator, isRaw, quoteSynExpr, isFromQueryExpression, range)

  override this.VisitExpr_App(context, exprAtomicFlag, isInfix, funcExpr, argExpr, range) =
    match funcExpr with
      // ...
```

* Default visiting is derivng from AstInheritableVisitor class.
* Hooking point are "BeforeVisit*_*" or "Visit*_*".
  * "BeforeVisit" is givening for all NON-VISITED args.
  * "Visit" is givening for all VISITED args.
  
## Build the package:

* TODO: Need more informations...

Pack to the filter package, using NuGet with following sample nuspec definitions:

```xml
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>fscx-sample-filter</id>
    <version>0.6.2-pre</version>
    <title>fscx-sample-filter</title>
    <authors>Kouji Matsui</authors>
    <owners>Kouji Matsui</owners>
    <summary>Expandable F# compiler's sample custom filter package.</summary>
    <description>Expandable F# compiler's sample custom filter package.</description>
    <copyright>Author: Kouji Matsui, bleis-tift</copyright>
    <projectUrl>http://github.com/fscx-projects/fscx</projectUrl>
    <iconUrl>https://raw.githubusercontent.com/fscx-projects/fscx/master/docs/files/img/fscx_128.png</iconUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes>Expandable F# compiler's sample custom filter package.</releaseNotes>
    <tags>fscx</tags>
    <dependencies>
      <!-- Including fscx dependency is highly recommended. -->
      <dependency id="FSharp.Expandable.Compiler.Build" version="0.6.2" />
    </dependencies>
  </metadata>
  <files>
    <!-- Add filter binaries (and pdb files if needed) into "build" package folder. -->
    <!-- Note that it's not "lib" folder because we should avoid to get these assemblies referenced automatically. -->
    <file src="bin/Debug/sample_filter.dll" target="build" />
    <file src="bin/Debug/sample_filter.pdb" target="build" />
  </files>
</package>
```

* Important: Package version must applied "-pre". Because current FSharp.Compiler.Services package depended pre-released .NET Core assembly (System.Reflection.Metadata.dll).

## Maintainer(s)

- [Kouji Matsui](https://github.com/kekyo) [twitter](https://twitter.com/kekyo2)
- [bleis-tift](https://github.com/bleis-tift) [twitter](https://twitter.com/bleis)
