#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
namespace Pupverse.Editor
{
    public static class AppleAccountBuild
    {
        [PostProcessBuild(100)] public static void Configure(BuildTarget target,string path)
        {
            if(target!=BuildTarget.iOS)return;
            string projectPath=PBXProject.GetPBXProjectPath(path);var project=new PBXProject();project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(),"AuthenticationServices.framework",false);
            project.WriteToFile(projectPath);
            var config=Resources.Load<PlayerAccountConfig>("PupverseAccountConfig");
            if(config==null || !config.IsConfigured || !config.appleEnabled)return;
            string entitlement=project.GetBuildPropertyForAnyConfig(project.GetUnityMainTargetGuid(),"CODE_SIGN_ENTITLEMENTS");
            if(string.IsNullOrEmpty(entitlement))entitlement="Pupverse.entitlements";
            var capabilities=new ProjectCapabilityManager(projectPath,entitlement,null,project.GetUnityMainTargetGuid());
            capabilities.AddSignInWithApple();capabilities.WriteToFile();
        }
    }
}
#endif
