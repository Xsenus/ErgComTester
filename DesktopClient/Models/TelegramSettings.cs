using System;

namespace MicroluxErgConnect.Models;

public sealed class TelegramSettings
{
    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = "Info";
    public bool ForwardReports { get; set; } = true;
    public bool ForwardJson { get; set; } = true;
    public bool ForwardRawData { get; set; } = true;
    public bool SendLogOnExit { get; set; } = true;

    public string DescribeSafety()
    {
        var chat = string.IsNullOrWhiteSpace(ChatId) ? "<не задан>" : ChatId;
        return $"enabled={(Enabled ? "да" : "нет")}, chatId={chat}, minLevel={MinimumLevel}, reports={(ForwardReports ? "да" : "нет")}, json={(ForwardJson ? "да" : "нет")}, raw={(ForwardRawData ? "да" : "нет")}, logOnExit={(SendLogOnExit ? "да" : "нет")}";
    }
}
