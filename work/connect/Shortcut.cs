using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AntigravityConnect {
// Explicit Unicode shell interface: WScript.Shell can lose characters outside
// the system ANSI code page when saving a shortcut on another Windows locale.
public sealed class Shortcut : IDisposable {
    readonly string path; IShellLinkW link;
    public Shortcut(string file) {
        path=file;link=(IShellLinkW)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
        try {if(File.Exists(file))((IPersistFile)link).Load(file,0);}
        catch {Dispose();throw;}
    }
    public string TargetPath {get {var b=new StringBuilder(32768);link.GetPath(b,b.Capacity,IntPtr.Zero,4);return b.ToString();}set {link.SetPath(value);}}
    public string Arguments {get {var b=new StringBuilder(32768);link.GetArguments(b,b.Capacity);return b.ToString();}set {link.SetArguments(value);}}
    public string WorkingDirectory {set {link.SetWorkingDirectory(value);}}
    public string Description {set {link.SetDescription(value);}}
    public void SetIcon(string file){link.SetIconLocation(file,0);}
    public void Save(){((IPersistFile)link).Save(path,true);}
    public void Dispose(){if(link!=null){Marshal.FinalReleaseComObject(link);link=null;}}

    [ComImport,Guid("000214F9-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLinkW {
        void GetPath([Out,MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int count,IntPtr findData,uint flags);
        void GetIDList(out IntPtr pidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out,MarshalAs(UnmanagedType.LPWStr)] StringBuilder text,int count);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
        void GetWorkingDirectory([Out,MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int count);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);
        void GetArguments([Out,MarshalAs(UnmanagedType.LPWStr)] StringBuilder args,int count);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int show);
        void SetShowCmd(int show);
        void GetIconLocation([Out,MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int count,out int index);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path,int index);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path,uint reserved);
        void Resolve(IntPtr window,uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }
}
}
