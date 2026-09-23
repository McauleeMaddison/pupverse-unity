using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public sealed class RunBattleChecks : ICallbacks
{
    static TestRunnerApi api;
    static RunBattleChecks()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new RunBattleChecks());
    }

    [MenuItem("Pupverse/Run Battle Checks (Temporary)")]
    public static void Run()
    {
        File.WriteAllText("/private/tmp/pupverse-stats/test-status.txt", "Running");
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            groupNames = new[] { "^Pupverse.Tests.BattleCardAnimatorTests", "^Pupverse.Tests.BattleRulesTests" } }));
    }
    public void RunStarted(ITestAdaptor tests) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, "/private/tmp/pupverse-stats/test-results.xml");
        File.WriteAllText("/private/tmp/pupverse-stats/test-status.txt", result.ResultState + " passed=" + result.PassCount + " failed=" + result.FailCount);
    }
}
