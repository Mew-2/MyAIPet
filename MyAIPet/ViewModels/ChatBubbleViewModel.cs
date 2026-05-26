using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MyAIPet.Core.Mvvm;

namespace MyAIPet.ViewModels;

public class ChatBubbleViewModel : ViewModelBase
{
    private string _fullText = string.Empty;
    private string _displayText = string.Empty;
    private bool _isTyping;
    private bool _isComplete;
    private bool _isVisible;
    private double _opacity = 1.0;
    private DispatcherTimer? _typeTimer;
    private DispatcherTimer? _displayTimer;
    private int _charIndex;
    private Action? _onTypingComplete;
    private Action? _onDisplayTimeout;
    private bool _canClick = false;

    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value);
    }

    public bool IsTyping
    {
        get => _isTyping;
        set => SetProperty(ref _isTyping, value);
    }

    public bool IsComplete
    {
        get => _isComplete;
        set
        {
            if (SetProperty(ref _isComplete, value) && value)
            {
                IsTyping = false;
                DisplayText = _fullText;
                StartDisplayTimer();
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    public bool CanClick
    {
        get => _canClick;
        set => SetProperty(ref _canClick, value);
    }

    public async Task ShowTextAsync(string text, Action onTypingComplete, Action onDisplayTimeout)
    {
        _fullText = text;
        _onTypingComplete = onTypingComplete;
        _onDisplayTimeout = onDisplayTimeout;
        _charIndex = 0;
        DisplayText = string.Empty;
        IsVisible = true;
        Opacity = 1.0;
        IsTyping = true;
        IsComplete = false;
        CanClick = false;

        _typeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _typeTimer.Tick += TypeTimer_Tick;
        _typeTimer.Start();
    }

    public void ShowThinking()
    {
        _fullText = string.Empty;
        _onTypingComplete = null;
        _onDisplayTimeout = null;
        DisplayText = "思考中...";
        IsVisible = true;
        Opacity = 1.0;
        IsTyping = false;
        IsComplete = false;
        CanClick = false;
    }

    private void TypeTimer_Tick(object? sender, EventArgs e)
    {
        if (_charIndex < _fullText.Length)
        {
            DisplayText = _fullText.Substring(0, _charIndex + 1);
            _charIndex++;
        }
        else
        {
            _typeTimer?.Stop();
            IsComplete = true;
            _onTypingComplete?.Invoke();
        }
    }

    public void SkipTyping()
    {
        if (IsTyping)
        {
            _typeTimer?.Stop();
            IsComplete = true;
            _onTypingComplete?.Invoke();
        }
    }

    private void StartDisplayTimer()
    {
        CanClick = false;
        _displayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(2000)
        };
        _displayTimer.Tick += (s, e) =>
        {
            _displayTimer?.Stop();
            CanClick = true;
            _onDisplayTimeout?.Invoke();
        };
        _displayTimer.Start();
    }

    public void StartFadeOut(double delaySeconds, Action onComplete)
    {
        Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ContinueWith(_ =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var fadeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(50)
                };
                var steps = 20;
                var currentStep = 0;

                fadeTimer.Tick += (s, e) =>
                {
                    currentStep++;
                    Opacity = 1.0 - (currentStep / (double)steps);

                    if (currentStep >= steps)
                    {
                        fadeTimer.Stop();
                        IsVisible = false;
                        Opacity = 1.0;
                        onComplete?.Invoke();
                    }
                };
                fadeTimer.Start();
            });
        });
    }

    public void Hide()
    {
        _typeTimer?.Stop();
        _displayTimer?.Stop();
        IsVisible = false;
    }

    public override void Destroy()
    {
        _typeTimer?.Stop();
        _displayTimer?.Stop();
        base.Destroy();
    }
}
