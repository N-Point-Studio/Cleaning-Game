using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExtensionMethod
{
    public static void SetLayerRecursively(this GameObject obj, string layerName, params GameObject[] exceptions)
    {
        obj.SetLayerRecursively(LayerMask.NameToLayer(layerName), exceptions);
    }

    public static void SetLayerRecursively(this GameObject obj, int layer, params GameObject[] exceptions)
    {
        if (!Array.Exists(exceptions, x => x.Equals(obj)))
        {
            obj.layer = layer;
        }

        foreach (Transform child in obj.transform)
        {
            child.gameObject.SetLayerRecursively(layer, exceptions);
        }
    }

    public static long GetCurrentTimestamp()
    {
        DateTime date = DateTime.Now;
        return ((DateTimeOffset)date).ToUnixTimeSeconds();
    }

    public static long GetCurrentUTCTimestamp()
    {
        DateTime date = DateTime.UtcNow;
        return ((DateTimeOffset)date).ToUnixTimeSeconds();
    }

    public static bool ContainsParam(this Animator _Anim, string _ParamName)
    {
        foreach (AnimatorControllerParameter param in _Anim.parameters)
        {
            if (param.name == _ParamName) return true;
        }
        return false;
    }

    public static bool ContainsParam(this Animator _Anim, int _ParamHash)
    {
        foreach (AnimatorControllerParameter param in _Anim.parameters)
        {
            if (param.nameHash == _ParamHash) return true;
        }
        return false;
    }

    public static void DestroyAllChildren(this Transform obj, float t = 0)
    {
        for (int i = 0; i < obj.childCount; i++)
        {
            UnityEngine.Object.Destroy(obj.GetChild(i).gameObject, t);
        }
    }

    public static void TurnOffAllChildren(this Transform obj)
    {
        for (int i = 0; i < obj.childCount; i++)
        {
            obj.GetChild(i).gameObject.SetActive(false);
        }
    }

    public static RectTransform Left(this RectTransform rt, float x)
    {
        rt.offsetMin = new Vector2(x, rt.offsetMin.y);
        return rt;
    }

    public static RectTransform Right(this RectTransform rt, float x)
    {
        rt.offsetMax = new Vector2(-x, rt.offsetMax.y);
        return rt;
    }

    public static RectTransform Bottom(this RectTransform rt, float y)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, y);
        return rt;
    }

    public static RectTransform Top(this RectTransform rt, float y)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -y);
        return rt;
    }

    public static bool ContainsIndex(this Array array, int index, int dimension)
    {
        if (index < 0)
            return false;

        return index < array.GetLength(dimension);
    }

    public static void Shuffle<T>(this List<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static Vector3 Round(this Vector3 v, int decimalPlaces)
    {
        float multiplier = Mathf.Pow(10f, decimalPlaces);
        return new Vector3(
            Mathf.Round(v.x * multiplier) / multiplier,
            Mathf.Round(v.y * multiplier) / multiplier,
            Mathf.Round(v.z * multiplier) / multiplier
        );
    }

    public static Vector2Int ToDirection(this Vector2Int v, bool prioritizeXAxis = false, bool prioritizeYAxis = false)
    {
        Vector2Int clamped = new Vector2Int(Mathf.Clamp(v.x, -1, 1), Mathf.Clamp(v.y, -1, 1));

        if (clamped.x != 0 && clamped.y != 0)
        {
            if (prioritizeXAxis)
            {
                clamped.y = 0;
            }
            else if (prioritizeYAxis)
            {
                clamped.x = 0;
            }
        }

        return clamped;
    }

    public static List<Vector3> SortPointsIntoPolygon(List<Vector3> points, Vector3 center)
    {
        //GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //obj.name = "lha";
        //obj.transform.position = center;
        //obj.transform.localScale = Vector3.one * 0.5f;

        points.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
            float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);

            int angleComparison = angleA.CompareTo(angleB);
            if (angleComparison != 0)
            {
                return angleComparison; // urut berdasarkan sudut terlebih dahulu
            }

            float distanceA = Vector3.SqrMagnitude(a - center);
            float distanceB = Vector3.SqrMagnitude(b - center);

            return distanceA.CompareTo(distanceB); // lalu jaraknya
        });

        return points;
    }

    public static List<Vector3> SortPointsIntoPolygon(List<Vector3> points)
    {
        return SortPointsIntoPolygon(points, GetCentroid(points));
    }

    private static Vector3 GetCentroid(List<Vector3> points)
    {
        Vector3 center = Vector3.zero;
        foreach (var point in points)
            center += point;
        center /= points.Count;
        return center;
    }

    private static Vector3 GetCenterBoundingBox(List<Vector3> points)
    {
        if (points.Count < 3)
            return Vector3.zero;

        Vector3 min = points[0];
        Vector3 max = points[0];

        foreach (Vector3 point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        return (min + max) / 2f;
    }

    public static List<Vector3> GenerateConcaveHull(List<Vector3> points, int k)
    {
        if (points.Count < 3)
            return new List<Vector3>(points);

        // Jangan terlalu kecil nilai k
        k = Mathf.Clamp(k, 3, points.Count - 1);

        List<Vector3> hull = new List<Vector3>();
        List<Vector3> remaining = new List<Vector3>(points);

        // Mulai dari titik paling bawah
        Vector3 start = remaining.OrderBy(p => p.y).ThenBy(p => p.x).First();
        Vector3 current = start;
        Vector3 prevDir = Vector3.left;
        hull.Add(start);
        remaining.Remove(start);

        int step = 0;
        while ((current != start || hull.Count == 1) && remaining.Count > 0 && step < 1000)
        {
            var kNearest = remaining
                .OrderBy(p => Vector3.SqrMagnitude(p - current))
                .Take(k)
                .ToList();

            Vector3 next = Vector3.zero;
            float smallestAngle = float.MaxValue;
            bool found = false;

            foreach (var neighbor in kNearest)
            {
                Vector3 dir = (neighbor - current).normalized;
                float angle = Vector2.SignedAngle(prevDir, dir);
                if (angle < 0) angle += 360;

                if (angle < smallestAngle)
                {
                    next = neighbor;
                    smallestAngle = angle;
                    found = true;
                }
            }

            if (!found) break;

            // Tambahkan titik jika tidak membuat self-intersection
            if (IsValidSegment(hull, current, next))
            {
                prevDir = (next - current).normalized;
                current = next;
                hull.Add(current);
                remaining.Remove(current);
            }

            step++;
        }

        return hull;
    }

    private static bool IsValidSegment(List<Vector3> hull, Vector3 a, Vector3 b)
    {
        int count = hull.Count;
        for (int i = 0; i < count - 2; i++)
        {
            if (DoLinesIntersect(hull[i], hull[i + 1], a, b))
                return false;
        }
        return true;
    }

    private static bool DoLinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        return (CCW(a1, b1, b2) != CCW(a2, b1, b2)) && (CCW(a1, a2, b1) != CCW(a1, a2, b2));
    }

    private static bool CCW(Vector3 a, Vector3 b, Vector3 c)
    {
        return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
    }

    public static string ToHMS(this TimeSpan timespan)
    {
        int hours = (int)timespan.TotalHours;
        int minutes = timespan.Minutes;
        int seconds = timespan.Seconds;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}