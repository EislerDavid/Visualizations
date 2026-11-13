using Silk.NET.OpenGL;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>Vstup pro vykreslení jednoho snímku.</summary>
/// <param name="Audio">Zvuková data sdílená všemi animacemi.</param>
/// <param name="DeltaTime">Čas od minulého snímku v sekundách.</param>
/// <param name="Time">Čas od spuštění aplikace v sekundách.</param>
/// <param name="Target">
/// Plátno, do kterého se kreslí. Při volání je už nastavené a vymazané na černo; animace s vlastními
/// pomocnými framebuffery ho musí před finálním kreslením znovu nastavit přes <see cref="RenderTarget.Bind"/>.
/// </param>
internal readonly record struct FrameContext(AudioFrame Audio, float DeltaTime, double Time, RenderTarget Target);

/// <summary>Jedna vizualizace (ekvivalent presetu ve Windows Media Playeru).</summary>
internal interface IVisualization : IDisposable
{
    /// <summary>Vytvoří GPU prostředky; volá se jednou, když je OpenGL kontext připravený.</summary>
    void Load(GL gl);

    /// <summary>Velikost plátna v pixelech; volá se po <see cref="Load"/> a při každé změně velikosti.</summary>
    void Resize(int width, int height);

    void Render(in FrameContext frame);
}
