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

            var apiKey = GetApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                var memoryService = Container.Resolve<IMemoryService>();
                var chatService = new KimiChatService(apiKey, memoryService);
                chatService.SetPersonalityPrompt(GetPersonalityPrompt());
                containerRegistry.RegisterSingleton<IChatService>(() => chatService);
                Log.Information("已使用Kimi API配置对话服务");
            }
            else
            {
                Log.Error("未配置Kimi API Key，请检查配置文件");
            }

            containerRegistry.Register<MainWindowViewModel>();
            containerRegistry.Register<ChatBubbleViewModel>();
        }

        private string GetApiKey()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            if (File.Exists(configPath))
            {
                var lines = File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("KIMI_API_KEY="))
                    {
                        return line.Substring("KIMI_API_KEY=".Length).Trim();
                    }
                }
            }
            return Environment.GetEnvironmentVariable("KIMI_API_KEY") ?? string.Empty;
        }

        private string GetPersonalityPrompt()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            if (File.Exists(configPath))
            {
                var lines = File.ReadAllLines(configPath);
                var personalityBuilder = new System.Text.StringBuilder();
                var inPersonality = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("PERSONALITY="))
                    {
                        inPersonality = true;
                        var value = trimmed.Substring("PERSONALITY=".Length).Trim();
                        if (!string.IsNullOrEmpty(value) && !value.StartsWith("#"))
                        {
                            personalityBuilder.Append(value);
                        }
                    }
                    else if (inPersonality && !trimmed.StartsWith("#") && !string.IsNullOrEmpty(trimmed))
                    {
                        personalityBuilder.Append("\n").Append(trimmed);
                    }
                }

                if (personalityBuilder.Length > 0)
                {
                    return personalityBuilder.ToString();
                }
            }
            return DefaultPersonality;
        }
    }
}
