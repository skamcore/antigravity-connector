using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace AntigravityConnect {
public sealed class LinkBackup {public string Path,Original;public bool Existed,Removed;}
public static class Setup {
    internal static string DesktopFolder=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    internal static string ProgramsFolder=Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    internal static string PinnedFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
    static string Manifest {get{return Path.Combine(Store.Root,"shortcuts.json");}}
    static List<LinkBackup> Read(){return File.Exists(Manifest)?new JavaScriptSerializer().Deserialize<List<LinkBackup>>(File.ReadAllText(Manifest)):new List<LinkBackup>();}
    public static void Link(string path,string args,string app,List<LinkBackup> records) {
        if(!records.Any(r=>String.Equals(r.Path,path,StringComparison.OrdinalIgnoreCase))) {
            records.Add(new LinkBackup{Path=path,Existed=File.Exists(path),Original=File.Exists(path)?Convert.ToBase64String(File.ReadAllBytes(path)):null});
            Store.WriteAtomic(Manifest,new JavaScriptSerializer().Serialize(records));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic link=shell.CreateShortcut(path);
        try {link.TargetPath=Store.Exe;link.Arguments=args;link.WorkingDirectory=Store.Root;link.IconLocation=(args=="--settings"?Path.Combine(Store.Root,"connector-v4.ico"):app)+",0";link.Description=args=="--settings"?"Antigravity Connector — connection settings":L.Text("Antigravity с вашим VPN");link.Save();}
        finally {System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
    }
    public static void Install(Options o) {
        if(!o.Consent)throw new IOException("Нужно разрешение на использование подключённого VPN.");
        Directory.CreateDirectory(Store.Root);string source=Application.ExecutablePath;
        if(!String.Equals(source,Store.Exe,StringComparison.OrdinalIgnoreCase)) {
            if(!File.Exists(Store.Exe) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(Store.Exe)))File.Copy(source,Store.Exe,true);
        }
        // Packaged launchers can redirect AppData. Persist the physical location
        // so Explorer and a later unpackaged launch use the exact same files.
        Store.Root=Path.GetDirectoryName(Store.PhysicalPath(Store.Exe));
        using(var icon=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Connector.ico")) {
            if(icon!=null)using(var file=File.Create(Path.Combine(Store.Root,"connector-v4.ico")))icon.CopyTo(file);
        }
        var records=Read();string desktop=DesktopFolder;
        string programs=ProgramsFolder;
        byte[] oldManifest=File.Exists(Manifest)?File.ReadAllBytes(Manifest):null;
        string settingsPath=Path.Combine(Store.Root,"settings.json");byte[] oldSettings=File.Exists(settingsPath)?File.ReadAllBytes(settingsPath):null;
        var transaction=new List<LinkBackup>();
        Action<string,string> update=(path,args)=>{
            transaction.Add(new LinkBackup{Path=path,Existed=File.Exists(path),Original=File.Exists(path)?Convert.ToBase64String(File.ReadAllBytes(path)):null});
            Link(path,args,o.AppPath,records);
        };
        try {
        // Preserve original links, including their arguments and icons, for rollback.
        foreach(string directory in new[]{desktop,programs,PinnedFolder}) {
            if(!Directory.Exists(directory))continue;
            foreach(string path in Directory.GetFiles(directory,"*.lnk",SearchOption.AllDirectories)) {
                dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic l=shell.CreateShortcut(path);bool replace=false;
                try {string target=Convert.ToString(l.TargetPath),args=Convert.ToString(l.Arguments);
                    replace=String.Equals(target,o.AppPath,StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(args);
                    if(Path.GetFileName(path)=="Antigravity через Happ.lnk" && String.Equals(target,Store.Exe,StringComparison.OrdinalIgnoreCase))replace=true;
                }finally {System.Runtime.InteropServices.Marshal.FinalReleaseComObject(l);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
                if(replace)update(path,"--launch");
            }
        }
        string connectorLink=Path.Combine(desktop,"Antigravity Connector.lnk");
        if(o.Desktop)update(connectorLink,"--settings");
        else {
            var record=records.FirstOrDefault(r=>String.Equals(r.Path,connectorLink,StringComparison.OrdinalIgnoreCase));
            if(record!=null && File.Exists(connectorLink) && IsOwnedLink(connectorLink)) {
                transaction.Add(new LinkBackup{Path=connectorLink,Existed=true,Original=Convert.ToBase64String(File.ReadAllBytes(connectorLink))});
                record.Removed=true;Store.WriteAtomic(Manifest,new JavaScriptSerializer().Serialize(records));File.Delete(connectorLink);
            }
        }
        update(Path.Combine(programs,"Antigravity.lnk"),"--launch");
        update(Path.Combine(programs,@"Antigravity Connect\Antigravity Connector.lnk"),"--settings");
        foreach(string legacy in new[]{Path.Combine(desktop,"Antigravity.lnk"),Path.Combine(desktop,"Antigravity через Happ.lnk"),Path.Combine(desktop,"Антигравити Коннектор.lnk"),Path.Combine(programs,@"Antigravity Connect\Антигравити Коннектор.lnk")}) {
        var legacyRecord=records.FirstOrDefault(r=>String.Equals(r.Path,legacy,StringComparison.OrdinalIgnoreCase));
        if(legacyRecord!=null && File.Exists(legacy) && IsOwnedLink(legacy)) {
            transaction.Add(new LinkBackup{Path=legacy,Existed=true,Original=Convert.ToBase64String(File.ReadAllBytes(legacy))});
            legacyRecord.Removed=true;Store.WriteAtomic(Manifest,new JavaScriptSerializer().Serialize(records));File.Delete(legacy);
        }
        }
        Store.Save(o);Store.Log("Installed; user VPN consent recorded");
        }catch {
            foreach(var before in transaction.AsEnumerable().Reverse()) {
                try {if(before.Existed)File.WriteAllBytes(before.Path,Convert.FromBase64String(before.Original));else if(File.Exists(before.Path))File.Delete(before.Path);}catch{}
            }
            RestoreSnapshot(Manifest,oldManifest);RestoreSnapshot(settingsPath,oldSettings);
            throw;
        }
    }
    static void RestoreSnapshot(string path,byte[] bytes){if(bytes==null){if(File.Exists(path))File.Delete(path);}else File.WriteAllBytes(path,bytes);}
    static bool IsOwnedLink(string path){
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic link=shell.CreateShortcut(path);
        try{return String.Equals(Convert.ToString(link.TargetPath),Store.Exe,StringComparison.OrdinalIgnoreCase);}
        finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
    }
    public static void Disable() {
        foreach(var r in Read()) {
            if(r.Removed && !File.Exists(r.Path)) {if(r.Existed)File.WriteAllBytes(r.Path,Convert.FromBase64String(r.Original));continue;}
            if(!File.Exists(r.Path))continue;
            dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic l=shell.CreateShortcut(r.Path);bool ours;
            try {ours=String.Equals(Convert.ToString(l.TargetPath),Store.Exe,StringComparison.OrdinalIgnoreCase);}
            finally {System.Runtime.InteropServices.Marshal.FinalReleaseComObject(l);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
            if(!ours)continue;
            if(r.Existed)File.WriteAllBytes(r.Path,Convert.FromBase64String(r.Original));else File.Delete(r.Path);
        }
        var options=Store.Load();options.Consent=false;Store.Save(options);
        // A new enable cycle must back up the user's current shortcuts, not
        // restore stale originals from a previous, fully disabled installation.
        Store.WriteAtomic(Manifest,"[]");Store.Log("Disabled; original shortcuts restored");
    }
}
public class SetupWindow : Form {
    CheckBox consent,desktop,tunnel;Label status,statusHeading;Button enable,check,disable,language;ProgressBar progress;Options options;bool installed,dirty,statusError;
    string statusTemplate;object[] statusArguments=new object[0];
    Color ink=Color.FromArgb(31,35,53);
    public SetupWindow(string error=null) {
        Text=L.Product;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(544,520);
        string fontName=FontFamily.Families.Any(f=>f.Name=="Inter")?"Inter":"Segoe UI";
        AutoScaleMode=AutoScaleMode.Dpi;Font=new Font(fontName,9.5f);BackColor=Color.White;ForeColor=ink;
        MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedSingle;
        options=Store.Load();installed=options.Consent && File.Exists(Store.Exe);
        L.Language=options.Language=="en"?"en":"ru";
        try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
        if(Icon!=null)Controls.Add(new PictureBox{Image=Icon.ToBitmap(),SizeMode=PictureBoxSizeMode.Zoom,Bounds=new Rectangle(24,25,32,32),TabStop=false});
        Label title=new Label{Text=L.Product,Font=new Font(fontName,15,FontStyle.Bold),AutoSize=false,Bounds=new Rectangle(66,27,330,30)};Controls.Add(title);
        language=new ActionButton{Bounds=new Rectangle(456,25,64,32),AccessibleName="Language / Язык",Font=new Font(fontName,9)};Controls.Add(language);
        Controls.Add(new Label{Text="Antigravity через ваш VPN",Bounds=new Rectangle(24,73,496,24),ForeColor=Color.FromArgb(120,122,138)});
        var statusCard=new SoftPanel{Bounds=new Rectangle(24,112,496,84),BackColor=Color.FromArgb(245,244,250)};Controls.Add(statusCard);
        statusHeading=new Label{Font=new Font(fontName,9.5f,FontStyle.Bold),Bounds=new Rectangle(16,12,464,20)};statusCard.Controls.Add(statusHeading);
        status=new Label{Bounds=new Rectangle(16,36,464,39),Font=new Font(fontName,8.5f),ForeColor=Color.FromArgb(113,113,134)};statusCard.Controls.Add(status);
        Controls.Add(new Label{Text="Настройки подключения",Bounds=new Rectangle(24,213,496,20),ForeColor=Color.FromArgb(133,134,148),Font=new Font(fontName,8.5f)});
        consent=new OptionRow{Text="Использовать мой VPN",Hint="Разрешить подключение через ваш VPN",Checked=options.Consent,Bounds=new Rectangle(24,238,496,53),BackColor=Color.White};Controls.Add(consent);
        tunnel=new OptionRow{Text="Системный туннель",Hint="Поддержка VPN в режиме TUN",Checked=options.AllowTunnel,Bounds=new Rectangle(24,294,496,53),BackColor=Color.White};Controls.Add(tunnel);
        desktop=new OptionRow{Text="Ярлык на рабочем столе",Hint="Настройки Antigravity Connector",Checked=options.Desktop,Bounds=new Rectangle(24,350,496,53),BackColor=Color.White,Divider=false};Controls.Add(desktop);
        progress=new ProgressBar{Bounds=new Rectangle(24,420,496,3),Style=ProgressBarStyle.Marquee,Visible=false};Controls.Add(progress);
        enable=new ActionButton{Primary=true,Text=installed?"Открыть Antigravity":"Включить",Bounds=new Rectangle(314,440,206,42),ForeColor=Color.White};Controls.Add(enable);
        check=new ActionButton{Text="Проверить VPN",Bounds=new Rectangle(24,440,140,42)};Controls.Add(check);
        disable=new ActionButton{Text="Отключить",Bounds=new Rectangle(172,440,130,42),Enabled=installed};Controls.Add(disable);
        Controls.Add(new Label{Text="by skam",AutoSize=false,Bounds=new Rectangle(24,491,496,18),TextAlign=ContentAlignment.MiddleRight,Font=new Font(fontName,8),ForeColor=Color.FromArgb(133,134,148)});
        TagTexts(this);
        statusTemplate=error??(installed?"Открывайте Antigravity обычным ярлыком. VPN оставляйте включённым.":"Подключите VPN и разрешите его использование ниже.");statusError=error!=null;
        language.Click+=delegate {
            string previous=L.Language;L.Language=previous=="en"?"ru":"en";
            try {var saved=Store.Load();saved.Language=L.Language;Store.Save(saved);options.Language=L.Language;ApplyLanguage();}
            catch(Exception e){L.Language=previous;SetError(e);}
        };
        enable.Click+=async delegate {
            if(installed && !dirty && consent.Checked) {Launch();return;}
            if(!consent.Checked){SetStatus("Поставьте галочку, чтобы разрешить использование вашего VPN.");return;}
            Busy(true,"Проверяем VPN и настраиваем ярлыки…");
            var o=new Options{Consent=consent.Checked,AllowTunnel=tunnel.Checked,Desktop=desktop.Checked,Language=L.Language};
            try {
                string route=await Task.Run(()=>{o.AppPath=Store.FindApp();var r=Discovery.Find(o.AllowTunnel);using(var relay=new Relay(r))Health.Check(relay);return r.Label;});
                // COM shortcut APIs run in the STA UI thread.
                Setup.Install(o);options=o;installed=true;dirty=false;RefreshButtons();disable.Enabled=true;
                SetStatus("Готово · {0}. Дальше открывайте обычный ярлык Antigravity.",route);
                Launch();
            }catch(Exception e){SetError(e);Store.Log("Setup error: "+e.GetType().Name);}
            finally{Busy(false,null);}
        };
        check.Click+=async delegate {
            if(!consent.Checked){SetStatus("Сначала разрешите использование подключённого VPN.");return;}
            bool allowTunnel=tunnel.Checked;Busy(true,"Проверяем доступ к серверам Google…");
            try {string label=await Task.Run(()=>{var r=Discovery.Find(allowTunnel);using(var relay=new Relay(r))Health.Check(relay);return r.Label;});SetStatus("Соединение исправно · {0}. Ответ модели проверяется в приложении.",label);}
            catch(Exception e){SetError(e);}
            finally{Busy(false,null);}
        };
        disable.Click+=delegate {try {Setup.Disable();installed=false;dirty=false;consent.Checked=false;RefreshButtons();disable.Enabled=false;SetStatus("Отключено. Прежние ярлыки восстановлены. Текущий сеанс завершится обычным образом.");}catch(Exception e){SetError(e);}};
        tunnel.CheckedChanged+=delegate {if(installed){dirty=true;RefreshButtons();}};
        desktop.CheckedChanged+=delegate {if(installed){dirty=true;RefreshButtons();}};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(progress.Visible)e.Cancel=true;};
        ApplyLanguage();
    }
    void TagTexts(Control parent){foreach(Control c in parent.Controls){if(c!=language&&c!=status&&c!=statusHeading&&!String.IsNullOrEmpty(c.Text))c.Tag=c.Text;TagTexts(c);}}
    void TranslateControls(Control parent){foreach(Control c in parent.Controls){if(c.Tag is string)c.Text=L.Text((string)c.Tag);var row=c as OptionRow;if(row!=null)row.AccessibleDescription=L.Text(row.Hint);TranslateControls(c);}}
    void RefreshButtons(){enable.Text=L.Text(installed?(dirty?"Сохранить и включить":"Открыть Antigravity"):"Включить");check.Text=L.Text("Проверить VPN");disable.Text=L.Text("Отключить");language.Text=L.Language=="en"?"EN":"RU";language.AccessibleDescription=L.Language=="en"?"Switch to Russian":"Переключить на английский";}
    void ApplyLanguage(){TranslateControls(this);RefreshButtons();RenderStatus();Invalidate(true);}
    void RenderStatus(){status.Text=L.Format(statusTemplate,statusArguments);statusHeading.Text=L.Text(statusError?"Требуется внимание":progress.Visible?"Проверяем подключение":installed?"Помощник включён":"Готов к настройке");statusHeading.ForeColor=statusError?Color.FromArgb(171,62,65):ink;}
    void SetStatus(string template,params object[] args){statusError=false;statusTemplate=template;statusArguments=args;RenderStatus();}
    void SetError(Exception e){statusError=true;statusTemplate=e.GetBaseException().Message;statusArguments=new object[0];RenderStatus();}
    void Busy(bool busy,string text){enable.Enabled=check.Enabled=consent.Enabled=desktop.Enabled=tunnel.Enabled=language.Enabled=!busy;disable.Enabled=!busy&&installed;progress.Visible=busy;if(text!=null)SetStatus(text);else RenderStatus();}
    void Launch(){try {Process.Start(new ProcessStartInfo(Store.Exe,"--launch"){UseShellExecute=false});}catch(Exception e){SetError(e);}}
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try {
            L.Language=Store.Load().Language=="en"?"en":"ru";
            if(args.Contains("--self-test")){Tests.Run();return 0;}
            if(args.Contains("--refresh-shortcuts")){Setup.Install(Store.Load());return 0;}
            if(args.Contains("--check")){var o=Store.Load();if(!o.Consent)throw new IOException("Нужно разрешение на использование подключённого VPN.");var route=Discovery.Find(o.AllowTunnel);using(var relay=new Relay(route))Health.Check(relay);Directory.CreateDirectory(Store.Root);File.WriteAllText(Path.Combine(Store.Root,"check-result.txt"),"OK: "+route.Label);return 0;}
            if(args.Contains("--launch"))return Launch();
            Application.Run(new SetupWindow());return 0;
        }catch(Exception e){Store.Log("Error: "+e.GetType().Name+" "+e.GetBaseException().Message);if(args.Contains("--self-test")||args.Contains("--check")||args.Contains("--refresh-shortcuts"))return 1;Application.Run(new SetupWindow(e.GetBaseException().Message));return 1;}
    }
    static int Launch() {
        var o=Store.Load();if(!o.Consent){Application.Run(new SetupWindow());return 1;}
        string app=File.Exists(o.AppPath)?o.AppPath:Store.FindApp();
        if(Process.GetProcessesByName("Antigravity").Length>0){Process.Start(app);return 0;}
        bool owns;using(var mutex=new Mutex(true,@"Local\AntigravityConnect.Launch",out owns)) {
            if(!owns)return 0;
            try {
                var route=Discovery.Find(o.AllowTunnel);using(var relay=new Relay(route)) {
                    Store.Log("Route: "+route.Label);Store.Prepare(app);
                    var info=new ProcessStartInfo(app,"--proxy-server=http://127.0.0.1:"+relay.Port+" --proxy-bypass-list=localhost;127.0.0.1;[::1]"){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(app)};
                    string proxy="http://127.0.0.1:"+relay.Port;
                    foreach(string name in new[]{"HTTP_PROXY","HTTPS_PROXY","ALL_PROXY","http_proxy","https_proxy","all_proxy","AG_LS_PROXY"})info.EnvironmentVariables[name]=proxy;
                    info.EnvironmentVariables["NO_PROXY"]="localhost,127.0.0.1,::1";info.EnvironmentVariables["no_proxy"]="localhost,127.0.0.1,::1";
                    Process.Start(info);Thread.Sleep(3000);
                    while(Process.GetProcessesByName("Antigravity").Length>0)Thread.Sleep(1500);
                    Store.Log("Session ended; connections="+relay.Connections+"; failed="+relay.Failures);return 0;
                }
            }finally{mutex.ReleaseMutex();}
        }
    }
}
}
