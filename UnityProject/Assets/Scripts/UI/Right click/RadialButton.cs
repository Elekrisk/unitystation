using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

public class RadialButton : MonoBehaviour{

	public Image Circle;
	public Image Icon;
	public Text title;
	public string Hiddentitle;
	public int DefaultPosition;
	public Color DefaultColour;
	public int MenuDepth;
	public Action Action;

	public RightClickMenuItem MenuItem;
	public float GlobalAngle;

	public void SetColour (Color Color)
	{
		Circle.color = Color;
	}
	public Color ReceiveCurrentColour()
	{
		return(Circle.color);
	}

	public bool HasSubMenus() => MenuItem.SubMenus != null && MenuItem.SubMenus.Count > 0;
}

