#pragma once
#include "DirectShowHelper.h"
#pragma warning(push, 2)
#pragma warning(disable : 4995)

#include <vector>
#pragma warning(pop)
using namespace std;

class CVMR9AllocatorPresenter
	: public CCritSec
	, public IVMRSurfaceAllocator9
	, public IVMRImagePresenter9
{

public:
	CVMR9AllocatorPresenter(IDirect3DDevice9* direct3dDevice,IVMR9Callback* callback,HMONITOR monitor);
    virtual ~CVMR9AllocatorPresenter();

    // IVMRSurfaceAllocator9
    virtual HRESULT STDMETHODCALLTYPE  InitializeDevice(DWORD_PTR dwUserID, VMR9AllocationInfo* lpAllocInfo, DWORD* lpNumBuffers);
    virtual HRESULT STDMETHODCALLTYPE  TerminateDevice(DWORD_PTR dwID);
    virtual HRESULT STDMETHODCALLTYPE  GetSurface(DWORD_PTR dwUserID, DWORD SurfaceIndex, DWORD SurfaceFlags, IDirect3DSurface9** lplpSurface);
    virtual HRESULT STDMETHODCALLTYPE  AdviseNotify(IVMRSurfaceAllocatorNotify9* lpIVMRSurfAllocNotify);

    // IVMRImagePresenter9
    virtual HRESULT STDMETHODCALLTYPE  StartPresenting(DWORD_PTR dwUserID);
    virtual HRESULT STDMETHODCALLTYPE  StopPresenting(DWORD_PTR dwUserID);
    virtual HRESULT STDMETHODCALLTYPE  PresentImage(DWORD_PTR dwUserID, VMR9PresentationInfo* lpPresInfo);

    // IUnknown
    virtual HRESULT STDMETHODCALLTYPE QueryInterface( 
        REFIID riid,
        void** ppvObject);

    virtual ULONG STDMETHODCALLTYPE AddRef();
    virtual ULONG STDMETHODCALLTYPE Release();

	void DrawTexture(FLOAT fx, FLOAT fy, FLOAT nw, FLOAT nh, FLOAT uoff, FLOAT voff, FLOAT umax, FLOAT vmax, long color);
	CSize GetVideoSize(bool fCorrectAR=true);


protected:
	void Paint(IDirect3DSurface9* pSurface,SIZE aspecRatio);
	void DeleteSurfaces();
	HRESULT AllocSurfaces();

	void Log(const char *fmt, ...) ;
	CComPtr<IVMRSurfaceAllocatorNotify9> m_pIVMRSurfAllocNotify;

	CComPtr<IDirect3DTexture9> m_pVideoTexture[2];
	CComPtr<IDirect3DSurface9> m_pVideoSurface[2];
    CComPtr<IDirect3DDevice9> m_pD3DDev;
	CComPtr<IDirect3D9> m_pD3D;
	CComPtr<IDirect3DSurface9> m_pVideoSurfaceOff;
	CComPtr<IDirect3DSurface9> m_pVideoSurfaceYUY2;
	vector<CComPtr<IDirect3DSurface9> >     m_pSurfaces;

	long		  m_refCount;
	int			  m_surfaceCount;
	HMONITOR	  m_hMonitor;
	IVMR9Callback* m_pCallback;
	CSize m_NativeVideoSize;
	CSize m_AspectRatio ;
	CRect m_WindowRect;
	CRect m_VideoRect;
	bool m_fVMRSyncFix;
	double m_fps ;
	long   previousEndFrame;
	D3DTEXTUREFILTERTYPE m_Filter;
	bool m_bfirstFrame;
};
