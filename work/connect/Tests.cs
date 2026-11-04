using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AntigravityConnect {
public static class Tests {
    static void Assert(bool condition,string message){if(!condition)throw new Exception("TEST: "+message);}
    public static void Run() {
        Assert(Net.Target("daily-cloudcode-pa.googleapis.com:443")=="daily-cloudcode-pa.googleapis.com:443","Preserve Google API destination through user VPN");
        Assert(Net.Target("daily-cloudcode-pa.googleapis.com.attacker.invalid:443")=="daily-cloudcode-pa.googleapis.com.attacker.invalid:443","Exact host match");
        Assert(Net.Target("cloudcode-pa.googleapis.com:80")=="cloudcode-pa.googleapis.com:80","Exact port match");
        byte[] patch=Encoding.ASCII.GetBytes("ineligible..ineligible!ineligibl");
        Assert(Store.Replace(patch,"ineligible","inexigible")==2,"Patch count");
        Assert(Encoding.ASCII.GetString(patch)=="inexigible..inexigible!ineligibl","Patch boundary");
        Assert(Store.Replace(patch,"ineligible","inexigible")==0,"Patch idempotency");
        byte[] payload=Enumerable.Range(0,180).Select(x=>(byte)x).ToArray();var record=new List<byte>(new byte[]{22,3,1,0,180});record.AddRange(payload);
        using(var input=new MemoryStream(record.ToArray()))using(var output=new MemoryStream()) {
            Net.Hello(input,output);output.Position=0;var rebuilt=new List<byte>();int records=0;
            while(output.Position<output.Length){var h=Net.Read(output,5);Assert(h[0]==22&&h[1]==3&&h[2]==1,"Record version preserved");rebuilt.AddRange(Net.Read(output,h[3]*256+h[4]));records++;}
            Assert(records==2 && rebuilt.SequenceEqual(payload),"TLS handshake bytes preserved");
        }
        using(var input=new MemoryStream(new byte[]{1,2})){bool failed=false;try{Net.Read(input,4);}catch(IOException){failed=true;}Assert(failed,"Truncated stream rejected");}
        ProxyTest("http",false);ProxyTest("socks",false);ProxyTest("socks",true);
        Assert(Net.IsConnectSuccess("HTTP/1.1 200 Connection established\r\n\r\n"),"Valid CONNECT status");
        Assert(!Net.IsConnectSuccess("HTTP/1.1 2000 Invalid\r\n\r\n"),"Malformed CONNECT status rejected");
        RelayDestinationTest();
        RelayLifecycleTest();
        string savedRoot=Store.Root;Store.Root=Path.Combine(Directory.GetCurrentDirectory(),"work","connect","test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Store.Root);
        try {
            string path=Path.Combine(Store.Root,"Ярлык с пробелами.lnk");
            using(var original=new Shortcut(path)) {
            original.TargetPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"notepad.exe");original.Arguments="original-profile";original.Save();
            }
            using(var unicodeLink=new Shortcut(path))Assert(unicodeLink.Arguments=="original-profile","Unicode shortcut path reopens correctly");
            byte[] before=File.ReadAllBytes(path);var records=new List<LinkBackup>();Setup.Link(path,"--launch","C:\\fake\\Antigravity.exe",records);
            Assert(!File.ReadAllBytes(path).SequenceEqual(before),"Shortcut installed");
            Setup.Link(path,"--launch","C:\\fake\\Antigravity.exe",records);Assert(records.Count==1,"Original shortcut backed up only once");
            Store.Save(new Options{Consent=true});Setup.Disable();Assert(File.ReadAllBytes(path).SequenceEqual(before),"Shortcut rollback byte-identical");
            Assert(!Store.Load().Consent,"Consent removed on disable");
            File.WriteAllText(Path.Combine(Store.Root,"settings.json"),"{bad json");Assert(!Store.Load().Consent,"Malformed config does not authorize VPN or crash UI");
            File.WriteAllText(Path.Combine(Store.Root,"settings.json"),"null");Assert(!Store.Load().Consent,"Null config stays disabled");Store.Save(new Options());
            string oldDesktop=Setup.DesktopFolder,oldPrograms=Setup.ProgramsFolder,oldPinned=Setup.PinnedFolder;
            try {
                Setup.DesktopFolder=Path.Combine(Store.Root,"desktop");Setup.ProgramsFolder=Path.Combine(Store.Root,"programs");Setup.PinnedFolder=Path.Combine(Store.Root,"pinned");
                Directory.CreateDirectory(Setup.DesktopFolder);Directory.CreateDirectory(Setup.ProgramsFolder);
                string oldLink=Path.Combine(Setup.DesktopFolder,"Antigravity.lnk");File.WriteAllBytes(oldLink,before);
                string unrelated=Path.Combine(Setup.DesktopFolder,"Antigravity через Happ.lnk");
                using(var unrelatedLink=new Shortcut(unrelated)) {
                unrelatedLink.TargetPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"calc.exe");unrelatedLink.Save();
                }
                byte[] unrelatedBefore=File.ReadAllBytes(unrelated);
                string app=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"notepad.exe");
                Setup.Install(new Options{Consent=true,Desktop=true,AllowTunnel=true,AppPath=app});
                Assert(File.Exists(Store.Exe),"Standalone executable installed");Assert(Store.Load().Consent,"Installation persisted consent");
                Assert(File.Exists(Path.Combine(Setup.ProgramsFolder,"Antigravity.lnk")),"Start menu launcher installed");
                Setup.Install(new Options{Consent=true,Desktop=false,AllowTunnel=true,AppPath=app});
                Assert(!File.Exists(Path.Combine(Setup.DesktopFolder,"Antigravity Connector.lnk")),"Disabling desktop option removes owned connector shortcut");
                Setup.Install(new Options{Consent=true,Desktop=true,AllowTunnel=true,AppPath=app});
                Assert(File.Exists(Path.Combine(Setup.DesktopFolder,"Antigravity Connector.lnk")),"Desktop shortcut can be enabled again");
                Assert(File.ReadAllBytes(unrelated).SequenceEqual(unrelatedBefore),"A matching filename cannot overwrite an unrelated shortcut");
                using(var window=new SetupWindow()) {
                    var fields=typeof(SetupWindow).GetFields(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                    var tunnel=(System.Windows.Forms.CheckBox)fields.Single(f=>f.Name=="tunnel").GetValue(window);
                    var disabled=(System.Windows.Forms.Button)fields.Single(f=>f.Name=="disable").GetValue(window);
                    tunnel.Checked=!tunnel.Checked;
                    typeof(SetupWindow).GetMethod("Busy",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(window,new object[]{false,null});
                    Assert(disabled.Enabled,"Unsaved options do not disable the installed connection's off button");
                    Assert((bool)fields.Single(f=>f.Name=="installed").GetValue(window),"Unsaved options preserve actual installation state");
                    var languages=(System.Windows.Forms.Button)fields.Single(f=>f.Name=="language").GetValue(window);
                    var click=typeof(System.Windows.Forms.Button).GetMethod("OnClick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                    var check=(System.Windows.Forms.Button)fields.Single(f=>f.Name=="check").GetValue(window);
                    bool draftTunnel=tunnel.Checked,savedTunnel=Store.Load().AllowTunnel,savedConsent=Store.Load().Consent;
                    click.Invoke(languages,new object[]{EventArgs.Empty});
                    Assert(check.Text=="Check VPN" && window.Text=="Antigravity Connector" && languages.Text=="EN","English UI and language button");
                    Assert(Store.Load().Language=="en","Language selection persists immediately");
                    Assert(tunnel.Checked==draftTunnel && Store.Load().AllowTunnel==savedTunnel && Store.Load().Consent==savedConsent,"Language switch preserves unsaved VPN options and stored permission");
                    using(var reopened=new SetupWindow()) {
                        var checkField=typeof(SetupWindow).GetField("check",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                        Assert(((System.Windows.Forms.Button)checkField.GetValue(reopened)).Text=="Check VPN","Reopened settings restore English");
                    }
                    click.Invoke(languages,new object[]{EventArgs.Empty});
                    Assert(check.Text=="Проверить VPN" && Store.Load().Language=="ru" && languages.Text=="RU","Russian language button and persistence");
                    L.Language="en";Assert(L.Format("Соединение исправно · {0}. Ответ модели проверяется в приложении.","Системный прокси · порт 10809").Contains("System proxy · port 10809"),"Dynamic connection status translated");
                    Assert(L.Format("Error with {literal braces}")=="Error with {literal braces}","Unformatted error messages preserve braces");L.Language="ru";
                }
                var currentRecords=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<List<LinkBackup>>(File.ReadAllText(Path.Combine(Store.Root,"shortcuts.json")));
                Setup.Link(oldLink,"--launch",app,currentRecords);
                Setup.Install(new Options{Consent=true,Desktop=true,AllowTunnel=true,AppPath=app});
                Assert(!File.Exists(oldLink) && File.Exists(Path.Combine(Setup.DesktopFolder,"Antigravity Connector.lnk")),"Migration keeps only the current connector desktop shortcut");
                Setup.Disable();Assert(File.ReadAllBytes(oldLink).SequenceEqual(before),"Full install rollback preserved desktop link");
                Assert(!File.Exists(Path.Combine(Setup.ProgramsFolder,"Antigravity.lnk")),"New start menu link removed on disable");
                Assert(File.ReadAllText(Path.Combine(Store.Root,"shortcuts.json"))=="[]","Completed disable clears stale shortcut history");
                byte[] changedOriginal=File.ReadAllBytes(unrelated);File.WriteAllBytes(oldLink,changedOriginal);
                Setup.Install(new Options{Consent=true,Desktop=true,AppPath=app});Setup.Disable();
                Assert(File.ReadAllBytes(oldLink).SequenceEqual(changedOriginal),"New enable cycle preserves externally changed original shortcut");
                File.WriteAllBytes(oldLink,before);
                string blocked=Path.Combine(Store.Root,"not-a-directory");File.WriteAllText(blocked,"blocked");Setup.ProgramsFolder=blocked;
                byte[] manifestBefore=File.ReadAllBytes(Path.Combine(Store.Root,"shortcuts.json"));
                byte[] settingsBefore=File.ReadAllBytes(Path.Combine(Store.Root,"settings.json"));
                bool rolledBack=false;try {Setup.Install(new Options{Consent=true,Desktop=true,AppPath=app});}catch(IOException){rolledBack=true;}
                Assert(rolledBack && File.ReadAllBytes(oldLink).SequenceEqual(before),"Interrupted install restored previously changed links");
                Assert(!Store.Load().Consent,"Failed installation did not enable connection");
                Assert(File.ReadAllBytes(Path.Combine(Store.Root,"shortcuts.json")).SequenceEqual(manifestBefore),"Failed installation restored shortcut manifest");
                Assert(File.ReadAllBytes(Path.Combine(Store.Root,"settings.json")).SequenceEqual(settingsBefore),"Failed installation preserved settings");
            }finally {Setup.DesktopFolder=oldDesktop;Setup.ProgramsFolder=oldPrograms;Setup.PinnedFolder=oldPinned;}
        }finally{Store.Root=savedRoot;}
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(),"work","connect","tests-result.txt"),"PASS: patch, TLS integrity, HTTP/SOCKS5 negotiation, strict CONNECT status, relay half-close/disposal, corrupt config recovery, install/disable and transactional manifest/settings rollback in isolated directories.");
    }
    static void RelayDestinationTest() {
        var upstream=new TcpListener(IPAddress.Loopback,0);upstream.Start();
        int port=((IPEndPoint)upstream.LocalEndpoint).Port;
        var server=Task.Run(()=>{using(var c=upstream.AcceptTcpClient()) {
            c.ReceiveTimeout=10000;var stream=c.GetStream();
            Assert(Encoding.ASCII.GetString(Net.Header(stream)).StartsWith("CONNECT daily-cloudcode-pa.googleapis.com:443 HTTP/1.1"),"Relay preserves actual Google API CONNECT destination");
            Net.Write(stream,"HTTP/1.1 200 OK\r\n\r\n");
            byte[] payload=Net.Read(stream,7);Assert(payload.SequenceEqual(new byte[]{22,3,1,0,2,1,2}),"Relay preserves TLS bytes exactly");
            Net.Write(stream,"OK");
        }});
        try {using(var relay=new Relay(new Route{Kind="http",Host="127.0.0.1",Port=port}))
        using(var c=Net.Open(new Route{Kind="http",Host="127.0.0.1",Port=relay.Port},"daily-cloudcode-pa.googleapis.com:443")) {
            var stream=c.GetStream();byte[] bytes={22,3,1,0,2,1,2};stream.Write(bytes,0,bytes.Length);
            Assert(Encoding.ASCII.GetString(Net.Read(stream,2))=="OK","Unmodified tunnel response");Assert(server.Wait(10000),"Destination mock completed");
        }}finally{upstream.Stop();}
    }
    static void RelayLifecycleTest() {
        var endpoint=new TcpListener(IPAddress.Loopback,0);endpoint.Start();int port=((IPEndPoint)endpoint.LocalEndpoint).Port;
        var server=Task.Run(()=>{using(var c=endpoint.AcceptTcpClient()){c.ReceiveTimeout=5000;var stream=c.GetStream();Assert(Encoding.ASCII.GetString(Net.Read(stream,7))=="request","Relayed request bytes");Assert(stream.ReadByte()==-1,"Upstream receives half-close");Net.Write(stream,"complete response");}});
        try {using(var relay=new Relay(new Route{Kind="direct"}))using(var client=Net.Tcp("127.0.0.1",relay.Port,2000)) {
            var stream=client.GetStream();Net.Write(stream,"CONNECT 127.0.0.1:"+port+" HTTP/1.1\r\n\r\n");Assert(Net.IsConnectSuccess(Encoding.ASCII.GetString(Net.Header(stream))),"Local CONNECT established");
            Net.Write(stream,"request");client.Client.Shutdown(SocketShutdown.Send);
            Assert(Encoding.ASCII.GetString(Net.Read(stream,17))=="complete response","Half-closed request does not truncate response");Assert(server.Wait(5000),"Half-close server completed");
        }}finally{endpoint.Stop();}
        using(var relay=new Relay(new Route{Kind="direct"}))using(var idle=Net.Tcp("127.0.0.1",relay.Port,2000)) {
            var deadline=DateTime.UtcNow.AddSeconds(3);while(relay.Connections==0 && DateTime.UtcNow<deadline)System.Threading.Thread.Sleep(10);
            relay.Dispose();bool closed=false;try{closed=idle.GetStream().ReadByte()==-1;}catch(IOException){closed=true;}
            Assert(closed,"Dispose closes idle clients waiting for headers");relay.Dispose();
        }
    }
    static void ProxyTest(string kind,bool reject) {
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
        var server=Task.Run(()=>{
            using(var c=listener.AcceptTcpClient()){c.ReceiveTimeout=5000;var s=c.GetStream();
                if(kind=="http") {string header=Encoding.ASCII.GetString(Net.Header(s));Assert(header.StartsWith("CONNECT example.org:443 HTTP/1.1"),"CONNECT authority");Net.Write(s,"HTTP/1.1 200 Connection established\r\n\r\n");}
                else {Assert(Net.Read(s,3).SequenceEqual(new byte[]{5,1,0}),"SOCKS greeting");s.Write(new byte[]{5,(byte)(reject?2:0)},0,2);if(reject)return;
                    var h=Net.Read(s,5);Assert(h.Take(4).SequenceEqual(new byte[]{5,1,0,3}),"SOCKS domain request");Assert(Encoding.ASCII.GetString(Net.Read(s,h[4]))=="example.org","Remote DNS authority");Assert(Net.Read(s,2).SequenceEqual(new byte[]{1,187}),"SOCKS target port");s.Write(new byte[]{5,0,0,1,127,0,0,1,0,0},0,10);}
            }
        });
        try {bool failed=false;try{using(Net.Open(new Route{Kind=kind,Host="127.0.0.1",Port=port},"example.org:443")){}}catch(IOException){failed=true;}Assert(failed==reject,"Proxy negotiation outcome");Assert(server.Wait(6000),"Mock proxy finished");}
        finally{listener.Stop();}
    }
}
}
