using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    /*[DrawGizmo(GizmoType.Pickable | GizmoType.Selected)]
    static void DrawGizmosSelected(Player launcher, GizmoType gizmoType)
    {
        {
            // the position of the handle
            var offsetPosition = launcher.transform.TransformPoint(launcher.offset);

            // draws line from launcher to projectilePoint
            // + adds a label
            Handles.DrawDottedLine(launcher.transform.position, offsetPosition, 3);
            Handles.Label(offsetPosition, "Offset");


            if (launcher.projectile != null)
            {
                // calculate the physical parameters of the projectile
                var positions = new List<Vector3>();
                var velocity = launcher.transform.right * launcher.velocity / launcher.projectile.mass;
                var position = offsetPosition;
                var physicsStep = 0.01f;

                // create every point of the line path
                for (var i = 0f; i <= 1f; i += physicsStep)
                {
                    positions.Add(position);
                    position += velocity * physicsStep;
                    velocity += Physics.gravity * physicsStep;
                }

                // draw every point to make a yellow line path 
                using (new Handles.DrawingScope(Color.yellow))
                {
                    Handles.DrawAAPolyLine(positions.ToArray());
                    Gizmos.DrawWireSphere(positions[positions.Count - 1], 0.125f);
                    Handles.Label(positions[positions.Count - 1], "Estimated Position (1 sec)");
                }
            }
        }
    }

    void OnSceneGUI()
    {
        var launcher = target as Player;
        var transform = launcher.transform;

        // checks if we moved the handle
        // if true then update the position and Re-Draw the line
        using (var cc = new EditorGUI.ChangeCheckScope())
        {
            var newOffset = transform.InverseTransformPoint( Handles.PositionHandle( transform.TransformPoint(launcher.offset),transform.rotation));

            if (cc.changed)
            {
                Undo.RecordObject(launcher, "Offset Change");
                launcher.offset = newOffset;
            }
        }*/

        /** making of the area of of where the button is **/
        /*Handles.BeginGUI();

        var rectMin = Camera.current.WorldToScreenPoint(launcher.transform.position + launcher.offset);
        var rect = new Rect();
        rect.xMin = rectMin.x;
        rect.yMin = SceneView.currentDrawingSceneView.position.height - rectMin.y;
        rect.width = 64;
        rect.height = 18;

        GUILayout.BeginArea(rect);

        // button active only in play mode
        using (new EditorGUI.DisabledGroupScope(!Application.isPlaying)) if (GUILayout.Button("Fire")) launcher.Fire();

        GUILayout.EndArea();
        Handles.EndGUI();
    }*/
}