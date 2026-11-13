// Použití: Visualizations.App [--process <název>] [--system]
//   --process <název>  zachytávat zvuk dané aplikace (výchozí: Spotify)
//   --system           začít zachytáváním celého výstupu výchozího zvukového zařízení
using System.Text;
using Visualizations.App;

Console.OutputEncoding = Encoding.UTF8;

int processIndex = Array.IndexOf(args, "--process");
string processName = processIndex >= 0 && processIndex + 1 < args.Length ? args[processIndex + 1] : "Spotify";

using var app = new VisualizerApp(processName, systemOutput: args.Contains("--system"));
app.Run();
