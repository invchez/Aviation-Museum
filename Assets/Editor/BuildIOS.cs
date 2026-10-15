using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Command-line iOS build entry points, e.g.:
//   Unity -batchmode -quit -buildTarget iOS -executeMethod BuildIOS.BuildSimulator
public static class BuildIOS
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Main Menu.unity",
        "Assets/Scenes/Museum.unity",
        "Assets/Scenes/Aircraft Viewer.unity",
    };

    public static void BuildSimulator()
    {
        Build(iOSSdkVersion.SimulatorSDK, "Builds/iOS-Simulator");
    }

    public static void BuildDevice()
    {
        Build(iOSSdkVersion.DeviceSDK, "Builds/iOS");
    }

    static void Build(iOSSdkVersion sdk, string outputPath)
    {
        UpgradeUrpAssets();

        PlayerSettings.iOS.sdkVersion = sdk;

        BuildReport report = BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.iOS, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"iOS build failed: {report.summary.result}");
        }

        Debug.Log($"iOS Xcode project generated at {outputPath}");
    }

    // The project was saved by an older editor, so the URP assets need a version
    // migration before the build preprocessor will accept them. URP normally
    // schedules that migration via EditorApplication.delayCall, which never fires
    // ahead of the build in -quit batch mode, so invoke the upgraders directly.
    static void UpgradeUrpAssets()
    {
        try
        {
            MethodInfo pipelineUpgrade = typeof(UniversalRenderPipelineAsset)
                .GetMethod("UpgradeAsset", BindingFlags.NonPublic | BindingFlags.Static);

            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && pipelineUpgrade != null)
                {
                    pipelineUpgrade.Invoke(null, new object[] { asset.GetInstanceID() });
                    EditorUtility.SetDirty(asset);
                }
            }

            // UniversalRenderPipelineGlobalSettings is internal, so it is only reachable via reflection.
            Type globalSettingsType = typeof(UniversalRenderPipelineAsset).Assembly
                .GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
            MethodInfo globalSettingsUpgrade = globalSettingsType?.GetMethod("UpgradeAsset", BindingFlags.Public | BindingFlags.Static);

            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings"))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && globalSettingsUpgrade != null && globalSettingsType.IsInstanceOfType(asset))
                {
                    globalSettingsUpgrade.Invoke(null, new object[] { asset.GetInstanceID() });
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            // Assets saved by the current editor are already at the last version;
            // this pre-pass only matters for stale assets, so never fail the build here.
            Debug.LogWarning($"URP asset upgrade pre-pass skipped: {e.Message}");
        }
    }
}
