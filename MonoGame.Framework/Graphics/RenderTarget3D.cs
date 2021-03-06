// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
#if PSM
using Sce.PlayStation.Core.Graphics;
#elif DIRECTX
using SharpDX.Direct3D11;
#elif OPENGL
#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#elif GLES
using OpenTK.Graphics.ES20;
using RenderbufferTarget = OpenTK.Graphics.ES20.All;
using RenderbufferStorage = OpenTK.Graphics.ES20.All;
#endif
#endif

namespace Microsoft.Xna.Framework.Graphics
{
	public class RenderTarget3D : Texture3D, IRenderTarget
	{
#if DIRECTX
	    private int _currentSlice;
        private RenderTargetView _renderTargetView;
        private DepthStencilView _depthStencilView;
#endif

		public DepthFormat DepthStencilFormat { get; private set; }
		
		public int MultiSampleCount { get; private set; }
		
		public RenderTargetUsage RenderTargetUsage { get; private set; }
		
		public bool IsContentLost { get { return false; } }
		
		public event EventHandler<EventArgs> ContentLost;

		public RenderTarget3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
			:base (graphicsDevice, width, height, depth, mipMap, preferredFormat, true)
		{
			DepthStencilFormat = preferredDepthFormat;
			MultiSampleCount = preferredMultiSampleCount;
			RenderTargetUsage = usage;

            // If we don't need a depth buffer then we're done.
            if (preferredDepthFormat == DepthFormat.None)
                return;

#if DIRECTX

            // Setup the multisampling description.
            var multisampleDesc = new SharpDX.DXGI.SampleDescription(1, 0);
            if ( preferredMultiSampleCount > 1 )
            {
                multisampleDesc.Count = preferredMultiSampleCount;
                multisampleDesc.Quality = (int)StandardMultisampleQualityLevels.StandardMultisamplePattern;
            }

            // Create a descriptor for the depth/stencil buffer.
            // Allocate a 2-D surface as the depth/stencil buffer.
            // Create a DepthStencil view on this surface to use on bind.
            using (var depthBuffer = new SharpDX.Direct3D11.Texture2D(graphicsDevice._d3dDevice, new Texture2DDescription
            {
                Format = SharpDXHelper.ToFormat(preferredDepthFormat),
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = multisampleDesc,
                BindFlags = BindFlags.DepthStencil,
            }))
            {
                // Create the view for binding to the device.
                _depthStencilView = new DepthStencilView(graphicsDevice._d3dDevice, depthBuffer, new DepthStencilViewDescription()
                { 
                    Format = SharpDXHelper.ToFormat(preferredDepthFormat),
                    Dimension = DepthStencilViewDimension.Texture2D
                });
            }

#endif // DIRECTX
        }
		
		public RenderTarget3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
			:this (graphicsDevice, width, height, depth, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents) 
		{}
		
		public RenderTarget3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
			: this(graphicsDevice, width, height, depth, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents) 
		{}

		protected override void Dispose(bool disposing)
		{
            if (!IsDisposed)
            {
#if DIRECTX
                if (disposing)
                {
                    if (_renderTargetView != null)
                    {
                        _renderTargetView.Dispose();
                        _renderTargetView = null;
                    }
                    if (_depthStencilView != null)
                    {
                        _depthStencilView.Dispose();
                        _depthStencilView = null;
                    }
                }
#endif
            }
            base.Dispose(disposing);
		}

#if DIRECTX

	    RenderTargetView IRenderTarget.GetRenderTargetView(int arraySlice)
	    {
            if (arraySlice >= Depth)
                throw new ArgumentOutOfRangeException("The arraySlice is out of range for this Texture3D.");

            // Dispose the previous target.
	        if (_currentSlice != arraySlice && _renderTargetView != null)
	        {
	            _renderTargetView.Dispose();
	            _renderTargetView = null;
	        }

            // Create the new target view interface.
	        if (_renderTargetView == null)
	        {
	            _currentSlice = arraySlice;

	            var desc = new RenderTargetViewDescription
	            {
	                Format = SharpDXHelper.ToFormat(format),
	                Dimension = RenderTargetViewDimension.Texture3D,
	                Texture3D =
	                    {
	                        DepthSliceCount = -1,
	                        FirstDepthSlice = arraySlice,
	                        MipSlice = 0,
	                    }
	            };

	            _renderTargetView = new RenderTargetView(GraphicsDevice._d3dDevice, _texture, desc);
	        }

	        return _renderTargetView;
	    }

	    DepthStencilView IRenderTarget.GetDepthStencilView()
	    {
	        return _depthStencilView;
	    }

#endif
	}
}
