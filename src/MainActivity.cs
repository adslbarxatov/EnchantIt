using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

#if DEBUG
[assembly: Application (Debuggable = true)]
#else
[assembly: Application (Debuggable = false)]
#endif

namespace RD_AAOW.Droid
	{
	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "PA detector",
		Icon = "@drawable/launcher_foreground",
		Theme = "@style/SplashTheme",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		/// <param name="savedInstanceState"></param>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			// Отмена темы для splash screen
			base.SetTheme (Resource.Style.MainTheme);

			// Инициализация и запуск
			base.OnCreate (savedInstanceState);
			global::Xamarin.Forms.Forms.Init (this, savedInstanceState);
			global::Xamarin.Essentials.Platform.Init (this, savedInstanceState);

			// Запрет на переход в ждущий режим
			this.Window.AddFlags (WindowManagerFlags.KeepScreenOn);

			LoadApplication (new App ());
			}

		/// <summary>
		/// Запрос разрешений для приложения
		/// </summary>
		public override void OnRequestPermissionsResult (int requestCode, string[] permissions,
			Android.Content.PM.Permission[] grantResults)
			{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult (requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult (requestCode, permissions, grantResults);
			}
		}
	}
