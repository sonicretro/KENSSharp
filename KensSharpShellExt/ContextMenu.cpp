#include "StdAfx.h"
#include <string>
#include <Shlwapi.h>
#include <Shellapi.h>
#include "ContextMenu.h"

extern HINSTANCE g_hInst;
extern long g_cDllRef;
extern wchar_t exepath[MAX_PATH];

wchar_t *itemargs[] = {
	L"-dk",
	L"-ck",
	L"-de",
	L"-ce",
	L"-de -l",
	L"-ce -l",
	L"-dn",
	L"-cn",
	L"-ds",
	L"-cs",
	L"-ds -n",
	L"-cs -n",
	L"-dkm",
	L"-ckm",
	L"-dkm -l",
	L"-ckm -l"
};

struct iteminfo { int id; wchar_t *text; iteminfo *subitems; };

int curid = 0;

#define defaultmenu(name) iteminfo name##menu[] = { \
{ curid++, L"&Decompress" }, \
{ curid++, L"&Compress" }, \
{ -1 } \
}

#define endianmenu(name) iteminfo name##menu[] = { \
{ curid++, L"&Decompress" }, \
{ curid++, L"&Compress" }, \
{ curid++, L"Decompress (Little Endian)" }, \
{ curid++, L"Compress (Little Endian)" }, \
{ -1 } \
}

defaultmenu(kos);
endianmenu(eni);
defaultmenu(nem);
iteminfo saxmenu[] = {
	{ curid++, L"&Decompress" },
	{ curid++, L"&Compress" },
	{ curid++, L"Decompress (No Size)" },
	{ curid++, L"Compress (No Size)" },
	{ -1 }
};
endianmenu(kosm);

int maxid = curid;

iteminfo rootmenu[] = {
	{ curid++, L"&Kosinski", kosmenu },
	{ curid++, L"&Enigma", enimenu },
	{ curid++, L"&Nemesis", nemmenu },
	{ curid++, L"&Saxman", saxmenu },
	{ curid++, L"&Moduled Kosinski", kosmmenu },
	{ -1 }
};

CContextMenu::CContextMenu(void) : m_cRef(1)
{
	InterlockedIncrement(&g_cDllRef);
}

CContextMenu::~CContextMenu(void)
{
	InterlockedDecrement(&g_cDllRef);
}

// IUnknown

IFACEMETHODIMP CContextMenu::QueryInterface(REFIID riid, void **ppv)
{
	static const QITAB qit[] = 
	{
		QITABENT(CContextMenu, IContextMenu),
		QITABENT(CContextMenu, IShellExtInit), 
		{ 0 },
	};
	return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) CContextMenu::AddRef()
{
	return InterlockedIncrement(&m_cRef);
}

IFACEMETHODIMP_(ULONG) CContextMenu::Release()
{
	ULONG cRef = InterlockedDecrement(&m_cRef);
	if (cRef == 0)
		delete this;

	return cRef;
}


// IShellExtInit

