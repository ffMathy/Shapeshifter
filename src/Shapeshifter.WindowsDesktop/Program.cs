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
            var assemblies = new Dictionary<string, Assembly>();
            var executingAssembly = Assembly.GetExecutingAssembly();
            var resources = executingAssembly
                .GetManifestResourceNames()
                .Where(n => n.EndsWith(".dll"));

            foreach (var resource in resources)

            {
                using (var stream = executingAssembly.GetManifestResourceStream(resource))

                {
                    if (stream == null)
                    {
                        continue;
                    }

                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);

                    try
                    {
                        assemblies.Add(resource, Assembly.Load(bytes));
                    }

                    catch (Exception ex)
                    {
                        Debug.Print("Failed to load: {0}, Exception: {1}", resource, ex.Message);
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                var assemblyName = new AssemblyName(e.Name);
                var path = $"{assemblyName.Name}.dll";
                if (assemblies.ContainsKey(path))
                {
                    return assemblies[path];
                }

                if (e.Name.Contains("Retargetable=Yes"))
                {
                    return Assembly.Load(new AssemblyName(e.Name));
                }

                return null;
            };

            App.Main();
        }
    }
}