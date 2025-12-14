using BlueprintExplorer.Properties;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BlueprintExplorer;
public class Node(Node parent = null)
{
    public Matrix Local = new();
    public Node Parent = parent;

    public Matrix World
    {
        get
        {
            if (Parent == null) return Local.Clone();
            Matrix m = Parent.World;
            m.Multiply(Local);
            return m;
        }
    }
}

public class Truck
{
    public static Bitmap Sprite = Resources.snow_truck;
    public static readonly int Height = 24;
    public readonly Node Self;
    public readonly Node FrontWheel;
    public readonly Node BackWheel;

    public float Velocity = 0;
    public float Acc = 0;

    public float TopSpeed = 70.0f;

    public bool Parked = true;

    public void Update(float dt)
    {
        Velocity += Acc * dt;
        Velocity = Math.Clamp(Velocity, -TopSpeed, TopSpeed);
        Self.Local.Translate(Velocity * dt, 0, MatrixOrder.Append);
    }

    public Truck(Node parent)
    {
        Self = new(parent);
        FrontWheel = new(Self);
        FrontWheel.Local.Translate(30, 0);
        BackWheel = new(Self);
        BackWheel.Local.Translate(0, 0);
    }

}

public static class SnowWorld
{
    public static readonly Node Root = new();
    public static readonly Truck Truck = new(Root);
}

public enum BearState
{
    Hiding,
    Emerging,
    Looking,
    LookingLeft,
    Looking2,
    LookingRight,
    Looking3,
    Submerging,
}
