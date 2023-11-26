using Android.Net;
using Java.Security;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
		// возникающих хотя бы в 55% случаев
		private const double factor = 0.01 * countOfSummas * 0.55 * scaleSizeFactor;

		// Минимальное количество срабатываний в ряд, необходимое для сертификации
		private const uint certLimit = 3;

		// Имена хранимых параметров
		private const string firstStartRegKey = "FirstStart_20";
		private const string currentMethodRegKey = "CoreMethod";
		private const string nightModeRegKey = "NightMode";
		private const string resultsRegKey = "HiResults";
		private const string certableRegKey = "CertableResults";
		private const string certNameRegKey = "CertableResultsName";

		// Настройки интерфейса
		private bool firstStart = true;
		private bool nightMode = false;

		private bool wifiMethod;
		private bool coreMethod;

		private GenerationMethods currentMethod = GenerationMethods.Core;
		private enum GenerationMethods
			{
			Core = 0,
			WiFi = 1,
			CorePlusWiFi = 2
			}

		private bool stopGeneration = false;
		private double oldScale = 0, newScale = 0;

		// Множитель шкалы для регистратора отклонений ГПСЧ
		private const double scaleSizeFactor = 8.0;

		// Счётчики результатов
		private string[] hiResults = new string[50];
		private string[] certableResults = new string[CertificateBuilder.MaxCertableResultsCount];
		private double[] summaValues = new double[countOfSummas];

		private int countOfMatches = -1;
		private uint countOfEvents = 0;
		private double totalSumma = 0.0;

		// Именная подпись сертификата
		private string certName = "";

		// Списки вариантов
		private List<string> methods = new List<string> ();

		// Вспомогательные переменные
		private ConnectivityManager cm;
		private Random rnd;
		private SecureRandom srnd;

#if TTM
		private const string resultsTTMRegKey = "TTMResults";
		private const int maxResultsTTMLength = 1000;

		private TalkToMe ttm;
