using System;
using UnityEditor;
using UnityEngine;

namespace AllNodes.NodeEditor
{
    public class Connection
    {
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;
        public Action<Connection> OnClickRemoveConnection;

        public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint, Action<Connection> OnClickRemoveConnection)
        {
            this.inPoint = inPoint;
            this.outPoint = outPoint;
            this.OnClickRemoveConnection = OnClickRemoveConnection;
        }

        public void Draw()
        {
            Handles.DrawBezier(
                inPoint.rect.center - new Vector2(5, 0),
                outPoint.rect.center + new Vector2(5, 0),
                inPoint.rect.center + Vector2.left * 50f,
                outPoint.rect.center - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            if (OnClickRemoveConnection != null)
            {
                if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
                {
                    OnClickRemoveConnection(this);
                }
            }
        }

        public void DrawAsLines()
        {

            Handles.DrawLine(inPoint.rect.center - new Vector2(5, 0), outPoint.rect.center + new Vector2(5, 0));

            if (OnClickRemoveConnection != null)
            {
                if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
                {
                    OnClickRemoveConnection(this);
                }
            }
        }
    }
}