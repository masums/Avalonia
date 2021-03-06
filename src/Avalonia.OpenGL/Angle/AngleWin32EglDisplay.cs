using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static Avalonia.OpenGL.EglConsts;

namespace Avalonia.OpenGL.Angle
{
    public class AngleWin32EglDisplay : EglDisplay
    {
        struct AngleInfo
        {
            public IntPtr Display { get; set; }
            public AngleOptions.PlatformApi PlatformApi { get; set; }
        }
        
        static AngleInfo CreateAngleDisplay(EglInterface _egl)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();
            var display = IntPtr.Zero;
            AngleOptions.PlatformApi angleApi = default;
            {
                if (_egl.GetPlatformDisplayEXT == null)
                    throw new OpenGlException("eglGetPlatformDisplayEXT is not supported by libegl.dll");

                var allowedApis = AvaloniaLocator.Current.GetService<AngleOptions>()?.AllowedPlatformApis
                                  ?? new [] { AngleOptions.PlatformApi.DirectX11, AngleOptions.PlatformApi.DirectX9 };

                foreach (var platformApi in allowedApis)
                {
                    int dapi;
                    if (platformApi == AngleOptions.PlatformApi.DirectX9)
                        dapi = EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE;
                    else if (platformApi == AngleOptions.PlatformApi.DirectX11)
                        dapi = EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE;
                    else
                        continue;

                    display = _egl.GetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                        new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, dapi, EGL_NONE });
                    if (display != IntPtr.Zero)
                    {
                        angleApi = platformApi;
                        break;
                    }
                }

                if (display == IntPtr.Zero)
                    throw new OpenGlException("Unable to create ANGLE display");
                return new AngleInfo { Display = display, PlatformApi = angleApi };
            }
        }

        private AngleWin32EglDisplay(EglInterface egl, AngleInfo info) : base(egl, info.Display)
        {
            PlatformApi = info.PlatformApi;
        }

        public AngleWin32EglDisplay(EglInterface egl) : this(egl, CreateAngleDisplay(egl))
        {
            
        }

        public AngleWin32EglDisplay() : this(new AngleEglInterface())
        {

        }

        public AngleOptions.PlatformApi PlatformApi { get; }

        public IntPtr GetDirect3DDevice()
        {
            if (!EglInterface.QueryDisplayAttribExt(Handle, EglConsts.EGL_DEVICE_EXT, out var eglDevice))
                throw new OpenGlException("Unable to get EGL_DEVICE_EXT");
            if (!EglInterface.QueryDeviceAttribExt(eglDevice, PlatformApi == AngleOptions.PlatformApi.DirectX9 ? EGL_D3D9_DEVICE_ANGLE : EGL_D3D11_DEVICE_ANGLE, out var d3dDeviceHandle))
                throw new OpenGlException("Unable to get EGL_D3D9_DEVICE_ANGLE");
            return d3dDeviceHandle;
        }

        public EglSurface WrapDirect3D11Texture(IntPtr handle)
        {
            if (PlatformApi != AngleOptions.PlatformApi.DirectX11)
                throw new InvalidOperationException("Current platform API is " + PlatformApi);
            return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, new[] { EGL_NONE, EGL_NONE });
        }
    }
}
