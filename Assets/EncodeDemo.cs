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

	int					UpdateCounter = 0;
	
	[Header("Encode a frame every N updates")]
	[Range(1,60)]
	public int			FrameFrequency = 60;
	
	[Header("Make a keyframe every N frames")]
	[Range(0,100)]
	public int			KeyframeFrequency = 10;

	[Header("Send EOF every N frames (will stop encoding)")]
	[Range(0,1000)]
	public int			EofFrequency = 1000;

	int					FramePerUpdateFrequency => Math.Max(1,FrameFrequency);
	int					KeyFramePerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * KeyframeFrequency);
	int					EofPerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * EofFrequency);

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
		SetPushLabel($"Pushed test data frame {UpdateCounter}");
		SetImage(Image);
	}
	
	void PushFrame()
	{
		if ( Encoder == null )
			return;

		if ( UpdateCounter % EofPerUpdateFrequency == EofPerUpdateFrequency-1)
		{
			Encoder.PushEndOfStream();
			SetPushLabel($"Pushed EOF on frame {UpdateCounter}");
		}
		else if ( UpdateCounter % KeyFramePerUpdateFrequency == KeyFramePerUpdateFrequency-1)
		{
			PushFrame(InputTexture,true);
			SetPushLabel($"Pushed Keyframe on frame {UpdateCounter}");
		}
		else if ( UpdateCounter % FrameFrequency == 0 )
		{
			PushFrame(InputTexture);
		}
		UpdateCounter++;
	}
	
	List<Texture2D> DecodedPlanes;
	List<PopH264.PixelFormat> DecodedPixelFormats;
	
	
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
