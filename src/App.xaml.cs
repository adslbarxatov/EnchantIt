using Android.Net;
using Java.Security;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App: Application
		{
		#region Общие переменные и константы

		// Частоты получения новых зёрен
		private const int generationStepWiFi = 5;
		private const int generationStepKernel = 7;

		// Количество псевдослучайных чисел
		private const uint countOfRN = 0x1000;

		// Верхняя исключённая граница диапазона генерации (нижняя равна нулю)
		private const int rangeOfRN = 0x100;

		// Шаг получения нового зерна
		private const uint newSeedStep = 0x20;

		// Количество суммовых значений, используемых для сбора отклонений
		private const uint countOfSummas = 10;

		// Фактор определения отклонения от нормы.
		// Определён сейчас как сумма десяти максимальных (по практическим наблюдениям) отклонений (~ 1%),
		// возникающих хотя бы в 51% случаев
		private const double factor = 0.01 * countOfSummas * 0.51 * scaleSizeFactor;

		// Минимальное количество срабатываний в ряд, необходимое для сертификации
		private const uint certLimit = 3;

		// Настройки интерфейса
		/*private bool firstStart = true;
		private bool nightMode = false;

		private bool wifiMethod;
		private bool coreMethod;

		private GenerationMethods currentMethod = GenerationMethods.Core;*/

		private bool stopGeneration = false;
		private double oldScale = 0, newScale = 0;

		private RDAppStartupFlags flags;

		// Множитель шкалы для регистратора отклонений ГПСЧ
		private const double scaleSizeFactor = 8.0;

		// Счётчики результатов
		private string[] hiResults = new string[50];
		private string[] certableResults = new string[CertificateBuilder.MaxCertableResultsCount];
		private double[] summaValues = new double[countOfSummas];

		private int countOfMatches = -1;
		private uint countOfEvents = 0;
		private double totalSumma = 0.0;

		/* Именная подпись сертификата
		private string certName = "";*/

		// Списки вариантов
		private List<string> methods = new List<string> ();

		// Вспомогательные переменные
		private ConnectivityManager cm;
		private Random rnd;
		private SecureRandom srnd;

		private const int maxResultsTTMLength = 1000;

		private TalkToMe ttm;

		#endregion

		#region Цветовая схема

		private readonly Color
			solutionMasterBackColor = Color.FromArgb ("#FFE8FF"),
			solutionMasterTextColor = Color.FromArgb ("#202020"),
			solutionFieldBackColor = Color.FromArgb ("#FFD0FF"),

			aboutMasterBackColor = Color.FromArgb ("#F0FFF0"),
			aboutFieldBackColor = Color.FromArgb ("#D0FFD0"),

			progressNormal = Color.FromArgb ("#00C000"),
			progressWarning1 = Color.FromArgb ("#80C000"),
			progressWarning2 = Color.FromArgb ("#C0C000"),
			progressWarning3 = Color.FromArgb ("#FFC000");

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, ttm1Page, ttm2Page;
		private const int pagesCount = 4;
		private Label aboutLabel, measureLabel, resultsLabel,
			space01, space02, resultsTTM1Label, aboutFontSizeField;
		private Label[] pixels = new Label[2];

		private Button startButton, stopButton, shareButton, methodButton,
			certButton, startTTM1Button, methodTTM1Button, resetTTM1Button,
			startTTM2Button, stopTTM2Button, methodTTM2Button, languageButton;
		private ProgressBar[] scale = new ProgressBar[3],
			factorScale = new ProgressBar[2];
		private Editor messageTTM1Editor;

		private List<string> pageVariants = new List<string> ();

		#endregion

		#region Хранилище настроек

		// Первый запуск
		private bool FirstStart
			{
			get
				{
				return RDGenerics.GetSettings (firstStartPar, true);
				}
			set
				{
				RDGenerics.SetSettings (firstStartPar, value);
				}
			}
		private const string firstStartPar = "FirstStart_20";

		// Метод генерации
		private GenerationMethods CurrentMethod
			{
			get
				{
				return (GenerationMethods)RDGenerics.GetSettings (currentMethodPar, (uint)GenerationMethods.Core);
				}
			set
				{
				RDGenerics.SetSettings (currentMethodPar, (uint)value);
				}
			}
		private const string currentMethodPar = "CoreMethod";

		private bool IsMethodWifi
			{
			get
				{
				switch (CurrentMethod)
					{
					case GenerationMethods.WiFi:
					case GenerationMethods.CorePlusWiFi:
						return true;

					default:
						return false;
					}
				}
			}

		private bool IsMethodCore
			{
			get
				{
				switch (CurrentMethod)
					{
					case GenerationMethods.Core:
					case GenerationMethods.CorePlusWiFi:
						return true;

					default:
						return false;
					}
				}
			}

		// Перечисление методов
		private enum GenerationMethods
			{
			Core = 0,
			WiFi = 1,
			CorePlusWiFi = 2
			}

		// Ночной режим
		private bool NightMode
			{
			get
				{
				return RDGenerics.GetSettings (nightModePar, false);
				}
			set
				{
				RDGenerics.SetSettings (nightModePar, value);
				}
			}
		private const string nightModePar = "NightMode";

		// Имя для сертификата
		private string CertificateName
			{
			get
				{
				return RDGenerics.GetSettings (certNamePar, "");
				}
			set
				{
				RDGenerics.SetSettings (certNamePar, value);
				}
			}
		private const string certNamePar = "CertableResultsName";

		// Чат TTM
		private string ResultsTTM
			{
			get
				{
				return RDGenerics.GetSettings (resultsTTMPar, "");
				}
			set
				{
				RDGenerics.SetSettings (resultsTTMPar, value);
				}
			}
		private const string resultsTTMPar = "TTMResults";

		// Имена хранимых параметров
		private const string resultsRegKey = "HiResults";
		private const string certableRegKey = "CertableResults";

		#endregion

		#region Запуск и настройка

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			flags = AndroidSupport.GetAppStartupFlags (RDAppStartupFlags.Huawei | RDAppStartupFlags.CanWriteFiles);

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			solutionPage = AndroidSupport.ApplyPageSettings (new SolutionPage (), "SolutionPage",
				RDLocale.GetText ("SolutionPage"), solutionMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (new AboutPage (), "AboutPage",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), aboutMasterBackColor);
			ttm1Page = AndroidSupport.ApplyPageSettings (new TTM1Page (), "TTM1Page",
				RDLocale.GetText ("TTM1Page"), solutionMasterBackColor);
			ttm2Page = AndroidSupport.ApplyPageSettings (new TTM2Page (), "TTM2Page",
				RDLocale.GetText ("TTM2Page"), solutionMasterBackColor);

			AndroidSupport.SetMasterPage (MainPage, solutionPage, solutionMasterBackColor);

			#region Основная страница

			startButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Start",
				RDDefaultButtons.Start, solutionFieldBackColor, StartGeneration);
			/*startButton.Margin = new Thickness  (3);*/

			stopButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Stop",
				RDDefaultButtons.Stop, solutionFieldBackColor, StopGeneration);
			stopButton.IsVisible = false;
			/*stopButton.Margin = new Thickness  (3);*/

			methodButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Method",
				RDDefaultButtons.Select, solutionFieldBackColor, SelectMethod);
			/*methodButton.Margin = new Thickness  (3);*/

			measureLabel = AndroidSupport.ApplyLabelSettings (solutionPage, "Measure", "",
				RDLabelTypes.HeaderCenter);
			measureLabel.IsVisible = false;

			resultsLabel = AndroidSupport.ApplyLabelSettings (solutionPage, "Results", "",
				RDLabelTypes.DefaultCenter);
			resultsLabel.Padding = resultsLabel.Margin;
			resultsLabel.FontSize *= 1.1;

			for (int i = 0; i < scale.Length; i++)
				{
				scale[i] = (ProgressBar)solutionPage.FindByName ("Scale" + (i + 1).ToString ());
				scale[i].IsVisible = false;
				}
			for (int i = 0; i < factorScale.Length; i++)
				{
				factorScale[i] = (ProgressBar)solutionPage.FindByName ("FactorScale" + (i + 1).ToString ());
				factorScale[i].IsVisible = false;
				factorScale[i].Progress = factor;
				}

			shareButton = AndroidSupport.ApplyButtonSettings (solutionPage, "ShareResults",
				RDDefaultButtons.Share, solutionFieldBackColor, ShareResults);
			/*shareButton.Margin = new Thickness  (3);*/

			certButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Certificate",
				RDDefaultButtons.SpecialFour, solutionFieldBackColor, CreateCertificate);
			/*certButton.Margin = new Thickness  (3);*/

			/*Button nmButton =*/ AndroidSupport.ApplyButtonSettings (solutionPage, "NightMode",
				RDDefaultButtons.NightMode, solutionFieldBackColor, SwitchNightMode);
			/*nmButton.Margin = new Thickness  (3);*/

			AndroidSupport.ApplyButtonSettings (solutionPage, "Menu",
				RDDefaultButtons.Menu, solutionFieldBackColor, SelectPage);
			/*mnButton.Margin = new Thickness  (3);*/

			space01 = AndroidSupport.ApplyLabelSettings (solutionPage, "Space01", RDLocale.RN,
				RDLabelTypes.DefaultCenter);
			space02 = AndroidSupport.ApplyLabelSettings (solutionPage, "Space02", RDLocale.RN,
				RDLabelTypes.DefaultCenter);
			space01.IsVisible = space02.IsVisible = false;

			#endregion

			#region Страница «О программе»

			// Описание приложения
			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, RDLabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualsButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "GenericSettingsLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_GenericSettings),
				RDLabelTypes.HeaderLeft);

			// Управление правами
			Label allowServiceTip;
			Button allowServiceButton;
			if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (aboutPage, "AllowWritingTip",
					RDLocale.GetDefaultText (RDLDefaultTexts.Message_ReadWritePermission), RDLabelTypes.ErrorTip);

				allowServiceButton = AndroidSupport.ApplyButtonSettings (aboutPage, "AllowWritingButton",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Open), aboutFieldBackColor, CallAppSettings, false);
				allowServiceButton.HorizontalOptions = LayoutOptions.Center;
				}
			else
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (aboutPage, "AllowWritingTip",
					" ", RDLabelTypes.Tip);
				allowServiceTip.IsVisible = false;

				allowServiceButton = AndroidSupport.ApplyButtonSettings (aboutPage, "AllowWritingButton",
					" ", aboutFieldBackColor, null, false);
				allowServiceButton.IsVisible = false;
				}

			// Кнопки управления
			AndroidSupport.ApplyLabelSettings (aboutPage, "RestartTipLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Message_RestartRequired),
				RDLabelTypes.Tip);

			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage),
				RDLabelTypes.DefaultLeft);
			languageButton = AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector",
				RDLocale.LanguagesNames[(int)RDLocale.CurrentLanguage],
				aboutFieldBackColor, SelectLanguage_Clicked, false);

			AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceFontSize),
				RDLabelTypes.DefaultLeft);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeInc",
				RDDefaultButtons.Increase, aboutFieldBackColor, FontSizeButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeDec",
				RDDefaultButtons.Decrease, aboutFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeField",
				" ", RDLabelTypes.DefaultCenter);

			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpHeaderLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				RDLabelTypes.HeaderLeft);
			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetEncoding (RDEncodings.UTF8).
				GetString ((byte[])RD_AAOW.Properties.Resources.ResourceManager.
				GetObject (RDLocale.GetHelpFilePath ())), RDLabelTypes.SmallLeft);

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страница «Поговори со мной»

			startTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Start",
				RDDefaultButtons.Up, solutionFieldBackColor, StartTalking);
			/*startTTM1Button.Margin = new Thickness  (3);*/

			methodTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Method",
				RDDefaultButtons.Select, solutionFieldBackColor, SelectMethod);
			/*methodTTM1Button.Margin = new Thickness  (3);*/

			resetTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Reset",
				RDDefaultButtons.Delete, solutionFieldBackColor, ResetTheChat);
			/*resetTTM1Button.Margin = new Thickness  (3);*/

			AndroidSupport.ApplyButtonSettings (ttm1Page, "NightMode", RDDefaultButtons.NightMode,
				solutionFieldBackColor, SwitchNightMode);

			resultsTTM1Label = AndroidSupport.ApplyLabelSettings (ttm1Page, "Results", "", RDLabelTypes.Field,
				solutionMasterBackColor);
			messageTTM1Editor = AndroidSupport.ApplyEditorSettings (ttm1Page, "Message", solutionFieldBackColor,
				Keyboard.Text, 100, "", null, true);

			#endregion

			#region Страница «Покажи скрытое»

			startTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Start",
				RDDefaultButtons.Start, solutionFieldBackColor, StartTTM);
			/*startTTM2Button.Margin = new Thickness  (3);*/

			stopTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Stop",
				RDDefaultButtons.Stop, solutionFieldBackColor, StopTTM);
			stopTTM2Button.IsVisible = false;
			/*stopTTM2Button.Margin = new Thickness  (3);*/

			methodTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Method",
				RDDefaultButtons.Select, solutionFieldBackColor, SelectMethod);
			AndroidSupport.ApplyButtonSettings (ttm2Page, "NightMode", RDDefaultButtons.NightMode,
				solutionFieldBackColor, SwitchNightMode);
			/*methodTTM2Button.Margin = new Thickness  (3);*/

			for (int i = 0; i < pixels.Length; i++)
				{
				pixels[i] = AndroidSupport.ApplyLabelSettings (ttm2Page, "Pixels" + (i + 1).ToString (), " ",
					 RDLabelTypes.Field, solutionFieldBackColor);
				pixels[i].TextType = TextType.Html;
				pixels[i].HorizontalOptions = new LayoutOptions (LayoutAlignment.Center, false);
				}

			#endregion

			// Инициализация состояния приложения
			cm = (ConnectivityManager)Android.App.Application.Context.GetSystemService (Android.App.Service.ConnectivityService);

			#region Получение сохранённых настроек

			for (int i = 0; i < hiResults.Length; i++)
				hiResults[i] = RDGenerics.GetAppRegistryValue (resultsRegKey + i.ToString ());

			for (int i = 0; i < certableResults.Length; i++)
				certableResults[i] = RDGenerics.GetAppRegistryValue (certableRegKey + i.ToString ());
			resultsTTM1Label.Text = ResultsTTM;

			// Обработка сохранённых настроек
			if (FirstStart)
				AndroidSupport.SetCurrentPage (aboutPage, aboutMasterBackColor);

			SwitchNightMode (null, null);

			#endregion

			// Первичная настройка поля результатов
			UpdateHiResults ();
			countOfMatches = 0; // Признак отмены обновления при отсутствии результатов

			// Отображение подсказок первого старта
			ShowTips ();
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowTips ()
			{
			// Контроль XPUN
			if (!flags.HasFlag (RDAppStartupFlags.Huawei))
				await AndroidSupport.XPUNLoop ();

			// Защита
			if (FirstStart)
				{
				// Требование принятия Политики
				await AndroidSupport.PolicyLoop ();
				/*RDGenerics.SetAppSettingsValue (firstStartRegKey, ProgramDescription.AssemblyVersion);*/
				FirstStart = false;

				await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip00"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

				await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip02"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				}
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			stopGeneration = true;
			/*try
				{
				RDGenerics.SetAppSettingsValue (currentMethodRegKey, ((uint)currentMethod).ToString ());
				RDGenerics.SetAppSettingsValue (nightModeRegKey, nightMode ? "1" : "0");
				RDGenerics.SetAppSettingsValue (certNameRegKey, certName);*/

			for (int i = 0; i < hiResults.Length; i++)
				RDGenerics.SetAppRegistryValue (resultsRegKey + i.ToString (), hiResults[i]);
			for (int i = 0; i < certableResults.Length; i++)
				RDGenerics.SetAppRegistryValue (certableRegKey + i.ToString (), certableResults[i]);

			/*RDGenerics.SetAppSettingsValue (resultsTTMRegKey, resultsTTM1Label.Text);
			*/
			ResultsTTM = resultsTTM1Label.Text;
			/*}
			catch { }*/
			}

		// Вызов настроек приложения (для Android 12 и выше)
		private void CallAppSettings (object sender, EventArgs e)
			{
			AndroidSupport.CallAppSettings ();
			}

		#endregion

		#region О приложении

		// Выбор языка приложения
		private async void SelectLanguage_Clicked (object sender, EventArgs e)
			{
			languageButton.Text = await AndroidSupport.CallLanguageSelector ();
			}

		// Вызов справочных материалов
		private async void ReferenceButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					AndroidSupport.MasterFontSize += 0.5;
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					AndroidSupport.MasterFontSize -= 0.5;
				}

			aboutFontSizeField.Text = AndroidSupport.MasterFontSize.ToString ("F1");
			aboutFontSizeField.FontSize = AndroidSupport.MasterFontSize;
			}

		#endregion

		#region Рабочая зона основной страницы

		// Метод извлекает зерно из данных сети и текущего состояния таймера.
		// Возвращает 0, если никакая сеть недоступна
		private int GetSeed ()
			{
			// Задержка между вызовами
			Thread.Sleep (generationStepWiFi);

			// Запрос состояния текущей сети
			if (cm.ActiveNetwork == null)
				return 0;

			NetworkCapabilities nc = cm.GetNetworkCapabilities (cm.ActiveNetwork);
			if (nc == null)
				return 0;

			if (!nc.HasTransport (TransportType.Wifi))
				return 0;

			// Значение теперь соответствует типичному диапазону dBm от -50 до -100
			double sd = Math.Pow ((double)nc.SignalStrength / 32.0, 10.0) * (double)(DateTime.Now.Ticks & int.MaxValue);
			// max sd = 2^20 * 2^31 = 2^51
			int seed = (int)((long)Math.Floor (sd) & int.MaxValue);
			nc.Dispose ();

			// Успешно (защита от совпадений)
			if (seed == 0)
				return (int)(DateTime.Now.Ticks & int.MaxValue);

			return seed;
			}

		// Запуск и остановка генерации
		private async void StartGeneration (object sender, EventArgs e)
			{
			ChangeButtonsState (true, false);

			AndroidSupport.ShowBalloon
				(RDLocale.GetText ("StartingGeneration") +
				RDLocale.RN + "(" + RDLocale.GetText ("Method" + ((uint)CurrentMethod).ToString ("D2")) + ")",
				true);

			/*coreMethod = (currentMethod == GenerationMethods.Core) || (currentMethod == GenerationMethods.CorePlusWiFi);
			wifiMethod = (currentMethod == GenerationMethods.WiFi) || (currentMethod == GenerationMethods.CorePlusWiFi);*/

			// Запуск петли
			while (await GeneratePRNG ())
				;
			}

		// Обновление состояния кнопок
		private async void ChangeButtonsState (bool GenerationStart, bool TTM)
			{
			// Общие
			space01.IsVisible = space02.IsVisible = measureLabel.IsVisible =
				stopTTM2Button.IsVisible =
				GenerationStart;
			stopButton.IsVisible = !TTM && GenerationStart;
			stopTTM2Button.IsVisible = TTM && GenerationStart;

			if (!TTM)
				{
				for (int i = 0; i < scale.Length; i++)
					scale[i].IsVisible = GenerationStart;
				for (int i = 0; i < factorScale.Length; i++)
					factorScale[i].IsVisible = GenerationStart;
				}

			// Частные
			if (GenerationStart)
				{
				stopGeneration = false;
				startButton.IsVisible = resultsLabel.IsVisible = shareButton.IsVisible = methodButton.IsVisible =
					certButton.IsVisible = false;
				startTTM1Button.IsEnabled = methodTTM1Button.IsVisible = resetTTM1Button.IsVisible =
					startTTM2Button.IsVisible = methodTTM2Button.IsVisible = false;
				}
			else
				{
				startButton.IsVisible = resultsLabel.IsVisible = methodButton.IsVisible = true;
				startTTM1Button.IsEnabled = methodTTM1Button.IsVisible = resetTTM1Button.IsVisible =
					startTTM2Button.IsVisible = methodTTM2Button.IsVisible = true;
				shareButton.IsVisible = !string.IsNullOrWhiteSpace (resultsLabel.Text);

				// Открытие доступа к сертификату
				certButton.IsVisible = !string.IsNullOrWhiteSpace (certableResults[0]);
				if (!TTM && (CertificateName == "") && certButton.IsVisible)
					{
					await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip01"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
					CertificateName = " ";
					}
				}
			}

		private void StopGeneration (object sender, EventArgs e)
			{
			stopGeneration = true;
			}

		// Метод-генератор ПСЧ, выполняющий расчёт среднего значения и его вывод в UI на основе сигнала сети
		private async Task<bool> GeneratePRNG ()
			{
			// Признак остановки
			if (stopGeneration)
				{
				ChangeButtonsState (false, false);
				return false;
				}

			// Запрос зерна и инициализация
			int seed;
			if (IsMethodWifi)
				{
				seed = await Task.Run<int> (GetSeed);
				if (seed == 0)
					{
					AndroidSupport.ShowBalloon (RDLocale.GetText ("ConnectionLost"), true);

					ChangeButtonsState (false, false);
					return false;
					}

				rnd = new Random (seed);
				}

			if (IsMethodCore)
				{
				if (srnd == null)
					srnd = new SecureRandom ();
				}

			// Расчёт суммы
			uint summa = 0;

			for (int i = 1; i <= countOfRN; i++)
				{
				// Обновление суммы
				if (IsMethodWifi)
					summa += (uint)rnd.Next (rangeOfRN);
				if (IsMethodCore)
					summa += (uint)srnd.NextInt (rangeOfRN);

				// Зазернение
				if (i % newSeedStep == 0)
					{
					if (IsMethodWifi)
						{
						seed = await Task.Run<int> (GetSeed);
						rnd = new Random (seed);
						}

					if (IsMethodCore)
						{
						await Task<bool>.Run (WaitABit);

						srnd.Dispose ();
						srnd = new SecureRandom ();
						}

					// Мягкое движение шкалы
					UpdateScale (i);
					}
				}

			// Обновление суммовых счётчиков
			UpdateCounters ((CurrentMethod == GenerationMethods.CorePlusWiFi) ? summa / 2 : summa);
			return true;
			}

		// Заглушка для создания искусственной паузы
		private bool WaitABit ()
			{
			Thread.Sleep (generationStepKernel);
			return true;
			}

		// Метод обновляет положение шкалы
		private void UpdateScale (int CurrentPosition)
			{
			for (int i = 0; i < scale.Length; i++)
				{
				if (i == 0)
					scale[i].Progress = oldScale + CurrentPosition * (newScale - oldScale) / countOfRN;
				else
					scale[i].Progress = scale[0].Progress;
				}
			}

		// Метод обновляет суммовые счётчики
		private void UpdateCounters (uint Summa)
			{
			// Обновление сумм
			oldScale = newScale;
			newScale = 0.0;

			for (int i = 0; i < summaValues.Length - 1; i++)
				{
				summaValues[i] = summaValues[i + 1];
				newScale += summaValues[i];
				}
			summaValues[summaValues.Length - 1] = Math.Abs (0.5 - (double)Summa / countOfRN / (rangeOfRN - 1));
			newScale += summaValues[summaValues.Length - 1];
			newScale *= scaleSizeFactor;

			measureLabel.Text = string.Format (RDLocale.GetText ("MethodName"), (uint)CurrentMethod + 1,
				(newScale * 1000.0).ToString ("F00"), countOfEvents);

			// Обновление цвета шкалы
			if (newScale > factor)
				{
				countOfMatches++;
				totalSumma += (newScale * 1000.0);

				if (scale[0].ProgressColor == progressNormal)
					scale[0].ProgressColor = measureLabel.TextColor = progressWarning1;
				else if (scale[0].ProgressColor == progressWarning1)
					scale[0].ProgressColor = measureLabel.TextColor = progressWarning2;
				else
					scale[0].ProgressColor = measureLabel.TextColor = progressWarning3;
				}
			else
				{
				UpdateHiResults ();
				scale[0].ProgressColor = measureLabel.TextColor = progressNormal;
				}

			for (int i = 1; i < scale.Length; i++)
				scale[i].ProgressColor = scale[0].ProgressColor;
			}

		// Метод обновляет наивысшие результаты
		private void UpdateHiResults ()
			{
			// Спуск предыдущих вниз по массиву и добавление нового
			if (countOfMatches > 0)
				{
				// Обновление списка результатов
				for (int i = hiResults.Length - 1; i > 0; i--)
					hiResults[i] = hiResults[i - 1];

				hiResults[0] = string.Format (RDLocale.GetText ("MethodName"), (uint)CurrentMethod + 1,
					(totalSumma / countOfMatches).ToString ("F00") + " [" + DateTime.Now.ToString () + "]",
					countOfMatches);

				// Обновление списка сертифицируемых результатов
				if (countOfMatches >= certLimit)
					{
					countOfEvents++;

					for (int i = certableResults.Length - 1; i > 0; i--)
						certableResults[i] = certableResults[i - 1];

					certableResults[0] = hiResults[0];
					}

				// Сброс состояния
				countOfMatches = 0;
				totalSumma = 0.0;
				}
			else if (countOfMatches == 0)
				{
				return;
				}

			// Отображение
			if (!string.IsNullOrWhiteSpace (hiResults[0]))
				{
				resultsLabel.Text = RDLocale.GetText ("HiResults");

				for (int i = 0; i < hiResults.Length; i++)
					{
					if (!string.IsNullOrWhiteSpace (hiResults[i]))
						resultsLabel.Text += (RDLocale.RN + hiResults[i]);
					else
						break;
					}

				// При изменении состояния во время генерации появляться не должна
				shareButton.IsVisible = startButton.IsVisible;
				}
			else
				{
				shareButton.IsVisible = false;
				}

			if (!string.IsNullOrWhiteSpace (certableResults[0]))
				certButton.IsVisible = startButton.IsVisible;
			else
				certButton.IsVisible = false;
			}

		// Метод оформляет и отправляет результаты
		private async void ShareResults (object sender, EventArgs e)
			{
			await Share.RequestAsync (new ShareTextRequest
				{
				Text = aboutLabel.Text + RDLocale.RNRN + resultsLabel.Text,
				Title = ProgramDescription.AssemblyVisibleName
				});
			}

		// Выбор метода генерации
		private async void SelectMethod (object sender, EventArgs e)
			{
			if (methods.Count < 1)
				methods = new List<string> {
					RDLocale.GetText ("Method00"),
					RDLocale.GetText ("Method01"),
					RDLocale.GetText ("Method02")
					};

			int res = await AndroidSupport.ShowList (RDLocale.GetText ("MethodSelect"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), methods);

			if (res < 0)
				return;

			CurrentMethod = (GenerationMethods)res;
			}

		// Включение / выключение ночного режима
		private void SwitchNightMode (object sender, EventArgs e)
			{
			if (e != null)
				NightMode = !NightMode;

			if (NightMode)
				{
				solutionPage.BackgroundColor =
					ttm1Page.BackgroundColor = ttm2Page.BackgroundColor = messageTTM1Editor.TextColor =
					resultsTTM1Label.BackgroundColor =
					solutionMasterTextColor;
				resultsLabel.TextColor =
					resultsTTM1Label.TextColor = messageTTM1Editor.BackgroundColor =
					solutionMasterBackColor;

				for (int i = 0; i < factorScale.Length; i++)
					factorScale[i].ProgressColor = solutionFieldBackColor;
				}

			else
				{
				solutionPage.BackgroundColor =
					ttm1Page.BackgroundColor = ttm2Page.BackgroundColor = messageTTM1Editor.TextColor =
					resultsTTM1Label.BackgroundColor =
					solutionMasterBackColor;
				resultsLabel.TextColor =
					resultsTTM1Label.TextColor = messageTTM1Editor.BackgroundColor =
					solutionMasterTextColor;

				for (int i = 0; i < factorScale.Length; i++)
					factorScale[i].ProgressColor = solutionMasterTextColor;
				}
			}

		// Метод формирует изображение сертификата
		private async void CreateCertificate (object sender, EventArgs e)
			{
			// Защита
			if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
					(RDLDefaultTexts.Message_ReadWritePermission),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			// Сбор сведений
			if (string.IsNullOrWhiteSpace (CertificateName))
				{
				CertificateName = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
					RDLocale.GetText ("CertNameHelp"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Accept),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
					CertificateBuilder.MaxCertificateNameLength, Keyboard.Text);

				if (string.IsNullOrWhiteSpace (CertificateName) || (CertificateName.Trim ().Length < 3))
					{
					CertificateName = " ";
					return;
					}
				}

			string certData = certableResults[0];
			for (int i = 1; i < certableResults.Length; i++)
				certData += ("\n" + certableResults[i].Replace ("\r", "").Replace ("\n", ", "));

			// Запуск сборки
			certButton.IsVisible = false;
			CertificateBuilder cb = new CertificateBuilder (CertificateName, certData);
			if (!cb.CertificateCreated)
				{
				AndroidSupport.ShowBalloon ("Unexpected failure in image generation process. Debug is required",
					true);
				cb.Dispose ();
				return;
				}

			// Сохранение
			string msg = "";
			switch (await cb.SaveToFile ())
				{
				case -1:
					msg = RDLocale.GetText ("SaveFileFailure");
					break;

				case 1:
					msg = RDLocale.GetText ("SaveFileSuccess");
					break;
				}

			if (msg != "")
				AndroidSupport.ShowBalloon (msg, true);
			cb.Dispose ();
			}

		// Выбор текущей страницы
		private async void SelectPage (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pageVariants.Count < 1)
				{
				pageVariants = new List<string> ()
					{
					RDLocale.GetText ("TTM1Page"),
					RDLocale.GetText ("TTM2Page"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
					};
				}

			int res = await AndroidSupport.ShowList (RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pageVariants);
			if (res < 0)
				return;

			// Вызов
			switch (res)
				{
				case 0:
					AndroidSupport.SetCurrentPage (ttm1Page, solutionMasterBackColor);
					break;

				case 1:
					AndroidSupport.SetCurrentPage (ttm2Page, solutionMasterBackColor);
					break;

				case 2:
					AndroidSupport.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;
				}
			}

		#endregion

		#region Рабочая зона Talk to me

		// Запуск и остановка генерации
		private async void StartTalking (object sender, EventArgs e)
			{
			// Инициализация
			ChangeButtonsState (true, true);

			if (CurrentMethod == GenerationMethods.Core)
				{
				ttm = new TalkToMe ();
				}
			else
				{
				if (!await InitTalkingFromWifi ())
					return;

				ttm = new TalkToMe (GetSeed, CurrentMethod == GenerationMethods.CorePlusWiFi);
				}

			// Запрос предложения
			resultsTTM1Label.Text = ("▲ " + messageTTM1Editor.Text + RDLocale.RNRN) + resultsTTM1Label.Text;
			/*resultsTTM1Label.Text += (RDLocale.RNRN + "▲ " + messageTTM1Editor.Text);*/
			messageTTM1Editor.Text = "";

			AndroidSupport.ShowBalloon (RDLocale.GetText ("GettingSentence") +
				RDLocale.RN + "(" +
				RDLocale.GetText ("Method" + ((uint)CurrentMethod).ToString ("D2")) + ")", true);

			string sentence = await Task.Run<string> (ttm.GetNextSentence);
			resultsTTM1Label.Text = ("▼ " + sentence + RDLocale.RNRN) + resultsTTM1Label.Text;
			/*resultsTTM1Label.Text += (RDLocale.RNRN + "▼ " + sentence);*/

			if (resultsTTM1Label.Text.Length > maxResultsTTMLength)
				resultsTTM1Label.Text = resultsTTM1Label.Text.Substring (resultsTTM1Label.Text.Length -
					maxResultsTTMLength, maxResultsTTMLength);
			messageTTM1Editor.Focus ();

			// Завершение
			ChangeButtonsState (false, true);
			}

		// Метод-генератор ПСЧ на основе сигнала сети
		private async Task<bool> InitTalkingFromWifi ()
			{
			// Защита
			int seed = await Task.Run<int> (GetSeed);
			if (seed == 0)
				{
				AndroidSupport.ShowBalloon (RDLocale.GetText ("ConnectionLost"), true);

				ChangeButtonsState (false, true);
				return false;
				}

			return true;
			}

		// Метод сбрасывает чат
		private async void ResetTheChat (object sender, EventArgs e)
			{
			if (await AndroidSupport.ShowMessage (RDLocale.GetText ("ChatResetRequest"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel)))
				resultsTTM1Label.Text = "";
			}

		#endregion

		#region Рабочая зона Show it to me

		// Запуск и остановка генерации
		private async void StartTTM (object sender, EventArgs e)
			{
			ChangeButtonsState (true, true);

			AndroidSupport.ShowBalloon (RDLocale.GetText ("StartingGeneration") +
				RDLocale.RN +
				"(" + RDLocale.GetText ("Method" + ((uint)CurrentMethod).ToString ("D2")) + ")",
				true);

			// Инициализация потока
			if (CurrentMethod == GenerationMethods.Core)
				{
				ttm = new TalkToMe ();
				}
			else
				{
				if (!await InitTalkingFromWifi ())
					return;

				ttm = new TalkToMe (GetSeed, CurrentMethod == GenerationMethods.CorePlusWiFi);
				}

			// Запуск
			while (await PRNGForTTM ())
				;
			}

		private void StopTTM (object sender, EventArgs e)
			{
			stopGeneration = true;
			}

		// Методы-генераторы ПСЧ
		private async Task<bool> PRNGForTTM ()
			{
			// Признак остановки
			if (stopGeneration)
				{
				ChangeButtonsState (false, true);
				return false;
				}

			// Обновление рисунка
			List<string> res = await Task<string>.Run (ttm.GetNextLights);
			for (int i = 0; i < pixels.Length; i++)
				pixels[i].Text = res[i];

			if (CurrentMethod != GenerationMethods.CorePlusWiFi)
				await Task<bool>.Run (WaitMore);
			else
				await Task<bool>.Run (WaitABit);

			return true;
			}

		// Заглушка для создания паузы для рисунка
		private bool WaitMore ()
			{
			Thread.Sleep (1000);
			return true;
			}

		#endregion
		}
	}
