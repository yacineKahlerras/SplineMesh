using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineComponent : MonoBehaviour, ISpline
{
    // The Data
    public bool closed = false;
    public List<Anchor> points = new List<Anchor>();
    public float? length;
    public bool layout2D = false;
    public Color splineColor = Color.white;
    public Color anchorPointColor = Color.white;
    public Color anglePointColor = Color.white;
    public Color anglePointLinesColor = Color.black;
    
    /// Index is used to provide uniform point searching.
    SplineIndex uniformIndex;
    SplineIndex Index
    {
        get
        {
            if (uniformIndex == null) uniformIndex = new SplineIndex(this);
            return uniformIndex;
        }
    }

    public void ResetIndex()
    {
        uniformIndex = null;
        length = null;
    }

    [Serializable]
    public class Point
    {
        public float t;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 normal;
    }

    [Serializable]
    public class Anchor
    {
        public Vector3 position;
        public Vector3 handleAPosition;
        public Vector3 handleBPosition;
        public Vector3 normal;
        public Vector3 forward;
    }

    [Serializable]
    public class AnglePoints
    {
        public Vector3 anglePointA;
        public Vector3 anglePointB;
        public Vector3 Q0, Q1, Q2, R0, R1;
    }

    void Reset()
    {
        List<Vector3>  newPositions = new List<Vector3>() {
            Vector3.forward * 3,
            Vector3.forward * 6,
            Vector3.forward * 9,
            Vector3.forward * 12
        };

        points = new List<Anchor>();

        for (int i = 0; i < newPositions.Count; i++)
        {
            Anchor newPoint = new Anchor();
            newPoint.position = newPositions[i];

            points.Add(newPoint);
        }
    }

    void OnValidate()
    {
        if (uniformIndex != null) uniformIndex.ReIndex();
    }

    public Vector3 GetPoint(float t) => Index.GetPoint(t);

    public Vector3 GetRight(float t)
    {
        var A = GetPoint(t - 0.001f);
        var B = GetPoint(t + 0.001f);
        var delta = (B - A);
        return new Vector3(-delta.z, 0, delta.x).normalized;
    }

    public Vector3 GetForward(float t)
    {
        var A = GetPoint(t - 0.001f);
        var B = GetPoint(t + 0.001f);
        return (B - A).normalized;
    }

    public Vector3 GetUp(float t)
    {
        var A = GetPoint(t - 0.001f);
        var B = GetPoint(t + 0.001f);
        var delta = (B - A).normalized;
        return Vector3.Cross(delta, GetRight(t));
    }


    public Vector3 GetLeft(float t) => -GetRight(t);


    public Vector3 GetDown(float t) => -GetUp(t);


    public Vector3 GetBackward(float t) => -GetForward(t);

    public float GetLength(float step = 0.001f)
    {
        var D = 0f;
        var A = GetNonUniformPoint(0);
        for (var t = 0f; t < 1f; t += step)
        {
            var B = GetNonUniformPoint(t);
            var delta = (B - A);
            D += delta.magnitude;
            A = B;
        }
        return D;
    }

    public Vector3 GetDistance(float distance)
    {
        if (length == null) length = GetLength();
        return uniformIndex.GetPoint(distance / length.Value);
    }

    public int ControlPointCount { get { throw new System.NotImplementedException(); } }


    public Vector3 GetControlPoint(int index)
    {
        return points[index].position;
    }

    public Vector3 FindClosest(Vector3 worldPoint)
    {
        var smallestDelta = float.MaxValue;
        var step = 1f / 1024;
        var closestPoint = Vector3.zero;
        for (var i = 0; i <= 1024; i++)
        {
            var p = GetPoint(i * step);
            var delta = (worldPoint - p).sqrMagnitude;
            if (delta < smallestDelta)
            {
                closestPoint = p;
                smallestDelta = delta;
            }
        }
        return closestPoint;
    }


    public Vector3 GetNonUniformPoint(float t)
    {
        switch (points.Count)
        {
            /*case 0:
                return Vector3.zero;
            case 1:
                return transform.TransformPoint(points[0].position);
            case 2:
                return transform.TransformPoint(Vector3.Lerp(points[0].position, points[1].position, t));
            case 3:
                return transform.TransformPoint(points[1].position);*/
            default:
                return Hermite(t);
        }
    }


    public void InsertControlPoint(int index, Vector3 position)
    {
        ResetIndex();
        if (index >= points.Count)
        {
            Anchor newPoint = new Anchor();
            newPoint.position = position;
            points.Add(newPoint);
        }
        else
        {
            Anchor newPoint = new Anchor();
            newPoint.position = position;
            points.Insert(index, newPoint);
        }
    }


    public void RemoveControlPoint(int index)
    {
        ResetIndex();
        points.RemoveAt(index);
    }


    public void SetControlPoint(int index, Vector3 position)
    {
        ResetIndex();
        points[index].position = position;
    }

    // The Interpolator
    internal static Vector3 Interpolate(Vector3 B, Vector3 angleA, Vector3 angleB, Vector3 A, float t)
    {
        return Mathf.Pow((1 - t), 3) * B + Mathf.Pow((1 - t), 2) * 3 * t * angleA + 3 * (1 - t) * Mathf.Pow((t), 2) * angleB + Mathf.Pow(t, 3) * A;
    }

    // The Hermite
    public Vector3 Hermite(float t)
    {
        var count = points.Count - (closed ? 0 : 1);
        var i = Mathf.Min(Mathf.FloorToInt(t * (float)count), count - 1);
        var u = t * (float)count - (float)i;
        var a = GetPointByIndex(i);
        var b = GetPointByIndex(i + 1);
        return transform.TransformPoint(Interpolate(a, GetPositionHandleBByIndex(i), GetPositionHandleAByIndex(i + 1), b, u));
    }

    Vector3 GetPointByIndex(int i)
    {
        if (i < 0) i += points.Count;
        return points[i % points.Count].position;
    }

    Vector3 GetPositionHandleAByIndex(int i)
    {
        if (i < 0) i += points.Count;
        return points[i % points.Count].handleAPosition;
    }

    Vector3 GetPositionHandleBByIndex(int i)
    {
        if (i < 0) i += points.Count;
        return points[i % points.Count].handleBPosition;
    }

    public AnglePoints CalculateAnglePoints(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t, float t1, float t2)
    {
        t = (t - t1) / (t2 - t1);

        Vector3 Q0 = P0 + (P1 - P0)*t;
        Vector3 Q1 = P1 + (P2 - P1)*t;
        Vector3 Q2 = P2 + (P3 - P2)*t;

        Vector3 R0 = Q0 + (Q1 - Q0) * t;
        Vector3 R1 = Q1 + (Q2 - Q1) * t;

        AnglePoints anglePoints = new AnglePoints();
        anglePoints.anglePointA = R0;
        anglePoints.anglePointB = R1;
        anglePoints.Q0 = Q0;
        anglePoints.Q1 = Q1;
        anglePoints.Q2 = Q2;
        anglePoints.R0 = R0;
        anglePoints.R1 = R1;

        return anglePoints;
    }

    // finds the t of a control point
    public float FindControlPointT(Vector3 controlPoint, float step = .0001f)
    {
        var closestT = 1f;
        float minDist = 9999999;

        for (float t = 0; t <= 1; t += step)
        {
            var p = transform.InverseTransformPoint(GetNonUniformPoint(t));
            var dist = (controlPoint - p).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                closestT = t;
            }
        }

        return closestT;
    }

    // gets a tangent for a point at t
    public Vector3 GetTangent(float t)
    {
        var count = points.Count - (closed ? 0 : 1);
        var i = Mathf.Min(Mathf.FloorToInt(t * (float)count), count - 1);
        var u = t * (float)count - (float)i;

        var P0 = GetPointByIndex(i);
        var P1 = GetPositionHandleBByIndex(i);
        var P2 = GetPositionHandleAByIndex(i+1);
        var P3 = GetPointByIndex(i+1);

        AnglePoints anglePoints = CalculateAnglePoints(P0, P1, P2, P3, t, FindControlPointT(P0), FindControlPointT(P3));

        return (anglePoints.R1 - anglePoints.R0).normalized;
    }

    // gets a 2D normal for a point at t
    public Vector2 GetNormal2D(float t)
    {
        var tangent = GetTangent(t);
        return new Vector2(-tangent.y, tangent.x);
    }

    // 3D normal for a point at t
    public Vector2 GetNormal3D(float t, Vector3 up)
    {
        var tangent = GetTangent(t);
        var binormal = Vector3.Cross(up, tangent).normalized;

        return Vector3.Cross(tangent,binormal);
    }

    // Direction in which each point look at
    public Quaternion GetOrientation2D(float t)
    {
        var tangent = GetTangent(t);
        var normal = GetNormal2D(t);

        return Quaternion.LookRotation(tangent, normal);
    }

    // Direction in which each point look at
    public Quaternion GetOrientation3D(float t, Vector3 up)
    {
        var tangent = GetTangent(t);
        var normal = GetNormal3D(t, up);

        return Quaternion.LookRotation(tangent, normal);
    }
}