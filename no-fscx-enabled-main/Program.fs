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

open System
open System.Runtime.InteropServices

[<DllImport("user32.dll", CharSet = CharSet.Unicode)>]
extern int MessageBox(IntPtr hWnd, string text, string caption, int options);

[<EntryPoint>]
let main argv = 
  //MessageBox(IntPtr.Zero, "Wait for attached debugger...", "fscx-enabled-main", 0 ||| 0x30) |> ignore
  FscxOutputSample1.f1 (123, "ABC", 456)
  FscxOutputSample2.f2 (789, "DEF", 111)
  printfn "%A" argv
  0
