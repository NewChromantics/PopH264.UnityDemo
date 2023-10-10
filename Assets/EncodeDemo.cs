using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.UI;


public class EncodeDemo : MonoBehaviour
{	
	public UIDocument	Document;
	public string		PushLabelName = "EncodePushLabel";
	public string		PopLabelName = "EncodePopLabel";
	public string		ImageName = "EncodeImage";

	PopH264.Encoder		Encoder;
	public PopH264.EncoderParams	EncoderParams;
	
	public DecodeDemo	PushToDecoder;
	
	List<Texture2D> DecodedPlanes;
	List<PopH264.PixelFormat> DecodedPixelFormats;
	


	void SetPushLabel(string Text)
	{
		var Label = Document.rootVisualElement.Q<Label>(PushLabelName);
		Label.text = Text;
	}
	void SetPopLabel(string Text)
	{
		var Label = Document.rootVisualElement.Q<Label>(PopLabelName);
		Label.text = Text;
	}
	
	public void SetImage(Texture texture)
	{
		var Element = Document.rootVisualElement.Q<VisualElement>(ImageName);
		Element.style.backgroundImage = new StyleBackground(texture as Texture2D);
	}

	void Start()
	{
		try
		{
			Encoder = new PopH264.Encoder(EncoderParams);
			SetPushLabel("Allocated encoder...");
		}
		catch(Exception e)
		{
			SetPushLabel($"Error getting version: {e.Message}");
		}
	}

	void Update()
	{	
		try
		{
			PopFrame();
		}
		catch(Exception e)
		{
			SetPopLabel($"{e.Message}");
		}
	}


	public void PushEndOfStream()
	{
		try
		{
			SetPushLabel($"Pushed EOF");
		}
		catch(Exception e)
		{
			SetPushLabel($"PushEof {e.Message}");
		}
	}
	public void PushFrame(int FrameNumber)
	{
		try
		{
			SetPushLabel($"Pushed EOF");
		}
		catch(Exception e)
		{
			SetPushLabel($"PushEof {e.Message}");
		}
	}
	

	public void PushFrame(Texture2D Image,int FrameNumber,bool Keyframe=false)
	{
		try
		{
			var Rgba = Image.GetRawTextureData();
			
			Encoder.PushFrame( Rgba, Image.width, Image.height, Image.format, Keyframe);
			if ( Keyframe )
				SetPushLabel($"Pushed test data frame {FrameNumber} (Keyframe={Keyframe})");
			SetImage(Image);
		}
		catch(Exception e)
		{
			SetPushLabel($"Pushed frame error {e.Message}");
		}
		
	}

	
	void PopFrame()
	{
		if ( Encoder == null )
			return;

		//	look for a new frame
		var FrameMaybe = Encoder.PopFrame();
		if ( FrameMaybe is PopH264.H264Frame Frame )
		{
			if ( Frame.EndOfStream )
				SetPopLabel($"Got EndOfStream");
			else
				SetPopLabel($"Got New frame {Frame.H264Data?.Length} bytes, encoder={Frame.EncoderMeta.EncoderName} encodeduration={Frame.EncoderMeta.EncodeDurationMs}ms");
			
			if ( PushToDecoder != null )
			{
				if ( Frame.EndOfStream )
					PushToDecoder.PushEndOfStream();
				else
					PushToDecoder.PushH264( Frame.H264Data );
			}
		}
	}
}
