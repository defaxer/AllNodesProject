using System;
using UnityEngine;

namespace AllNodes.NodeEditor
{
    public enum ConnectionPointType { In, Out }

    public class ConnectionPoint
    {
        public Rect rect;
        public Node node;

        public ConnectionPointType type;
        public Action<ConnectionPoint> OnClickConnectionPoint;

        public ConnectionPoint(Node node, ConnectionPointType type, Action<ConnectionPoint> OnClickConnectionPoint)
        {
            this.node = node;
            this.type = type;
            this.OnClickConnectionPoint = OnClickConnectionPoint;
            rect = new Rect(0, 0, 14f, 14f);
        }

        public void Draw()
        {
            float x;
            if (type == ConnectionPointType.In)
                x = -node.Rect.width * 0.5f - rect.width * 0.5f;
            else
                x = node.Rect.width * 0.5f - rect.width * 0.5f;
            float y = -rect.height * 0.5f;

            rect.position = node.Rect.center + new Vector2(x, y);
            if (GUI.Button(rect, "", "sv_label_0"))
                OnClickConnectionPoint?.Invoke(this);
        }
    }
}