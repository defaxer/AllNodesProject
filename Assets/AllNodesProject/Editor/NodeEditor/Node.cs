using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AllNodes.NodeEditor
{
    public class Node
    {
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;
        public object reference;

        private Rect rect;
        private string name;
        private Action<Node> onRemoveNode;

        public Rect Rect { get => rect; set => rect = value; }
        public string Name { get => name; set => name = value; }
        public bool isSelected { get; set; }

        public Node()
        {
            Rect = new Rect(0, 0, 200, 40);
            inPoint = new ConnectionPoint(this, ConnectionPointType.In, null);
            outPoint = new ConnectionPoint(this, ConnectionPointType.Out, null);
            onRemoveNode = null;
        }

        public Node(Vector2 position, float width, float height, Action<ConnectionPoint> OnClickInPoint, Action<ConnectionPoint> OnClickOutPoint, Action<Node> OnClickRemoveNode)
        {
            Rect = new Rect(position.x, position.y, width, height);
            inPoint = new ConnectionPoint(this, ConnectionPointType.In, OnClickInPoint);
            outPoint = new ConnectionPoint(this, ConnectionPointType.Out, OnClickOutPoint);
            onRemoveNode = OnClickRemoveNode;
        }

        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }

        public void Draw(string search)
        {
            if (string.IsNullOrEmpty(search) || name.ToLower().Contains(search) || search.Contains(name.ToLower()))
            {
                DrawBody();
                inPoint.Draw();
                outPoint.Draw();
            }
            else
            {
                DrawEmpty();
            }
        }

        public bool ProcessEvents(Event e)
        {
            if (e.button == 1 && e.type == EventType.MouseDown && Rect.Contains(e.mousePosition))
            {
                ProcessContextMenu();
                e.Use();
                return true;
            }

            return false;
        }

        protected virtual void AddContextCommands(GenericMenu menu) { }

        protected virtual void DrawBody()
        {
            GUI.Box(Rect, name, isSelected ? "flow node 0 on" : "flow node 0");
        }

        protected virtual void DrawEmpty()
        {
            GUI.Box(Rect, name);
        }

        private void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();

            if (onRemoveNode != null)
                genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);

            AddContextCommands(genericMenu);

            if (genericMenu.GetItemCount() > 0)
                genericMenu.ShowAsContext();
        }

        private void OnClickRemoveNode()
        {
            onRemoveNode?.Invoke(this);
        }
    }
}