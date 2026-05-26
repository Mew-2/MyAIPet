using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MyAIPet.Services.Interfaces;

namespace MyAIPet.Services;

public class AnimationService : IAnimationService
{
    private readonly string _resourcesPath;
    private readonly Dictionary<AnimationType, AnimationInfo> _animationMap;
    private readonly List<IdleAction> _majorActions;
    private CancellationTokenSource? _idleCts;
    private readonly Random _random = new();

    public AnimationService(string resourcesPath)
    {
        _resourcesPath = resourcesPath;
        _animationMap = InitializeAnimationMap();
        _majorActions = InitializeMajorActions();
    }

    private List<IdleAction> InitializeMajorActions()
    {
        return new List<IdleAction>
        {
            new IdleAction { Type = AnimationType.ThinkEvil, Weight = 18, Name = "腹黑思考" },
            new IdleAction { Type = AnimationType.RaiseHand, Weight = 18, Name = "举手" },
            new IdleAction { Type = AnimationType.Think, Weight = 18, Name = "思考" },
            new IdleAction { Type = AnimationType.ThinkSerious, Weight = 13, Name = "认真思考" },
            new IdleAction { Type = AnimationType.Surprised, Weight = 13, Name = "惊讶" },
            new IdleAction { Type = AnimationType.DrawIn, Weight = 10, Name = "画画" },
            new IdleAction { Type = AnimationType.Number1233, Weight = 5, Name = "1233" },
            new IdleAction { Type = AnimationType.GiveSticker, Weight = 5, Name = "递贴纸" }
        };
    }

    public async Task StartIdleLoopAsync(CancellationToken cancellationToken = default)
    {
        _idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _idleCts.Token;

        while (!token.IsCancellationRequested)
        {
            var loopCount = 2 + _random.Next(3);

            for (int i = 0; i < loopCount && !token.IsCancellationRequested; i++)
            {
                await PlayAnimationAsync(AnimationType.Loop, 1, 12, token);
            }

            if (token.IsCancellationRequested) break;

            var action = SelectWeightedRandomAction();
            if (action.Type == AnimationType.DrawIn)
            {
                var drawLoopCount = 1 + _random.Next(3);
                await PlayDrawAnimationAsync(drawLoopCount, 12, token);
            }
            else
            {
                await PlayAnimationAsync(action.Type, 1, 12, token);
            }
        }
    }

    public void StopIdleLoop()
    {
        _idleCts?.Cancel();
    }

    private IdleAction SelectWeightedRandomAction()
    {
        var totalWeight = 0;
        foreach (var action in _majorActions)
        {
            totalWeight += action.Weight;
        }

        var randomValue = _random.Next(totalWeight);
        var cumulative = 0;

        foreach (var action in _majorActions)
        {
            cumulative += action.Weight;
            if (randomValue < cumulative)
            {
                return action;
            }
        }

        return _majorActions[0];
    }

