using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


public class TutorialSelector : EditorWindow
{
    private VisualElement root;
    private Label label;
    private Manifest manifest;

    [MenuItem("Tutorial/TutorialSelector")]
    public static void ShowExample()
    {
        TutorialSelector wnd = GetWindow<TutorialSelector>();
        wnd.titleContent = new GUIContent("TutorialSelector");
    }

    public void CreateGUI()
    {
        manifest = Manifest.GetManifest();
        
        // Each editor window contains a root VisualElement object
        root = rootVisualElement;

        if (false)  // enable tutorial edit tools
        {
            var saveButton = new Button();
            saveButton.text = "Save Current Step";
            saveButton.clicked += SaveStep;
            root.Add(saveButton);
            
            var addStepButton = new Button();
            addStepButton.text = "Add Step";
            addStepButton.clicked += AddStep;
            root.Add(addStepButton);
        }

        label = new Label($"Current Step: {manifest.CurrentStepIdx}");
        root.Add(label);
        
        for (int i = 1; i <= manifest.NumSteps; i++)
        {
            var button = new Button();
            button.text = "Load Step " + i;
            int val = i;
            button.clicked += () => LoadStep(val);
            root.Add(button);
        }
    }


    public void SaveStep()
    {
        manifest.SaveStep();

        EditorUtility.DisplayDialog("Tutorial", $"Saved Step {manifest.CurrentStepIdx}", "OK");
    }

    public void AddStep()
    {
        manifest.AddStep();

        var button = new Button();
        int step = manifest.NumSteps;
        button.text = "Load Step " + step;
        button.clicked += () => LoadStep(step);
        root.Add(button);
    }

    public void LoadStep(int step)
    {
        manifest.LoadStep(step);
        EditorUtility.DisplayDialog("Tutorial", $"Loading Step {step}", "OK");
        AssetDatabase.Refresh();
        var destDir = new DirectoryInfo(Application.dataPath);
        if (!destDir.Exists)
        {
            Debug.LogError("Could not find assets directory");
            return;
        }
        
        // load the scene file in the root directory
        foreach (var file in destDir.GetFiles())
        {
            if (file.Extension == ".unity")
            {
                EditorSceneManager.OpenScene(file.FullName);
            }
        }

        label.text = $"Currently Step: {step}";
    }
}


[Serializable]
public struct FileEntry
{
    public string Path;
    public string Md5;
}

[Serializable]
public class Step
{
    public List<string> Directories;
    public List<FileEntry> Files;
}

[Serializable]
public class Manifest
{
    public List<Step> Steps;
    public int CurrentStepIdx = 0;
    public int NumSteps = 0;
    
    public static Manifest GetManifest()
    {
        var manifestPath = ManifestPath();

        Manifest manifest = null;
        
        if (File.Exists(manifestPath))
        {
            string text = File.ReadAllText(manifestPath);
            manifest = JsonUtility.FromJson<Manifest>(text);
        }
        else
        {
            manifest = new Manifest();
            manifest.Steps = new List<Step>();
            manifest.AddStep();
            manifest.CurrentStepIdx = 1;
            manifest.NumSteps = 1;
            manifest.Save();
        }

        return manifest;
    }

    public static string ManifestPath()
    {
        return Path.Combine(Application.dataPath, $"../TutorialContent/manifest.json");
    }

    public static string StepPath(int step)
    {
        return Path.Combine(Application.dataPath, $"../TutorialContent/Step_{step}/");
    }
    
    public Step GetStep(int i)
    {
        return Steps[i - 1]; 
    }

    private void Save()
    {
        var json = JsonUtility.ToJson(this);
        File.WriteAllText(ManifestPath(), json);
    }

    public void AddStep()
    {
        var step = new Step();
        step.Directories = new List<string>();
        step.Files = new List<FileEntry>();
        Steps.Add(step);
        NumSteps++;
        Directory.CreateDirectory(StepPath(Steps.Count));
        Save();
    }

    public void LoadStep(int stepIdx)
    {
        CopyAllToDir(Application.dataPath, StepPath(stepIdx), GetStep(stepIdx));
        CurrentStepIdx = stepIdx;
        Save();
    }

