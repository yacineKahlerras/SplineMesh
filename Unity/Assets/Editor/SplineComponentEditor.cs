using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineComponent))]
public class SplineComponentEditor : Editor
{
    int hotIndex = -1;
    int removeIndex = -1;
    bool modifyBothControlPoints = false;

    // buttons on the inspector
    public override void OnInspectorGUI()
    {
        // information box
        EditorGUILayout.HelpBox("Hold Shift and click to append and insert curve points. Backspace to delete points.", MessageType.Info);
        var spline = target as SplineComponent;
        GUILayout.BeginHorizontal();

        // closed spline bool button
        var closed = GUILayout.Toggle(spline.closed, "Closed", "button");
        if (spline.closed != closed)
        {
            spline.closed = closed;
            spline.ResetIndex();
        }

        // flatten Y axis button
        if (GUILayout.Button("Flatten Y Axis"))
        {
            Undo.RecordObject(target, "Flatten Y Axis");
            Flatten(spline.points);
            spline.ResetIndex();
        }

        // center arround origin button
        if (GUILayout.Button("Center around Origin"))
        {
            Undo.RecordObject(target, "Center around Origin");
            CenterAroundOrigin(spline.points);
            spline.ResetIndex();
        }
        GUILayout.EndHorizontal();

        // the 2D Layout Button
        List<GUILayoutOption> layoutFor2DLayout = new List<GUILayoutOption>();
        layoutFor2DLayout.Add(GUILayout.Width(80));
        layoutFor2DLayout.Add(GUILayout.Height(20));
        var options = layoutFor2DLayout.ToArray();
        spline.layout2D = GUILayout.Toggle(spline.layout2D, "2D Layout", "button", options);

        // coloring choice for the spline
        spline.splineColor = EditorGUILayout.ColorField("SplineColor", spline.splineColor);
        spline.anchorPointColor = EditorGUILayout.ColorField("AnchorPointColor", spline.anchorPointColor);
        spline.anglePointColor = EditorGUILayout.ColorField("AnglePointColor", spline.anglePointColor);
        spline.anglePointLinesColor = EditorGUILayout.ColorField("AnglePointLinesColor", spline.anglePointLinesColor);
    }

    // drawings on High and Low Res
    [DrawGizmo(GizmoType.NonSelected)]
    static void DrawGizmosLoRes(SplineComponent spline, GizmoType gizmoType)
    {
        Gizmos.color = spline.splineColor;
        DrawGizmo(spline, 64);
    }
    [DrawGizmo(GizmoType.Selected)]
    static void DrawGizmosHiRes(SplineComponent spline, GizmoType gizmoType)
    {
        Gizmos.color = spline.splineColor;
        DrawGizmo(spline, 1024);
    }

    // actually draws the line of the spline
    static void DrawGizmo(SplineComponent spline, int stepCount)
    {
        if (spline.points.Count > 0)
        {
            var P = 0f;
            var start = spline.GetNonUniformPoint(0);
            var step = 1f / stepCount;
            do
            {
                P += step;
                var here = spline.GetNonUniformPoint(P);
                Gizmos.DrawLine(start, here);
                start = here;
            } while (P + step <= 1);
        }
    }

