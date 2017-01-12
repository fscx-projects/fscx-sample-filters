//////////////////////////////////////////////////////////////////////////////
// 
// fscx - Expandable F# compiler project
//   Author: Kouji Matsui (@kekyo2), bleis-tift (@bleis-tift)
//   GutHub: https://github.com/fscx-projects/
//
// Creative Commons Legal Code
// 
// CC0 1.0 Universal
// 
//   CREATIVE COMMONS CORPORATION IS NOT A LAW FIRM AND DOES NOT PROVIDE
//   LEGAL SERVICES.DISTRIBUTION OF THIS DOCUMENT DOES NOT CREATE AN
//   ATTORNEY-CLIENT RELATIONSHIP.CREATIVE COMMONS PROVIDES THIS
//   INFORMATION ON AN "AS-IS" BASIS.CREATIVE COMMONS MAKES NO WARRANTIES
//   REGARDING THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS
//   PROVIDED HEREUNDER, AND DISCLAIMS LIABILITY FOR DAMAGES RESULTING FROM
//   THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS PROVIDED
//   HEREUNDER.
//
//////////////////////////////////////////////////////////////////////////////

module Filter

open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Ast.Visitors
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.Expandable

let (+>) a b = SynExpr.Sequential(SuppressSequencePointOnStmtOfSequential, true, a, b, zeroRange)
let writeLineM = MethodInfo.extract <@ System.Console.WriteLine() @>

let logStartCallMethod name (ident : string) = 
    genCallStaticMethod (writeLineM, 
                         [ genStringLit ("calling " + name + ". args: {0}")
                           genIdent ident ])

let logFinishCallMethod name (ident : string) = 
    genCallStaticMethod (writeLineM, 
                         [ genStringLit ("called " + name + ". args: {0}")
                           genIdent ident ])

let logExn name (ident : string) = 
    genCallStaticMethod (writeLineM, 
                         [ genStringLit ("raised exn from " + name + ". exn: {0}")
                           genIdent ident ])

let rec addSep sep = 
    function 
    | [] -> []
    | [ x ] -> [ x ]
    | x :: xs -> x :: sep :: (addSep sep xs)

let isIdent = 
    function 
    | SynExpr.Ident _ | SynExpr.LongIdent _ -> true
    | _ -> false

////////////////////////////////////////////////
let outerVisitor (defaultVisitor : FscxVisitorContext<NoContext> * SynExpr -> SynExpr, 
                  context : FscxVisitorContext<NoContext>, expr : SynExpr) : SynExpr option = 
    match expr with
    | SynExpr.Quote(operator, _, _, _, _) -> 
        // DEBUG
        printfn "%A" operator
        None
    | SynExpr.App(exprAtomicFlag, isInfix, funcExpr, argExpr, range) -> 
        match funcExpr with
        | SynExpr.Ident _ | SynExpr.LongIdent _ -> 
            let results = 
                match funcExpr with
                | SynExpr.Ident ident -> [ ident.idText ], ident.idRange
                | SynExpr.LongIdent(_, longIdent, _, range) -> 
                    let elems = longIdent.Lid |> List.map (fun i -> i.idText)
                    elems, range
                | _ -> failwith "Unknown"
            
            let funcNameElems, funcIdentRange = results
            // Lookup by symbol information (FCS)
            let opt = 
                context.SymbolInformation.GetSymbolUseAtLocation
                    (funcIdentRange.Start.Line, funcIdentRange.End.Column, "", funcNameElems) |> Async.RunSynchronously
            
            // TODO : If this node is not target then not translate this node.
            let asm = 
                match opt with
                | Some symbolUse -> 
                    let symbol = symbolUse.Symbol
                    let asm = symbol.Assembly
                    asm.SimpleName
                | None -> "unknown"
            
            let funcName = 
                (funcNameElems |> String.concat ".") 
                + (sprintf " [Line %d, Column %d]" funcIdentRange.Start.Line funcIdentRange.Start.Column)
            // from
            //   f(expr1, expr2, ..., exprN)
            // to
            //   try
            //     let arg1 = expr1
            //     let arg2 = expr2
            //     ...
            //     let argN = exprN
            //     let args = string arg1 + ", " + string arg2 + ", " + ... + string argN
            //     log1 ("f(" + args + ")")
            //     let res = f(arg1, arg2, ..., argN)
            //     log2 ("f(" + args + ")")
            //     res
            //   with
            //   | e ->
            //       log3 ("f", e)
            //       reraise ()
            match argExpr with
            // if "f ()"    => Const(Unit)
            // if "f (())"  => Paren(Const(Unit))
            | SynExpr.Const(SynConst.Unit, x) | SynExpr.Paren(SynExpr.Const(SynConst.Unit, x), _, _, _) -> 
                let tryExpr = 
                    genLetExpr 
                        ("args", genStringLit "()", 
                         logStartCallMethod funcName "args" 
                         +> (genLetExpr 
                                 ("res", SynExpr.App(exprAtomicFlag, isInfix, funcExpr, argExpr, zeroRange), 
                                  logFinishCallMethod funcName "res" +> genIdent "res")))
                Some(genTryExpr (tryExpr, [ genClause ("e", logExn funcName "e" +> (genReraise())) ], range))
            // if "f (x, y, ...)"  => Paren(Tuple(exprs))
            | SynExpr.Paren(SynExpr.Tuple(exprs, commaRange, trange), x, y, z) -> 
                let tryExpr = 
                    let exprs = exprs |> List.mapi (fun i x -> (i + 1, x))
                    let args = exprs |> List.map (fun (n, _) -> SynExpr.Ident(Ident("arg" + string n, zeroRange)))
                    
                    let seed = 
                        genLetExpr 
                            ("args", 
                             genOpChain ("op_Addition", 
                                         args
                                         |> List.map (fun arg -> genAppFun ("string", arg))
                                         |> addSep (genStringLit ", ")), 
                             logStartCallMethod funcName "args" 
                             +> genLetExpr 
                                    ("res", 
                                     SynExpr.App
                                         (exprAtomicFlag, isInfix, funcExpr, 
                                          SynExpr.Paren(SynExpr.Tuple(args, commaRange, trange), x, y, z), zeroRange), 
                                     logFinishCallMethod funcName "res" +> (genIdent "res")))
                    
                    let x = 
                        (exprs, seed) ||> List.foldBack (fun (n, expr) acc -> 
                                              let name = "arg" + string n
                                              genLetExpr (name, expr, acc))
                    
                    x
                Some(genTryExpr (tryExpr, [ genClause ("e", logExn funcName "e" +> genReraise()) ], range))
            // if "f (x)"   => Paren(expr)
            // if "f x"     => expr
            | SynExpr.Paren(expr, _, _, _) | expr -> 
                let tryExpr = 
                    genLetExpr 
                        ("args", expr, 
                         logStartCallMethod funcName "args" 
                         +> (genLetExpr 
                                 ("res", 
                                  SynExpr.App
                                      (exprAtomicFlag, isInfix, funcExpr, 
                                       SynExpr.Paren(expr, zeroRange, None, zeroRange), zeroRange), 
                                  logFinishCallMethod funcName "res" +> genIdent "res")))
                Some(genTryExpr (tryExpr, [ genClause ("e", logExn funcName "e" +> genReraise()) ], range))
        // Another SynExpr --> None is default visiting.
        | _ -> None
    // Another SynExpr --> None is default visiting.
    | _ -> None

type InsertLoggingVisitor() = 
    inherit DeclareFscxFunctionalVisitor(outerVisitor)

