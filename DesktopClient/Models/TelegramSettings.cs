using System;

namespace MicroluxErgConnect.Models;

public sealed class TelegramSettings
{
    private const string DefaultBotToken = "8258402461:AAF3-llnHwTfr0HiBiRTgjJBq8c9SOY2r90";
    private const string DefaultChatId = "314241927";

    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = "Info";
    public bool ForwardReports { get; set; } = true;
    public bool ForwardJson { get; set; } = true;
    public bool ForwardRawData { get; set; } = true;
    public bool SendLogOnExit { get; set; } = true;

    public static TelegramSettings CreateDefault()
        => new()
        {
            Enabled = true,
            BotToken = DefaultBotToken,
            ChatId = DefaultChatId,
            MinimumLevel = "Info",
            ForwardReports = true,
            ForwardJson = true,
            ForwardRawData = true,
            SendLogOnExit = true
        };

    public string DescribeSafety()
    {
        var chat = string.IsNullOrWhiteSpace(ChatId) ? "<не задан>" : ChatId;
        return $"enabled={(Enabled ? "да" : "нет")}, chatId={chat}, minLevel={MinimumLevel}, reports={(ForwardReports ? "да" : "нет")}, json={(ForwardJson ? "да" : "нет")}, raw={(ForwardRawData ? "да" : "нет")}, logOnExit={(SendLogOnExit ? "да" : "нет")}"; 
    }
}
