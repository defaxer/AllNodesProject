using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AllNodes.NodeEditor
{
    public class Comment
    {
        public enum State
        {
            None,
            Select,
            Edit,
            Drag,
            Resize
        }

        public State state = State.None;

        private Rect rect;
        private string text;
        private System.Action<Comment> onRemoveNode;

        static GUIStyle defaultNodeStyle;
        static GUIStyle selectedNodeStyle;

        public Rect Rect { get => rect; set => rect = value; }
        public string Text { get => text; set => text = value; }
        public bool isSelected => state == State.Select;

        static Comment()
        {
            defaultNodeStyle = new GUIStyle();
            defaultNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/cmd.png") as Texture2D;
            defaultNodeStyle.alignment = TextAnchor.UpperLeft;
            defaultNodeStyle.border = new RectOffset(12, 12, 12, 12);

            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/cmd focus.png") as Texture2D;
            selectedNodeStyle.alignment = TextAnchor.UpperLeft;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
        }

        public Comment(Rect rect, string text, System.Action<Comment> onRemoveNode)
        {
            this.rect = rect;
            this.text = text;
            this.onRemoveNode = onRemoveNode;
        }

        public void Draw()
        {
            switch (state)
            {
                case State.None:
                    GUI.Box(rect, text, defaultNodeStyle);
                    break;
                case State.Select:
                case State.Drag:
                case State.Resize:
                    GUI.Box(rect, text, selectedNodeStyle);
                    break;
                case State.Edit:
                    GUI.Box(rect, text, selectedNodeStyle);
                    text = GUI.TextField(rect, text);
                    break;
            }
        }

        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }

        public void Resize(Vector2 delta)
        {
            rect.size += delta;
        }

        public bool ProcessEvents(Event e)
        {
            if (e.button != 0 && e.button != 1) return false;

            switch (e.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(e);
                    break;

                case EventType.MouseDrag:
                    HandleMouseDrag(e);
                    break;
            }

            return false;
        }

        private void HandleMouseDown(Event e)
        {
            state = rect.Contains(e.mousePosition) ? State.Select : State.None;

            if (e.clickCount > 1)
            {
                if (state == State.Select)
                    state = State.Edit;
            }
            else
            {
                if (GetDragArea().Contains(e.mousePosition))
                    state = State.Drag;

                if (GetResizeArea().Contains(e.mousePosition))
                    state = State.Resize;
            }

            if (e.button == 1 && state == State.Select)
            {
                ProcessContextMenu();
                e.Use();
            }
        }

        private void HandleMouseDrag(Event e)
        {
            if (e.button != 0) return;

            if (state == State.Drag)
                Drag(e.delta);

            if (state == State.Resize)
                Resize(e.delta);
        }

        private Rect GetDragArea()
        {
            return new Rect(rect.x, rect.y, rect.width, 20);
        }

        private Rect GetResizeArea()
        {
            return new Rect(rect.x + rect.width - 20, rect.y + rect.height - 20, 20, 20);
        }

        private void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();

            if (onRemoveNode != null)
                genericMenu.AddItem(new GUIContent("Remove comment"), false, OnClickRemoveNode);

            if (genericMenu.GetItemCount() > 0)
                genericMenu.ShowAsContext();
        }

        private void OnClickRemoveNode()
        {
            onRemoveNode?.Invoke(this);
        }
    }
}