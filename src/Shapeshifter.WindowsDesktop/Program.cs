namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            const string libraryExtension = ".dll";

            var assemblyCache = new Dictionary<string, Assembly>();
            var executingAssembly = Assembly.GetExecutingAssembly();
            var allResources = executingAssembly
                .GetManifestResourceNames();
            var resources = allResources
                .Where(n => n.EndsWith(libraryExtension))
                .Select(x => x.Substring(0, x.Length - libraryExtension.Length));

            foreach (var resource in resources)
            {
                LoadResourceToAssemblyCache(executingAssembly, resource, assemblyCache);
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => ResolveAssemblyFromCache(e, assemblyCache);

            App.Main();
        }

        static Assembly ResolveAssemblyFromCache(ResolveEventArgs e, Dictionary<string, Assembly> assemblyCache)
        {
            var assemblyName = new AssemblyName(e.Name);
            var path = $"{assemblyName.Name}.dll"
                .ToLower();
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

        static void LoadResourceToAssemblyCache(Assembly executingAssembly, string resource, Dictionary<string, Assembly> assemblies)
        {
            var assemblyBytes = LoadResourceBytes(executingAssembly, $"{resource}.dll");
            var symbolBytes = LoadResourceBytes(executingAssembly, $"{resource}.pdb");

            try
            {
                var assembly = symbolBytes != null
                                   ? Assembly.Load(assemblyBytes, symbolBytes)
                                   : Assembly.Load(assemblyBytes);
                assemblies.Add(resource.ToLower(), assembly);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load: {0}, Exception: {1}", resource, ex.Message);
            }
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