using System;
using System.IO;
using AntigravityConnect;
public static class AuditRunner {
    [STAThread] public static int Main() {
        Store.Root=Path.Combine(Directory.GetCurrentDirectory(),"work","connect","audit","state");
        try {Tests.Run();Console.WriteLine("AUDIT TESTS PASSED");return 0;}
        catch(Exception e){Console.WriteLine(e.ToString());return 1;}
    }
}
