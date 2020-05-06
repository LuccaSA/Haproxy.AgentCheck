using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Useless on netcore")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification ="overkill in test")]
