using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Ports;
using TusurUI.Source;
using TusurUI.ExternalSources;
using System.Windows.Threading;

namespace TusurUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _comPortUpdateTimer;

        private bool isVaporizerWorks = false;
        private double currentValue { get; set; }
        private ushort voltageValue = 6;

        private bool isShutterOpenButtonClicked = false;
        private bool isShutterCloseButtonClicked = false;
        private bool isMotorStopButtonClicked = false;

        private string powerSupplyCOM = "";
        private string stepMotorCOM = "";

        public MainWindow()
        {
            InitializeComponent();
            InitializeComPortUpdateTimer();
            PopulateComPortComboBoxes();
        }

        private void InitializeComPortUpdateTimer()
        {
            _comPortUpdateTimer = new DispatcherTimer();
            _comPortUpdateTimer.Interval = TimeSpan.FromSeconds(5);
            _comPortUpdateTimer.Tick += ComPortUpdateTimer_Tick;
            _comPortUpdateTimer.Start();
        }

        private void ComPortUpdateTimer_Tick(object? sender, EventArgs e) { PopulateComPortComboBoxes(); }

        private void PopulateComPortComboBoxes()
        {
            string[] ports = SerialPort.GetPortNames();
            PowerSupplyComPortComboBox.ItemsSource = ports;
            ShutterComPortComboBox.ItemsSource = ports;

            if (ports.Length > 0)
            {
                if (!PowerSupplyComPortComboBox.Items.Contains(PowerSupplyComPortComboBox.SelectedItem))
                    PowerSupplyComPortComboBox.SelectedIndex = 0;
                if (!ShutterComPortComboBox.Items.Contains(ShutterComPortComboBox.SelectedItem))
                    ShutterComPortComboBox.SelectedIndex = 0;
                PowerSupplyComPortComboBox.IsEnabled = true;
                ShutterComPortComboBox.IsEnabled = true;
            }
            else
            {
                PowerSupplyComPortComboBox.IsEnabled = false;
                ShutterComPortComboBox.IsEnabled = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _comPortUpdateTimer?.Stop();
        }

        private bool IsValidInput(string text)
        {
            foreach (var c in text)
                if (!char.IsDigit(c) && c != ',' && c != '.')
                    return false;
            return true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == PowerSupplyComPortComboBox && PowerSupplyComPortComboBox.SelectedItem == ShutterComPortComboBox.SelectedItem)
                ShutterComPortComboBox.SelectedIndex = -1;
            else if (sender == ShutterComPortComboBox && ShutterComPortComboBox.SelectedItem == PowerSupplyComPortComboBox.SelectedItem)
                PowerSupplyComPortComboBox.SelectedIndex = -1;
        }

        private void TurnOnPowerSupply()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);

                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.TurnOn()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }

            ApplyVoltageOnPowerSupply();
        }

        private void ApplyVoltageOnPowerSupply()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);

                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.SetCurrentVoltage((ushort)currentValue, voltageValue)))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                TurnOffPowerSupply();
                UncheckVaporizerButton();
            }
        }

        private void ReadCurrentVoltageAndChangeTextBox()
        {
            try
            {
                /*.
		            currentVoltageValues[0] - Actual current value.
		            currentVoltageValues[1] - Actual voltage value.
	            */
                ushort[]? currentVoltageValues = PowerSupply.ReadCurrentVoltage();
                if (currentVoltageValues != null)
                    CurrentValueLabel.Content = currentVoltageValues[0].ToString() + " A";
                else
                    ShowWarning("Не удалось получить данные о текущем напряжении/токе.");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void TurnOffPowerSupply()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);

                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.TurnOff()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }
        }

        private void OpenShutter()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Forward()))
                    return;

                SetShutterImageToOpened();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                SetShutterImageToClosed();
            }
        }

        private void CloseShutter()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Reverse()))
                    return;

                SetShutterImageToClosed();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void StopStepMotor()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Stop()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowWarning(string message, string title = "Предупреждение") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning); }

        private void ShowError(string message, string title = "Ошибка") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error); }

        private bool IsPowerSupplyErrorCodeStatusFailed(int errorCode)
        {
            if (errorCode > 0)
            {
                ShowWarning(PowerSupply.GetErrorMessage(errorCode));
                UncheckVaporizerButton();
            }
            return errorCode == 0;
        }

        private bool IsStepMotorErrorCodeStatusFailed(int errorCode)
        {
            if (errorCode > 0)
            {
                ShowWarning(StepMotor.GetErrorMessage(errorCode));
                SetShutterImageToClosed();
            }
            return errorCode == 0;
        }

        private bool IsPowerSupplyCOMportNull()
        {
            string? port = PowerSupplyComPortComboBox.SelectedItem.ToString();
            if (port == null)
            {
                ShowError($"Возникла ошибка при выборе COM-порта для управления блоком питания. Выбранный COM-порт: <{PowerSupplyComPortComboBox.SelectedItem}>");
                UncheckVaporizerButton();
                return true;
            }
            powerSupplyCOM = port;
            return false;
        }

        private bool IsStepMotorCOMportNull()
        {
            string? port = ShutterComPortComboBox.SelectedItem.ToString();
            if (port == null)
            {
                ShowError($"Возникла ошибка при выборе COM-порта для управления заслонкой. Выбранный COM-порт: <{ShutterComPortComboBox.SelectedItem}>");
                SetShutterImageToClosed();
                return true;
            }
            stepMotorCOM = port;
            return false;
        }

        private void CheckVaporizerButton()
        {
            // Changing color of the switch and moving button to the right
            VaporizerButtonBase.Background = new SolidColorBrush(Colors.Green);
            DockPanel.SetDock(VaporizerButtonInside, Dock.Right);
            ComponentManager.ChangeIndicatorPicture(Indicator, "Images/индикатор вкл.jpg");
            isVaporizerWorks = true;
        }

        private void UncheckVaporizerButton()
        {
            SystemStateLabel.Content = "Система не работает";
            SystemStateLabel.Foreground = new SolidColorBrush(Colors.Red);
            VaporizerButtonBase.Background = new SolidColorBrush(Colors.White);
            DockPanel.SetDock(VaporizerButtonInside, Dock.Left);
            ComponentManager.ChangeIndicatorPicture(Indicator, "Images/индикатор откл.jpg");
            isVaporizerWorks = false;
        }

        private void SetShutterImageToClosed() { ComponentManager.ChangeIndicatorPicture(Vaporizer, "Images/заслонка закр фото.png"); }

        private void SetShutterImageToOpened() { ComponentManager.ChangeIndicatorPicture(Vaporizer, "Images/заслонка откр.png"); }

        private void VaporizerButtonBase_Checked(object sender, RoutedEventArgs e)
        {
            CheckVaporizerButton();
            TurnOnPowerSupply();
        }

        private void VaporizerButtonBase_Unchecked(object sender, RoutedEventArgs e)
        {
            UncheckVaporizerButton();
            TurnOffPowerSupply();
        }

        private void CurrentSetPointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string newText = textBox.Text.Replace(',', '.');
                if (double.TryParse(newText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value) && (value >= 0 && value <= 200))
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    StartButton.IsEnabled = true;
                    currentValue = value;
                    textBox.ToolTip = "Введите значение от 0 до 200";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    StartButton.IsEnabled = false;
                    textBox.ToolTip = "Неверное значение. Допустимый диапазон: 0-200";
                }
            }
        }
        private void CurrentSetPointTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) { e.Handled = !IsValidInput(e.Text); }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVaporizerWorks)
            {
                SystemStateLabel.Content = "Система работает";
                SystemStateLabel.Foreground = new SolidColorBrush(Colors.Green);

                try
                {
                    // 1. Turning on the power supply and setting the current and voltage.
                    TurnOnPowerSupply();

                    // 2. Reading actual current of the vaporizer.
                    ReadCurrentVoltageAndChangeTextBox();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            else
                ShowWarning("Сперва нужно включить управление испарителем");
        }

        private void OpenShutterButton_Click(object sender, RoutedEventArgs e)
        {
            // Preventing double click on the same button.
            if (isShutterOpenButtonClicked)
                return;
            isShutterOpenButtonClicked = true;
            isShutterCloseButtonClicked = false;
            isMotorStopButtonClicked = false;

            OpenShutter();
        }

        private void CloseShutterButton_Click(object sender, RoutedEventArgs e)
        {
            // Preventing double click on the same button.
            if (isShutterCloseButtonClicked)
                return;
            isShutterOpenButtonClicked = false;
            isShutterCloseButtonClicked = true;
            isMotorStopButtonClicked = false;

            CloseShutter();
        }

        private void StopShutterButton_Click(object sender, RoutedEventArgs e)
        {
            // Preventing double click on the same button.
            if (isMotorStopButtonClicked)
                return;
            isShutterOpenButtonClicked = false;
            isShutterCloseButtonClicked = false;
            isMotorStopButtonClicked = true;

            StopStepMotor();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //TurnOffPowerSupply();
            //StopStepMotor();
        }
    }
}
