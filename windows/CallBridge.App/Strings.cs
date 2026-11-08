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
		["MetricsLine"] = ("Delay ~{0} ms · jitter {1} ms · late {2}", "Задержка ~{0} мс · джиттер {1} мс · задержанных {2}"),
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
		["EchoCancel"] = ("Echo cancellation (for speakers)", "Эхоподавление (для колонок)"),
		["Output"] = ("Sound output", "Вывод звука"),
		["Startup"] = ("Startup", "Запуск"),
		["StartWithWindows"] = ("Start with Windows", "Запускать с Windows"),
		["StartWithWindowsTip"] = ("Start CallBridge in the tray when you sign in to Windows", "Запускать CallBridge в трее при входе в Windows"),
		["AutostartError"] = ("Unable to change the startup setting: {0}", "Не удалось изменить настройку запуска: {0}"),
		["AddToStartMenu"] = ("Add to Start menu", "Добавить в меню «Пуск»"),
		["AddToStartMenuTip"] = ("Create a CallBridge shortcut in your Start menu", "Создать ярлык CallBridge в меню «Пуск»"),
		["AddToDesktop"] = ("Add to desktop", "Добавить на рабочий стол"),
		["AddToDesktopTip"] = ("Create a CallBridge shortcut on your desktop", "Создать ярлык CallBridge на рабочем столе"),
		["ShortcutError"] = ("Unable to change the shortcut: {0}", "Не удалось изменить ярлык: {0}"),
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
		["Hold"] = ("Hold", "Удержание"),
		["HoldTip"] = ("Put the call on hold or resume it", "Поставить звонок на удержание или вернуть"),
		["Keypad"] = ("Keypad", "Клавиши"),
		["KeypadTip"] = ("Show the keypad to send tones into the call", "Показать клавиатуру для тоновых сигналов в звонке"),
		["OnHold"] = ("On hold", "На удержании"),
		["HoldRefused"] = ("The network refused to hold this call", "Сеть не разрешила удержание этого звонка"),
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
