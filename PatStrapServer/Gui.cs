// using System.Net;
// using System.Net.Sockets;
// using System.Text;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using PatStrapServer.PatStrap;
// using PatStrapServer.protocol;
//
// namespace PatStrapServer;
//
// class HapticControl
// {
//     public string HapticAreaType;
//     public TrackBar TrackBar;
//     public Label ValueLabel;
//     public Label NameLabel;
//     
// }
//
// public class Gui(PatStrap.Service service, IHostApplicationLifetime lifeTime) : BackgroundService
// {
//     
//     private static TrackBar trackBarThreshold;
//     private static Label valueLabelThreshold;
//     
//     private static TrackBar trackBarMin;
//     private static Label valueLabelMin;
//     
//     private static TrackBar trackBarMax;
//     private static Label valueLabelMax;
//     
//     private static Label valueLabelVcc;
//     
//     
//     private void Create()
//     {
//         Form myform = new Form()
//         {
//             AutoSizeMode = AutoSizeMode.GrowOnly,
//             Name = "PatStrap Configuration",
//             AutoSize = true
//         };
//         // Создаем TrackBar для каждого значения
//         foreach (string contactName in service.Haptics.Keys)
//         {
//             HapticControl hapticControl = new HapticControl();
//             // Создаем Label для названия области
//             hapticControl.ValueLabel = new Label
//             {
//                 Text = $"{contactName} Value: 0.0",
//                 Dock = DockStyle.Top,
//                 TextAlign = System.Drawing.ContentAlignment.MiddleLeft
//             };
//
//             // Создаем TrackBar
//             hapticControl.TrackBar = new TrackBar
//             {
//                 Minimum = 0,
//                 Maximum = 100,
//                 TickFrequency = 10,
//                 LargeChange = 10,
//                 SmallChange = 1,
//                 Value = 0,
//                 Dock = DockStyle.Top,
//                 Name = contactName // Устанавливаем имя для TrackBar
//             };
//
//             // Подписка на событие изменения значения
//             hapticControl.TrackBar.ValueChanged += (sender, e) =>
//             {
//                 Console.WriteLine($"{contactName}: {hapticControl.TrackBar.Value}");
//                 // Получаем текущее значение слайдера и преобразуем его в диапазон от 0 до 1
//                 double value = hapticControl.TrackBar.Value / 100.0;
//
//                 // Обновляем текст метки
//                 hapticControl.ValueLabel.Text = $"{contactName} Value: {value:F2}";
//
//                 service.SetHapticValue(contactName, (float)value);
//                 OnValueChanged(service.Haptics["RightEar"].Value);
//             };
//
//             // Добавляем Label и TrackBar на форму
//             myform.Controls.Add(hapticControl.TrackBar);
//             myform.Controls.Add(hapticControl.ValueLabel);
//             
//             
//         }
//
//         trackBarThreshold = new TrackBar
//         {
//             Minimum = 0,
//             Maximum = 100, // Устанавливаем максимум на 100 для удобства
//             TickFrequency = 1,
//             LargeChange = 10,
//             SmallChange = 1,
//             Value = 0,
//             Dock = DockStyle.Top
//         };
//
//         // Создание и настройка Label для отображения значения
//         valueLabelThreshold = new Label
//         {
//             Text = "Zero Threshold: 0.0",
//             Dock = DockStyle.Top,
//             TextAlign = System.Drawing.ContentAlignment.MiddleLeft
//         };
//
//         // Подписка на событие изменения значения
//         trackBarThreshold.ValueChanged += trackBarThreshold_ValueChanged;
//         
//         myform.Controls.Add(trackBarThreshold);
//         myform.Controls.Add(valueLabelThreshold);
//
//         trackBarMin = new TrackBar
//         {
//             Minimum = 0,
//             Maximum = 100, // Устанавливаем максимум на 100 для удобства
//             TickFrequency = 1,
//             LargeChange = 10,
//             SmallChange = 1,
//             Value = 0,
//             Dock = DockStyle.Top
//         };
//
//         // Создание и настройка Label для отображения значения
//         valueLabelMin = new Label
//         {
//             Text = "Minimum Haptic Power: 0.0",
//             Dock = DockStyle.Top,
//             TextAlign = System.Drawing.ContentAlignment.MiddleLeft
//         };
//
//         // Подписка на событие изменения значения
//         trackBarMin.ValueChanged += trackBarMin_ValueChanged;
//         
//         myform.Controls.Add(trackBarMin);
//         myform.Controls.Add(valueLabelMin);
//
//         trackBarMax = new TrackBar
//         {
//             Minimum = 0,
//             Maximum = 100, // Устанавливаем максимум на 100 для удобства
//             TickFrequency = 1,
//             LargeChange = 10,
//             SmallChange = 1,
//             Value = 0,
//             Dock = DockStyle.Top
//         };
//
//         // Создание и настройка Label для отображения значения
//         valueLabelMax = new Label
//         {
//             Text = "Maximum Haptic Power: 0.0",
//             Dock = DockStyle.Top,
//             TextAlign = System.Drawing.ContentAlignment.MiddleLeft
//         };
//
//         // Подписка на событие изменения значения
//         trackBarMax.ValueChanged += trackBarMax_ValueChanged;
//         
//         myform.Controls.Add(trackBarMax);
//         myform.Controls.Add(valueLabelMax);
//
//         // Создание и настройка Label для отображения значения
//         valueLabelVcc = new Label
//         {
//             Text = "Approximated Voltage: 0.0 V",
//             Dock = DockStyle.Top,
//             TextAlign = System.Drawing.ContentAlignment.MiddleLeft
//         };
//         myform.Controls.Add(valueLabelVcc);
//
//         myform.Closed += (sender, args) =>
//         {
//             lifeTime.StopApplication();
//         };
//         
//         myform.ShowDialog();
//     }
//     
//     private void trackBarThreshold_ValueChanged(object sender, EventArgs e)
//     {
//         // Получаем текущее значение слайдера и преобразуем его в диапазон от 0 до 1
//         double value = trackBarThreshold.Value / 100.0;
//
//         // Обновляем текст метки
//         valueLabelThreshold.Text = $"Zero threshold: {value:F2}";
//
//         // Вызываем коллбек (в данном случае просто выводим в консоль)
//         // OnValueChanged(value);
//         ((ModernProtocol)service._proto).zeroThreshhold = (float)value;
//         service.lastChangedTime = DateTime.Now.Ticks;
//         OnValueChanged(service.Haptics["RightEar"].Value);
//     }
//     
//     private void trackBarMin_ValueChanged(object sender, EventArgs e)
//     {
//         // Получаем текущее значение слайдера и преобразуем его в диапазон от 0 до 1
//         double value = trackBarMin.Value / 100.0;
//
//         // Обновляем текст метки
//         valueLabelMin.Text = $"Minimum Haptic Power: {value:F2}";
//
//         // Вызываем коллбек (в данном случае просто выводим в консоль)
//         // OnValueChanged(value);
//         ((ModernProtocol)service._proto).minValue = (float)value;
//         service.lastChangedTime = DateTime.Now.Ticks;
//         OnValueChanged(service.Haptics["RightEar"].Value);
//     }
//     
//     private void trackBarMax_ValueChanged(object sender, EventArgs e)
//     {
//         // Получаем текущее значение слайдера и преобразуем его в диапазон от 0 до 1
//         double value = trackBarMax.Value / 100.0;
//
//         // Обновляем текст метки
//         valueLabelMax.Text = $"Maximum Haptic Power: {value:F2}";
//
//         // Вызываем коллбек (в данном случае просто выводим в консоль)
//         // OnValueChanged(value);
//         ((ModernProtocol)service._proto).maxValue = (float)value;
//         service.lastChangedTime = DateTime.Now.Ticks;
//
//         OnValueChanged(service.Haptics["RightEar"].Value);
//     }
//
//     private void OnValueChanged(double newValue)
//     {
//         // Здесь можно добавить логику, которая будет выполняться при изменении значения
//         double value = ((ModernProtocol)service._proto).MapValue(newValue) * ((ModernProtocol)service._proto).VCC;
//         valueLabelVcc.Text = $"Approximated Voltage: {value:F2} V";
//     }
//     
//     protected override Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         Create();
//         return Task.CompletedTask;
//     }
//
// }