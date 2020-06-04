using JPBotelho;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FlyCam : MonoBehaviour
{
    [SerializeField]
    private float speed = 10;
    [SerializeField]
    private float positionDampTime = 0.2f;
    [SerializeField]
    private float tangentDampTime = 4;
    [SerializeField]
    private float farDetectionRange = 25;
    [SerializeField]
    private float obstacleDetectionRange = 2;
    [SerializeField]
    private float advanceFrameRatio = 0.5f;
    [SerializeField]
    private int splineResolution = 20;

    private const int mask = 1 << 8;
    private const int controlPointCount = 6;
    private int maxIndex;
    private int index;
    private float t;

    private Frame frame;
    private CaveSystem cave;
    private CatmullRom spline;
    private CatmullRom.CatmullRomPoint[] splinePoints;
    private Queue<Vector3> controlPoints;
    private bool waitForCaveReady = true;

    private Vector3 position;
    private Vector3 posDampVelo;
    private Vector3 tangent;
    private Vector3 tanDampVelo;

    void Start()
    {
        cave = FindObjectOfType<CaveSystem>();
        cave.Initialize(transform);
        cave.OnReset();

        frame = new Frame(transform);
        spline = new CatmullRom(splineResolution);
        controlPoints = new Queue<Vector3>(controlPointCount);
        controlPoints.Enqueue(frame.origin);
        maxIndex = splineResolution * 3 - 1;
    }

    private void LateUpdate()
    {
        if (!waitForCaveReady)
        {
            WalkSpline();
        }
        else if (cave.AllChunksActive())
        {
            waitForCaveReady = false;
            UpdateSpline();
        }
    }

    private void WalkSpline()
    {
        if (t < 1f)
        {
            t += speed * Time.deltaTime;
        }
        else
        {
            t -= 1f;
            if (index < maxIndex)
            {
                index++;
            }
            else
            {
                index = splineResolution * 2;
                controlPoints.Dequeue();
                UpdateSpline();
            }
        }

        var a = splinePoints[index];
        var b = splinePoints[index + 1];
        Vector3 pos = Vector3.Lerp(a.position, b.position, t);
        Vector3 tan = Vector3.Lerp(a.tangent, b.tangent, t);

        pos += GetCentroid(transform, obstacleDetectionRange);
        position = Vector3.SmoothDamp(position, pos, ref posDampVelo, positionDampTime);
        tangent = Vector3.SmoothDamp(tangent, tan, ref tanDampVelo, tangentDampTime);
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(tangent);

        // spline.DrawSpline(Color.white);
    }

    private void UpdateSpline()
    {
        while (controlPoints.Count < controlPointCount)
        {
            frame.origin += GetFarthest(frame, farDetectionRange) * advanceFrameRatio;
            controlPoints.Enqueue(frame.origin);
        }
        spline.Update(controlPoints);
        splinePoints = spline.GetPoints();
    }

    private static Vector3 GetFarthest(Frame frame, float range, float angle = 45, float resolution = 6)
    {
        Ray[] rays = frame.GetRays(angle);
        Vector3 delta = Vector3.zero;

        for (int i = 0; i < rays.Length; i++)
        {
            if (Physics.Raycast(rays[i], out RaycastHit hit, range, mask))
            {
                if (hit.distance > delta.magnitude)
                {
                    delta = hit.point - rays[i].origin;
                }
            }
            else
            {
                delta = rays[i].direction * range;
                break;
            }
        }

        if (angle < resolution)
        {
            // 5.625 (45/8) degree resolution in 4th iteration, 17 raycasts total.
            return delta;
        }

        frame.Update(delta.normalized);
        return GetFarthest(frame, range, angle * 0.5f, resolution);
    }

    public static Vector3 GetCentroid(Transform origin, float range)
    {
        Vector3 c = Vector3.zero;
        Vector3 p = origin.position;
        Quaternion r = origin.rotation;

        for (int i = 0; i < sphere.Length; i++)
        {
            Vector3 d = r * sphere[i];
            c += Physics.Raycast(p, d, out RaycastHit hit, range, mask)
                ? hit.point - p
                : d * range;
        }

        return c / (float)sphere.Length;
    }

    private static Vector3[] sphere = CalcFibonacciSphere(32);

    private static Vector3[] CalcFibonacciSphere(int n)
    {
        Vector3[] v = new Vector3[n];
        float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < n; i++)
        {
            float y = 1 - (i / (n - 1f)) * 2;
            float theta = phi * i;
            float r = Mathf.Sqrt(1 - y * y);
            float x = Mathf.Cos(theta) * r;
            float z = Mathf.Sin(theta) * r;
            v[i] = new Vector3(x, y, z);
        }
        return v;
    }
}

public class Frame
{
    public Vector3 origin;
    public Vector3 right;
    public Vector3 up;
    public Vector3 forward;

    public Frame(Transform transform) : this(transform.position, transform.forward, transform.up)
    {
    }

    public Frame(Vector3 origin, Vector3 forward, Vector3 up)
    {
        this.origin = origin;
        Update(forward, up);
    }

    public void Update(Vector3 forward, Vector3 up)
    {
        this.forward = forward;
        this.right = Vector3.Cross(forward, -up);
        this.up = Vector3.Cross(forward, right);
    }

    public void Update(Vector3 forward)
    {
        Update(forward, up);
    }

    public Ray[] GetRays(float angle)
    {
        return angle < 45
            ? new Ray[4]
                {
                    new Ray(origin, Quaternion.AngleAxis(angle, up) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, -up) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, right) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, -right) * forward)
                }
            : new Ray[5]
                {
                    new Ray(origin, forward), // only needed for 1st iteration
                    new Ray(origin, Quaternion.AngleAxis(angle, up) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, -up) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, right) * forward),
                    new Ray(origin, Quaternion.AngleAxis(angle, -right) * forward)
                };
    }

    public void Draw()
    {
        Debug.DrawRay(origin, right, Color.red);
        Debug.DrawRay(origin, up, Color.green);
        Debug.DrawRay(origin, forward, Color.blue);
    }
}