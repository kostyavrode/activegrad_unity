#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Для шагомера на iOS: описание разрешения «Движение и фитнес» и фреймворк CoreMotion.
/// </summary>
public static class StepsIosPostProcess
{
    private const string MotionUsageDescription =
        "АктивГрад считает ваши шаги, чтобы засчитывать прогресс в квестах, даже когда игра закрыта.";

    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS)
            return;

        var plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetString("NSMotionUsageDescription", MotionUsageDescription);
        plist.WriteToFile(plistPath);

        var projectPath = PBXProject.GetPBXProjectPath(buildPath);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "CoreMotion.framework", false);
        project.WriteToFile(projectPath);
    }
}
#endif
