using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace MediaOrcestrator.Runner;

// TODO: Перевести остальные плагины после соглосования
public sealed class WinFormsAuthUI(Control owner, ILogger logger) : IAuthUI
{
    // TODO: Подумать наж полноценной формой
    public Task<string?> PromptInputAsync(string prompt, bool isPassword = false)
    {
        return Task.FromResult(owner.Invoke(() =>
        {
            using var form = new Form
            {
                Text = "Авторизация",
                Width = 400,
                Height = 170,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
            };

            var label = new Label
            {
                Text = prompt,
                Left = 15,
                Top = 15,
                Width = 350,
            };

            var textBox = new TextBox
            {
                Left = 15,
                Top = 40,
                Width = 350,
            };

            if (isPassword)
            {
                textBox.PasswordChar = '*';
            }

            var okButton = new Button
            {
                Text = "OK",
                Left = 210,
                Top = 75,
                Width = 75,
                DialogResult = DialogResult.OK,
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                Left = 290,
                Top = 75,
                Width = 75,
                DialogResult = DialogResult.Cancel,
            };

            form.Controls.AddRange([label, textBox, okButton, cancelButton]);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            return form.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text)
                ? textBox.Text
                : null;
        }));
    }

    public async Task<string?> OpenBrowserAsync(string url, string? existingStatePath = null)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            Args = ["--disable-blink-features=AutomationControlled"],
        });

        var contextOptions = new BrowserNewContextOptions();
        if (!string.IsNullOrEmpty(existingStatePath) && File.Exists(existingStatePath))
        {
            contextOptions.StorageStatePath = existingStatePath;
        }
        else if (!string.IsNullOrEmpty(existingStatePath))
        {
            var directory = Path.GetDirectoryName(existingStatePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        logger.LogInformation("Открытие {Url}...", url);
        await page.GotoAsync(url, new()
        {
            Timeout = 0,
        });

        var confirmed = owner.Invoke(() =>
            MessageBox.Show(owner, "Зайдите в свой профиль и нажмите OK, или Отмена, если передумали",
                "Сохранить авторизацию?", MessageBoxButtons.OKCancel)
            == DialogResult.OK);

        if (!confirmed || string.IsNullOrEmpty(existingStatePath))
        {
            return null;
        }

        await context.StorageStateAsync(new()
        {
            Path = existingStatePath,
        });

        logger.LogInformation("Auth state сохранён: {Path}", existingStatePath);
        return existingStatePath;
    }

    public Task ShowMessageAsync(string message)
    {
        owner.Invoke(() => MessageBox.Show(owner, message, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information));
        return Task.CompletedTask;
    }
}
