﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using EclipseUpdater.Api;

namespace EclipseUpdater
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            //Task.Run(TestReleases);
            app.Run(new MainWindow());
        }

        private static async Task TestReleases() {
            var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

            var releases = await releasesClient.GetReleasesAsync(new Guid("0836bbd7-d9b4-466a-a566-7670bd568e3b"), 0, 1);

            if (releases.Items.Count > 0) {
                var latestRelease = releases.Items[0];

                var downloadUrl = latestRelease.GetDownloadUrl();
            }
        }
    }
}
