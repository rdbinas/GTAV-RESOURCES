﻿using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Diagnostics;
using RageCoop.Core;

class ResourceManifest
{
    public string Name="RageCoop.Resources.Default";
    public string Description="Resource description";
    public string[] ClientResources = new string[0];
    public string[] ServerResources = new string[0];
    public Version Version=new(0,0,0,0);
}
public class Program
{
    public static void Main(string[] args)
    {
        File.WriteAllText("ResourceManifest.json",JsonConvert.SerializeObject(new ResourceManifest(),Formatting.Indented));
        foreach (var dir in Directory.GetDirectories("Resources", "*", SearchOption.AllDirectories))
        {
            try
            {
                var manifestPath = Path.Combine(dir, "ResourceManifest.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }
                Console.WriteLine("building resource from dir: " + dir);
                var manifest = JsonConvert.DeserializeObject<ResourceManifest>(File.ReadAllText(manifestPath));
                try
                {
                    BuildResource(manifest, Path.GetFullPath(dir));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to build resource:{dir}\n{ex.ToString()}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
    static void BuildResource(ResourceManifest manifest,string workingDir)
    {
        List<string> builtFolders = new List<string>();
        var binPath = Path.Combine(workingDir, "bin");
        if (Directory.Exists(binPath)) { Directory.Delete(binPath,true); }
        foreach(var c in manifest.ClientResources)
        {
            Build(c, true);
        }
        foreach (var s in manifest.ServerResources)
        {
            Build(s, false);
        }
        foreach(var fol in builtFolders)
        {
            Pack(fol);
        }
        var output = Path.Combine(workingDir, manifest.Name + ".respkg");
        if (File.Exists(output)) { File.Delete(output); }
        Console.WriteLine("Packaging to "+output);
        PackFinal(Path.Combine(workingDir,"bin","tmp"), output,Path.Combine(workingDir,"ResourceManifest.json"));
        Console.WriteLine($"Resource \"{manifest.Name}\" built successfully");
        
        void Build(string project,bool client)
        {
            var proc = new Process();
            var s = client ? "Client" : "Server";
            var buildPath = $"bin/tmp/{s}/{Path.GetFileNameWithoutExtension(project)}";
            proc.StartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = workingDir,
                FileName = "dotnet",
                Arguments = $"publish \"{project}\" --configuration Release -o \"{buildPath}\""
            };
            proc.Start();
            proc.WaitForExit();
            if(proc.ExitCode != 0) { throw new Exception("Build failed"); }
            builtFolders.Add(Path.Combine(workingDir,buildPath));
        }

        void Pack(string folder)
        {
            var target = Path.Combine(Directory.GetParent(folder).FullName, Path.GetFileName(folder) + ".res");

            Console.WriteLine("Packing project: " + target);
            using ZipFile zip = ZipFile.Create(target);
            zip.BeginUpdate();
            foreach (var dir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
            {
                zip.AddDirectory(dir[(folder.Length + 1)..]);
            }
            foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).CanBeIgnored()) { continue; }
                zip.Add(file, file[(folder.Length + 1)..]);
            }
            zip.CommitUpdate();
            zip.Close();
        }
        void PackFinal(string tmpDir,string output,string manifestPath)
        {
            var server = Path.Combine(tmpDir, "Server");
            var client = Path.Combine(tmpDir, "Client");
            using ZipFile zip = ZipFile.Create(output);
            zip.BeginUpdate();
            zip.AddDirectory("Client");
            zip.AddDirectory("Server");
            zip.Add(manifestPath, "ResourceManifest.json");
            foreach (var file in Directory.GetFiles(server,"*.res",SearchOption.TopDirectoryOnly))
            {
                zip.Add(file, file[(tmpDir.Length + 1)..]);
            }
            foreach (var file in Directory.GetFiles(client, "*.res", SearchOption.TopDirectoryOnly))
            {
                zip.Add(file, file[(tmpDir.Length + 1)..]);
            }
            zip.CommitUpdate();
            zip.Close();

        }
    }
}