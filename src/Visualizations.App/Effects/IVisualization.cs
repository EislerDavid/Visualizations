using Silk.NET.OpenGL;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>Jedna vizualizace (ekvivalent presetu ve Windows Media Playeru).</summary>
internal interface IVisualization : IDisposable
{
    string Name { get; }

    /// <summary>Vytvoří GPU prostředky; volá se jednou, když je OpenGL kontext připravený.</summary>
    void Load(GL gl);

    /// <summary>Velikost framebufferu v pixelech.</summary>
    void Resize(int width, int height);

    void Render(SpectrumAnalyzer audio, float deltaTime);
}
