using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;


public class DecodeDemo : MonoBehaviour
{	
	public UIDocument	Document;
	public string		DecodePushLabelName = "DecodePushLabel";
	public string		DecodePopLabelName = "DecodePopLabel";
	public string		DecodeImageName = "DecodeImage";

	PopH264.Decoder		Decoder;
	public PopH264.DecoderParams	DecoderParams;
	public string		TestDataName = "RainbowGradient.h264";
	int FrameCounter = 0;
	int FrameFrequency = 100;
	int KeyFrameFrequency => FrameFrequency * 100000;
	
	List<byte[]>		DecodeH264Queue;
	

	void SetPushLabel(string Text)
	{
		var Label = Document.rootVisualElement.Q<Label>(DecodePushLabelName);
		Label.text = Text;
	}
	void SetPopLabel(string Text)
	{
		var Label = Document.rootVisualElement.Q<Label>(DecodePopLabelName);
		Label.text = Text;
	}
	
	void SetImage(Texture texture)
	{
		var Element = Document.rootVisualElement.Q<VisualElement>(DecodeImageName);
		Element.style.backgroundImage = new StyleBackground(texture as Texture2D);
	}

	void Start()
	{
		try
		{
			Decoder = new PopH264.Decoder(DecoderParams,true);
			SetPushLabel("Allocated decoder...");
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

	
	void PushFrame()
	{
		//	send EOF every so often
		if ( FrameCounter % KeyFrameFrequency == 0 )
		{
			//Decoder.PushEndOfStream();
			//SetPushLabel($"Pushed EOF on frame {FrameCounter}");
		}
		else
		{
			//	push data asap from queue
			if ( !String.IsNullOrEmpty(TestDataName) )
			{
				if ( FrameCounter % FrameFrequency == 0 )
				{
					//	grab test data
					var H264Data = PopH264.GetH264TestData(TestDataName);
					var InputFrame = new PopH264.FrameInput();
					InputFrame.Bytes = H264Data;
					InputFrame.FrameNumber = FrameCounter;
					Decoder.PushFrameData(InputFrame);
					SetPushLabel($"Pushed test data frame {FrameCounter} x{H264Data.Length}"); 
				}
			}
			else if ( DecodeH264Queue != null && DecodeH264Queue.Count > 0 )
			{
				var PoppedPacket = DecodeH264Queue[0];
				DecodeH264Queue.RemoveAt(0);
				var InputFrame = new PopH264.FrameInput();
				InputFrame.Bytes = PoppedPacket;
				InputFrame.FrameNumber = FrameCounter;
				Decoder.PushFrameData(InputFrame);
				SetPushLabel($"Pushed input data frame {FrameCounter} x{PoppedPacket.Length}"); 
			}
		}
		FrameCounter++;
	}
	
	List<Texture2D> DecodedPlanes;
	List<PopH264.PixelFormat> DecodedPixelFormats;
	
	
	void PopFrame()
	{
		//	look for a new frame
		var MetaMaybe = Decoder.GetNextFrameAndMeta( ref DecodedPlanes, ref DecodedPixelFormats );
		if ( MetaMaybe.HasValue )
		{
			var Meta = MetaMaybe.Value;
			var PlaneMeta = $"{DecodedPlanes[0].width} x {DecodedPlanes[0].height}";
			SetPopLabel($"Got New frame {Meta.FrameNumber} {PlaneMeta}");
			SetImage( DecodedPlanes[0] );
		}
		
		if ( Decoder.HadEndOfStream )
		{
			SetPopLabel($"Decoder popped EOF");
			Decoder = new PopH264.Decoder(DecoderParams,true);
		}
	}
		
	public void PushH264(byte[] H264Data)
	{
		if ( DecodeH264Queue == null )
			DecodeH264Queue = new List<byte[]>();
		DecodeH264Queue.Add(H264Data);
	}
}
