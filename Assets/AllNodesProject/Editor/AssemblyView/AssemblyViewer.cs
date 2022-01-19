using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using AllNodes.NodeEditor;

namespace AllNodes.AssemblyView
{
    public class AssemblyViewer
    {
        static readonly string project = "Assembly-CSharp";
        static Assembly projectAssembly;
        static Dictionary<string, Node> nodes;
        static List<Connection> links;
        static NodeEditorWindow window;

        [MenuItem("Window/Node Editor/Assembly View")]
        private static void OpenWindow()
        {
            nodes = new Dictionary<string, Node>();
            links = new List<Connection>();

            if (window == null)
            {
                window = EditorWindow.GetWindow<NodeEditorWindow>();
                window.titleContent = new GUIContent("Assembly View");
                window.readOnly = true;
                window.onDrawUI += DrawUI;
            }

            try
            {
                projectAssembly = Assembly.Load(project);
                var types = projectAssembly.GetTypes();

                HashSet<string> availableTypes = new HashSet<string>();

                CreateNodes(types);

                ConnectNodes(types);

                PlaceNodes();
            }
            catch (FileNotFoundException e)
            {
                Debug.LogWarning(e);

                bool closed = EditorUtility.DisplayDialog("Error", "Error Loading Project Assembly!", "Close");
                if (closed)
                    window.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static void CreateNodes(System.Type[] types)
        {
            foreach (var type in types)
            {
                TypeNode node = new TypeNode(type);

                if (type.FullName != null && !nodes.ContainsKey(type.FullName))
                    nodes.Add(type.FullName, node);

                window.AddNode(node);
            }
        }

        private static void ConnectNodes(System.Type[] types)
        {
            foreach (var type in types)
            {
                Node parent = nodes[type.FullName];
                string[] refs = GetMembers(type);
                foreach (var r in refs)
                {
                    if (r == null)
                        continue;

                    Node child;
                    nodes.TryGetValue(r, out child);
                    if (child != null && parent != child)
                    {
                        Connection link = window.CreateConnection(parent, child);
                        links.Add(link);
                    }
                }
            }
        }

        private static void PlaceNodes()
        {
            int rootCount = 0;
            int tailCount = 0;
            int midCount = 0;
            int soleCount = 0;

            //move roots
            foreach (var node in nodes.Values)
            {
                bool root = true;
                bool tail = true;
                Rect rect = node.Rect;

                foreach (var c in links)
                {
                    if (c.inPoint == node.inPoint)
                        root = false;
                    if (c.outPoint == node.outPoint)
                        tail = false;
                }

                if (root && tail)
                {
                    rect.x = 6 * 200;
                    rect.y = soleCount * 50;
                    soleCount += 1;
                }
                else if (root)
                {
                    rect.x = 0;
                    rect.y = rootCount * 50;
                    rootCount += 1;
                }
                else if (tail)
                {
                    rect.x = 4 * 200;
                    rect.y = tailCount * 50;
                    tailCount += 1;
                }
                else if (!root && !tail)
                {
                    rect.x = 2 * 200;
                    rect.y = midCount * 50;
                    midCount += 1;
                }

                node.Rect = rect;
            }
        }

        private static void SaveNodes()
        {
            string filename = Application.dataPath.Replace("/Assets", "/CodeStructure.xml");

            NodesData data = new NodesData();
            Node[] output = nodes == null ? new Node[0] : window.Nodes.ToArray();
            Comment[] coutput = window.Comments == null ? new Comment[0] : window.Comments.ToArray();
            data.SetData(output, coutput);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NodesData));
                using (TextWriter writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, data);
                    writer.Close();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SerializeXML: Failed to serialize object to a file " + filename + " (Reason: " + e.ToString() + ")");
            }
        }

        private static void LoadNodes()
        {
            string filename = Application.dataPath.Replace("/Assets", "/CodeStructure.xml");
            if (!File.Exists(filename)) return;

            XmlSerializer deserializer = new XmlSerializer(typeof(NodesData));
            TextReader reader = new StreamReader(filename);
            NodesData obj = (NodesData)deserializer.Deserialize(reader);
            reader.Close();
            Dictionary<string, Rect> dataNodes = new Dictionary<string, Rect>();
            foreach (var node in obj.nodes)
            {
                if (!dataNodes.ContainsKey(node.name))
                {
                    dataNodes.Add(node.name, node.GetRect());
                }
            }
            foreach (var node in window.Nodes)
            {
                if (dataNodes.ContainsKey(node.Name))
                    node.Rect = dataNodes[node.Name];
            }
            foreach (var node in obj.comments)
            {
                window.CreateComment(node.GetRect(), node.name);
            }
        }

        private static string[] GetMembers(System.Type type)
        {
            List<string> dependencies = new List<string>();
            var members = type.GetMembers();

            foreach (MemberInfo m in members)
            {
                switch (m.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo f = (FieldInfo)m;
                        dependencies.Add(f.FieldType.FullName);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo p = (PropertyInfo)m;
                        dependencies.Add(p.PropertyType.FullName);
                        break;
                    case MemberTypes.Event:
                        EventInfo e = (EventInfo)m;
                        dependencies.Add(e.EventHandlerType.FullName);
                        break;
                    case MemberTypes.Method:
                        MethodInfo d = (MethodInfo)m;
                        dependencies.Add(d.ReturnType.FullName);
                        break;
                    case MemberTypes.NestedType:
                        TypeInfo t = (TypeInfo)m;
                        dependencies.Add(t.AsType().FullName);
                        GetMembers(t.AsType());
                        break;
                }
            }

            return dependencies.ToArray();
        }

        private static void DrawUI(Rect rect)
        {
            if (GUI.Button(new Rect(210, 0, 50, 20), "Load"))
                LoadNodes();
            if (GUI.Button(new Rect(262, 0, 50, 20), "Save"))
                SaveNodes();

            GUI.BeginGroup(new Rect(10, rect.height - 240, 130, 210));

            Rect r = new Rect(0, 0, 130, 210);
            GUI.Box(r, "Legend");

            r = new Rect(5, 20, 120, 24);
            GUI.Box(r, "MonoBehaviour", "flow node 0");
            r.y += 26;
            GUI.Box(r, "ScriptableObject", "flow node 1");
            r.y += 26;
            GUI.Box(r, "Unity.Object", "flow node 2");
            r.y += 26;
            GUI.Box(r, "System.Object", "flow node 3");
            r.y += 26;
            GUI.Box(r, "Interface", "flow node 4");
            r.y += 26;
            GUI.Box(r, "Value Type", "flow node 5");
            r.y += 26;
            GUI.Box(r, "Event", "flow node 6");

            GUI.EndGroup();
        }
    }
}

