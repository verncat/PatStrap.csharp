using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatStrapServer.PatStrap;
using Button = Avalonia.Controls.Button;
using System;
using System.Timers;
using Microsoft.Extensions.Configuration;
using PatStrapServer.Config;
using TextBox = Avalonia.Controls.TextBox;
using Timer = System.Timers.Timer;

namespace AvaloniaApplication1;

public class Haptic : INotifyPropertyChanged
{
    private double _value;

    public bool IsContentExtended
    {
        get => _isContentExtended;
        set
        {
            _isContentExtended = value;
            OnPropertyChanged(nameof(IsContentExtended));
        }
    }

    private bool _isContentExtended = false;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string _name = String.Empty;

    public string ContactName
    {
        get => _contactName;
        set
        {
            _contactName = value;
            HasContactName = (value.Length > 0);
            OnPropertyChanged(nameof(ContactName));
            OnPropertyChanged(nameof(Name));
        }
    }

    private string _contactName = String.Empty;

    public bool HasContactName
    {
        get => _hasContactName;
        set
        {
            _hasContactName = value;
            OnPropertyChanged(nameof(HasContactName));
        }
    }

    private bool _hasContactName = false;

    public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            OnPropertyChanged(nameof(MinValue));
        }
    }

    private double _minValue = 0;

    public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            OnPropertyChanged(nameof(MaxValue));
        }
    }

    private double _maxValue = 1;

    public double ZeroThreshold
    {
        get => _zeroThreshold;
        set
        {
            _zeroThreshold = value;
            OnPropertyChanged(nameof(ZeroThreshold));
        }
    }

    private double _zeroThreshold = 0;

    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ValueText));
        }
    }

    public string ValueText =>
        $"Value: {Value}";

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private IHostApplicationLifetime hostApplicationLifetime;
    private PatStrapServer.PatStrap.ServiceLocator serviceLocator;
    private PatStrapServer.PatStrap.Service service;

    private Timer _timer;

    private ObservableCollection<Haptic> Haptics { get; }

    private bool isConnected;

    public bool IsConnected
    {
        get => isConnected;
        set
        {
            if (isConnected != value)
            {
                isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    private bool isConnectedOscServer;

    public bool IsConnectedOscServer
    {
        get => isConnectedOscServer;
        set
        {
            if (isConnectedOscServer != value)
            {
                isConnectedOscServer = value;
                OnPropertyChanged(nameof(IsConnectedOscServer));
            }
        }
    }

    private string statusText = "Searching compatible hardware...";

    public string StatusText
    {
        get => statusText;
        set
        {
            if (statusText != value)
            {
                statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    private string statusTextOscServer = "Connecting to VRChat...";

    public string StatusTextOscServer
    {
        get => statusTextOscServer;
        set
        {
            if (statusTextOscServer != value)
            {
                statusTextOscServer = value;
                OnPropertyChanged(nameof(StatusTextOscServer));
            }
        }
    }

    private bool isShowProcessRing = true;

    public bool IsShowProcessRing
    {
        get => isShowProcessRing;
        set
        {
            if (isShowProcessRing != value)
            {
                isShowProcessRing = value;
                OnPropertyChanged(nameof(IsShowProcessRing));
            }
        }
    }

    private bool isShowProcessRingOscServer = true;

    public bool IsShowProcessRingOscServer
    {
        get => isShowProcessRingOscServer;
        set
        {
            if (isShowProcessRingOscServer != value)
            {
                isShowProcessRingOscServer = value;
                OnPropertyChanged(nameof(IsShowProcessRingOscServer));
            }
        }
    }

    private double batteryLevel = 0;

    public double BatteryLevel
    {
        get => batteryLevel;
        set
        {
            if (batteryLevel != value)
            {
                batteryLevel = value;
                OnPropertyChanged(nameof(BatteryLevel));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void TrackBar_ValueChanged(object? sender, RangeBaseValueChangedEventArgs rangeBaseValueChangedEventArgs)
    {
        var source = (Slider)rangeBaseValueChangedEventArgs.Source!;
        if (source == null)
        {
            return;
        }

        var haptic = (Haptic)source.Tag;
        if (haptic == null)
            return;

        if (source.Name == "Min")
        {
            service.Haptics[haptic.ContactName].MinValue = (float)rangeBaseValueChangedEventArgs.NewValue;
            AppSettings.SaveConfiguration(service);
        }

        if (source.Name == "Max")
        {
            service.Haptics[haptic.ContactName].MaxValue = (float)rangeBaseValueChangedEventArgs.NewValue;
            AppSettings.SaveConfiguration(service);
        }

        if (source.Name == "ZeroThreshold")
        {
            service.Haptics[haptic.ContactName].ZeroThreshold = (float)rangeBaseValueChangedEventArgs.NewValue;
            AppSettings.SaveConfiguration(service);
        }
    }

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();

        hostApplicationLifetime = App.AppHost.Services.GetRequiredService<IHostApplicationLifetime>();
        service = App.AppHost.Services.GetRequiredService<Service>();
        serviceLocator = App.AppHost.Services.GetRequiredService<ServiceLocator>();


        Haptics = new ObservableCollection<Haptic>
            { };

        serviceLocator.PatstrapServiceLocated += (sender, info) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"Connected to {info.IpAddress}:{info.Port}";
                IsConnected = true;
                IsShowProcessRing = false;
                BatteryLevel = 100;
            });
        };

        service.OnHapticUpdated += (sender, args) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var haptic = Haptics?.FirstOrDefault(p => p.ContactName == args.Name);
                if (haptic != null) haptic.Value = args.Value.Value;
            });
        };

        service.OnHapticListUpdated += (sender) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Haptics?.Clear();
                int i = 0;
                foreach (var pair in service.Haptics.ToDictionary())
                {
                    Haptics?.Add(new Haptic()
                    {
                        Name = $"Haptic {++i}",
                        ContactName = pair.Key,
                        MinValue = pair.Value.MinValue,
                        MaxValue = pair.Value.MaxValue,
                        ZeroThreshold = pair.Value.ZeroThreshold,
                        Value = pair.Value.Value
                    });
                }
            });
        };
        Haptics?.Clear();

        foreach (var pair in service.Haptics)
        {
            var haptic = new Haptic()
            {
                Name = "Unknown",
                ContactName = pair.Key,
                MinValue = pair.Value.MinValue,
                MaxValue = pair.Value.MaxValue,
                ZeroThreshold = pair.Value.ZeroThreshold,
                Value = pair.Value.Value
            };
            Haptics?.Add(haptic);
        }

        this.FindControl<ItemsControl>("TrackersControl")!.ItemsSource = Haptics;


        this.Closing += (s, e) =>
        {
            e.Cancel = true;
            Title = "Closing...";
            hostApplicationLifetime.StopApplication();
        };
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.Name == "EditButton")
        {
            var haptic = (Haptic)button.Tag;
            haptic.IsContentExtended = true;
        }
        else if (button.Name == "RemoveButton")
        {
            var haptic = (Haptic)button.Tag;
            service.AddHaptic(haptic.ContactName);
            
        }
        else if (button.Name == "CollapseButton")
        {
            var haptic = (Haptic)button.Tag;
            haptic.IsContentExtended = false;
        }
        else if (button.Name == "TestButton")
        {
            var haptic = (Haptic)button.Tag;
            // int index = Haptics
            //     .Select((haptic, idx) => new { haptic, idx })
            //     .FirstOrDefault(x => x.haptic.Name == haptic.ContactName)?.idx ?? -1;

            if (_timer != null && _timer.Enabled)
                return;

            _timer = new Timer(50);
            float testProgress = 0;
            _timer.Elapsed += (o, args) =>
            {
                testProgress += 0.1f;
                float progress = 1 - MathF.Pow(1 - (testProgress / 1), 2); // ease-out

                if (progress >= 1)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    service.SetHapticValue(haptic.ContactName, 0);
                    return;
                }

                service.SetHapticValue(haptic.ContactName, progress);
            };
            _timer.Start();
        }
    }

    private void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        service.AddHaptic($"undefined_{service.Haptics.Count}");
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // var textbox = (TextBox)e.Source;
        // var haptic = (Haptic)textbox.Tag;
        // service.Haptics[haptic.ContactName].
        // AppSettings.SaveConfiguration(service);
    }
}