    void OnSceneGUI()
    {
        var spline = target as SplineComponent;
        var e = Event.current;
        GUIUtility.GetControlID(FocusType.Passive);
        var mousePos = (Vector2)Event.current.mousePosition;
        var view = SceneView.currentDrawingSceneView.camera.ScreenToViewportPoint(Event.current.mousePosition);
        var mouseIsOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
        //if (mouseIsOutside) return;
        var points = serializedObject.FindProperty("points");

        // press shift an connect to the closest point
        if (Event.current.shift)
        {
            if (spline.closed)
                ShowClosestPointOnClosedSpline(spline.points); // Closed Spline
            else
                ShowClosestPointOnOpenSpline(spline.points); // Open Spline
        }

        //// Moving The Points and Removing Them Giving Each Control Point a handle
        for (int i = 0; i < spline.points.Count; i++)
        {
            var prop = spline.points[i].position;
            var point = prop;
            var wp = spline.transform.TransformPoint(point);
            var buttonSize = HandleUtility.GetHandleSize(wp) * 0.1f;

            // if we selected a control point move it and stuff
            if (hotIndex == i)
            {
                if (!spline.layout2D) // not a 2D layout we draw handles for every point
                {
                    var newWp = Handles.PositionHandle(wp, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : spline.transform.rotation);
                    var delta = spline.transform.InverseTransformDirection(newWp - wp);
                    if (delta.sqrMagnitude > 0)
                    {
                        // move the point by [delta]
                        prop = point + delta;
                        spline.points[i].position = prop;

                        // move the angle points by [delta]
                        spline.points[i].handleAPosition += delta;
                        spline.points[i].handleBPosition += delta;

                        spline.ResetIndex();
                    }
                }
                HandleCommands(wp);
            }
            
            // 2D style
            if (spline.layout2D)
            {
                // custom GUI button for Anchor Points
                CreateAnchorPointButton2D(wp, i, buttonSize, spline);
            }
            // 3D style
            else
            {
                // color both spline ends red
                Handles.color = i == 0 | i == spline.points.Count - 1 ? spline.anchorPointColor : spline.anchorPointColor;
                // if clicked on a point we make it hot 
                if (Handles.Button(wp, Quaternion.identity, buttonSize, buttonSize, Handles.CubeHandleCap))
                    hotIndex = i;
            }

            // labeling the control points by number
            var v = SceneView.currentDrawingSceneView.camera.transform.InverseTransformPoint(wp);
            var labelIsOutside = v.z < 0;
            if (!labelIsOutside) Handles.Label(wp, i.ToString());

            // remove point if we have more than 2 points
            if (removeIndex >= 0 && points.arraySize > 2)
            {
                points.DeleteArrayElementAtIndex(removeIndex);
                spline.ResetIndex();
            }

            // Draw the Angle Points
            if(spline.layout2D)CreateTheAnglePointsForAnchorPointsFor2D(spline.points[i], wp, i);
            else CreateTheAnglePointsForAnchorPoints(spline.points[i], wp, i);

            // setting this up so that it doesnt delete everything
            removeIndex = -1;
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Delete a point
    void HandleCommands(Vector3 wp)
    {
        var spline = target as SplineComponent;

        if (Event.current.type == EventType.ExecuteCommand)
        {
            if (Event.current.commandName == "FrameSelected")
            {
                SceneView.currentDrawingSceneView.Frame(new Bounds(wp, Vector3.one * 10), false);
                Event.current.Use();
            }
        }
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Backspace)
            {
                removeIndex = hotIndex;
                if (hotIndex == spline.points.Count - 1) hotIndex--;
                Event.current.Use();
            }

            if (Event.current.keyCode == KeyCode.LeftControl)
            {
                modifyBothControlPoints = true;
                Event.current.Use();
            }
        }

        if(Event.current.type == EventType.KeyUp)
        {
            if (Event.current.keyCode == KeyCode.LeftControl)
            {
                modifyBothControlPoints = false;
                Event.current.Use();
            }
        }
    }

