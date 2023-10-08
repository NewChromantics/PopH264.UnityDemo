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

	public Texture2D	InputTexture;
	PopH264.Encoder		Encoder;
	public PopH264.EncoderParams	EncoderParams;
	
	public DecodeDemo	PushToDecoder;

	int FrameCounter = 0;
	int FrameFrequency = 100;
	int KeyFrameFrequency => FrameFrequency * 10;

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
	
	void SetImage(Texture texture)
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
			PushFrame();
		}
		catch(Exception e)
		{
			SetPushLabel($"{e.Message}");
		}

		try
		{
			PopFrame();
		}
		catch(Exception e)
		{
			SetPopLabel($"{e.Message}");
		}
	}


	void PushFrame(Texture2D Image,bool Keyframe=false)
	{
		var Rgba = Image.GetRawTextureData();
		
		Encoder.PushFrame( Rgba, Image.width, Image.height, Image.format, Keyframe);
		SetPushLabel($"Pushed test data frame {FrameCounter}");
		SetImage(Image);
	}
	
	void PushFrame()
	{
		if ( Encoder == null )
			return;

		//	gr: for testing, we should EOF once in a while
		bool SendEof = false;
		if ( SendEof )
		{
			Encoder.PushEndOfStream();
			SetPushLabel($"Pushed EOF on frame {FrameCounter}");
		}
		//	send keyframe every so often
		else if ( FrameCounter % KeyFrameFrequency == KeyFrameFrequency-1)
		{
			PushFrame(InputTexture,true);
		}
		else if ( FrameCounter % FrameFrequency == 0 )
		{
			PushFrame(InputTexture);
		}
		FrameCounter++;
	}
	
	List<Texture2D> DecodedPlanes;
	List<PopH264.PixelFormat> DecodedPixelFormats;
	
	
	void PopFrame()
	{
		if ( Encoder == null )
			return;

		//	look for a new frame
		var FrameMaybe = Encoder.PopFrame();
		if ( FrameMaybe.HasValue )
		{
			var Frame = FrameMaybe.Value;
			SetPopLabel($"Got New frame {Frame.H264Data} bytes, encoder={Frame.EncoderMeta.EncoderName} encodeduration={Frame.EncoderMeta.EncodeDurationMs}ms");
			
			if ( PushToDecoder != null )
			{
				PushToDecoder.PushH264( Frame.H264Data );
			}
		}
	}
}
