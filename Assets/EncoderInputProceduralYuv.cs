using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EncoderInputProceduralYuv : MonoBehaviour
{
	public EncodeDemo	PushToEncoder;

	
	[Header("Yuv_8_88(NV12) or Yuv_8_8_8(I420)")]
	public bool			BiPlanarChroma = true;
	public PopH264.PixelFormat	YuvFormat => BiPlanarChroma ?  PopH264.PixelFormat.Yuv_8_8_8 :  PopH264.PixelFormat.Yuv_8_88;
	[Range(0,255)]
	public int			Luma = 128;
	[Range(0,255)]
	public int			ColourJump = 60;
	[Range(1,1024)]
	public int			Width = 192;
	[Range(1,1024)]
	public int			Height = 192;

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
	int					FrameNumber => UpdateCounter / FrameFrequency;
	
	int					FramePerUpdateFrequency => Math.Max(1,FrameFrequency);
	int					KeyFramePerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * KeyframeFrequency);
	int					EofPerUpdateFrequency => Math.Max(1,FramePerUpdateFrequency * EofFrequency);

	byte[]				Pixels;

	struct YuvLayout
	{
		public int		Width;
		public int		Height;
		public int		ChromaWidth => Width / 2;
		public int		ChromaHeight => Height / 2;
		public int		LumaSize => Width * Height;
		public int		ChromaPlaneSize => ChromaWidth * ChromaHeight;
		public int		ChromaUvSize => ChromaPlaneSize * 2;
		public int		TotalSize => LumaSize + ChromaUvSize;
		public int		LumaStart => 0;
		public int		ChromaUStart;
		public int		ChromaVStart;
		public int		ChromaStep;	//	1 or 2 depending on if its striped or interlaced
	}

	YuvLayout			GetYuvLayout()
	{
		YuvLayout Layout;
		Layout.Width = Width;
		Layout.Height = Height;
		//	need to intialise these to use funcs
		Layout.ChromaUStart = 0;
		Layout.ChromaVStart = 0;
		Layout.ChromaStep = 0;
		
		if ( YuvFormat == PopH264.PixelFormat.Yuv_8_8_8 )
		{
			Layout.ChromaUStart = Layout.LumaSize;
			Layout.ChromaVStart = Layout.ChromaPlaneSize;
			Layout.ChromaStep = 1;
		}
		else if ( YuvFormat == PopH264.PixelFormat.Yuv_8_88 )
		{
			Layout.ChromaUStart = Layout.LumaSize;
			Layout.ChromaVStart = Layout.ChromaUStart+1;
			Layout.ChromaStep = 2;
		}
		else
		{
			throw new Exception($"Uhandled pixel format {this.YuvFormat}");
		}
		return Layout;
	}

	byte[]				GetPixels()
	{
		var Layout = GetYuvLayout();
		var Size = Layout.TotalSize;
		if ( Pixels != null && Pixels.Length != Size )
			Pixels = null;
			
		if ( Pixels != null )
			return Pixels;
			
		Pixels = new byte[Size];
		WriteLuma( (byte)Luma );
		WriteChromaUv( 0, 0 );
		return Pixels;
	}
	
	void				WriteLuma(byte Luma)
	{
		var Pixels = GetPixels();
		var Layout = GetYuvLayout();
		for ( int i=0;	i<Layout.LumaSize;	i++ )
		{
			var LumaIndex = Layout.LumaStart + i;
			Pixels[LumaIndex] = Luma;
		}	
	}
	
	void				WriteLumaLine(int y,byte Luma)
	{
		var Pixels = GetPixels();
		var Layout = GetYuvLayout();
		for ( int i=0;	i<Layout.Width;	i++ )
		{
			var LumaIndex = Layout.LumaStart + (y*Layout.Width) + i;
			Pixels[LumaIndex] = Luma;
		}	
	}

	void				WriteChromaUv(byte Chromau,byte Chromav)
	{
		var Pixels = GetPixels();
		var Layout = GetYuvLayout();
		
		for ( int i=0;	i<Layout.ChromaPlaneSize;	i++ )
		{
			var ChromaUIndex = Layout.ChromaUStart + (i * Layout.ChromaStep);
			Pixels[ChromaUIndex] = Chromau;

			var ChromaVIndex = Layout.ChromaVStart + (i * Layout.ChromaStep);
			Pixels[ChromaVIndex] = Chromav;
		}	
		
	}

	void				WriteChromaUvLine(int y,byte Chromau,byte Chromav)
	{
		var Pixels = GetPixels();
		var Layout = GetYuvLayout();
		
		if ( y >= Layout.ChromaHeight || y < 0 )
			throw  new Exception($"y={y} out of range for chroma w/h ({Layout.ChromaWidth}/{Layout.ChromaHeight}");
		
		for ( int i=0;	i<Layout.ChromaWidth;	i++ )
		{
			var ChromaULineStart = Layout.ChromaUStart + (y*Layout.ChromaWidth* Layout.ChromaStep); 
			var ChromaUIndex = ChromaULineStart + (i * Layout.ChromaStep);
			Pixels[ChromaUIndex] = Chromau;

			var ChromVLineStart = Layout.ChromaVStart + (y*Layout.ChromaWidth* Layout.ChromaStep); 
			var ChromaVIndex = ChromVLineStart + (i * Layout.ChromaStep);
			Pixels[ChromaVIndex] = Chromav;
		}	
		
	}

	void UpdateImage()
	{
		var Layout = GetYuvLayout();
		
		//	new colour every iteration
		int ColourIndex = (FrameNumber / Layout.Height);
		//	but make the change distinctive
		var ColourByte = (byte)(ColourIndex * ColourJump);
		
		var y = FrameNumber % Layout.Height;

		//WriteLuma( ColourLuma );
		WriteLumaLine( y, ColourByte );
	}
	
	
	void PushFrame()
	{
		if ( PushToEncoder == null )
			return;

		var Pixels = GetPixels();

		if ( UpdateCounter % EofPerUpdateFrequency == EofPerUpdateFrequency-1)
		{
			PushToEncoder.PushEndOfStream();
		}
		else if ( UpdateCounter % KeyFramePerUpdateFrequency == KeyFramePerUpdateFrequency-1)
		{
			PushToEncoder.PushFrame(Pixels,Width,Height,YuvFormat,UpdateCounter,true);
		}
		else if ( UpdateCounter % FrameFrequency == 0 )
		{
			PushToEncoder.PushFrame(Pixels,Width,Height,YuvFormat,UpdateCounter,false);
		}
		UpdateCounter++;
	}
	
	void Update()
	{
		UpdateImage();
		PushFrame();
	}
}