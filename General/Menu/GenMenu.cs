using Godot;
using System;

public partial class GenMenu : Node2D, IControllable<Node>
{
    [Export] private GenMenu parentMenu;
    [Export] private Node2D menuItemsStart;
    private Vector2 menuItemStartPos;
    [Export] private int spaceBetweenItems;
    [Export] public Godot.Collections.Array<Node2D> MenuItems { get; set; }
    private int cursorIndex;

    public override void _Ready() {
        Visible = false;
        if (menuItemsStart == null)
        {
            if (MenuItems != null) menuItemStartPos = MenuItems[0].GlobalPosition;
        }
        else menuItemStartPos = menuItemsStart.GlobalPosition;
    }

    public virtual void GainControl()
    {
        MoveCursor(0);
        if (!Visible && parentMenu==null)
        {
            MenuCursor.Inst.GlobalPosition = MenuCursor.Inst.HoverPos;
        }
        Visible = true;
    }
    public virtual void LoseControl()
    {
        Visible = false;
    }

    public void ClearMenuItems()
    {
        foreach (Node2D n2d in MenuItems) { n2d.QueueFree(); }
        MenuItems.Clear();
    }
    public void PushToMenu(Node2D newItem)
    {
        AddChild(newItem);
        newItem.GlobalPosition = menuItemStartPos + new Vector2(0, spaceBetweenItems * MenuItems.Count);
        MenuItems.Add(newItem);
    }
    public void RepositionItems()
    {
        int running = 0;
        for (int i = 0; i < MenuItems.Count; i++)
        {
            if (!MenuItems[i].Visible) continue;
            MenuItems[i].GlobalPosition = menuItemStartPos + new Vector2(0, running);
            running += spaceBetweenItems;
        }
    }

    public void CInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_up")) MoveCursor(-1);
        if (Input.IsActionJustPressed("ui_down")) MoveCursor(1);
        if (Input.IsActionJustPressed("ui_accept")) SelectItem(MenuItems[cursorIndex]);
        if (Input.IsActionJustPressed("ui_cancel")) ControlSystem.ReleaseControl(this);
    }

    protected void MoveCursor(int _inc)
    {
        if (MenuItems == null) return;
        if (MenuItems.Count < 1) return;
        for (int i = 0; i < MenuItems.Count; i++) 
        {
            cursorIndex = Mathf.Wrap(cursorIndex + _inc, 0, MenuItems.Count);
            if (MenuItems[cursorIndex].Visible) break;
            else if (_inc == 0) _inc = 1;
        }
        MenuCursor.Inst.HoverPos = MenuItems[cursorIndex].GlobalPosition;
    }
    protected bool SelectItem(Node item)
    {
        if (parentMenu != null)
        {
            if (parentMenu.SelectItem(item)) return true;
        }
        if (SwitchItemName(item.Name.ToString())) return true;
        if (item is Button) {
            Button b = item as Button;
            item.EmitSignal(Button.SignalName.Pressed);
            return true;
        }
        return false; 
    }
    protected virtual bool SwitchItemName(string itemName) { return false; }

}
