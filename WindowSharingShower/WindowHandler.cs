using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WindowSharingShower
{
    public static class WindowHandler
    {
        public const int WdaNone = 0;
        // QUERY_INFORMATION | CREATE_THREAD | VM_OPERATION | VM_READ | VM_WRITE.
        const uint ProcessAccess = 0x043A;
        const uint WaitObject0 = 0, WaitTimeout = 258, Infinite = 0xFFFFFFFF;

        delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll", SetLastError = true)] static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr window);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowDisplayAffinity")]
        static extern bool ReadAffinity(IntPtr window, out uint affinity);
        [DllImport("user32.dll", SetLastError = true)] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, uint processId);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool IsWow64Process(IntPtr process, out bool wow64);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint allocation, uint protection);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint freeType);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr process, IntPtr address, UIntPtr size, uint protection, out uint oldProtection);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr process, IntPtr address, [Out] byte[] data, UIntPtr size, out UIntPtr read);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] data, UIntPtr size, out UIntPtr written);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateRemoteThread(IntPtr process, IntPtr attributes, UIntPtr stackSize, IntPtr entry, IntPtr parameter, uint flags, IntPtr threadId);
        [DllImport("kernel32.dll", SetLastError = true)] static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool CloseHandle(IntPtr handle);
        [DllImport("psapi.dll", SetLastError = true)] static extern bool EnumProcessModulesEx(IntPtr process, [Out] IntPtr[] modules, uint bytes, out uint needed, uint filter);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern uint GetModuleFileNameEx(IntPtr process, IntPtr module, StringBuilder path, uint capacity);
        [DllImport("psapi.dll", SetLastError = true)] static extern bool GetModuleInformation(IntPtr process, IntPtr module, out ModuleInfo info, uint size);

        [StructLayout(LayoutKind.Sequential)]
        struct ModuleInfo { public IntPtr Base; public uint Size; public IntPtr Entry; }
        sealed class Module { public string Name; public IntPtr Base; public uint Size; }

        static Win32Exception Error(string operation)
        {
            var code = Marshal.GetLastWin32Error();
            return new Win32Exception(code, operation + " 失败（Win32 " + code + "）：" + new Win32Exception(code).Message);
        }

        public static Dictionary<IntPtr, string> GetVisibleWindows()
        {
            var windows = new Dictionary<IntPtr, string>();
            if (!EnumWindows((window, parameter) =>
            {
                if (!IsWindowVisible(window)) return true;
                int cloaked;
                if (DwmGetWindowAttribute(window, 14, out cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
                var length = GetWindowTextLength(window);
                if (length == 0) return true;
                var text = new StringBuilder(length + 1);
                GetWindowText(window, text, text.Capacity);
                windows[window] = text.ToString() == "Program Manager" ? "Desktop and Icons" : text.ToString();
                return true;
            }, IntPtr.Zero)) throw Error("EnumWindows");
            return windows;
        }

        public static bool TryGetWindowDisplayAffinity(IntPtr window, out uint affinity, out string error)
        {
            if (ReadAffinity(window, out affinity)) { error = null; return true; }
            error = Error("GetWindowDisplayAffinity").Message;
            return false;
        }

        public static int GetWindowDisplayAffinity(IntPtr window)
        {
            uint affinity;
            if (!ReadAffinity(window, out affinity)) throw Error("GetWindowDisplayAffinity");
            return unchecked((int)affinity);
        }

        public static bool IsHiddenFromCapture(IntPtr window) { return GetWindowDisplayAffinity(window) != WdaNone; }

        static IntPtr Add(IntPtr address, long offset) { return new IntPtr(checked(address.ToInt64() + offset)); }

        static byte[] Read(IntPtr process, IntPtr address, int count)
        {
            var data = new byte[count];
            UIntPtr read;
            if (!ReadProcessMemory(process, address, data, (UIntPtr)(uint)count, out read)) throw Error("ReadProcessMemory");
            if (read.ToUInt64() != (ulong)count) throw new InvalidOperationException("ReadProcessMemory 返回的数据不完整。");
            return data;
        }

        static void Write(IntPtr process, IntPtr address, byte[] data)
        {
            UIntPtr written;
            if (!WriteProcessMemory(process, address, data, (UIntPtr)(uint)data.Length, out written)) throw Error("WriteProcessMemory");
            if (written.ToUInt64() != (ulong)data.Length) throw new InvalidOperationException("WriteProcessMemory 写入的数据不完整。");
        }

        static List<Module> GetModules(IntPtr process)
        {
            var handles = new IntPtr[256];
            uint needed;
            while (true)
            {
                if (!EnumProcessModulesEx(process, handles, (uint)(handles.Length * IntPtr.Size), out needed, 3)) throw Error("EnumProcessModulesEx");
                if (needed <= handles.Length * IntPtr.Size) break;
                if (needed > 4096 * IntPtr.Size) throw new InvalidOperationException("目标进程模块数量异常。");
                handles = new IntPtr[needed / IntPtr.Size + 16];
            }
            var modules = new List<Module>();
            for (int i = 0; i < needed / IntPtr.Size; i++)
            {
                var path = new StringBuilder(1024);
                if (GetModuleFileNameEx(process, handles[i], path, (uint)path.Capacity) == 0) throw Error("GetModuleFileNameEx");
                ModuleInfo info;
                if (!GetModuleInformation(process, handles[i], out info, (uint)Marshal.SizeOf(typeof(ModuleInfo)))) throw Error("GetModuleInformation");
                modules.Add(new Module { Name = Path.GetFileName(path.ToString()), Base = info.Base, Size = info.Size });
            }
            return modules;
        }

        // Resolve the target's own PE exports, including forwarders. Local addresses cannot
        // be reused across different process architectures or module load addresses.
        static IntPtr ResolveExport(IntPtr process, List<Module> modules, string moduleName, string name, int depth = 0)
        {
            if (depth > 8) throw new InvalidOperationException("导出转发链过长。");
            if (!moduleName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) moduleName += ".dll";
            var module = modules.Find(m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));
            if (module == null) throw new InvalidOperationException("目标进程未加载 " + moduleName);
            var dos = Read(process, module.Base, 64);
            if (BitConverter.ToUInt16(dos, 0) != 0x5A4D) throw new InvalidOperationException("模块缺少有效的 DOS 头。");
            int ntOffset = BitConverter.ToInt32(dos, 60);
            if (ntOffset < 64 || ntOffset > module.Size - 144) throw new InvalidOperationException("模块 PE 头偏移无效。");
            var nt = Read(process, Add(module.Base, ntOffset), 144);
            if (BitConverter.ToUInt32(nt, 0) != 0x4550) throw new InvalidOperationException("模块缺少有效的 PE 头。");
            ushort magic = BitConverter.ToUInt16(nt, 24);
            if (magic != 0x10B && magic != 0x20B) throw new InvalidOperationException("不支持的 PE 位数。");
            int directory = 24 + (magic == 0x10B ? 96 : 112);
            uint rva = BitConverter.ToUInt32(nt, directory), size = BitConverter.ToUInt32(nt, directory + 4);
            if (rva == 0 || size < 40 || size > 16 * 1024 * 1024 || (ulong)rva + size > module.Size)
                throw new InvalidOperationException("模块导出目录无效。");
            var exports = Read(process, Add(module.Base, rva), (int)size);
            uint ordinalBase = BitConverter.ToUInt32(exports, 16);
            uint functionCount = BitConverter.ToUInt32(exports, 20), nameCount = BitConverter.ToUInt32(exports, 24);
            if (functionCount > 65536 || nameCount > 65536) throw new InvalidOperationException("模块导出表数量异常。");
            uint functionsRva = BitConverter.ToUInt32(exports, 28), namesRva = BitConverter.ToUInt32(exports, 32), ordinalsRva = BitConverter.ToUInt32(exports, 36);
            int index = -1;
            if (name.StartsWith("#", StringComparison.Ordinal))
            {
                uint ordinal;
                if (uint.TryParse(name.Substring(1), out ordinal) && ordinal >= ordinalBase)
                    index = checked((int)(ordinal - ordinalBase));
            }
            else
            {
                var names = Read(process, Add(module.Base, namesRva), checked((int)nameCount * 4));
                var ordinals = Read(process, Add(module.Base, ordinalsRva), checked((int)nameCount * 2));
                for (int i = 0; i < nameCount; i++)
                {
                    uint nameRva = BitConverter.ToUInt32(names, i * 4);
                    if (ExportString(exports, rva, nameRva) == name) { index = BitConverter.ToUInt16(ordinals, i * 2); break; }
                }
            }
            if (index < 0 || index >= functionCount) throw new InvalidOperationException("未找到目标导出 " + moduleName + "!" + name);
            uint functionRva = BitConverter.ToUInt32(Read(process, Add(module.Base, functionsRva + index * 4L), 4), 0);
            if (functionRva == 0 || functionRva >= module.Size) throw new InvalidOperationException("目标导出地址无效。");
            if (functionRva >= rva && (ulong)functionRva < (ulong)rva + size)
            {
                string forwarder = ExportString(exports, rva, functionRva);
                int dot = forwarder.LastIndexOf('.');
                if (dot <= 0) throw new InvalidOperationException("无效的导出转发：" + forwarder);
                return ResolveExport(process, modules, forwarder.Substring(0, dot), forwarder.Substring(dot + 1), depth + 1);
            }
            return Add(module.Base, functionRva);
        }

        static string ExportString(byte[] exports, uint start, uint rva)
        {
            if (rva < start || (ulong)rva >= (ulong)start + (uint)exports.Length)
                throw new InvalidOperationException("导出字符串超出目录范围。");
            int offset = checked((int)(rva - start)), end = offset;
            while (end < exports.Length && exports[end] != 0 && end - offset < 1024) end++;
            if (end == exports.Length || exports[end] != 0) throw new InvalidOperationException("导出字符串未终止。");
            return Encoding.ASCII.GetString(exports, offset, end - offset);
        }

        static byte[] BuildCall(bool target32, IntPtr window, IntPtr setAffinity, IntPtr getLastError, IntPtr result)
        {
            var code = new List<byte>();
            if (target32)
            {
                code.Add(0x68); code.AddRange(BitConverter.GetBytes(0)); // push WDA_NONE
                code.Add(0x68); code.AddRange(BitConverter.GetBytes(unchecked((uint)window.ToInt64())));
                code.Add(0xB8); code.AddRange(BitConverter.GetBytes(unchecked((uint)setAffinity.ToInt64())));
                code.AddRange(new byte[] { 0xFF, 0xD0 }); // call eax
                code.Add(0xA3); code.AddRange(BitConverter.GetBytes(unchecked((uint)result.ToInt64()))); // success
                code.Add(0xB8); code.AddRange(BitConverter.GetBytes(unchecked((uint)getLastError.ToInt64())));
                code.AddRange(new byte[] { 0xFF, 0xD0 });
                code.Add(0xA3); code.AddRange(BitConverter.GetBytes(unchecked((uint)result.ToInt64()) + 4)); // error
                code.AddRange(new byte[] { 0x31, 0xC0, 0xC2, 0x04, 0x00 }); // DWORD WINAPI thread(void*)
            }
            else
            {
                // Entry RSP is 8 modulo 16. Reserve 32 shadow bytes plus 8 for alignment.
                code.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 });
                code.AddRange(new byte[] { 0x48, 0xB9 }); code.AddRange(BitConverter.GetBytes(window.ToInt64()));
                code.Add(0xBA); code.AddRange(BitConverter.GetBytes(0)); // mov edx, WDA_NONE
                code.AddRange(new byte[] { 0x48, 0xB8 }); code.AddRange(BitConverter.GetBytes(setAffinity.ToInt64()));
                code.AddRange(new byte[] { 0xFF, 0xD0 });
                code.AddRange(new byte[] { 0x49, 0xBB }); code.AddRange(BitConverter.GetBytes(result.ToInt64()));
                code.AddRange(new byte[] { 0x41, 0x89, 0x03 }); // mov [r11], eax
                code.AddRange(new byte[] { 0x48, 0xB8 }); code.AddRange(BitConverter.GetBytes(getLastError.ToInt64()));
                code.AddRange(new byte[] { 0xFF, 0xD0 });
                code.AddRange(new byte[] { 0x49, 0xBB }); code.AddRange(BitConverter.GetBytes(result.ToInt64()));
                code.AddRange(new byte[] { 0x41, 0x89, 0x43, 0x04, 0x31, 0xC0, 0x48, 0x83, 0xC4, 0x28, 0xC3 });
            }
            return code.ToArray();
        }

        public static void ShowInCapture(IntPtr window)
        {
            if (!IsHiddenFromCapture(window)) return;
            uint processId;
            if (GetWindowThreadProcessId(window, out processId) == 0) throw Error("GetWindowThreadProcessId");
            IntPtr process = OpenProcess(ProcessAccess, false, processId);
            if (process == IntPtr.Zero) throw Error("OpenProcess PID=" + processId);
            IntPtr codeAddress = IntPtr.Zero, resultAddress = IntPtr.Zero, thread = IntPtr.Zero;
            bool running = false;
            try
            {
                bool wow64;
                if (!IsWow64Process(process, out wow64)) throw Error("IsWow64Process");
                bool target32 = !Environment.Is64BitOperatingSystem || wow64;
                if (Environment.Is64BitOperatingSystem && IntPtr.Size == 4 && !target32)
                    throw new NotSupportedException("请使用 x64 版本处理 64 位目标窗口。");
                var modules = GetModules(process);
                IntPtr setAffinity = ResolveExport(process, modules, "user32.dll", "SetWindowDisplayAffinity");
                IntPtr getLastError = ResolveExport(process, modules, "kernel32.dll", "GetLastError");
                resultAddress = VirtualAllocEx(process, IntPtr.Zero, (UIntPtr)8u, 0x3000, 0x04);
                if (resultAddress == IntPtr.Zero) throw Error("VirtualAllocEx result");
                Write(process, resultAddress, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0 });
                var code = BuildCall(target32, window, setAffinity, getLastError, resultAddress);
                codeAddress = VirtualAllocEx(process, IntPtr.Zero, (UIntPtr)(uint)code.Length, 0x3000, 0x04);
                if (codeAddress == IntPtr.Zero) throw Error("VirtualAllocEx code");
                Write(process, codeAddress, code);
                uint oldProtection;
                if (!VirtualProtectEx(process, codeAddress, (UIntPtr)(uint)code.Length, 0x20, out oldProtection)) throw Error("VirtualProtectEx");
                if (!FlushInstructionCache(process, codeAddress, (UIntPtr)(uint)code.Length)) throw Error("FlushInstructionCache");
                thread = CreateRemoteThread(process, IntPtr.Zero, UIntPtr.Zero, codeAddress, IntPtr.Zero, 0, IntPtr.Zero);
                if (thread == IntPtr.Zero) throw Error("CreateRemoteThread");
                running = true;
                uint wait = WaitForSingleObject(thread, 5000);
                if (wait == WaitTimeout) throw new TimeoutException("目标线程 5 秒内未完成，已停止重试该窗口。");
                if (wait != WaitObject0) throw Error("WaitForSingleObject");
                running = false;
                uint exitCode;
                if (!GetExitCodeThread(thread, out exitCode)) throw Error("GetExitCodeThread");
                if (exitCode != 0) throw new InvalidOperationException("目标线程异常退出：0x" + exitCode.ToString("X8"));
                var result = Read(process, resultAddress, 8);
                if (BitConverter.ToInt32(result, 0) <= 0)
                {
                    int error = BitConverter.ToInt32(result, 4);
                    throw new Win32Exception(error, "目标 SetWindowDisplayAffinity 失败（Win32 " + error + "）：" + new Win32Exception(error).Message);
                }
                uint affinity = unchecked((uint)GetWindowDisplayAffinity(window));
                if (affinity != WdaNone)
                    throw new InvalidOperationException("调用成功后捕获标志仍为 0x" + affinity.ToString("X") + "，目标可能已重新设置保护。");
            }
            finally
            {
                if (running)
                {
                    // Never release instructions while a target thread may still execute them.
                    var pendingProcess = process; var pendingThread = thread;
                    var pendingCode = codeAddress; var pendingResult = resultAddress;
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        if (WaitForSingleObject(pendingThread, Infinite) == WaitObject0)
                            ReleaseMemory(pendingProcess, pendingCode, pendingResult);
                        CloseHandle(pendingThread);
                        CloseHandle(pendingProcess);
                    });
                }
                else
                {
                    ReleaseMemory(process, codeAddress, resultAddress);
                    if (thread != IntPtr.Zero) CloseHandle(thread);
                    CloseHandle(process);
                }
            }
        }

        static void ReleaseMemory(IntPtr process, IntPtr code, IntPtr result)
        {
            if (code != IntPtr.Zero) VirtualFreeEx(process, code, UIntPtr.Zero, 0x8000);
            if (result != IntPtr.Zero) VirtualFreeEx(process, result, UIntPtr.Zero, 0x8000);
        }
    }
}
