using System.Globalization;
using System.Windows.Markup;

namespace CallBridge.App;

/// <summary>UI texts in English and Russian, picked by the Windows display language.</summary>
public static class Strings
{
	static readonly bool Russian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

	static readonly Dictionary<string, (string En, string Ru)> Table = new()
	{
		["WaitingPhone"] = ("Phone not connected", "Телефон не подключён"),
		["ConnectedTo"] = ("Connected to {0}", "Подключён: {0}"),
		["ScanTitle"] = ("Connect the phone", "Подключите телефон"),
		["ScanHint"] = ("Open CallBridge on the phone and scan this code", "Откройте CallBridge на телефоне и отсканируйте этот код"),
		["QrName"] = ("Pairing QR code", "QR-код для подключения"),
		["NewCode"] = ("New code", "Новый код"),
		["NoCall"] = ("No call", "Нет звонка"),
		["WaitingCall"] = ("Incoming calls appear here", "Входящие появятся здесь"),
		["NameOrNumber"] = ("Name or number", "Имя или номер"),
		["Call"] = ("Call", "Позвонить"),
		["Microphone"] = ("Microphone", "Микрофон"),
		["MicMeter"] = ("Microphone level", "Уровень микрофона"),
		["Gain"] = ("Gain", "Усиление"),
		["Output"] = ("Sound output", "Вывод звука"),
		["Volume"] = ("Volume", "Громкость"),
		["MuteMic"] = ("Mute", "Без микрофона"),
		["MuteTip"] = ("Mute the microphone (Ctrl+M)", "Выключить микрофон (Ctrl+M)"),
		["MuteSound"] = ("Sound off", "Без звука"),
		["SoundTip"] = ("Turn off the caller's sound (Ctrl+D)", "Выключить звук собеседника (Ctrl+D)"),
		["Accept"] = ("Accept", "Принять"),
		["AcceptTip"] = ("Accept (Ctrl+Enter)", "Принять (Ctrl+Enter)"),
		["Decline"] = ("Decline", "Отклонить"),
		["End"] = ("End", "Завершить"),
		["EndTip"] = ("End the call (Ctrl+E)", "Завершить звонок (Ctrl+E)"),
		["ToPhone"] = ("To phone", "На телефон"),
		["ToPc"] = ("To PC", "На ПК"),
		["UnknownNumber"] = ("Unknown number", "Неизвестный номер"),
		["IncomingCall"] = ("Incoming call", "Входящий звонок"),
		["Calling"] = ("Calling…", "Вызов…"),
		["InCall"] = ("In call", "Разговор"),
		["OnHold"] = ("On hold", "На удержании"),
		["InCallOnPhone"] = ("In call · sound on the phone", "Разговор · звук на телефоне"),
		["MoveToPhone"] = ("Move to phone", "Перевести на телефон"),
		["MoveToPc"] = ("Move to PC", "Перевести на ПК"),
		["AudioUnavailable"] = ("Call sound unavailable: {0}", "Звук звонка недоступен: {0}"),
		["AudioDeviceError"] = ("Unable to use the sound device: {0}", "Не удалось использовать аудиоустройство: {0}"),
		["NoSuchContact"] = ("No matching contact. Enter a number or choose a contact from the list.", "Такого контакта нет. Введите номер или выберите контакт из списка."),
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