#endif

		#endregion

		#region Цветовая схема

		private readonly Color
			solutionMasterBackColor = Color.FromHex ("#FFE8FF"),
			solutionMasterTextColor = Color.FromHex ("#202020"),
			solutionFieldBackColor = Color.FromHex ("#FFD0FF"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0"),

			progressNormal = Color.FromHex ("#00C000"),
			progressWarning1 = Color.FromHex ("#80C000"),
			progressWarning2 = Color.FromHex ("#C0C000"),
			progressWarning3 = Color.FromHex ("#FFC000");

		#endregion

		#region Переменные страниц

#if TTM

		private ContentPage solutionPage, aboutPage, ttm1Page, ttm2Page;
		private const int pagesCount = 4;
		private Label aboutLabel, measureLabel, resultsLabel, /*instructionsLabel,*/
			space01, space02, resultsTTM1Label, aboutFontSizeField;
		private Label[] pixels = new Label[2];

		private Xamarin.Forms.Button startButton, stopButton, shareButton, methodButton,
			certButton, startTTM1Button, methodTTM1Button, resetTTM1Button,
			startTTM2Button, stopTTM2Button, methodTTM2Button, languageButton;
		private Xamarin.Forms.ProgressBar[] scale = new Xamarin.Forms.ProgressBar[3],
			factorScale = new Xamarin.Forms.ProgressBar[2];
		private Xamarin.Forms.Editor messageTTM1Editor;

#else

		private ContentPage solutionPage, aboutPage;
		private const int pagesCount = 2;

		private Label aboutLabel, measureLabel, resultsLabel, space01, space02, aboutFontSizeField;

		private Xamarin.Forms.Button startButton, stopButton, shareButton, methodButton, certButton,
			languageButton;

		private Xamarin.Forms.ProgressBar[] scale = new Xamarin.Forms.ProgressBar[3],
			factorScale = new Xamarin.Forms.ProgressBar[2];

#endif

		#endregion

		#region Запуск и завершение

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App (bool Huawei)
			{
			// Инициализация
			InitializeComponent ();

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			solutionPage = AndroidSupport.ApplyPageSettings (MainPage, "SolutionPage",
				Localization.GetText ("SolutionPage"), solutionMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (MainPage, "AboutPage",
				Localization.GetDefaultText (LzDefaultTextValues.Control_AppAbout), aboutMasterBackColor);
			AndroidSupport.SetMainPage (MainPage);

#if TTM
			ttm1Page = AndroidSupport.ApplyPageSettings (MainPage, "TTM1Page",
				Localization.GetText ("TTM1Page"), solutionMasterBackColor);
			ttm2Page = AndroidSupport.ApplyPageSettings (MainPage, "TTM2Page",
				Localization.GetText ("TTM2Page"), solutionMasterBackColor);
#endif

			#region Основная страница

			startButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Start",
				ASButtonDefaultTypes.Start, solutionFieldBackColor, StartGeneration);
			stopButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Stop",
				ASButtonDefaultTypes.Stop, solutionFieldBackColor, StopGeneration);
			stopButton.IsVisible = false;
			methodButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Method",
				ASButtonDefaultTypes.Select, solutionFieldBackColor, SelectMethod);

			measureLabel = AndroidSupport.ApplyLabelSettings (solutionPage, "Measure", "",
				ASLabelTypes.HeaderCenter);
			measureLabel.IsVisible = false;

			resultsLabel = AndroidSupport.ApplyLabelSettings (solutionPage, "Results", "",
				ASLabelTypes.DefaultCenter);
			resultsLabel.Padding = resultsLabel.Margin;
			resultsLabel.FontSize *= 1.1;

			for (int i = 0; i < scale.Length; i++)
				{
				scale[i] = (Xamarin.Forms.ProgressBar)solutionPage.FindByName ("Scale" + (i + 1).ToString ());
				scale[i].IsVisible = false;
				}
			for (int i = 0; i < factorScale.Length; i++)
				{
				factorScale[i] = (Xamarin.Forms.ProgressBar)solutionPage.FindByName ("FactorScale" + (i + 1).ToString ());
				factorScale[i].IsVisible = false;
				factorScale[i].Progress = factor;
				}

			shareButton = AndroidSupport.ApplyButtonSettings (solutionPage, "ShareResults",
				ASButtonDefaultTypes.Share, solutionFieldBackColor, ShareResults);
			certButton = AndroidSupport.ApplyButtonSettings (solutionPage, "Certificate",
				ASButtonDefaultTypes.Copy, solutionFieldBackColor, CreateCertificate);

			AndroidSupport.ApplyButtonSettings (solutionPage, "NightMode", ASButtonDefaultTypes.NightMode,
				solutionFieldBackColor, SwitchNightMode);

			space01 = AndroidSupport.ApplyLabelSettings (solutionPage, "Space01", Localization.RN,
				ASLabelTypes.DefaultCenter);
			space02 = AndroidSupport.ApplyLabelSettings (solutionPage, "Space02", Localization.RN,
				ASLabelTypes.DefaultCenter);
			space01.IsVisible = space02.IsVisible = false;

			#endregion

			#region Страница «О программе»

			try
				{
				firstStart = RDGenerics.GetAppSettingsValue (firstStartRegKey) == "";
				}
			catch { }

			// Описание приложения
			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, ASLabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualsButton",
				Localization.GetDefaultText (LzDefaultTextValues.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "HelpButton",
				Localization.GetDefaultText (LzDefaultTextValues.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "GenericSettingsLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_GenericSettings),
				ASLabelTypes.HeaderLeft);

			// Кнопки управления
			AndroidSupport.ApplyLabelSettings (aboutPage, "RestartTipLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Message_RestartRequired),
				ASLabelTypes.Tip);

			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_InterfaceLanguage),
				ASLabelTypes.DefaultLeft);
			languageButton = AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector",
				Localization.LanguagesNames[(int)Localization.CurrentLanguage],
				aboutFieldBackColor, SelectLanguage_Clicked, false);

			AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_InterfaceFontSize),
				ASLabelTypes.DefaultLeft);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeInc",
				ASButtonDefaultTypes.Increase, aboutFieldBackColor, FontSizeButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeDec",
				ASButtonDefaultTypes.Decrease, aboutFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeField",
				" ", ASLabelTypes.DefaultCenter);

			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetEncoding (SupportedEncodings.UTF8).
				GetString ((byte[])RD_AAOW.Properties.Resources.ResourceManager.
				GetObject (Localization.GetHelpFilePath ())), ASLabelTypes.SmallLeft);

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страница «Поговори со мной»

