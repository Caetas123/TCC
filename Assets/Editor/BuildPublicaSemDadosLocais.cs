using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Evita que uma build distribuida seja gerada com informacoes de desenvolvimento
/// que podem revelar caminhos absolutos da maquina usada para compilar o jogo.
/// </summary>
public sealed class BuildPublicaSemDadosLocais : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        const BuildOptions opcoesDeDesenvolvimento =
            BuildOptions.Development |
            BuildOptions.AllowDebugging |
            BuildOptions.ConnectWithProfiler |
            BuildOptions.EnableDeepProfilingSupport;

        if ((report.summary.options & opcoesDeDesenvolvimento) != 0)
        {
            throw new BuildFailedException(
                "Build publica bloqueada: desmarque Development Build, Script Debugging, " +
                "Autoconnect Profiler e Deep Profiling antes de gerar a versao para testes.");
        }
    }

    [MenuItem("Ferramentas/Build/Verificar build publica")]
    private static void VerificarBuildPublica()
    {
        UnityEngine.Debug.Log(
            "Build publica configurada: sem Development Build, sem Script Debugging, " +
            "sem Profiler e sem logs persistentes do jogador.");
    }
}
