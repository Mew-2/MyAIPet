using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MyAIPet.Core.Mvvm;
using MyAIPet.Services.Interfaces;
using Prism.Commands;
using Serilog;

namespace MyAIPet.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IAnimationService? _animationService;
        private readonly IChatService? _chatService;
        private const double ScaleRatio = 0.3;
        private double _direction = -1;
        private BitmapSource? _currentFrame;
        private string _inputText = string.Empty;
        private bool _isChatting;
        private bool _isInputEnabled = true;
        private bool _isInputVisible = false;
        private ChatBubbleViewModel? _chatBubble;
        private List<string> _pendingSentences = new();
        private int _currentSentenceIndex;
        private bool _isWaitingForNext;
        private CancellationTokenSource? _chatCts;
        private string _currentEmotion = "normal";

        public double ScaleX => ScaleRatio * _direction;

        public string CurrentEmotion
        {
            get => _currentEmotion;
            set => SetProperty(ref _currentEmotion, value);
        }

        // TODO: 情绪驱动立绘切换
        private void OnEmotionChanged(string emotion, int affection)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentEmotion = emotion;
                // TODO: 有情绪立绘素材后，根据 emotion 切换宠物表情
                // happy → 开心表情/动作
                // sad  → 低落表情
                // angry → 生气表情
                // normal → 默认表情
            });
        }

        public BitmapSource? CurrentFrame
        {
            get => _currentFrame;
            set => SetProperty(ref _currentFrame, value);
        }

        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public bool IsChatting
        {
            get => _isChatting;
            set => SetProperty(ref _isChatting, value);
        }

        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set => SetProperty(ref _isInputEnabled, value);
        }

        public bool IsInputVisible
        {
            get => _isInputVisible;
            set => SetProperty(ref _isInputVisible, value);
        }

        public ChatBubbleViewModel? ChatBubble
        {
            get => _chatBubble;
            set => SetProperty(ref _chatBubble, value);
        }

        public DelegateCommand SendMessageCommand { get; }
        public DelegateCommand BubbleClickCommand { get; }
        public DelegateCommand ToggleBubbleCommand { get; }

        public MainWindowViewModel(IAnimationService animationService, IChatService chatService, IMemoryService memoryService, ChatBubbleViewModel chatBubble)
        {
            _animationService = animationService;
            _chatService = chatService;
            _chatBubble = chatBubble;

            SendMessageCommand = new DelegateCommand(async () => await SendMessageAsync(), () => IsInputEnabled)
                .ObservesProperty(() => IsInputEnabled);
            BubbleClickCommand = new DelegateCommand(OnBubbleClick);
            ToggleBubbleCommand = new DelegateCommand(OnToggleBubble);

            _chatService.OnEmotionChanged += OnEmotionChanged;

            _animationService.OnFrameChanged += OnFrameChanged;
            StartIdleLoop();
        }

        public void UpdatePosition(Window window)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var centerX = window.Left + window.Width / 2;
            _direction = centerX > screenWidth / 2 ? -1 : 1;
            RaisePropertyChanged(nameof(ScaleX));
        }

        private async void StartIdleLoop()
        {
            try
            {
                await _animationService!.StartIdleLoopAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "待机动画循环异常");
            }
        }

        public void StopIdleLoop()
        {
            _animationService?.StopIdleLoop();
        }

        private void OnFrameChanged(object? sender, BitmapSource frame)
        {
            CurrentFrame = frame;
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || !IsInputEnabled || _chatService == null)
                return;

            var userMessage = InputText.Trim();

            InputText = string.Empty;
            IsInputEnabled = false;
            IsInputVisible = false;
            IsChatting = true;
            _chatCts = new CancellationTokenSource();

            ChatBubble?.ShowThinking();

            try
            {
                var sentences = await _chatService.SendMessageWithHistoryAsync(userMessage, _chatCts.Token);

                _pendingSentences = sentences;
                _currentSentenceIndex = 0;
                _isWaitingForNext = false;

                await ShowNextSentence();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "发送消息失败");
                ChatBubble?.ShowTextAsync("希格雯的连接断开了，主人检查一下后端有没有启动哦~",
                    OnTypingComplete, OnDisplayTimeout);
            }
        }

        private async Task ShowNextSentence()
        {
            if (_currentSentenceIndex >= _pendingSentences.Count)
            {
                await Task.Delay(3000);
                ChatBubble?.StartFadeOut(0.5, OnBubbleFadeOutComplete);
                return;
            }

            var sentence = _pendingSentences[_currentSentenceIndex];
            _isWaitingForNext = false;

            await ChatBubble!.ShowTextAsync(sentence, OnTypingComplete, OnDisplayTimeout);
        }

        private void OnTypingComplete()
        {
            _isWaitingForNext = true;
        }

        private void OnDisplayTimeout()
        {
            _currentSentenceIndex++;

            if (_currentSentenceIndex >= _pendingSentences.Count)
            {
                ChatBubble?.StartFadeOut(0.5, OnBubbleFadeOutComplete);
            }
            else
            {
                _ = ShowNextSentence();
            }
        }

        private void OnBubbleClick()
        {
            if (ChatBubble == null) return;

            if (ChatBubble.IsTyping)
            {
                ChatBubble.SkipTyping();
            }
            else if (_isWaitingForNext && ChatBubble.CanClick)
            {
                _currentSentenceIndex++;
                if (_currentSentenceIndex >= _pendingSentences.Count)
                {
                    ChatBubble.StartFadeOut(0.5, OnBubbleFadeOutComplete);
                }
                else
                {
                    _ = ShowNextSentence();
                }
            }
        }

        private void OnBubbleFadeOutComplete()
        {
            IsChatting = false;
            IsInputEnabled = true;
            _pendingSentences.Clear();
            _currentSentenceIndex = 0;
            _chatCts?.Dispose();
            _chatCts = null;
        }

        private void OnToggleBubble()
        {
            IsInputVisible = !IsInputVisible;
        }

        public void CancelChat()
        {
            _chatCts?.Cancel();
            ChatBubble?.Hide();
            IsChatting = false;
            IsInputEnabled = true;
            _pendingSentences.Clear();
        }

        public override void Destroy()
        {
            StopIdleLoop();
            _animationService.OnFrameChanged -= OnFrameChanged;
            if (_chatService != null)
            {
                _chatService.OnEmotionChanged -= OnEmotionChanged;
            }
            ChatBubble?.Destroy();
            _chatCts?.Cancel();
            _chatCts?.Dispose();
            base.Destroy();
        }
    }
}