#if TTM
			startTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Start",
				ASButtonDefaultTypes.Up, solutionFieldBackColor, StartTalking);
			methodTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Method",
				ASButtonDefaultTypes.Select, solutionFieldBackColor, SelectMethod);
			resetTTM1Button = AndroidSupport.ApplyButtonSettings (ttm1Page, "Reset",
				ASButtonDefaultTypes.Delete, solutionFieldBackColor, ResetTheChat);

			AndroidSupport.ApplyButtonSettings (ttm1Page, "NightMode", ASButtonDefaultTypes.NightMode,
				solutionFieldBackColor, SwitchNightMode);

			resultsTTM1Label = AndroidSupport.ApplyLabelSettings (ttm1Page, "Results", "", ASLabelTypes.Field,
				solutionMasterBackColor);
			messageTTM1Editor = AndroidSupport.ApplyEditorSettings (ttm1Page, "Message", solutionFieldBackColor,
				Keyboard.Text, 100, "", null, true);
#endif

			#endregion

			#region Страница «Покажи скрытое»

#if TTM
			startTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Start",
				ASButtonDefaultTypes.Start, solutionFieldBackColor, StartTTM);
			stopTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Stop",
				ASButtonDefaultTypes.Stop, solutionFieldBackColor, StopTTM);
			stopTTM2Button.IsVisible = false;

			methodTTM2Button = AndroidSupport.ApplyButtonSettings (ttm2Page, "Method",
				ASButtonDefaultTypes.Select, solutionFieldBackColor, SelectMethod);
			AndroidSupport.ApplyButtonSettings (ttm2Page, "NightMode", ASButtonDefaultTypes.NightMode,
				solutionFieldBackColor, SwitchNightMode);

			for (int i = 0; i < pixels.Length; i++)
				{
				pixels[i] = AndroidSupport.ApplyLabelSettings (ttm2Page, "Pixels" + (i + 1).ToString (), " ",
					 ASLabelTypes.Field, solutionFieldBackColor);
				pixels[i].TextType = TextType.Html;
				pixels[i].HorizontalOptions = new LayoutOptions (LayoutAlignment.Center, false);
				}
