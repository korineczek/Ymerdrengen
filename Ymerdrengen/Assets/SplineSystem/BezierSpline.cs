﻿// <copyright file="BezierSpline.cs" company="Team4">
//     Team4 copyright tag.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class handles bezierspline points manipulation and strage
/// </summary>
public class BezierSpline : MonoBehaviour
{
    /// <summary>
    /// This holds the different modes of the bezier points, these modes are used for better control when creating a curve
    /// </summary>
    [SerializeField]
    private BezierControlPointMode[] modes;

    /// <summary>
    /// This is the spline points, these points determine the spline layout
    /// </summary>
    [SerializeField]
    public Vector3[] Points;

    [SerializeField]
    public List<BezierSpline> nextSplines;
    [SerializeField]
    public List<BezierSpline> previusSplines;

    [SerializeField]
    public bool endPoint;

    [SerializeField]
    public int index;

    /// <summary>
    /// Gets the current count of curves in the spline
    /// </summary>
    public int CurveCount
    {
        get
        {
            return (Points.Length - 1) / 3;
        }
    }

    /// <summary>
    ///  Gets control point count of the current spline NOT the same thing as CurveCount
    /// </summary>
    public int ControlPointCount
    {
        get
        {
            return Points.Length;
        }
    }

    /// <summary>
    /// Get the length of the spline
    /// </summary>
    /// <returns>Returns the length of the spline</returns>
    public float GetSplineLength()
    {
        int lengthCalcDensity = 20;
        int checkPointCount = lengthCalcDensity * CurveCount;
        float length = 0;
        for (int i = 0; i < checkPointCount - 1; i++)
        {
            length += Vector3.Distance(GetPoint(i), GetPoint(i + 1));
        }

        return length;
    }

    /// <summary>
    /// Get the control point of index i
    /// </summary>
    /// <param name="i">Index of control point</param>
    /// <returns>a vector 3 control point</returns>
    public Vector3 GetControlPoint(int i)
    {
        return Points[i];
    }

    public void addConnectionTospline(BezierSpline spline)
    {
        nextSplines.Add(spline);
        spline.addPreviusSpline(this);
    }

    public void addPreviusSpline(BezierSpline spline)
    {
        previusSplines.Add(spline);
    }

    /// <summary>
    /// Sets the control point of index i
    /// </summary>
    /// <param name="index">Index of control point</param>
    /// <param name="p">V3 Point</param>
    public void SetControlPoint(int index, Vector3 p)
    {
        if (index % 3 == 0)
        {
            Vector3 delta = p - Points[index];
            if (index > 0)
            {
                Points[index - 1] += delta;
            }

            if (index + 1 < Points.Length)
            {
                Points[index + 1] += delta;
            }
        }

        Debug.Log("ENFORCING");

        EnforceConnections(index);

        Points[index] = p;
        EnforceMode(index);
    }

    public void joinSpline()
    {
        pathSystem path = transform.parent.GetComponent<pathSystem>();

        path.JoinSpline(this);
    }

    public void EnforceConnections(int index)
    {
        Vector3 p = GetControlPoint(index);
        if (index == 0)
        {
            for (int i = 0; i < previusSplines.Count; i++)
            {
                previusSplines[i].Points[previusSplines[i].Points.Length - 1] = p;
                for(int y = 0; y < previusSplines[i].nextSplines.Count; y++)
                {
                    previusSplines[i].nextSplines[y].Points[0] = p;
                }
            }
        }
        if (index == Points.Length - 1)
        {
            for (int i = 0; i < nextSplines.Count; i++)
            {
                nextSplines[i].Points[0] = p;
                for (int y = 0; y < nextSplines[i].previusSplines.Count; y++)
                {
                    nextSplines[i].previusSplines[y].Points[nextSplines[i].previusSplines[y].Points.Length - 1] = p;
                }
            }
        }
    }

    public BezierControlPointMode GetControlPointMode(int index)
    {
        return modes[(index + 1) / 3];
    }

    public void SetControlPointMode(int index, BezierControlPointMode mode)
    {
        modes[(index + 1) / 3] = mode;
        EnforceMode(index);
    }

