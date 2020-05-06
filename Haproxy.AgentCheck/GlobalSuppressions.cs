using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "double dispose pattern is overkill in our case")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "No plan to localize error messages now")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Useless on netcore")]
[assembly: SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Unused code is useless")]
