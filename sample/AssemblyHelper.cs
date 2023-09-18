using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SilkierQuartz
{
    public static class AssemblyHelper
    {
        public static List<Assembly> GetAssemblies(
            Assembly entryAssembly = null, string filter = null)
        {
            string localFilter
                = String.IsNullOrWhiteSpace(filter)
                ? "*"
                : filter;

            var returnAssemblies = new List<Assembly>();
            var loadedAssemblies = new HashSet<string>();
            var assembliesToCheck = new Queue<Assembly>();

            void addAssembly(AssemblyName asmName)
            {
                if (!loadedAssemblies.Contains(asmName.FullName))
                {
                    try
                    {
                        var assembly = Assembly.Load(asmName);

                        if (string.IsNullOrEmpty(filter)
                            || Regex.IsMatch(asmName.FullName, WildCardToRegular(localFilter)))
                        {
                            loadedAssemblies.Add(asmName.FullName);
                            returnAssemblies.Add(assembly);
                        }

                        assembliesToCheck.Enqueue(assembly);
                    }
                    catch
                    {
                        loadedAssemblies.Add(asmName.FullName);
                    }
                }
            }

            addAssembly((entryAssembly ?? Assembly.GetEntryAssembly()).GetName());
            while (assembliesToCheck.Any())
            {
                var assemblyToCheck = assembliesToCheck.Dequeue();
                foreach (var reference in assemblyToCheck.GetReferencedAssemblies()
                    .Where(x => !IsExcluded(x)))
                {
                    addAssembly(reference);
                }
            }

            return returnAssemblies;
        }

        public static List<Assembly> GetAssembliesFromExecutionFolder(
            Assembly entryAssembly = null, string filter = null)
        {
            string localFilter
                = String.IsNullOrWhiteSpace(filter)
                ? "*"
                : filter;
                       
            List<Assembly> returnAssemblies = new();
            string path = Path.GetDirectoryName(entryAssembly.Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                var assembly = Assembly.LoadFile(dll);

                if ((string.IsNullOrEmpty(filter)
                    || Regex.IsMatch(assembly.FullName, WildCardToRegular(localFilter)))
                    && !IsExcluded(assembly.GetName()))
                {                    
                    returnAssemblies.Add(assembly);
                }                
            }            

            return returnAssemblies;
        }

        private static bool IsExcluded(AssemblyName assemblyName)
        {            
            if (!assemblyName.Name.StartsWith("Microsoft")
                && !assemblyName.Name.StartsWith("System")
                && !assemblyName.Name.StartsWith("AutoMapper")
                && !assemblyName.Name.StartsWith("MediatR")
                && !assemblyName.Name.StartsWith("Swashbuckle")
                && !assemblyName.Name.StartsWith("Serilog")
                && !assemblyName.Name.StartsWith("AspNetCore")
                && !assemblyName.Name.StartsWith("Hangfire")
                && !assemblyName.Name.StartsWith("FluentValidation")
                && !assemblyName.Name.StartsWith("LazyCache")
                && !assemblyName.Name.StartsWith("NetTopologySuite")
                && !assemblyName.Name.StartsWith("Nito")
                && !assemblyName.Name.StartsWith("EFCore")
                && !assemblyName.Name.StartsWith("Npgsql")
                && !assemblyName.Name.StartsWith("Mailkit")
                && !assemblyName.Name.StartsWith("AppAny")
                && !assemblyName.Name.StartsWith("Quartz")
                && !assemblyName.Name.StartsWith("Newtonsoft")
                && !assemblyName.Name.StartsWith("Irony")
                && !assemblyName.Name.StartsWith("XLParser")
                && !assemblyName.Name.StartsWith("netstandard")
                && !assemblyName.Name.StartsWith("ExcelDataReader")
                && !assemblyName.Name.StartsWith("ClosedXML")
                && !assemblyName.Name.StartsWith("DocumentFormat"))
            { 
                return false;
            }

            return true;
        }

        // If you want to implement both "*" and "?"
        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}
