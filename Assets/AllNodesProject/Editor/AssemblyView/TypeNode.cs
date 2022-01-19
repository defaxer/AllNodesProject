using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AllNodes.NodeEditor;

namespace AllNodes.AssemblyView
{
    public class TypeNode : Node 
    {
        public string style;

        static readonly string[] nodeStyles = new string[]
        {
            "flow node 0",
            "flow node 1",
            "flow node 2",
            "flow node 3",
            "flow node 4",
            "flow node 5",
            "flow node 6",
            "flow node hex 0",
            "flow node hex 1",
            "flow node hex 2",
            "flow node hex 3",
            "flow node hex 4",
            "flow node hex 5",
            "flow node hex 6"
        };

        public TypeNode(System.Type type) : base()
        {
            if (type.IsValueType)
                style = nodeStyles[5];
            else if (type.IsInterface)
                style = nodeStyles[4];
            else if (type.BaseType.Equals(typeof(System.MulticastDelegate)))
                style = nodeStyles[6];
            else if (type.IsClass)
            {
                if (type.IsSubclassOf(typeof(MonoBehaviour)))
                    style = nodeStyles[0];
                else if (type.IsSubclassOf(typeof(ScriptableObject)))
                    style = nodeStyles[1];
                else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                    style = nodeStyles[2];
                else
                    style = nodeStyles[3];
            }
            else
                style = nodeStyles[0];

            Name = type.Name;
            reference = type;
        }

        protected override void DrawBody()
        {
            GUI.Box(Rect, Name, isSelected ? style + " on" : style);
        }

        protected override void AddContextCommands(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Show In Project View"), false, OnClickFindScript);
        }

        private void OnClickFindScript()
        {
            var file = TypeFileFinder.FindFile(Name);
            if (file != null)
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(file.projectPath));
        }
    }
}