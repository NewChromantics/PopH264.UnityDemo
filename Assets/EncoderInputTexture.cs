using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EncoderInputTexture : MonoBehaviour
{
	public EncodeDemo	PushToEncoder;

	public Texture2D	InputTexture;

	[Header("Encode a frame every N updates")]
	[Range(1,60)]
	public int			FrameFrequency = 60;
	
	[Header("Make a keyframe every N frames")]
	[Range(0,100)]
	public int			KeyframeFrequency = 10;

	[Header("Send EOF every N frames (will stop encoding)")]
	[Range(0,1000)]
	public int			EofFrequency = 1000;

	int					UpdateCounter = 0;
	
	int					FramePerUpdateFrequency => Math.Max(1,FrameFrequency);
	int					KeyFramePerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * KeyframeFrequency);
	int					EofPerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * EofFrequency);

	
	void PushFrame()
	{
		if ( PushToEncoder == null )
			return;

		if ( UpdateCounter % EofPerUpdateFrequency == EofPerUpdateFrequency-1)
		{
			PushToEncoder.PushEndOfStream();
		}
		else if ( UpdateCounter % KeyFramePerUpdateFrequency == KeyFramePerUpdateFrequency-1)
		{
			PushToEncoder.PushFrame(InputTexture,UpdateCounter,true);
		}
		else if ( UpdateCounter % FrameFrequency == 0 )
		{
			PushToEncoder.PushFrame(InputTexture,UpdateCounter,false);
		}
		UpdateCounter++;
	}
	
	void Update()
	{	
		PushFrame();
	}
}