using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AllNodes.NodeEditor
{
    public class NodeEditorWindow : EditorWindow
    {
        public bool readOnly;
        public string search = "";

        public System.Action onWindowClosed;
        public System.Action<Rect> onDrawUI;

        private List<Node> nodes;
        private List<Connection> connections;
        private List<Comment> comments;

        private ConnectionPoint selectedInPoint;
        private ConnectionPoint selectedOutPoint;

        private Vector2 offset;
        private Vector2 drag;

        private float zoomScale = 1.0f;
        private bool commenting = false;

        private Vector2 selectionStart = Vector2.zero;
        private Vector2 selectionEnd = Vector2.zero;
        private Rect selectionBox = Rect.zero;
        private List<Node> selectedNodes = new List<Node>();
        private bool isDraggingNodes = false;
        private Vector2 startInput;
        private Vector2 endInput;
        private Rect resultRect;

        public List<Node> Nodes => nodes;
        public List<Connection> Connections => connections;
        public List<Comment> Comments => comments;

        [MenuItem("Window/Node Editor/Open Demo Window")]
        private static void OpenWindow()
        {
            NodeEditorWindow window = GetWindow<NodeEditorWindow>();
            window.titleContent = new GUIContent("Node Based Editor");
        }

        private void OnEnable() { }

        private void OnDisable()
        {
            onWindowClosed?.Invoke();
            onWindowClosed = null;
        }

        private void OnGUI()
        {
            GUI.EndGroup();

            //Scale my gui matrix
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * zoomScale, new Vector2(0, 20));
            Rect area = new Rect(0, 20, Screen.width * (1 / zoomScale), Screen.height * (1 / zoomScale));
            GUI.BeginGroup(area);

            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);
            DrawComments();
            DrawNodes();
            DrawConnections();
            DrawConnectionLine(Event.current);
            DrawSelectionBox();

            if (commenting)
                ProcessCreatingComment(Event.current);
            else
                ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);

            //end zoomed drawing
            GUI.matrix = oldMatrix;

            GUI.EndGroup();
            GUI.BeginGroup(new Rect(0, 20, Screen.width, Screen.height));

            //search bar
            GUI.BeginGroup(new Rect(2, 2, 200, 20));
            search = GUI.TextField(new Rect(0, 0, 180, 20), search, "SearchTextField");
            if (!string.IsNullOrEmpty(search))
            {
                if (GUI.Button(new Rect(180, 0, 20, 20), "", "SearchCancelButton"))
                    search = "";
            }
            GUI.EndGroup();

            onDrawUI?.Invoke(new Rect(0, 0, Screen.width, Screen.height));

            if (GUI.changed) Repaint();
        }

        public void AddNode(Node node)
        {
            if (nodes == null)
                nodes = new List<Node>();
            nodes.Add(node);
        }

        public Connection CreateConnection(Node from, Node to)
        {
            Connection result;

            if (readOnly)
                result = new Connection(to.inPoint, from.outPoint, null);
            else
                result = new Connection(to.inPoint, from.outPoint, OnClickRemoveConnection);

            if (connections == null)
                connections = new List<Connection>();
            connections.Add(result);

            return result;
        }

        public Comment CreateComment(Rect rect, string comment)
        {
            Comment result = new Comment(rect, "Comment", OnClickRemoveComment);
            result.Text = comment;

            if (comments == null)
                comments = new List<Comment>();
            comments.Add(result);

            return result;
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width * (1 / zoomScale) / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height * (1 / zoomScale) / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            offset += drag * 0.5f;
            Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height * (1 / zoomScale), 0f) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width * (1 / zoomScale), gridSpacing * j, 0f) + newOffset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawComments()
        {
            if (comments != null)
            {
                for (int i = 0; i < comments.Count; i++)
                {
                    comments[i].Draw();
                }
            }
        }

        private void DrawNodes()
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Draw(search.ToLower());
                }
            }
        }

        private void DrawConnections()
        {
            if (!string.IsNullOrEmpty(search)) return;

            if (connections != null)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    connections[i].Draw();
                }
            }
        }

        private void DrawConnectionLine(Event e)
        {
            if (selectedInPoint != null && selectedOutPoint == null)
            {
                Handles.DrawBezier(
                    selectedInPoint.rect.center,
                    e.mousePosition,
                    selectedInPoint.rect.center + Vector2.left * 50f,
                    e.mousePosition - Vector2.left * 50f,
                    Color.white,
                    null,
                    2f
                );

                GUI.changed = true;
            }

            if (selectedOutPoint != null && selectedInPoint == null)
            {
                Handles.DrawBezier(
                    selectedOutPoint.rect.center,
                    e.mousePosition,
                    selectedOutPoint.rect.center - Vector2.left * 50f,
                    e.mousePosition + Vector2.left * 50f,
                    Color.white,
                    null,
                    2f
                );

                GUI.changed = true;
            }
        }

        private void DrawSelectionBox()
        {
            GUI.Box(selectionBox, "");
        }

        private void ProcessEvents(Event e)
        {
            drag = Vector2.zero;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        ClearConnectionSelection();
                        //Selection box starts only when clicked on non selected node
                        //OR empty space
                        if (e.clickCount == 1)
                        {
                            if (!SelectionContains(e.mousePosition))
                            {
                                ClearSelection();
                                isDraggingNodes = false;
                                selectionStart = e.mousePosition;
                                selectionEnd = e.mousePosition;
                                selectionBox = new Rect(selectionStart, Vector2.one);
                                ProcessNodes(selectionBox);
                                if (selectedNodes.Count > 0)
                                {
                                    isDraggingNodes = true;
                                    selectionStart = Vector3.zero;
                                    selectionEnd = Vector3.zero;
                                    selectionBox = Rect.zero;
                                }
                                GUI.changed = true;
                            }
                            else //clicked on selected node can drag
                            {
                                isDraggingNodes = true;
                                selectionStart = Vector3.zero;
                                selectionEnd = Vector3.zero;
                                selectionBox = Rect.zero;
                                GUI.changed = true;
                            }
                        }
                        else
                        {
                            Rect rect = new Rect(e.mousePosition, Vector2.one);
                            ClearSelection();
                            ProcessNodes(rect);
                            if (selectedNodes.Count > 0)
                                SelectBranch(selectedNodes[0], e.control);
                        }
                    }

                    if (e.button == 1)
                    {
                        ProcessContextMenu(e.mousePosition);
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        isDraggingNodes = false;
                        selectionStart = Vector2.zero;
                        selectionEnd = Vector2.zero;
                        selectionBox = Rect.zero;
                        GUI.changed = true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        if (!isDraggingNodes)
                        {
                            ProcessNodes(selectionBox);
                            selectionEnd = e.mousePosition;
                            GUI.changed = true;
                        }
                        else
                        {
                            DragSelection(e.delta);
                            GUI.changed = true;
                        }
                    }

                    if (e.button == 2)
                    {
                        OnDrag(e.delta);
                    }
                    break;

                case EventType.ScrollWheel:
                    OnZoom(e.delta.y);
                    break;
            }

            selectionBox = new Rect(
                Mathf.Min(selectionStart.x, selectionEnd.x),
                Mathf.Min(selectionStart.y, selectionEnd.y),
                Mathf.Abs(selectionEnd.x - selectionStart.x),
                Mathf.Abs(selectionEnd.y - selectionStart.y)
                );
        }

        private void ProcessNodeEvents(Event e)
        {
            if (nodes != null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);

                    if (guiChanged)
                        GUI.changed = true;
                }
            }

            if (comments != null)
            {
                for (int i = comments.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = comments[i].ProcessEvents(e);

                    if (guiChanged)
                        GUI.changed = true;
                }
            }
        }

        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();

            if (!readOnly)
                genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));

            genericMenu.AddItem(new GUIContent("Create Comment"), false, OnClickCreateComment);

            if (genericMenu.GetItemCount() > 0)
                genericMenu.ShowAsContext();
        }

        private void ProcessCreatingComment(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    startInput = e.mousePosition;
                    endInput = e.mousePosition;
                    resultRect = new Rect();
                    GUI.changed = true;
                    break;
                case EventType.MouseUp:
                    resultRect = new Rect(
                        Mathf.Min(startInput.x, endInput.x),
                        Mathf.Min(startInput.y, endInput.y),
                        Mathf.Abs(endInput.x - startInput.x),
                        Mathf.Abs(endInput.y - startInput.y)
                        );
                    startInput = Vector2.zero;
                    endInput = Vector2.zero;
                    CreateComment(resultRect, "Comment");
                    commenting = false;
                    GUI.changed = true;
                    break;
                case EventType.MouseDrag:
                    endInput = e.mousePosition;
                    GUI.changed = true;
                    break;
                case EventType.Repaint:
                    Rect temp = new Rect(
                        Mathf.Min(startInput.x, endInput.x),
                        Mathf.Min(startInput.y, endInput.y),
                        Mathf.Abs(endInput.x - startInput.x),
                        Mathf.Abs(endInput.y - startInput.y)
                        );
                    if (temp.size.magnitude > 10)
                        GUI.Box(temp, "", "window");
                    break;
            }
        }

        private void OnDrag(Vector2 delta)
        {
            drag = delta;

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Drag(delta);
                }
            }
            if (comments != null)
            {
                for (int i = 0; i < comments.Count; i++)
                {
                    comments[i].Drag(delta);
                }
            }

            GUI.changed = true;
        }

        private void OnZoom(float zoom)
        {
            var zoomDelta = 0.1f;
            zoomDelta = zoom < 0 ? zoomDelta : -zoomDelta;
            zoomScale += zoomDelta;
            zoomScale = Mathf.Clamp(zoomScale, 0.25f, 1.25f);

            GUI.changed = true;
        }

        private bool SelectionContains(Vector2 position)
        {
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                if (selectedNodes[i].Rect.Contains(position))
                    return true;
            }

            return false;
        }

        private void ClearSelection()
        {
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                selectedNodes[i].isSelected = false;
            }

            selectedNodes.Clear();
        }

        private void DragSelection(Vector2 delta)
        {
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                selectedNodes[i].Drag(delta);
            }
        }

        private void ProcessNodes(Rect selection)
        {
            if (nodes != null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = false;

                    if (selection.Overlaps(nodes[i].Rect))
                    {
                        if (!nodes[i].isSelected)
                        {
                            nodes[i].isSelected = true;
                            selectedNodes.Add(nodes[i]);
                            guiChanged = true;
                        }
                    }

                    if (guiChanged)
                    {
                        GUI.changed = true;
                    }
                }
            }
        }

        private void SelectBranch(Node root, bool left = true)
        {
            foreach (Connection connection in connections)
            {
                if ((left && connection.inPoint == root.inPoint) || (!left && connection.outPoint == root.outPoint))
                {
                    Node node = left ? connection.outPoint.node : connection.inPoint.node;
                    if (node != null)
                    {
                        node.isSelected = true;
                        if (!selectedNodes.Contains(node))
                            selectedNodes.Add(node);
                        SelectBranch(node, left);
                    }
                }
            }
        }

        private void OnClickAddNode(Vector2 mousePosition)
        {
            if (nodes == null)
            {
                nodes = new List<Node>();
            }

            nodes.Add(new Node(mousePosition, 200, 50, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
        }

        private void OnClickInPoint(ConnectionPoint inPoint)
        {
            selectedInPoint = inPoint;

            if (selectedOutPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        private void OnClickOutPoint(ConnectionPoint outPoint)
        {
            selectedOutPoint = outPoint;

            if (selectedInPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        private void OnClickRemoveNode(Node node)
        {
            if (connections != null)
            {
                List<Connection> connectionsToRemove = new List<Connection>();

                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i].inPoint == node.inPoint || connections[i].outPoint == node.outPoint)
                    {
                        connectionsToRemove.Add(connections[i]);
                    }
                }

                for (int i = 0; i < connectionsToRemove.Count; i++)
                {
                    connections.Remove(connectionsToRemove[i]);
                }

                connectionsToRemove = null;
            }

            nodes.Remove(node);
        }

        private void OnClickRemoveComment(Comment comment)
        {
            comments.Remove(comment);
        }

        private void OnClickRemoveConnection(Connection connection)
        {
            connections.Remove(connection);
        }

        private void OnClickCreateComment()
        {
            if (selectedNodes == null || selectedNodes.Count == 0)
                commenting = true;
            else
            {
                Rect rect = new Rect(selectedNodes[0].Rect);
                foreach (var node in selectedNodes)
                {
                    Rect nr = node.Rect;

                    rect.xMin = Mathf.Min(rect.xMin, nr.xMin) - 10;
                    rect.yMin = Mathf.Min(rect.yMin, nr.yMin) - 10;

                    rect.xMax = Mathf.Max(rect.xMax, nr.xMax) + 10;
                    rect.yMax = Mathf.Max(rect.yMax, nr.yMax) + 10;
                }

                CreateComment(rect, "New Comment");
            }
        }

        private void CreateConnection()
        {
            if (connections == null)
            {
                connections = new List<Connection>();
            }

            connections.Add(new Connection(selectedInPoint, selectedOutPoint, OnClickRemoveConnection));
        }

        private void ClearConnectionSelection()
        {
            selectedInPoint = null;
            selectedOutPoint = null;
        }
    }
}