using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CallBridge.Core.Protocol;
using CallBridge.Core.Session;

namespace CallBridge.App;

public sealed class ContactAvatar : INotifyPropertyChanged
{
	ImageBrush? photo;

	public ContactAvatar(Contact contact)
	{
		Contact = contact;
		Initials = Initialize(contact.Name);
	}

	public Contact Contact { get; }

	public string Name => Contact.Name;

	public string Number => Contact.Number;

	public string Initials { get; }

	public bool HasPhoto => photo != null;

	public ImageBrush? Photo
	{
		get => photo;
		set
		{
			photo = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photo)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasPhoto)));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public static string Initialize(string? name)
	{
		var words = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0])));
	}
}

public sealed class ContactPhotoSource
{
	readonly ContactPhotoCache cache;
	readonly Dictionary<string, ImageSource?> decoded = new();
	readonly HashSet<string> requested = [];
	PhoneSession? session;

	public ContactPhotoSource(string? directory)
	{
		cache = new ContactPhotoCache(directory);
	}

	public event Action<string>? PhotoReady;

	public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CallBridge", "photos");

	public void Attach(PhoneSession phone)
	{
		session = phone;
		requested.Clear();
		phone.PhotoReceived += (key, data) => System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => OnReply(key, data));
	}

	public void Detach()
	{
		session = null;
		requested.Clear();
	}

	public ImageSource? Get(string? number)
	{
		var key = PhotoKey.Normalize(number);
		if (key == null)
			return null;
		if (!decoded.TryGetValue(key, out var image) && cache.TryGet(key, out var bytes))
		{
			image = bytes == null ? null : Decode(bytes);
			decoded[key] = image;
		}
		if (session != null && requested.Add(key))
			_ = session.RequestPhotoAsync(key, number!).ContinueWith(_ => requested.Remove(key), TaskContinuationOptions.OnlyOnFaulted);
		return image;
	}

	public void Offer(string number, byte[]? data)
	{
		var key = PhotoKey.Normalize(number);
		if (key == null)
			return;
		cache.Put(key, data);
		decoded[key] = data == null ? null : Decode(data);
		PhotoReady?.Invoke(key);
	}

	void OnReply(string key, byte[]? data)
	{
		cache.Put(key, data);
		decoded[key] = data == null ? null : Decode(data);
		PhotoReady?.Invoke(key);
	}

	public static ImageSource? Decode(byte[] bytes)
	{
		try
		{
			var image = new BitmapImage();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.DecodePixelWidth = 96;
			image.StreamSource = new MemoryStream(bytes);
			image.EndInit();
			image.Freeze();
			return image;
		}
		catch (Exception e) when (e is NotSupportedException or FileFormatException or IOException or InvalidOperationException)
		{
			return null;
		}
	}

	public static ImageBrush Brush(ImageSource image)
	{
		var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
		brush.Freeze();
		return brush;
	}
}
