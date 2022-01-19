using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AllNodes.NodeEditor;

namespace AllNodes.AssemblyView
{
    [System.Serializable]
    public class NodesData
    {
        [System.Serializable]
        public class NodeData
        {
            public string name;
            public string type;
            public string rect;

            public NodeData()
            {
                this.name = "";
                this.rect = "";
            }

            public NodeData(Node node)
            {
                this.name = node.Name;
                this.rect = node.Rect.ToString();
            }

            public NodeData(Comment node)
            {
                this.name = node.Text;
                this.rect = node.Rect.ToString();
            }

            public Rect GetRect()
            {
                try
                {
                    int i = rect.IndexOf("x:") + 2;
                    float x = float.Parse(rect.Substring(i, rect.IndexOf(",", i) - i));
                    i = rect.IndexOf("y:") + 2;
                    float y = float.Parse(rect.Substring(i, rect.IndexOf(",", i) - i));
                    i = rect.IndexOf("width:") + 6;
                    float w = float.Parse(rect.Substring(i, rect.IndexOf(",", i) - i));
                    i = rect.IndexOf("height:") + 7;
                    float h = float.Parse(rect.Substring(i, rect.IndexOf(")") - i));
                    return new Rect(x, y, w, h);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Could not parce Node position " + e.ToString());
                }

                return new Rect(0, 0, 200, 50);
            }
        }

        public NodeData[] nodes;
        public NodeData[] comments;

        public void SetData(Node[] editorNodes, Comment[] editorComments)
        {
            List<NodeData> data = new List<NodeData>();
            foreach (Node node in editorNodes)
                data.Add(new NodeData(node));

            List<NodeData> cdata = new List<NodeData>();
            foreach (Comment node in editorComments)
                cdata.Add(new NodeData(node));

            nodes = data.ToArray();
            comments = cdata.ToArray();
        }
    }
}