#endif

			#endregion

			// Инициализация состояния приложения
			cm = (ConnectivityManager)Android.App.Application.Context.GetSystemService (Android.App.Service.ConnectivityService);

			#region Получение сохранённых настроек

			try
				{
				// Безопасные
				nightMode = RDGenerics.GetAppSettingsValue (nightModeRegKey) == "1";

				for (int i = 0; i < hiResults.Length; i++)
					hiResults[i] = RDGenerics.GetAppSettingsValue (resultsRegKey + i.ToString ());

				for (int i = 0; i < certableResults.Length; i++)
					certableResults[i] = RDGenerics.GetAppSettingsValue (certableRegKey + i.ToString ());
				certName = RDGenerics.GetAppSettingsValue (certNameRegKey);

				// Могут сбойнуть
#if TTM
				resultsTTM1Label.Text = RDGenerics.GetAppSettingsValue (resultsTTMRegKey);
#endif
				currentMethod = (GenerationMethods)uint.Parse (RDGenerics.GetAppSettingsValue (currentMethodRegKey));
				}
			catch { }

			// Обработка сохранённых настроек
			if (firstStart)
				((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[pagesCount - 1];
			SwitchNightMode (null, null);

			#endregion

			// Первичная настройка поля результатов
			UpdateHiResults ();
			countOfMatches = 0; // Признак отмены обновления при отсутствии результатов

			// Отображение подсказок первого старта
			ShowTips (Huawei);
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowTips (bool Huawei)
			{
			// Контроль XPUN
			await AndroidSupport.XPUNLoop (Huawei);

			// Защита
			if (firstStart)
				{
				// Требование принятия Политики
				await AndroidSupport.PolicyLoop ();
				RDGenerics.SetAppSettingsValue (firstStartRegKey, ProgramDescription.AssemblyVersion);

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip00"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

#if TTM
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip02"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
#endif
				}
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			stopGeneration = true;
			try
				{
				RDGenerics.SetAppSettingsValue (currentMethodRegKey, ((uint)currentMethod).ToString ());
				RDGenerics.SetAppSettingsValue (nightModeRegKey, nightMode ? "1" : "0");
				RDGenerics.SetAppSettingsValue (certNameRegKey, certName);

				for (int i = 0; i < hiResults.Length; i++)
					RDGenerics.SetAppSettingsValue (resultsRegKey + i.ToString (), hiResults[i]);
				for (int i = 0; i < certableResults.Length; i++)
					RDGenerics.SetAppSettingsValue (certableRegKey + i.ToString (), certableResults[i]);

#if TTM
				RDGenerics.SetAppSettingsValue (resultsTTMRegKey, resultsTTM1Label.Text);
#endif
				}
			catch { }
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
			await AndroidSupport.CallHelpMaterials (HelpMaterialsSets.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (HelpMaterialsSets.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase))
					AndroidSupport.MasterFontSize += 0.5;
				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease))
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
				(Localization.GetText ("StartingGeneration") +
				Localization.RN + "(" + Localization.GetText ("Method" + ((uint)currentMethod).ToString ("D2")) + ")",
				true);

			coreMethod = (currentMethod == GenerationMethods.Core) || (currentMethod == GenerationMethods.CorePlusWiFi);
			wifiMethod = (currentMethod == GenerationMethods.WiFi) || (currentMethod == GenerationMethods.CorePlusWiFi);

			// Запуск петли
			while (await GeneratePRNG ())
				;
			}

		// Обновление состояния кнопок
		private async void ChangeButtonsState (bool GenerationStart, bool TTM)
			{
			// Общие
			space01.IsVisible = space02.IsVisible = measureLabel.IsVisible =
#if TTM
				stopTTM2Button.IsVisible =
#endif
				GenerationStart;
			stopButton.IsVisible = !TTM && GenerationStart;
#if TTM
			stopTTM2Button.IsVisible = TTM && GenerationStart;
#endif

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
#if TTM
				startTTM1Button.IsEnabled = methodTTM1Button.IsVisible = resetTTM1Button.IsVisible =
					startTTM2Button.IsVisible = methodTTM2Button.IsVisible = false;
#endif
				}
			else
				{
				startButton.IsVisible = resultsLabel.IsVisible = methodButton.IsVisible = true;
#if TTM
				startTTM1Button.IsEnabled = methodTTM1Button.IsVisible = resetTTM1Button.IsVisible =
					startTTM2Button.IsVisible = methodTTM2Button.IsVisible = true;
#endif
				shareButton.IsVisible = !string.IsNullOrWhiteSpace (resultsLabel.Text);

				// Открытие доступа к сертификату
				certButton.IsVisible = !string.IsNullOrWhiteSpace (certableResults[0]);
				if (!TTM && (certName == "") && certButton.IsVisible)
					{
					await AndroidSupport.ShowMessage (Localization.GetText ("Tip01"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
					certName = " ";
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
			if (wifiMethod)
				{
				seed = await Task.Run<int> (GetSeed);
				if (seed == 0)
					{
					AndroidSupport.ShowBalloon (Localization.GetText ("ConnectionLost"), true);

					ChangeButtonsState (false, false);
					return false;
					}

				rnd = new Random (seed);
				}

			if (coreMethod)
				{
				if (srnd == null)
					srnd = new SecureRandom ();
				}

			// Расчёт суммы
			uint summa = 0;

			for (int i = 1; i <= countOfRN; i++)
				{
				// Обновление суммы
				if (wifiMethod)
					summa += (uint)rnd.Next (rangeOfRN);
				if (coreMethod)
					summa += (uint)srnd.NextInt (rangeOfRN);

				// Зазернение
				if (i % newSeedStep == 0)
					{
					if (wifiMethod)
						{
						seed = await Task.Run<int> (GetSeed);
						rnd = new Random (seed);
						}

					if (coreMethod)
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
			UpdateCounters ((currentMethod == GenerationMethods.CorePlusWiFi) ? summa / 2 : summa);
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

			measureLabel.Text = string.Format (Localization.GetText ("MethodName"), (uint)currentMethod + 1,
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

				hiResults[0] = string.Format (Localization.GetText ("MethodName"), (uint)currentMethod + 1,
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
				resultsLabel.Text = Localization.GetText ("HiResults");

				for (int i = 0; i < hiResults.Length; i++)
					{
					if (!string.IsNullOrWhiteSpace (hiResults[i]))
						resultsLabel.Text += (Localization.RN + hiResults[i]);
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
				Text = aboutLabel.Text + Localization.RNRN + resultsLabel.Text,
				Title = ProgramDescription.AssemblyVisibleName
				});
			}

		// Выбор метода генерации
		private async void SelectMethod (object sender, EventArgs e)
			{
			if (methods.Count < 1)
				methods = new List<string> {
					Localization.GetText ("Method00"),
					Localization.GetText ("Method01"),
					Localization.GetText ("Method02")
					};

			int res = await AndroidSupport.ShowList (Localization.GetText ("MethodSelect"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), methods);

			if (res < 0)
				return;

			currentMethod = (GenerationMethods)res;
			}

		// Включение / выключение ночного режима
		private void SwitchNightMode (object sender, EventArgs e)
			{
			if (e != null)
				nightMode = !nightMode;

			if (nightMode)
				{
				solutionPage.BackgroundColor =
#if TTM
					ttm1Page.BackgroundColor = ttm2Page.BackgroundColor = messageTTM1Editor.TextColor =
					resultsTTM1Label.BackgroundColor =
#endif
					solutionMasterTextColor;
				resultsLabel.TextColor =
#if TTM
					resultsTTM1Label.TextColor = messageTTM1Editor.BackgroundColor =
#endif
					solutionMasterBackColor;
				for (int i = 0; i < factorScale.Length; i++)
					factorScale[i].ProgressColor = solutionFieldBackColor;
				}
			else
				{
				solutionPage.BackgroundColor =
#if TTM
					ttm1Page.BackgroundColor = ttm2Page.BackgroundColor = messageTTM1Editor.TextColor =
					resultsTTM1Label.BackgroundColor =
#endif
					solutionMasterBackColor;
				resultsLabel.TextColor =
#if TTM
					resultsTTM1Label.TextColor = messageTTM1Editor.BackgroundColor =
#endif
					solutionMasterTextColor;
				for (int i = 0; i < factorScale.Length; i++)
					factorScale[i].ProgressColor = solutionMasterTextColor;
				}
			}

		// Метод формирует изображение сертификата
		private async void CreateCertificate (object sender, EventArgs e)
			{
			// Контроль разрешений
			await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageWrite> ();
			if (await Xamarin.Essentials.Permissions.CheckStatusAsync<Xamarin.Essentials.Permissions.StorageWrite> () !=
				PermissionStatus.Granted)
				{
				AndroidSupport.ShowBalloon (Localization.GetText ("SaveFileFailure"), true);
				return;
				}

			// Сбор сведений
			if (string.IsNullOrWhiteSpace (certName))
				{
				certName = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
					Localization.GetText ("CertNameHelp"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Accept),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
					CertificateBuilder.MaxCertificateNameLength, Keyboard.Text);

				if (string.IsNullOrWhiteSpace (certName) || (certName.Trim ().Length < 3))
					{
					certName = " ";
					return;
					}
				}

			string certData = certableResults[0];
			for (int i = 1; i < certableResults.Length; i++)
				certData += ("\n" + certableResults[i].Replace ("\r", "").Replace ("\n", ", "));

			// Запуск сборки
			certButton.IsVisible = false;
			CertificateBuilder cb = new CertificateBuilder (certName, certData);
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
					msg = Localization.GetText ("SaveFileFailure");
					break;

				case 1:
					msg = Localization.GetText ("SaveFileSuccess");
					break;
				}

			if (msg != "")
				AndroidSupport.ShowBalloon (msg, true);
			cb.Dispose ();
			}

		#endregion

		#region Рабочая зона Talk to me

#if TTM
		// Запуск и остановка генерации
		private async void StartTalking (object sender, EventArgs e)
			{
			// Инициализация
			ChangeButtonsState (true, true);

			if (currentMethod == GenerationMethods.Core)
				{
				ttm = new TalkToMe ();
				}
			else
				{
				if (!await InitTalkingFromWifi ())
					return;

				ttm = new TalkToMe (GetSeed, currentMethod == GenerationMethods.CorePlusWiFi);
				}

			// Запрос предложения
			resultsTTM1Label.Text = ("▲ " + messageTTM1Editor.Text + Localization.RNRN) + resultsTTM1Label.Text;
			messageTTM1Editor.Text = "";

			AndroidSupport.ShowBalloon (Localization.GetText ("GettingSentence") +
				Localization.RN + "(" +
				Localization.GetText ("Method" + ((uint)currentMethod).ToString ("D2")) + ")", true);

			string sentence = await Task.Run<string> (ttm.GetNextSentence);
			resultsTTM1Label.Text = ("▼ " + sentence + Localization.RNRN) + resultsTTM1Label.Text;
			if (resultsTTM1Label.Text.Length > maxResultsTTMLength)
				resultsTTM1Label.Text = resultsTTM1Label.Text.Substring (0, maxResultsTTMLength);

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
				AndroidSupport.ShowBalloon (Localization.GetText ("ConnectionLost"), true);

				ChangeButtonsState (false, true);
				return false;
				}

			return true;
			}

		// Метод сбрасывает чат
		private async void ResetTheChat (object sender, EventArgs e)
			{
			if (await AndroidSupport.ShowMessage (Localization.GetText ("ChatResetRequest"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Yes),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel)))
				resultsTTM1Label.Text = "";
			}
#endif

		#endregion

		#region Рабочая зона Show it to me

#if TTM
		// Запуск и остановка генерации
		private async void StartTTM (object sender, EventArgs e)
			{
			ChangeButtonsState (true, true);

			AndroidSupport.ShowBalloon (Localization.GetText ("StartingGeneration") +
				Localization.RN +
				"(" + Localization.GetText ("Method" + ((uint)currentMethod).ToString ("D2")) + ")",
				true);

			// Инициализация потока
			if (currentMethod == GenerationMethods.Core)
				{
				ttm = new TalkToMe ();
				}
			else
				{
				if (!await InitTalkingFromWifi ())
					return;

				ttm = new TalkToMe (GetSeed, currentMethod == GenerationMethods.CorePlusWiFi);
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

			if (currentMethod != GenerationMethods.CorePlusWiFi)
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
#endif

		#endregion
		}
	}