    private Dictionary<AnimationType, AnimationInfo> InitializeAnimationMap()
    {
        return new Dictionary<AnimationType, AnimationInfo>
        {
            [AnimationType.Eye] = new AnimationInfo
            {
                Folder = "eye",
                Prefix = "xgw-eye",
                FrameCount = 24,
                DefaultFps = 12
            },
            [AnimationType.ThinkEvil] = new AnimationInfo
            {
                Folder = "flft_lf",
                Prefix = "xgw-思考腹黑",
                FrameCount = 98,
                DefaultFps = 12
            },
            [AnimationType.DrawIn] = new AnimationInfo
            {
                Folder = "glgl",
                Prefix = "xgw-画画in",
                FrameCount = 25,
                DefaultFps = 12
            },
            [AnimationType.DrawLoop] = new AnimationInfo
            {
                Folder = "glgl",
                Prefix = "xgw-画画loop",
                FrameCount = 161,
                DefaultFps = 12
            },
            [AnimationType.DrawOut] = new AnimationInfo
            {
                Folder = "glgl",
                Prefix = "xgw-画画out",
                FrameCount = 83,
                DefaultFps = 12
            },
            [AnimationType.RaiseHand] = new AnimationInfo
            {
                Folder = "iwrt",
                Prefix = "xgw-举手",
                FrameCount = 75,
                DefaultFps = 12
            },
            [AnimationType.Think] = new AnimationInfo
            {
                Folder = "lnft",
                Prefix = "xgw-思考",
                FrameCount = 98,
                DefaultFps = 12
            },
            [AnimationType.ThinkSerious] = new AnimationInfo
            {
                Folder = "lnft_ll",
                Prefix = "xgw-思考认真",
                FrameCount = 98,
                DefaultFps = 12
            },
            [AnimationType.Loop] = new AnimationInfo
            {
                Folder = "loop",
                Prefix = "xgw-loop",
                FrameCount = 161,
                DefaultFps = 12
            },
            [AnimationType.Surprised] = new AnimationInfo
            {
                Folder = "nyya",
                Prefix = "xgw-惊讶",
                FrameCount = 98,
                DefaultFps = 12
            },
            [AnimationType.Number1233] = new AnimationInfo
            {
                Folder = "rudi",
                Prefix = "xgw-1233",
                FrameCount = 30,
                DefaultFps = 12
            },
            [AnimationType.GiveSticker] = new AnimationInfo
            {
                Folder = "send_trkk",
                Prefix = "xgw-递贴纸",
                FrameCount = 75,
                DefaultFps = 12
            }
        };
    }

    public BitmapSource[] LoadAnimationFrames(AnimationType type)
    {
        if (!_animationMap.TryGetValue(type, out var info))
        {
            throw new ArgumentException($"Unknown animation type: {type}");
        }

        var frames = new BitmapSource[info.FrameCount];
        var folderPath = Path.Combine(_resourcesPath, "SequenceFrame", info.Folder);

        for (int i = 0; i < info.FrameCount; i++)
        {
            var fileName = $"{info.Prefix}_{i:D2}.gif";
            var filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                fileName = $"{info.Prefix}_{i:D3}.gif";
                filePath = Path.Combine(folderPath, fileName);
            }

            if (File.Exists(filePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                frames[i] = bitmap;
            }
            else
            {
                throw new FileNotFoundException($"Frame not found: {filePath}");
            }
        }

        return frames;
    }

    public async Task PlayAnimationAsync(AnimationType type, int loopCount = 1, int fps = 12, CancellationToken cancellationToken = default)
    {
        if (!_animationMap.TryGetValue(type, out var info))
        {
            throw new ArgumentException($"Unknown animation type: {type}");
        }

        var frames = LoadAnimationFrames(type);
        var frameDelay = 1000 / fps;

        for (int loop = 0; loop < loopCount; loop++)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnFrameChanged?.Invoke(this, frames[i]);
                await Task.Delay(frameDelay, cancellationToken);
            }
        }
    }

    public async Task PlayDrawAnimationAsync(int loopCount, int fps = 12, CancellationToken cancellationToken = default)
    {
        var frameDelay = 1000 / fps;

        var inFrames = LoadAnimationFrames(AnimationType.DrawIn);
        var loopFrames = LoadAnimationFrames(AnimationType.DrawLoop);
        var outFrames = LoadAnimationFrames(AnimationType.DrawOut);

        for (int i = 0; i < inFrames.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OnFrameChanged?.Invoke(this, inFrames[i]);
            await Task.Delay(frameDelay, cancellationToken);
        }

        for (int loop = 0; loop < loopCount; loop++)
        {
            for (int i = 0; i < loopFrames.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnFrameChanged?.Invoke(this, loopFrames[i]);
                await Task.Delay(frameDelay, cancellationToken);
            }
        }

        for (int i = 0; i < outFrames.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OnFrameChanged?.Invoke(this, outFrames[i]);
            await Task.Delay(frameDelay, cancellationToken);
        }
    }

    public event EventHandler<BitmapSource>? OnFrameChanged;
}

public class AnimationInfo
{
    public string Folder { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int FrameCount { get; set; }
    public int DefaultFps { get; set; } = 12;
}