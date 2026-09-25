using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public sealed class RunBattleChecks : ICallbacks
{
    static TestRunnerApi api;
    static string ResultsDirectory => Path.Combine(Application.temporaryCachePath, "BattleChecks");
    static RunBattleChecks()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new RunBattleChecks());
    }

    [MenuItem("Pupverse/Run Battle Checks (Temporary)")]
    public static void Run()
    {
        Directory.CreateDirectory(ResultsDirectory);
        File.WriteAllText(Path.Combine(ResultsDirectory, "test-status.txt"), "Running");
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            groupNames = new[] { "^Pupverse.Tests.BattleCardAnimatorTests", "^Pupverse.Tests.BattleRulesTests", "^Pupverse.Tests.BattleHudPresentationTests", "^Pupverse.Tests.SceneIntegrityTests" } }));
    }
    public void RunStarted(ITestAdaptor tests) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, Path.Combine(ResultsDirectory, "test-results.xml"));
        File.WriteAllText(Path.Combine(ResultsDirectory, "test-status.txt"), result.ResultState + " passed=" + result.PassCount + " failed=" + result.FailCount);
    }
}