    public void SaveStep()
    {
        var step = GetStep(CurrentStepIdx);
        var oldFiles = step.Files; // cache before we update the manifest
        UpdateStep(step);
        CopyAllToDir(StepPath(CurrentStepIdx), Application.dataPath, step);
        UpdateMatchingFilesInSuccessiveSteps(step.Files, oldFiles, CurrentStepIdx);
        Save();
    }

    private string md5AtPath(List<FileEntry> files, string path)
    {
        foreach (var f in files)
        {
            if (f.Path == path)
            {
                return f.Md5;
            }
        }

        return null;
    }

    private void updateMd5AtPath(List<FileEntry> files, string path, string newMd5)
    {
        for (int i = 0; i < files.Count; i++)
        {
            var f = files[i];
            if (f.Path == path)
            {
                f.Md5 = newMd5;
                files[i] = f;
                return;
            }
        }
    }

    // todo untested
    private void UpdateMatchingFilesInSuccessiveSteps(List<FileEntry> newFiles, List<FileEntry> oldFiles, int stepIdx)
    {
        var srcDir = StepPath(stepIdx);

        foreach (var file in newFiles)
        {
            var oldMd5 = md5AtPath(oldFiles, file.Path);
            if (oldMd5 == null || oldMd5.Equals(file.Md5)) // file is new in this step or unchanged
            {
                continue;
            }

            for (int i = stepIdx + 1; i <= NumSteps; i++)
            {
                var stepFiles = GetStep(i).Files;
                var nextMd5 = md5AtPath(stepFiles, file.Path);
                if (oldMd5 != nextMd5)  
                {
                    // the path in this step does not match the old file, so the path
                    // should not be updated in this step or the following steps
                    break;
                }

                File.Copy(Path.Combine(srcDir, file.Path),
                    Path.Combine(StepPath(i), file.Path),
                    true);

                updateMd5AtPath(stepFiles, file.Path, file.Md5);
            }
        }
    }

    private string getFileMD5(string path)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(path))
            {
                var hash = md5.ComputeHash(stream);
                return ToHex(hash);
            }
        }
    }

    public static string ToHex(byte[] bytes)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);

        for (int i = 0; i < bytes.Length; i++)
            result.Append(bytes[i].ToString("x2"));

        return result.ToString();
    }

    private List<FileEntry> ScanFiles(string path)
    {
        var fileEntries = new List<FileEntry>();

        foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
        {
            var fileRelative = file.Remove(0, path.Length + 1); 
            if (!fileRelative.StartsWith("Common"))
            {
                fileEntries.Add(new FileEntry
                {
                    Path = fileRelative,
                    Md5 = getFileMD5(file)
                });
            }
        }

        return fileEntries;
    }

    private List<string> ScanSubdirs(string path)
    {
        var subdirs = new List<string>();

        foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
        {
            var dirRelative = dir.Remove(0, path.Length + 1) + "/";
            if (!dirRelative.StartsWith("Common"))
            {
                subdirs.Add(dirRelative);
            }
        }

        return subdirs;
    }

    public void UpdateStep(Step step)
    {
        // for a step, read Assets to update the list of dirs, files, and hashes 
        step.Directories = ScanSubdirs(Application.dataPath);
        step.Files = ScanFiles(Application.dataPath);
    }

    public void CopyAllToDir(string destDir, string srcDir, Step step)
    {
        // clear the contents of the destDir (except for Common)
        var destInfo = new DirectoryInfo(destDir);
        foreach (var dir in destInfo.EnumerateDirectories())
        {
            if (!dir.Name.StartsWith("Common"))
            {
                Directory.Delete(dir.FullName, true);
            }
        }

        foreach (var file in destInfo.GetFiles())
        {
            if (!file.Name.StartsWith("Common"))
            {
                file.Delete();
            }
        }

        // create all subdirs
        foreach (var dir in step.Directories)
        {
            Directory.CreateDirectory(Path.Combine(destDir, dir));
        }

        // copy the files
        foreach (var file in step.Files)
        {
            File.Copy(Path.Combine(srcDir, file.Path),
                Path.Combine(destDir, file.Path),
                true);
        }
    }
}