    // Closed Spline closest point
    void ShowClosestPointOnClosedSpline(List<SplineComponent.Anchor> points)
    {
        var spline = target as SplineComponent;
        var plane = new Plane(spline.transform.up, spline.transform.position);
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float center;

        if (plane.Raycast(ray, out center))
        {
            // extrapolate from mouse position on spline to get the point
            var hit = ray.origin + ray.direction * center;
            Handles.DrawWireDisc(hit, spline.transform.up, 5);
            var p = SearchForClosestPoint(Event.current.mousePosition);
            var i = Mathf.FloorToInt(p * spline.points.Count) % spline.points.Count;

            var sp = spline.GetNonUniformPoint(p);
            Handles.DrawLine(hit, sp);

            /** drawing tangents **/
            //DrawVirtualTangentLines(i, p);

            // if SHIFT + LEFT MOUSE DOWN = add point
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.shift)
            {
                i++;
                // create a point from position and add it to the list
                SplineComponent.Anchor point = new SplineComponent.Anchor();
                point.position = spline.transform.InverseTransformPoint(sp);

                /** setting up the angle points for the point **/
                AnglePointPositionSetter(point, i, p);
                points.Insert(i, point);
                hotIndex = i;
            }
        }
    }

    // Open Spline close point
    void ShowClosestPointOnOpenSpline(List<SplineComponent.Anchor> points)
    {
        // create raycast from mouse to spline
        var spline = target as SplineComponent;
        var plane = new Plane(spline.transform.up, spline.transform.position);
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float center;
        if (plane.Raycast(ray, out center))
        {
            var hit = ray.origin + ray.direction * center;
            var discSize = HandleUtility.GetHandleSize(hit);
            Handles.DrawWireDisc(hit, spline.transform.up, discSize);
            var p = SearchForClosestPoint(Event.current.mousePosition);

            if ((hit - spline.GetNonUniformPoint(0)).sqrMagnitude < 25) p = 0;
            if ((hit - spline.GetNonUniformPoint(1)).sqrMagnitude < 25) p = 1;

            var sp = spline.GetNonUniformPoint(p);

            var extend = Mathf.Approximately(p, 0) || Mathf.Approximately(p, 1);

            Handles.color = extend ? Color.red : Color.white;
            Handles.DrawLine(hit, sp);
            Handles.color = Color.white;

            var i = Mathf.FloorToInt(p * (spline.points.Count - 1));

            /** drawing tangents **/
            //DrawVirtualTangentLines(i,p);

            // if SHIFT + LEFT MOUSE DOWN = INSERT POINT
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.shift)
            {
                if (extend)
                {
                    // if SHIFT + LEFT MOUSE DOWN = add point
                    if (i == spline.points.Count - 1) i++;
                    SplineComponent.Anchor point = new SplineComponent.Anchor();
                    point.position = spline.transform.InverseTransformPoint(hit);
                    points.Insert(i, point);
                    hotIndex = i;

                    AnglePointAInitialPositionSetter(spline, i);
                }
                else
                {
                    // if SHIFT + LEFT MOUSE DOWN = add point
                    i++;
                    SplineComponent.Anchor point = new SplineComponent.Anchor();
                    point.position = spline.transform.InverseTransformPoint(sp);

                    /** setting up the angle points for the point **/
                    AnglePointPositionSetter(point, i, p);
                    points.Insert(i, point);
                    hotIndex = i;

                    
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    // gives the t of the closest point to the mouse
    float SearchForClosestPoint(Vector2 screenPoint, float A = 0f, float B = 1f, float steps = 1000)
    {
        var spline = target as SplineComponent;
        var smallestDelta = float.MaxValue;
        var step = (B - A) / steps;
        var closestI = A;
        for (var i = 0; i <= steps; i++)
        {
            var p = spline.GetNonUniformPoint(i * step);
            var gp = HandleUtility.WorldToGUIPoint(p);
            var delta = (screenPoint - gp).sqrMagnitude;
            if (delta < smallestDelta)
            {
                closestI = i;
                smallestDelta = delta;
            }
        }
        return closestI * step;
    }

    // flattens the Y axis of a point
    void Flatten(List<SplineComponent.Anchor> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].position = Vector3.Scale(points[i].position , new Vector3(1, 1, 0));
            points[i].handleAPosition = Vector3.Scale(points[i].handleAPosition, new Vector3(1, 1, 0));
            points[i].handleBPosition = Vector3.Scale(points[i].handleBPosition, new Vector3(1, 1, 0));
        }
    }

    // centers arround the origin
    void CenterAroundOrigin(List<SplineComponent.Anchor> points)
    {
        var center = Vector3.zero;
        var centerAnglePointA = Vector3.zero;
        var centerAnglePointB = Vector3.zero;

        for (int i = 0; i < points.Count; i++)
        {
            center += points[i].position;
            centerAnglePointA += points[i].handleAPosition;
            centerAnglePointB += points[i].handleBPosition;
        }

        center /= points.Count;
        centerAnglePointA /= points.Count;
        centerAnglePointB /= points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            points[i].position -= center;
            points[i].handleAPosition -= centerAnglePointA;
            points[i].handleBPosition -= centerAnglePointB;
        }
    }

    // draws all the angle points
    void CreateTheAnglePointsForAnchorPoints(SplineComponent.Anchor point, Vector3 worldPointOfThePoint, int index)
    {
        SplineComponent spline = (SplineComponent)target;
        Vector3 newPosition;
        Vector3 wpA = spline.transform.TransformPoint(point.handleAPosition);
        Vector3 wpB = spline.transform.TransformPoint(point.handleBPosition);
        var buttonSize = HandleUtility.GetHandleSize(worldPointOfThePoint) * 0.1f;

        // if we selected the point show The Angle Points
        if (hotIndex == index)
        {
            // the A angle point
            Handles.color = spline.anglePointColor;
            Handles.SphereHandleCap(0, wpA, Quaternion.identity, buttonSize, EventType.Repaint);
            EditorGUI.BeginChangeCheck();
            newPosition = Handles.PositionHandle(wpA, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Change Anchor Handle A Position");
                point.handleAPosition = spline.transform.InverseTransformPoint(newPosition);
                spline.ResetIndex();
                serializedObject.Update();
            }

            // the B angle point
            Handles.color = spline.anglePointColor;
            Handles.SphereHandleCap(0, wpB, Quaternion.identity, buttonSize, EventType.Repaint);
            EditorGUI.BeginChangeCheck();
            newPosition = Handles.PositionHandle(wpB, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Change Anchor Handle B Position");
                point.handleBPosition = spline.transform.InverseTransformPoint(newPosition);
                spline.ResetIndex();
                serializedObject.Update();
            }

            Handles.color = spline.anglePointLinesColor;
            Handles.DrawLine(worldPointOfThePoint, wpA);
            Handles.DrawLine(worldPointOfThePoint, wpB);
        }
    }

    // draws all the angle points
    void CreateTheAnglePointsForAnchorPointsFor2D(SplineComponent.Anchor point, Vector3 worldPointOfThePoint, int index)
    {
        SplineComponent spline = (SplineComponent)target;
        Vector3 wpA = spline.transform.TransformPoint(point.handleAPosition);
        Vector3 wpB = spline.transform.TransformPoint(point.handleBPosition);
        var buttonSize = HandleUtility.GetHandleSize(worldPointOfThePoint) * 0.1f;

        /** if we selected the point show The Angle Points**/
        // the A angle point
        CreateAnglePointButton2D(wpA, index, buttonSize, spline, true);

        // the B angle point
        CreateAnglePointButton2D(wpB, index, buttonSize, spline, false);

        Handles.color = spline.anglePointLinesColor;
        Handles.DrawLine(worldPointOfThePoint, wpA);
        Handles.DrawLine(worldPointOfThePoint, wpB);
    }

    // draws lines for the tangent
    void DrawVirtualTangentLines(int i, float p)
    {
        var spline = target as SplineComponent;

        // so that the index doesnt get out of bound
        if (i > spline.points.Count - 1 && !spline.closed) i--;
        var Myi = (i + 1) % (spline.points.Count);
        if (i == spline.points.Count - 1 && spline.closed) Myi = 0;

        // points for interpolation
        var a = spline.transform.TransformPoint(spline.points[i].position);
        var b = spline.transform.TransformPoint(spline.points[i].handleBPosition);
        var c = spline.transform.TransformPoint(spline.points[Myi].handleAPosition);
        var d = spline.transform.TransformPoint(spline.points[Myi].position);
        var t1 = spline.FindControlPointT(spline.points[i].position);
        var t2 = spline.FindControlPointT(spline.points[Myi].position);

        // Calculating the new angle points
        var anglePoints = spline.CalculateAnglePoints(a, b, c, d, p, t1, t2 == 0 ? 1 : t2);

        Handles.color = Color.yellow;
        Handles.DrawLine(anglePoints.Q0, anglePoints.Q1);
        Handles.DrawLine(anglePoints.Q1, anglePoints.Q2);

        Handles.Label(anglePoints.Q0, "Q0");
        Handles.Label(anglePoints.Q1, "Q1");
        Handles.Label(anglePoints.Q2, "Q2");

        Handles.color = Color.blue;
        Handles.DrawLine(anglePoints.R0, anglePoints.R1);

        Handles.Label(anglePoints.R0, "R0");
        Handles.Label(anglePoints.R1, "R1");

        Handles.color = Color.green;
        Handles.DrawLine(b, c);
    }

    // angle point's position setter
    void AnglePointPositionSetter(SplineComponent.Anchor point , int i, float p)
    {
        var spline = target as SplineComponent;
        
        var previousIndex = i - 1;
        var nextIndex = i;

        if (spline.closed && previousIndex == spline.points.Count - 1) nextIndex = 0;

        /** setting up the angle points for the point **/
        var a = spline.transform.TransformPoint(spline.points[previousIndex].position);
        var b = spline.transform.TransformPoint(spline.points[previousIndex].handleBPosition);
        var c = spline.transform.TransformPoint(spline.points[nextIndex].handleAPosition);
        var d = spline.transform.TransformPoint(spline.points[nextIndex].position);
        var t1 = spline.FindControlPointT(spline.points[previousIndex].position);
        var t2 = spline.FindControlPointT(spline.points[nextIndex].position);

        // the new angle points
        var anglePoints = spline.CalculateAnglePoints(a, b, c, d, p, t1, t2 == 0 ? 1 : t2);
        var A = anglePoints.anglePointA;
        var B = anglePoints.anglePointB;
        point.handleAPosition = spline.transform.InverseTransformPoint(A);
        point.handleBPosition = spline.transform.InverseTransformPoint(B);
        // regulate the other angle points with the new point
        var Q0 = anglePoints.Q0;
        var Q2 = anglePoints.Q2;
        spline.points[previousIndex].handleBPosition = spline.transform.InverseTransformPoint(Q0);
        spline.points[nextIndex].handleAPosition = spline.transform.InverseTransformPoint(Q2);
    }

    // custom GUI button for Anchor Points
    bool CreateAnchorPointButton2D(Vector3 handlePosition, int i, float size, SplineComponent spline)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive); // Gets a new ControlID for the handle
        Vector3 screenPosition = Handles.matrix.MultiplyPoint(handlePosition); // getting screen position of the handle
        bool buttonOutput = false; // returns true when pressed
        float circleRangeSize = size; // is normal when not pressed on, gets bigger when pressed on

        // so that the handle doesnt escape our grasp when dragging
        if (GUIUtility.hotControl == controlID) circleRangeSize = 9999;

        switch (Event.current.GetTypeForControl(controlID))
        {
            // initialization of setting of the closeness Range
            case EventType.Layout:
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(screenPosition, circleRangeSize));
                break;

            // when dragging the handle
            case EventType.MouseDrag:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                {
                    // CTRL + DRAG = move angle points in opposite direction
                    if (modifyBothControlPoints)
                    {
                        MoveAnglePoint2D(spline, i, true);
                        Event.current.Use();
                        return true;
                    }

                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    MoveAnchorPoint2D(spline, i);
                    hotIndex = -1;
                    Event.current.Use();
                }
                break;

            // if clicked on button the create a position handle for it
            case EventType.MouseUp:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                {
                    hotIndex = i;
                }
                break;

            // how the handle looks
            case EventType.Repaint:
                if (HandleUtility.nearestControl == controlID) // This is the part that switches according to mouse over, and the rectangles act as the visual element of the button
                {
                    Handles.color = Color.red;
                    Handles.CubeHandleCap(controlID, handlePosition, Quaternion.identity, size, EventType.Repaint);
                }
                else
                {
                    Handles.color = spline.anchorPointColor;
                    Handles.CubeHandleCap(controlID, handlePosition, Quaternion.identity, size, EventType.Repaint);
                }

                if(hotIndex==i)
                {
                    Handles.color = Color.blue;
                    Handles.CubeHandleCap(controlID, handlePosition, Quaternion.identity, size, EventType.Repaint);
                }
                break;
        }

        return buttonOutput;
    }

    // moves the custom GUI button for Anchor Points and Angle Points with the mouse drag
    void MoveAnchorPoint2D(SplineComponent spline, int i)
    {
        // get the position of mouse in world coordinates on 2D plane
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        mousePosition = spline.transform.InverseTransformPoint(ray.origin);
        mousePosition.y = spline.points[i].position.y;

        // get the dragging distance
        var oldPos = spline.points[i].position;
        spline.points[i].position = mousePosition; // apply new point position
        var delta = mousePosition - oldPos;

        // applying the dragging distance
        spline.points[i].handleAPosition += delta;
        spline.points[i].handleBPosition += delta;
    }

    // custom GUI button for Angle Points
    bool CreateAnglePointButton2D(Vector3 handlePosition, int i, float size, SplineComponent spline, bool isAnglePointA)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive); // Gets a new ControlID for the handle
        Vector3 screenPosition = Handles.matrix.MultiplyPoint(handlePosition); // getting screen position of the handle
        bool buttonOutput = false; // returns true when pressed
        float anglePointCircleRangeSize = size; // is normal when not pressed on, gets bigger when pressed on

        // so that the handle doesnt escape our grasp when dragging
        if (GUIUtility.hotControl == controlID) anglePointCircleRangeSize = 9999;

        switch (Event.current.GetTypeForControl(controlID))
        {
            // initialization of setting of the closeness Range
            case EventType.Layout:
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(screenPosition, anglePointCircleRangeSize));
                break;

            // when dragging the handle
            case EventType.MouseDrag:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                {
                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    MoveAnglePoint2D(spline, i, isAnglePointA);
                    Event.current.Use();
                }
                break;

            // how the handle looks
            case EventType.Repaint:
                if (HandleUtility.nearestControl == controlID) // This is the part that switches according to mouse over, and the rectangles act as the visual element of the button
                {
                    Handles.color = Color.red;
                    Handles.SphereHandleCap(controlID, handlePosition, Quaternion.identity, size, EventType.Repaint);
                }
                else
                {
                    Handles.color = spline.anglePointColor;
                    Handles.SphereHandleCap(controlID, handlePosition, Quaternion.identity, size, EventType.Repaint);
                }
                break;
        }

        return buttonOutput;
    }

    // moves the custom GUI button for Angle Points with the mouse drag
    void MoveAnglePoint2D(SplineComponent spline, int i, bool isAnglePointA)
    {
        // get the position of mouse in world coordinates on 2D plane
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        mousePosition = spline.transform.InverseTransformPoint(ray.origin);
        mousePosition.y = spline.points[i].position.y;

        // apply new point position
        if (isAnglePointA) spline.points[i].handleAPosition = mousePosition;
        else spline.points[i].handleBPosition = mousePosition;

        // if we're pressing control move the angle point to the opposite of one another
        if (modifyBothControlPoints)
        {
            if (isAnglePointA) // angle point A
            {
                var delta = spline.points[i].handleAPosition - spline.points[i].position;
                spline.points[i].handleBPosition = spline.points[i].position - delta;
            }
            else // angle point B
            {
                var delta = spline.points[i].handleBPosition - spline.points[i].position;
                spline.points[i].handleAPosition = spline.points[i].position - delta;
            }
        }
    }

    // when creating a point make it a straight line (the angle points is in the direction of the other point)
    void AnglePointAInitialPositionSetter(SplineComponent spline, int i)
    {
        if (i == 0)
        {
            var p0 = spline.points[0].position;
            var p1 = spline.points[1].position;

            var distanceZeroToOne = (p1 - p0)*.3f;
            var distanceOneToZero = (p0 - p1)*.3f;

            spline.points[0].handleBPosition = p0 + distanceZeroToOne;
            spline.points[0].handleAPosition = p0 - distanceZeroToOne;
            spline.points[1].handleAPosition = p1 + distanceOneToZero;
        }
        else if(i == spline.points.Count - 1)
        {
            var p0 = spline.points[i].position;
            var p1 = spline.points[i-1].position;

            var distanceZeroToOne = (p1 - p0) * .3f;
            var distanceOneToZero = (p0 - p1) * .3f;

            spline.points[i].handleAPosition = p0 + distanceZeroToOne;
            spline.points[i].handleBPosition = p0 - distanceZeroToOne;
            spline.points[i-1].handleBPosition = p1 + distanceOneToZero;
        }
    }
}