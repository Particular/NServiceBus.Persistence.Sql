using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

class AssemblyResolver : IAssemblyResolver
{
    Action<string> log;
    Dictionary<string, AssemblyDefinition> assemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCultureIgnoreCase);
    Dictionary<string, string> referenceDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    public AssemblyResolver(Action<string> log, IEnumerable<string> references)
    {
        this.log = log;
        foreach (var filePath in references)
        {
            referenceDictionary[Path.GetFileNameWithoutExtension(filePath)] = filePath;
        }
    }

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference)
    {
        return Resolve(assemblyNameReference, new ReaderParameters {ReadSymbols = false});
    }

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
    {
        if (parameters == null)
        {
            parameters = new ReaderParameters {ReadSymbols = false};
        }

        string fileFromDerivedReferences;
        if (referenceDictionary.TryGetValue(assemblyNameReference.Name, out fileFromDerivedReferences))
        {
            return GetAssembly(fileFromDerivedReferences, parameters);
        }

        return TryToReadFromDirs(assemblyNameReference, parameters);
    }

    AssemblyDefinition TryToReadFromDirs(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
    {
        var filesWithMatchingName = SearchDirForMatchingName(assemblyNameReference).ToList();
        foreach (var filePath in filesWithMatchingName)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);
            if (assemblyNameReference.Version == null || assemblyName.Version == assemblyNameReference.Version)
            {
                return GetAssembly(filePath, parameters);
            }
        }
        foreach (var filePath in filesWithMatchingName.OrderByDescending(s => AssemblyName.GetAssemblyName(s).Version))
        {
            return GetAssembly(filePath, parameters);
        }

        var joinedReferences = string.Join(Environment.NewLine, referenceDictionary.Values.OrderBy(x => x));
        log(string.Format("Can not find '{0}'.{1}Tried:{1}{2}", assemblyNameReference.FullName, Environment.NewLine, joinedReferences));
        return null;
    }

    IEnumerable<string> SearchDirForMatchingName(AssemblyNameReference assemblyNameReference)
    {
        var fileName = assemblyNameReference.Name + ".dll";
        return referenceDictionary.Values
            .Select(x => Path.Combine(Path.GetDirectoryName(x), fileName))
            .Where(File.Exists);
    }

    AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        AssemblyDefinition assemblyDefinition;
        if (assemblyDefinitionCache.TryGetValue(file, out assemblyDefinition))
        {
            return assemblyDefinition;
        }
        if (parameters.AssemblyResolver == null)
        {
            parameters.AssemblyResolver = this;
        }
        try
        {
            assemblyDefinitionCache[file] = assemblyDefinition = ModuleDefinition.ReadModule(file, parameters).Assembly;
            return assemblyDefinition;
        }
        catch (Exception exception)
        {
            throw new Exception($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition Resolve(string fullName)
    {
        return Resolve(AssemblyNameReference.Parse(fullName));
    }

    public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
    {
        if (fullName == null)
        {
            throw new ArgumentNullException("fullName");
        }

        return Resolve(AssemblyNameReference.Parse(fullName), parameters);
    }
}