    /// <summary>
    /// Get point returns a position on the spline to a time between 0 and 1, 0 being the start of the spline and 1 the end of the spline.
    /// </summary>
    /// <param name="t">Time between 0 and 1</param>
    /// <returns>Point on the spline to time t</returns>
    public Vector3 GetPoint(float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = Points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }

        return transform.TransformPoint(Bezier.GetPoint(Points[i], Points[i + 1], Points[i + 2], Points[i + 3], t));
    }

    /// <summary>
    /// Get the velocity of a point on the spline, this velocity is according to the direction of the point
    /// </summary>
    /// <param name="t">Time between 0 and 1</param>
    /// <returns>Velocity of point on spline</returns>
    public Vector3 GetVelocity(float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = Points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }

        return transform.TransformPoint(Bezier.GetFirstDerivative(Points[i], Points[i + 1], Points[i + 2], Points[i + 3], t)) -
            transform.position;
    }

    /// <summary>
    /// Get direction of a point on the spline to the time t
    /// </summary>
    /// <param name="t">Time between 0 and 1</param>
    /// <returns>Direction of time</returns>
    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    /// <summary>
    /// Adds a curve to the current spline, this is for editor view use only
    /// </summary>
    public void AddCurve()
    {
        Vector3 point = Points[Points.Length - 1];
        Array.Resize(ref Points, Points.Length + 3);
        point.x += 1f;
        Points[Points.Length - 3] = point;
        point.x += 1f;
        Points[Points.Length - 2] = point;
        point.x += 1f;
        Points[Points.Length - 1] = point;

        Array.Resize(ref modes, modes.Length + 1);
        modes[modes.Length - 1] = modes[modes.Length - 2];
        EnforceMode(Points.Length - 4);
    }

    public void SplitCurve()
    {
        Vector3 point = Points[Points.Length - 1];
        pathSystem path = transform.parent.GetComponent<pathSystem>();

        path.SplitPath(this);

        //EnforceMode(Points.Length - 4);
    }

    /// <summary>
    /// Adds a curve to the current spline, this is for editor view use only
    /// </summary>
    public void AddCurve(Vector3 startPoint)
    {
        Vector3 point = startPoint;
        Array.Resize(ref Points, Points.Length + 3);
        point.x += 1f;
        Points[Points.Length - 3] = point;
        point.x += 1f;
        Points[Points.Length - 2] = point;
        point.x += 1f;
        Points[Points.Length - 1] = point;

        Array.Resize(ref modes, modes.Length + 1);
        modes[modes.Length - 1] = modes[modes.Length - 2];
        EnforceMode(Points.Length - 4);
    }

    /// <summary>
    /// Resets the current spline to a standard one curve spline, this is for editor view use only
    /// </summary>
    public void Reset(int index)
    {
        Points = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0),
            new Vector3(3, 0, 0),
            new Vector3(4, 0, 0)
        };

        modes = new BezierControlPointMode[]
        {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
        };

        this.index = index; 
        nextSplines = new List<BezierSpline>();
        previusSplines = new List<BezierSpline>();
    }

    public void Reset(Vector3 pos, int index)
    {
        Points = new Vector3[] 
        {
            new Vector3(0, 0, 0) + pos,
            new Vector3(1, 0, 0) + pos,
            new Vector3(2, 0, 0) + pos,
            new Vector3(3, 0, 0) + pos
        };

        modes = new BezierControlPointMode[] 
        {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
        };

        this.index = index;
        nextSplines = new List<BezierSpline>();
        previusSplines = new List<BezierSpline>();
    }

    private void EnforceMode(int index)
    {
        int modeIndex = (index + 1) / 3;
        BezierControlPointMode mode = modes[modeIndex];
        if (mode == BezierControlPointMode.Free || modeIndex == 0 || modeIndex == modes.Length - 1)
        {
            return;
        }

        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;
            enforcedIndex = middleIndex + 1;
        }
        else
        {
            fixedIndex = middleIndex + 1;
            enforcedIndex = middleIndex - 1;
        }

        Vector3 middle = Points[middleIndex];
        Vector3 enforcedTangent = middle - Points[fixedIndex];
        if (mode == BezierControlPointMode.Aligned)
        {
            enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, Points[enforcedIndex]);
        }

        Points[enforcedIndex] = middle + enforcedTangent;
    }
}
