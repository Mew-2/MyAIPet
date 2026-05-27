using MyAIPet.Services;
using MyAIPet.Services.Interfaces;
using MyAIPet.ViewModels;
using MyAIPet.Views;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace MyAIPet
{
    public partial class App : PrismApplication
    {
        private const string DefaultPersonality = "你是一个可爱的桌面宠物AI，性格活泼开朗，喜欢和用户聊天，说话方式轻松有趣。你会记住和用户的对话内容，逐渐了解用户的偏好。";

        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/app.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Debug()
                .CreateLogger();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            containerRegistry.RegisterSingleton<IAnimationService>(() => new AnimationService(resourcesPath));

            containerRegistry.RegisterSingleton<IMemoryService, MemoryService>();

            var chatService = new BackendChatService("http://localhost:8000");
            containerRegistry.RegisterSingleton<IChatService>(() => chatService);
            Log.Information("已配置后端聊天服务 (http://localhost:8000)");

            containerRegistry.Register<MainWindowViewModel>();
            containerRegistry.Register<ChatBubbleViewModel>();
        }
    }
}
