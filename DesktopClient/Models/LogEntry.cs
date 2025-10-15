using System;

namespace MicroluxErgConnect.Models;

public record LogEntry(DateTime Timestamp, string Level, string Message);
