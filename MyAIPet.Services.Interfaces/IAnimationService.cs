using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MyAIPet.Services.Interfaces;

public interface IAnimationService
{
    BitmapSource[] LoadAnimationFrames(AnimationType type);
    Task PlayAnimationAsync(AnimationType type, int loopCount = 1, int fps = 12, CancellationToken cancellationToken = default);
    Task PlayDrawAnimationAsync(int loopCount, int fps = 12, CancellationToken cancellationToken = default);
    Task StartIdleLoopAsync(CancellationToken cancellationToken = default);
    void StopIdleLoop();
    event EventHandler<BitmapSource>? OnFrameChanged;
}

public class IdleAction
{
    public AnimationType Type { get; set; }
    public int Weight { get; set; }
    public string Name { get; set; } = string.Empty;
}

public enum AnimationType
{
    Eye,
    ThinkEvil,
    DrawIn,
    DrawLoop,
    DrawOut,
    RaiseHand,
    Think,
    ThinkSerious,
    Loop,
    Surprised,
    Number1233,
    GiveSticker
}
