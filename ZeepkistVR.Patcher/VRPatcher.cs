using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

public static class Patcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly)
    {

    }

    public static void Initialize()
    {
        Console.WriteLine("Patching Zeepkist...");

        var installerPath = Assembly.GetExecutingAssembly().Location;
        Console.WriteLine("installerPath " + installerPath);

        //"M:\\SteamLibrary\\steamapps\\common\\Zeepkist\\Zeepkist.exe";
        var gameExePath = Process.GetCurrentProcess().MainModule.FileName;
        Console.WriteLine("gameExePath " + gameExePath);

        var gamePath = Path.GetDirectoryName(gameExePath);
        var gameName = Path.GetFileNameWithoutExtension(gameExePath);
        var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        var gameManagersPath = Path.Combine(dataPath, $"globalgamemanagers");
        var gameManagersBackupPath = CreateGameManagersBackup(gameManagersPath);
        var patcherPath = Path.GetDirectoryName(installerPath);
        var classDataPath = Path.Combine(patcherPath, "classdata.tpk");

        PatchVR(gameManagersBackupPath, gameManagersPath, classDataPath);

        Console.WriteLine("Installed successfully, probably.");
    }

    private static string CreateGameManagersBackup(string gameManagersPath)
    {
        Console.WriteLine($"Backing up '{gameManagersPath}'...");
        var backupPath = gameManagersPath + ".bak";
        if (File.Exists(backupPath))
        {
            Console.WriteLine($"Backup already exists.");
            return backupPath;
        }

        File.Copy(gameManagersPath, backupPath);
        Console.WriteLine($"Created backup in '{backupPath}'");
        return backupPath;
    }

    private static void PatchVR(string gameManagersBackupPath, string gameManagersPath, string classDataPath)
    {
        Console.WriteLine($"Using classData file from path '{classDataPath}'");

        var manager = new AssetsManager();
        manager.LoadClassPackage(classDataPath);

        var afileInst = manager.LoadAssetsFile(gameManagersBackupPath, false);
        var afile = afileInst.file;
        manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);

        var buildSettings = afile.GetAssetsOfType(AssetClassID.BuildSettings);
        foreach (var goInfo in buildSettings)
        {
            var goBase = manager.GetBaseField(afileInst, goInfo);
            var vrDevicesComponent = goBase["enabledVRDevices.Array"];

            var oculusArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrDevicesComponent);
            oculusArrayItem.AsString = "Oculus";
            vrDevicesComponent.Children.Add( oculusArrayItem );

            var openvrArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrDevicesComponent);
            openvrArrayItem.AsString = "OpenVR";
            vrDevicesComponent.Children.Add(openvrArrayItem);

            var noneArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrDevicesComponent);
            noneArrayItem.AsString = "None";
            vrDevicesComponent.Children.Add(noneArrayItem);

            goInfo.SetNewData(goBase);
        }

        using (AssetsFileWriter writer = new AssetsFileWriter(gameManagersPath))
        {
            afile.Write(writer);
        }
    }
}