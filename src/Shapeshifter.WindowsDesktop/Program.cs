namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Windows;

    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var temporaryPath = Path.GetTempPath();
            if (Environment.CurrentDirectory != temporaryPath)
            {
                WriteDependentAssembliesToDisk(temporaryPath);
                File.WriteAllBytes(
                    Path.Combine(Environment.CurrentDirectory, ));
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => ResolveAssemblyFromCache(e, assemblyCache);

            Initialize();
        }

        static void WriteDependentAssembliesToDisk(string temporaryPath)
        {
            const string libraryExtension = ".dll";

            var executingAssembly = Assembly.GetExecutingAssembly();
            var allResources = executingAssembly
                .GetManifestResourceNames();
            var resources = allResources
                .Where(n => n.EndsWith(libraryExtension))
                .Select(x => x.Substring(0, x.Length - libraryExtension.Length));

            foreach (var resource in resources)
            {
                WriteResourceToDisk(executingAssembly, resource, temporaryPath);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Initialize()
        {
            try
            {
                App.Main();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        static Assembly ResolveAssemblyFromCache(ResolveEventArgs e, IReadOnlyDictionary<string, Assembly> assemblyCache)
        {
            var assemblyName = new AssemblyName(e.Name);
            var path = $"{assemblyName.Name}.dll";
            if (assemblyCache.ContainsKey(path))
            {
                return assemblyCache[path];
            }

            if (e.Name.Contains("Retargetable=Yes"))
            {
                return Assembly.Load(new AssemblyName(e.Name));
            }

            return null;
        }

        static void WriteResourceToDisk(Assembly executingAssembly, string resource, string targetPath)
        {
            var assemblyFileName = $"{resource}.dll";
            var assemblyBytes = LoadResourceBytes(executingAssembly, assemblyFileName);
            var symbolFileName = $"{resource}.pdb";
            var symbolBytes = LoadResourceBytes(executingAssembly, symbolFileName);

            File.WriteAllBytes(Path.Combine(targetPath, assemblyFileName), assemblyBytes);
            File.WriteAllBytes(Path.Combine(targetPath, symbolFileName), symbolBytes);
        }

        static byte[] LoadResourceBytes(Assembly executingAssembly, string resourceName)
        {
            using (var stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                return data;
            }
        }
    }
}