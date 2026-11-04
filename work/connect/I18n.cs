using System;
using System.Collections.Generic;

namespace AntigravityConnect {
public static class L {
    public const string Product="Antigravity Connector";
    public static string Language="ru";
    static readonly Dictionary<string,string> English=new Dictionary<string,string> {
        {"Antigravity через ваш VPN","Antigravity through your VPN"},
        {"Настройки подключения","Connection settings"},
        {"Использовать мой VPN","Use my VPN"},
        {"Разрешить подключение через ваш VPN","Allow connections through your VPN"},
        {"Системный туннель","System tunnel"},
        {"Поддержка VPN в режиме TUN","Support VPNs running in TUN mode"},
        {"Ярлыки на рабочем столе","Desktop shortcuts"},
        {"Ярлык на рабочем столе","Desktop shortcut"},
        {"Настройки Antigravity Connector","Antigravity Connector settings"},
        {"Antigravity и настройки коннектора","Antigravity and connector settings"},
        {"Открывайте Antigravity обычным ярлыком. VPN оставляйте включённым.","Open Antigravity as usual. Keep your VPN connected."},
        {"Подключите VPN и разрешите его использование ниже.","Connect your VPN and allow access below."},
        {"Требуется внимание","Needs attention"},
        {"Проверяем подключение","Checking connection"},
        {"Помощник включён","Connector enabled"},
        {"Готов к настройке","Ready to set up"},
        {"Один раз включите. Дальше — обычный запуск Antigravity.","Enable once. Then open Antigravity as usual."},
        {"Разрешаю использовать мой подключённый VPN","Allow access to my connected VPN"},
        {"Поддерживать VPN в режиме системного туннеля (TUN)","Support VPNs using a system tunnel (TUN)"},
        {"Создать ярлыки приложения и коннектора","Create desktop shortcuts for the app and connector"},
        {"Автопоиск HTTP / SOCKS5-прокси и TUN. VPN остаётся вашим.\r\nПомощник запускается вместе с приложением через его ярлыки.","Automatically finds HTTP / SOCKS5 proxies and TUN connections.\r\nThe helper starts with Antigravity through its shortcuts."},
        {"Подключение включено. VPN должен быть подключён.","Connector enabled. Keep your VPN connected."},
        {"Подключите VPN, поставьте галочку и нажмите «Включить».","Connect your VPN, grant permission, then click Enable."},
        {"Открыть Antigravity","Open Antigravity"},{"Включить","Enable"},{"Проверить VPN","Check VPN"},{"Отключить","Disable"},
        {"Сохранить и включить","Save and enable"},
        {"Поставьте галочку, чтобы разрешить использование вашего VPN.","Select the permission checkbox to allow use of your VPN."},
        {"Сначала разрешите использование подключённого VPN.","First allow access to your connected VPN."},
        {"Проверяем VPN и настраиваем ярлыки…","Checking your VPN and setting up shortcuts…"},
        {"Проверяем доступ к серверам Google…","Checking access to Google servers…"},
        {"Готово · {0}. Дальше открывайте обычный ярлык Antigravity.","Ready · {0}. Open Antigravity using its usual shortcut."},
        {"Соединение исправно · {0}. Ответ модели проверяется в приложении.","Connection OK · {0}. Test model responses inside the app."},
        {"Отключено. Прежние ярлыки восстановлены. Текущий сеанс завершится обычным образом.","Disabled. Original shortcuts restored. The current session can finish normally."},
        {"Нужно разрешение на использование подключённого VPN.","Permission to use your connected VPN is required."},
        {"Соединение закрыто","Connection closed"},{"Слишком большой заголовок прокси","Proxy response header is too large"},
        {"Прокси не отвечает","The proxy is not responding"},{"VPN-прокси отклонил соединение","The VPN proxy rejected the connection"},
        {"SOCKS5 требует неподдерживаемую авторизацию","This SOCKS5 proxy requires unsupported authentication"},
        {"Имя сервера слишком длинное","The server name is too long"},{"SOCKS5 не смог подключиться","SOCKS5 could not connect"},
        {"Некорректный ответ SOCKS5","Invalid SOCKS5 response"},{"Неверный запрос прокси","Invalid proxy request"},
        {"Неподдерживаемый запрос","Unsupported request"},
        {"Подключите VPN и повторите запуск. Поддерживаются локальные HTTP/SOCKS5-прокси и распознаваемые TUN-адаптеры.","Connect your VPN and try again. Local HTTP/SOCKS5 proxies and recognized TUN adapters are supported."},
        {"Сервер Google не ответил","The Google server did not respond"},
        {"Не удалось определить путь установки.","Could not resolve the installation folder."},
        {"Antigravity не найден. Сначала установите приложение.","Antigravity was not found. Install the app first."},
        {"Обновлённая версия Antigravity требует проверки совместимости.","This Antigravity version needs a compatibility check."},
        {"Системный туннель VPN","System VPN tunnel"},
        {"Антигравити Коннектор — настройки подключения","Antigravity Connector — connection settings"},
        {"Antigravity с вашим VPN","Antigravity with your VPN"}
    };
    public static string Text(string text) {
        if(Language!="en" || text==null)return text;
        string translated;if(English.TryGetValue(text,out translated))return translated;
        foreach(var pair in new[]{new[]{"Системный прокси · порт ","System proxy · port "},new[]{"HTTP-прокси · порт ","HTTP proxy · port "},new[]{"SOCKS5-прокси · порт ","SOCKS5 proxy · port "}})
            if(text.StartsWith(pair[0],StringComparison.Ordinal))return pair[1]+text.Substring(pair[0].Length);
        return text;
    }
    public static string Format(string template,params object[] args) {
        if(args.Length==0)return Text(template);
        object[] translated=new object[args.Length];for(int i=0;i<args.Length;i++)translated[i]=args[i] is string?Text((string)args[i]):args[i];
        return String.Format(Text(template),translated);
    }
}
}
