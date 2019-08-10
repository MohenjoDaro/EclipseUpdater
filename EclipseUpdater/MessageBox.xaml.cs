using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EclipseUpdater
{
    class MessageBox : Window
    {
        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<int> Show(string title, string text, string[] buttons, Window parent = null)
        {
            var msgbox = new MessageBox
            {
                Title = title
            };
            msgbox.FindControl<TextBlock>("Text").Text = text;
            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            for (int i = 0; i < buttons.Length; ++i)
            {
                AddButton(buttons[i], i);
            }

            var res = -1;
            void AddButton(string button, int index)
            {
                var btn = new Button { Content = button };
                btn.Click += (_, __) => {
                    res = index;
                    msgbox.Close();
                };
                buttonPanel.Children.Add(btn);
            }

            var tcs = new TaskCompletionSource<int>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };
            if (parent != null)
                msgbox.ShowDialog(parent);
            else msgbox.Show();
            return tcs.Task;
        }
    }
}