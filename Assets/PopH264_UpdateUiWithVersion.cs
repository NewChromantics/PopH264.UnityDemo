using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PopH264_UpdateUiWithVersion : MonoBehaviour
{
	public UIDocument	Document;
	public string		UiLabelName = "VersionLabel";

	void SetLabel(string Text)
	{
		var Label = Document.rootVisualElement.Q<Label>(UiLabelName);
		Label.text = Text;
	}

	void Start()
	{
		//	get target label
		
		
		try
		{
			var Version = PopH264.GetVersion();
			var Label = $"PopH264 version {Version}";
			SetLabel(Label);
		}
		catch(Exception e)
		{
			SetLabel($"Error getting version: {e.Message}");
		}

	}
}
