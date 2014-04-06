// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "ClassFactory.h"
#include <Shlwapi.h>

// {40376849-AF26-439e-AA72-E3B5E7298301}
static const CLSID CLSID_KensSharp = 
{ 0x40376849, 0xaf26, 0x439e, { 0xaa, 0x72, 0xe3, 0xb5, 0xe7, 0x29, 0x83, 0x1 } };

long        g_cDllRef   = 0;
wchar_t exepath[MAX_PATH];

BOOL APIENTRY DllMain( HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
	)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		{
			wchar_t buf[MAX_PATH];
			GetModuleFileName(hModule, buf, MAX_PATH);
			PathRemoveFileSpec(buf);
			PathCombine(exepath, buf, L"KensSharp.exe");
			DisableThreadLibraryCalls(hModule);
		}
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void **ppv)
{
	HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;

	if (IsEqualCLSID(CLSID_KensSharp, rclsid))
	{
		hr = E_OUTOFMEMORY;

		CClassFactory *pClassFactory = new CClassFactory();
		if (pClassFactory)
		{
			hr = pClassFactory->QueryInterface(riid, ppv);
			pClassFactory->Release();
		}
	}

	return hr;
}

STDAPI DllCanUnloadNow()
{
	return g_cDllRef > 0 ? S_FALSE : S_OK;
}