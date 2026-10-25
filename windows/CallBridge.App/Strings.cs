using System.Globalization;
using System.Windows.Markup;

namespace CallBridge.App;

/// <summary>UI texts in English and Russian, picked by the Windows display language.</summary>
public static class Strings
{
	static readonly bool Russian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

	static readonly Dictionary<string, (string En, string Ru)> Table = new()
	{
		["WaitingPhone"] = ("Waiting for phone", "Жду телефон"),
		["ConnectedTo"] = ("Connected · {0}", "На связи · {0}"),
		["ScanHint"] = ("Open CallBridge on your phone and scan the code", "Откройте CallBridge на телефоне и отсканируйте код"),
		["NewCode"] = ("New code", "Новый код"),
		["NoCall"] = ("No call", "Нет звонка"),
		["WaitingCall"] = ("Waiting for a call", "Жду звонка"),
		["NameOrNumber"] = ("Name or number", "Имя или номер"),
		["Microphone"] = ("MICROPHONE", "МИКРОФОН"),
		["Output"] = ("OUTPUT", "ВЫВОД"),
		["MuteMic"] = ("Mute mic", "Выкл. микрофон"),
		["MuteSound"] = ("Mute sound", "Выкл. звук"),
		["UnknownNumber"] = ("Unknown number", "Неизвестный номер"),
		["IncomingCall"] = ("Incoming call", "Входящий звонок"),
		["Calling"] = ("Calling…", "Вызов…"),
		["InCall"] = ("In call", "Разговор"),
		["OnHold"] = ("On hold", "На удержании"),
		["AudioUnavailable"] = ("Call audio unavailable: {0}", "Звук звонка недоступен: {0}"),
		["AudioDeviceError"] = ("Audio device error: {0}", "Ошибка аудиоустройства: {0}"),
		["NoSuchContact"] = ("No such contact. Type a number or pick a contact from the list", "Нет такого контакта. Введите номер или выберите контакт из списка"),
		["PortBusy"] = ("Port {0} is busy: {1}", "Порт {0} занят: {1}"),
		["TrayOpen"] = ("Open CallBridge", "Открыть CallBridge"),
		["TrayQuit"] = ("Quit", "Выход"),
		["TrayStillRunning"] = ("Still running in the tray, calls will be shown here", "Работаю в трее, звонки будут видны здесь"),
	};

	public static string Get(string key) => Russian ? Table[key].Ru : Table[key].En;

	public static string Format(string key, params object[] args) => string.Format(Get(key), args);
}

/// <summary>XAML access to <see cref="Strings"/>: Text="{local:L NoCall}".</summary>
public sealed class LExtension(string key) : MarkupExtension
{
	public override object ProvideValue(IServiceProvider serviceProvider) => Strings.Get(key);
}
