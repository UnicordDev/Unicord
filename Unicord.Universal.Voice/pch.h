#pragma once

#ifndef _FILE_DEFINED
struct _iobuf {
    char* _ptr;
    int   _cnt;
    char* _base;
    int   _flag;
    int   _file;
    int   _charbuf;
    int   _bufsiz;
    char* _tmpfname;
};
typedef struct _iobuf FILE;
#define _FILE_DEFINED
#endif


#include <unknwn.h>
#include <ppltasks.h>
#include <pplawait.h>
#include <gsl/gsl>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Diagnostics.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.System.Threading.h>

#define CLEANUP_SAFE(x, methodName) if (x != nullptr) { x.methodName(); x = nullptr; }
#define SAFE_CLOSE(x) CLEANUP_SAFE(x, Close)
#define SAFE_CANCEL(x) CLEANUP_SAFE(x, Cancel)

#include <windows.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <iostream>
#include <fstream>

