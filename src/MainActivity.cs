using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
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
	[Activity (Label = "Paranormal activity detector",
		Icon = "@drawable/launcher_foreground",
		Theme = "@style/SplashTheme",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity: global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Принудительная установка масштаба шрифта
		/// </summary>
		/// <param name="base">Существующий набор параметров</param>
		protected override void AttachBaseContext (Context @base)
			{
			if (baseContextOverriden)
				{
				base.AttachBaseContext (@base);
				return;
				}

			Configuration overrideConfiguration = new Configuration ();
			overrideConfiguration = @base.Resources.Configuration;
			overrideConfiguration.FontScale = 0.9f;

			Context context = @base.CreateConfigurationContext (overrideConfiguration);
			baseContextOverriden = true;

			base.AttachBaseContext (context);
			}
		private bool baseContextOverriden = false;

		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
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

			/*#if HUAWEI
						LoadApplication (new App (true));
			#else
						LoadApplication (new App (false));
			#endif*/
			RDAppStartupFlags flags = AndroidSupportX.GetAppStartupFlags (RDAppStartupFlags.Huawei |
				RDAppStartupFlags.CanWriteFiles, this);
			LoadApplication (new App (flags));
			}

		/*/// <summary>
		/// Запрос разрешений для приложения
		/// </summary>
		public override void OnRequestPermissionsResult (int requestCode, string[] permissions,
			Permission[] grantResults)
			{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult (requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult (requestCode, permissions, grantResults);
			}*/
		}
	}
