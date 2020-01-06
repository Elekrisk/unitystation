using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// All angle variables are in radians, except serialized fields.
// Comments use degrees for ease of understanding.

/// <summary>
/// A radial menu which appears when right clicking tiles.
/// </summary>
public class RadialMenu : MonoBehaviour
{
	/// <summary>
	/// The center of the menu.
	/// </summary>
	Vector2 menuCenter;
	bool initialized = false;

	[SerializeField]
	[Tooltip("The minimum distance between buttons, in pixels.")]
	private float minimumButtonDistance = 80;

	[SerializeField]
	[Tooltip("The maximum angle between the leftmost and rightmost buttons, in degrees. " +
		"This does not apply to buttons with depth 0. " +
		"Should always be 360 or below.")]
	private float maximumButtonAngleRange = 360f * 2 / 3;

	[SerializeField]
	[Tooltip("The base prefab to use for buttons.")]
	private RadialButton buttonPrefab;

	[SerializeField]
	[Tooltip("The distance between layers of buttons.")]
	private float layerDistance = 100f;

	/// <summary>
	/// A reference to the last selected button, or null if none previously selected.
	/// </summary>
	//private RadialButton lastSelectedButton = null;

	/// <summary>
	/// The depth of the deepest button layer.
	/// </summary>
	private int MaximumDepth => buttonLayers.Count - 1;

	private List<ButtonLayer> buttonLayers = new List<ButtonLayer>();

	/// <summary>
	/// The UIManager's scale, used for correctly getting distances
	/// </summary>
	private float scale;

	/// <summary>
	/// Class used for storing radial buttons and placement information.
	/// </summary>
	public class ButtonLayer
	{
		/// <summary>
		/// A list containing the buttons in this layer.
		/// </summary>
		public List<RadialButton> Buttons;
		/// <summary>
		/// The angle where the center of the layer should be placed.
		/// </summary>
		public float Angle;
		/// <summary>
		/// The angle between the left and right edges of the button placement area.
		/// Buttons are not placed at the edges of the range, but distributed so that
		/// if the range where to loop (e.g. a full circle), all buttons are an equal
		/// distance from each other.
		/// </summary>
		public float Range;
		/// <summary>
		/// The currently selected button in the layer, if any.
		/// </summary>
		public RadialButton SelectedButton = null;

		/// <summary>
		/// The distance between neighbouring buttons, in radians.
		/// </summary>
		public float ButtonDistance => Range / Buttons.Count;

		public ButtonLayer(List<RadialButton> buttons, float angle, float range)
		{
			Buttons = buttons;
			Angle = angle;
			Range = range;
		}
	}
	/// <summary>
	/// Sets the menu up, i.e. instantiates the buttons into the scene.
	/// </summary>
	/// <param name="items">A list of buttons to spawn.</param>
	public void SetupMenu(List<RightClickMenuItem> items)
	{
		// Set the center of the menu to the cursor's position,
		// to make the menu appear around the mouse.
		menuCenter = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y);

		GameObject uiManager = GameObject.Find("UIManager");
		// Hoping that the UIManager's scale always is equal in x and y
		scale = uiManager.transform.localScale.x;

		SpawnButtons(items, 0, 0);

