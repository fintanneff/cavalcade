using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

public interface IControllable<T> where T : Node
{
    void CInput(InputEvent @event);
    void GainControl() { }
    void LoseControl() { }
}

public partial class ControlSystem : Node
{
    public static ControlSystem Inst { get; private set; }
    private static Stack<IControllable<Node>> controllingStack;
    public static void GiveControl(IControllable<Node> _c) {
        controllingStack.Push(_c);
        _c.GainControl();
    }
    public static void ReleaseControl(IControllable<Node> _c) {
        IControllable<Node> temp = null;
        if (!controllingStack.TryPeek(out temp)) return;
        if (temp != _c) return;
        temp.LoseControl();
        controllingStack.Pop();
        IControllable<Node> newControlled = GetControlling();
        if (newControlled != null) newControlled.GainControl();
    }
    public static IControllable<Node> GetControlling() {
        IControllable<Node> temp = null;
        controllingStack.TryPeek(out temp);
        return temp;
    }
    public static int GetControllingCount() { return controllingStack.Count; }
    public static void ReleaseAllControl() 
    {
        while (GetControllingCount() > 0)
        {
            ReleaseControl(GetControlling());
        }  
    }


    public override void _Ready()
    {
        controllingStack = new Stack<IControllable<Node>>();
        Inst = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (controllingStack == null) return;
        if (controllingStack.Count < 1) return;
        IControllable<Node> temp = null;
        if (!controllingStack.TryPeek(out temp)) return;
        temp.CInput(@event);
    }
}
