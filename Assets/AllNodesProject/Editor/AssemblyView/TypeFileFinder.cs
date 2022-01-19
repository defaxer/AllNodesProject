using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AllNodes.AssemblyView
{
    public static class TypeFileFinder
    {
        static List<string> classFiles;
        static string searchPattern = @"\b(class|interface|struct|delegate|enum)+\s*([a-zA-Z\<\>_1-9]*)?\s(?<name>[a-zA-Z\<\>_1-9]+)";

        public static TypeFileDetails FindTypeFile(System.Type t)
        {
            return FindFile(t.Name);
        }

        public static TypeFileDetails FindFile(string typeName)
        {
            TypeFileDetails details = DatabaseLink.GetTypeFileDetails(typeName);
            if (details == null)
            {
                //Lookup class name in file names
                classFiles = new List<string>();
                FindAllCSharpFiles(Application.dataPath);
                for (int i = 0; i < classFiles.Count; i++)
                {
                    if (classFiles[i].EndsWith(typeName + ".cs"))
                    {
                        details = new TypeFileDetails(typeName, classFiles[i], File.GetLastAccessTimeUtc(classFiles[i]));
                    }
                }
                //Lookup class name in the class file text 
                if (details == null)
                {
                    Regex re = new Regex(searchPattern);
                    for (int i = 0; i < classFiles.Count; i++)
                    {
                        string codeFile = File.ReadAllText(classFiles[i]);
                        //Match match = re.Match(codeFile);
                        MatchCollection matches = re.Matches(codeFile);
                        for (int j = 0; j < matches.Count; j++)// var match in matches)
                        {
                            Match match = matches[j];
                            if (match.Success && match.Groups["name"].Value.Equals(typeName))
                            {
                                details = new TypeFileDetails(typeName, classFiles[i], File.GetLastAccessTimeUtc(classFiles[i]));
                            }
                        }
                    }
                }
                if (details == null)
                {
                    Debug.LogWarning("Failed to lookup source file for type " + typeName);
                }
                return details;
            }
            else
            {
                return details;
            }
        }

        static void FindAllCSharpFiles(string startDir)
        {
            try
            {
                foreach (string file in Directory.GetFiles(startDir))
                {
                    if (file.EndsWith(".cs"))
                        classFiles.Add(file);
                }
                foreach (string dir in Directory.GetDirectories(startDir))
                {
                    FindAllCSharpFiles(dir);
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }

    public class TypeFileDetails
    {
        public int OID { get; set; }
        public string className { get; set; }
        public string path { get; set; }
        public string projectPath { get; set; }
        public System.DateTime updateTime { get; set; }

        internal TypeFileDetails() { }
        internal TypeFileDetails(string setClassName, string setPath, System.DateTime setUpdateTime)
        {
            className = setClassName;
            path = setPath;
            projectPath = (setPath.Replace(Application.dataPath, "Assets")).Replace("\\", "/");
            updateTime = setUpdateTime;

            DatabaseLink.StoreTypeFileDetails(this);
        }
    }

    public static class DatabaseLink
    {
        static Dictionary<string, TypeFileDetails> fileDetails = new Dictionary<string, TypeFileDetails>();

        public static void StoreTypeFileDetails(TypeFileDetails typeFile)
        {
            if (fileDetails.ContainsKey(typeFile.className)) return;

            fileDetails.Add(typeFile.className, typeFile);
        }
        public static TypeFileDetails GetTypeFileDetails(string className)
        {
            TypeFileDetails result;

            if (fileDetails.TryGetValue(className, out result))
                return result;

            return null;
        }
    }
}