		initialized = true;
	}

	/// <summary>
	/// Instantiates buttons at the specified depth, using placement information
	/// from the buttons one layer below the specified depth.
	/// </summary>
	/// <param name="items">A list of buttons to spawn.</param>
	/// <param name="depth">The depth at which items will be spawned. Must not be negative.</param>
	public void SpawnButtons(List<RightClickMenuItem> items, int depth, float parentAngle)
	{
		float angle = parentAngle; // Angle of center of items
		float range; // Range of angles of items

		if (depth > 0)
		{
			ButtonLayer parentButtons = buttonLayers[depth - 1];
			// Requested range
			range = parentButtons.Range / parentButtons.Buttons.Count;
			// The absolute minimum range required.
			float minRange = items.Count * (minimumButtonDistance / ((depth + 1) * layerDistance));
			//Debug.Log($"Min button distance: {minimumButtonDistance}, degree version: {minimumButtonDistance / (depth * layerDistance) * Mathf.Rad2Deg}");
			//Debug.Log($"Total minimum range: {minRange * Mathf.Rad2Deg}");
			// The max range, set by serialized field.
			float maxRange = maximumButtonAngleRange * Mathf.Deg2Rad;
			// Range is clamped between max and min range, but if min range
			// is larger than max range, min range has priority.
			range = Mathf.Min(range, maxRange);
			range = Mathf.Max(range, minRange);
			// Range should never be above 360°
			range = Mathf.Min(range, Mathf.PI * 2);
		}
		else
		{
			// Range of items in level 0 is a full circle.
			range = Mathf.PI * 2;
		}

		// Angle distance between buttons, protected against division by zero
		float angleDistance = items.Count > 0 ? range / items.Count : range;
		// Start angle of buttons, i.e. angle of leftmost button
		float startAngle = angle - range / 2;

		// Temporary layer variable, so that buttons can be added to it in the loop below.
		ButtonLayer newButtonLayer = new ButtonLayer(new List<RadialButton>(), angle, range);

		for (var i = 0; i < items.Count; ++i)
		{
			RadialButton button = Instantiate(buttonPrefab, transform) as RadialButton;

			// The button angle in the menu angle system (0° straight up, between -180° and 180°,
			// positive clockwise)
			float buttonAngle = startAngle + angleDistance * i + angleDistance / 2;
			// The button angle, adjusted to the normal angle system (0° right,
			// positive counter-clockwise) needed for getting coordinates
			float actualButtonAngle = AdjustAngle(-buttonAngle, -Mathf.PI / 2);
			Debug.Log($"Radial button {i}: {items[i].Label} angle: {buttonAngle}");
			float distance = layerDistance * (depth + 1);
			float xPos = Mathf.Cos(actualButtonAngle);
			float yPos = Mathf.Sin(actualButtonAngle);
			button.transform.localPosition = new Vector2(xPos, yPos) * distance;

			// Set some properties on the button.
			// These are mostly unchanged from the old radial menu.
			button.Circle.color = items[i].BackgroundColor;
			button.Icon.sprite = items[i].IconSprite;
			if (items[i].BackgroundSprite != null)
			{
				button.Circle.sprite = items[i].BackgroundSprite;
			}

			button.MenuDepth = depth;
			button.Hiddentitle = items[i].Label;

			// These are new:
			button.MenuItem = items[i];
			button.GlobalAngle = buttonAngle; // Save the angle for submenus
			newButtonLayer.Buttons.Add(button);
		}
		// Simple check for depth
		if (buttonLayers.Count <= depth)
		{
			buttonLayers.Add(newButtonLayer);
		}
	}

	void Update()
	{
		if (initialized)
		{
			// Get mouse coordinates relative the menu center
			Vector2 mousePos = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y);
			Vector2 relativeMenu = mousePos - menuCenter;

			// Get the angle from relative mouse coordinates and convert them to the same
			// system as the buttons, i.e. 0° straight up, left being negative and right
			// positive. The range should be from -180° to 180°.
			float angle = AdjustAngle(-Mathf.Atan2(relativeMenu.y, relativeMenu.x), -Mathf.PI / 2);
			//Debug.Log(angle);

			// Compensate for UIManager's scale
			float distance = relativeMenu.magnitude / scale;

			// Get which layer the mouse is on. If the mouse is close to the center,
			// layerDepth becomes -1.
			int layerDepth = Mathf.RoundToInt((distance - layerDistance) / layerDistance);
			// If the mouse is at a layer which does not exist, it should be clamped to the
			// maximum depth.
			layerDepth = Mathf.Min(layerDepth, MaximumDepth);
			//Debug.Log(layerDepth);

			// If the mouse is too far to the left or right to select a button in a layer,
			// drop the layer depth by 1. Keep doing this until the mouse is above a button,
			// or the depth reaches -1.
			while (layerDepth > -1)
			{
				ButtonLayer selectedLayer = buttonLayers[layerDepth];
				float relativeAngle = AdjustAngle(angle, selectedLayer.Angle);
				if (Mathf.Abs(relativeAngle) > selectedLayer.Range / 2)
				{
					layerDepth -= 1;
				}
				else
				{
					break;
				}
			}

			// If the mouse is close to the center, deselect all buttons.
			if (layerDepth < 0)
			{
				for (int layer = MaximumDepth; layer >= 0; --layer)
				{
					DeselectButton(buttonLayers[layer]);
				}
			}
			else
			{
				// Get the index of the selected item
				ButtonLayer selectedLayer = buttonLayers[layerDepth];
				// Transforms the angle to layer relative angle
				float relativeAngle = AdjustAngle(angle, selectedLayer.Angle);
				int itemIndex = (int)((relativeAngle + selectedLayer.Range / 2) / selectedLayer.ButtonDistance);
				if (itemIndex < 0 || itemIndex >= selectedLayer.Buttons.Count)
				{
					// Should have already been handled in the while loop above
					Debug.LogError("Item index outside bounds, should never happen!");
					return;
				}
				// Select the button, which handles opening and closing submenus of above layers
				// and deselecting of buttons in above layers
				SelectButton(selectedLayer, selectedLayer.Buttons[itemIndex]);
			}
		}

		// On release, activate the selected button of the deepest layer and destroy
		if (CommonInput.GetMouseButtonUp(1))
		{
			if (buttonLayers[MaximumDepth].SelectedButton != null)
			{
				buttonLayers[MaximumDepth].SelectedButton.MenuItem.Action?.Invoke();
			}
			Destroy(gameObject);
		}
	}

	/// <summary>
	/// Selects the specified button in the specified layer, changing the button's appearance
	/// and opening/closing submenus as needed.
	/// </summary>
	/// <param name="layer">The layer which the button resides in.</param>
	/// <param name="button">The button which should be selected.</param>
	private void SelectButton(ButtonLayer layer, RadialButton button)
	{
		// Prevent unneccesary reselection
		if (layer.SelectedButton != button)
		{
			// Make sure no other button in this layer is selected
			DeselectButton(layer);
			// Change appearance
			button.title.text = button.Hiddentitle;
			button.DefaultColour = button.ReceiveCurrentColour();
			button.DefaultPosition = button.transform.GetSiblingIndex();
			button.SetColour(button.DefaultColour + (Color.white / 3f));
			button.transform.SetAsLastSibling();

			// Spawn submenus if applicable, one layer deeper with this buttons angle
			if (button.HasSubMenus())
			{
				SpawnButtons(button.MenuItem.SubMenus, button.MenuDepth + 1, button.GlobalAngle);
			}

			layer.SelectedButton = button;
		}
		// Deselect all buttons in layers above this one
		for (int depth = MaximumDepth; depth > button.MenuDepth; --depth)
		{
			DeselectButton(buttonLayers[depth]);
		}
	}

	/// <summary>
	/// Deselects the selected button in the specified layer, if any, changing it's appearance
	/// and closes it's submenus, if applicable.
	/// </summary>
	/// <param name="buttonLayer">The layer whose selected button should be deselected.</param>
	private void DeselectButton(ButtonLayer buttonLayer)
	{
		RadialButton button = buttonLayer.SelectedButton;
		if (button != null)
		{
			// Reset appearance
			button.title.text = "";
			button.transform.SetSiblingIndex(button.DefaultPosition);
			button.SetColour(button.DefaultColour);

			// Delete all layers above this one
			if (button.HasSubMenus())
			{
				DeleteLayer(button.MenuDepth + 1);
			}

			buttonLayer.SelectedButton = null;
		}
	}

	/// <summary>
	/// Deletes all layers with the specified depth and above
	/// </summary>
	/// <param name="layer">The index of the bottom layer to delete</param>
	private void DeleteLayer(int layer)
	{
		while (MaximumDepth >= layer)
		{
			buttonLayers[layer].Buttons.ForEach(b => Destroy(b.gameObject));
			buttonLayers.RemoveAt(layer);
		}
	}

	/// <summary>
	/// Adjust an angle in the range -180°, 180° to the same angle in the same range where
	/// the angle is mapped so that when it equals direction, the result is 0°.
	/// </summary>
	/// <param name="angle">The angle to adjust.</param>
	/// <param name="direction">The angle which will be mapped to 0.</param>
	/// <returns>The mapped angle.</returns>
	private float AdjustAngle(float angle, float direction)
	{
		angle -= direction;
		if (angle > Mathf.PI)
		{
			angle -= Mathf.PI * 2;
		}
		else if (angle < -Mathf.PI)
		{
			angle += Mathf.PI * 2;
		}
		return angle;
	}
}