IFACEMETHODIMP CContextMenu::Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT pDataObj, HKEY hKeyProgID)
{
	if (pDataObj == NULL)
		return E_INVALIDARG;

	HRESULT hr = E_FAIL;

	FORMATETC fe = { CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
	STGMEDIUM stm;

	// The pDataObj pointer contains the objects being acted upon. In this 
	// example, we get an HDROP handle for enumerating the selected files and 
	// folders.
	if (SUCCEEDED(pDataObj->GetData(&fe, &stm)))
	{
		// Get an HDROP handle.
		HDROP hDrop = static_cast<HDROP>(GlobalLock(stm.hGlobal));
		if (hDrop != NULL)
		{
			// Determine how many files are involved in this operation. This 
			// code sample displays the custom context menu item when only 
			// one file is selected. 
			UINT nFiles = DragQueryFile(hDrop, 0xFFFFFFFF, NULL, 0);
			wchar_t buf[MAX_PATH];
			for (unsigned int i = 0; i < nFiles; i++)
				// Get the path of the file.
				if (DragQueryFile(hDrop, i, buf, MAX_PATH) != 0)
					selectedFiles.push_back(buf);
				else
					return hr;
			hr = S_OK;

			GlobalUnlock(stm.hGlobal);
		}

		ReleaseStgMedium(&stm);
	}

	// If any value other than S_OK is returned from the method, the context 
	// menu item is not displayed.
	return hr;
}

// IContextMenu

HMENU ProcessSubMenu(iteminfo *info, UINT idCmdFirst)
{
	HMENU hSubmenu = CreatePopupMenu();

	int i = 0;
	while (info[i].id != -1)
	{
		MENUITEMINFO mii = { sizeof(MENUITEMINFO) };
		mii.fMask = MIIM_STRING | MIIM_ID;
		mii.wID = info[i].id + idCmdFirst;
		mii.dwTypeData = info[i].text;
		if (info[i].subitems != nullptr)
		{
			mii.fMask |= MIIM_SUBMENU;
			mii.hSubMenu = ProcessSubMenu(info[i].subitems, idCmdFirst);
		}
		InsertMenuItem(hSubmenu, i++, TRUE, &mii);
	}
	return hSubmenu;
}

IFACEMETHODIMP CContextMenu::QueryContextMenu(HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags)
{
	// If uFlags include CMF_DEFAULTONLY then we should not do anything.
	if (CMF_DEFAULTONLY & uFlags)
		return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));

	MENUITEMINFO mii = { sizeof(mii) };
	mii.fMask = MIIM_STRING | MIIM_ID | MIIM_SUBMENU;
	mii.wID = idCmdFirst + curid;
	mii.dwTypeData = L"KensSharp";
	mii.hSubMenu = ProcessSubMenu(rootmenu, idCmdFirst);
	if (!InsertMenuItem(hMenu, indexMenu, TRUE, &mii))
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Return an HRESULT value with the severity set to SEVERITY_SUCCESS. 
	// Set the code value to the offset of the largest command identifier 
	// that was assigned, plus one (1).
	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(curid + 1));
}

IFACEMETHODIMP CContextMenu::InvokeCommand(LPCMINVOKECOMMANDINFO pici)
{
	bool fUnicode = pici->cbSize == sizeof(CMINVOKECOMMANDINFOEX) && pici->fMask & CMIC_MASK_UNICODE;

	void *ptr;

	if (fUnicode)
		ptr = (void *)((CMINVOKECOMMANDINFOEX *)pici)->lpVerbW;
	else
		ptr = (void *)pici->lpVerb;

	if (HIWORD(ptr) != 0)
		return E_INVALIDARG;

	// Is the command identifier offset supported by this context menu 
	// extension?
	if (LOWORD(pici->lpVerb) < maxid)
	{
		PROCESS_INFORMATION pi;
		STARTUPINFO si = { sizeof(STARTUPINFO) };
		std::wstring args = L" ";
		args += itemargs[LOWORD(pici->lpVerb)]; // -ck, -dk, etc
		args += L" -s \""; // -ck -s "
		for (auto i = selectedFiles.begin(); i != selectedFiles.end(); i++)
		{
			std::wstring arg2 = args;
			arg2 += *i; // -ck -s "C:\Folder\File.unc
			arg2 += L"\""; // -ck -s "C:\Folder\File.unc"
			wchar_t *tmp = new wchar_t[arg2.length() + 1];
			wcscpy_s(tmp, arg2.length() + 1, arg2.c_str());
			CreateProcess(exepath, tmp, 0, 0, false, CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
			WaitForSingleObject(pi.hProcess, INFINITE);
		}
	}
	else
		return E_FAIL;
	return S_OK;
}

IFACEMETHODIMP CContextMenu::GetCommandString(UINT_PTR idCommand, UINT uFlags, UINT *pwReserved, LPSTR pszName, UINT cchMax)
{
	return E_INVALIDARG;
}