using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Net.Http.Headers;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Controls;

namespace EldenRingPractice
{
    public class ERLink : IDisposable
    {
        //public const uint PROCESS_ALL_ACCESS = 2035711;             // FIGURE OUT WHAT THIS IS
        public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const string erProcessName = "eldenring";
        private Process _erProcess = null;
        private IntPtr _erProcessHandle = IntPtr.Zero;
        public IntPtr erBaseAddress = IntPtr.Zero;
        int erSize = 0;

        public bool linkActive = false;

        bool disposed = false;

        //bool targetDisplayHooked = false;

        Thread erMonitorThread = null;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAcess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int iSize, ref int lpNumberOfBytesRead);

        [DllImport("ntdll.dll")]
        static extern int NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, UInt32 NumberOfBytesToWrite, ref UInt32 NumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            { return; }

            if (disposing)
            {
                erMonitorThread = null;
            }

            linkActive = false;

            disposed = true;
        }

        public ERLink()
        {
            InitGameLink();

            // TEST
            //getEntityCount();
        }

        public void InitGameLink()
        {
            if (AttachProcess())
            {
                LocateBaseAddress();
                LocateOffsets();

                NoLogoPatch();

                linkActive = true;

                erMonitorThread = new Thread(() => { ERMonitor(); });
                erMonitorThread.Start();

                
            }
            else
            {
                linkActive = false;
            }
        }

        private bool AttachProcess()
        {
            var processList = Process.GetProcesses();

            foreach (var process in processList)
            {
                if (process.ProcessName.ToLower().Equals(erProcessName) && !process.HasExited)
                {
                    if (_erProcessHandle == IntPtr.Zero)
                    {
                        _erProcess = process;
                        _erProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, bInheritHandle: false, _erProcess.Id);
                        return true;
                    }
                }
            }
            return false;
        }

        private void LocateBaseAddress()
        {
            try
            {
                foreach (var module in _erProcess.Modules)
                {
                    var processModule = module as ProcessModule;
                    var moduleName = processModule.ModuleName.ToLower();
                    if (moduleName == erProcessName + ".exe")
                    {
                        erBaseAddress = processModule.BaseAddress;
                        erSize = processModule.ModuleMemorySize;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Sadge not work????"); }

            // needed???
            if (erBaseAddress == IntPtr.Zero)
            {
                throw new Exception("Couldn't find ER base address");
            }
        }

        // 
        // MEMORY OFFSETS
        //

        int quitoutOffset;

        int worldChrManOffset;
        uint worldChrManPlayerOffset;

        uint playerGameDataOffset;

        uint chrSetOffset;
        uint torrentIDOffset;

        int logoOffset;
        int quitoutBase;

        int gameDataManager;

        int fontDrawOffset;
        int enemyTargetViewOffset;
        int soundDrawPatchOffset;

        // player offsets
        int noDeathAllOffset;
        uint noDeathOffset;

        int infiniteConsumablesOffset;

        int scaduOffset;

        // enemy offsets
        int aiUpdateOffset;
        int aiRepeatOffset;

        List<int> menuOffsets = new List<int>();

        // TARGET OFFSETS
        int codeCavePtrLoc;
        int targetHookLocation;
        int targetHookOffset;
        int codeCaveCodeLoc { get { return codeCavePtrLoc + 0x10; } }
        // END

        // render offsets
        int weaponHitboxBase; //currently no RTTI name for this.
        int weaponHitboxOffset;
        int meshesOffset;

        int caveOffset;

        // other stuff
        int csFlipperOffset; // what does this even mean
        int gameSpeedOffset;
        int frameTimeOffset;

        int itemSpawnStart { get { return caveOffset + 0x100; } }
        int mapItemManOffset = 0x3C32B20;
        int itemSpawnCall = 0x5539E0;
        int itemSpawnData { get { return itemSpawnStart + 0x30; } }


        // NOT SCANNED FOR
        // TODO: Find scanning patterns

        int graceOffset = 0x3d6cfc0;


        // TESTING ONES
        int entityListBase;


        public bool dlcAvailable() { return scaduOffset > 0 && scaduOffset < 0x10000; }

        void LocateOffsets()
        {
            //var scanner = new AOBScanner(_erProcessHandle, erBaseAddress, erSize);

            using (var scanner2 = new AOBMemoryScanner(_erProcessHandle, erBaseAddress, erSize))
            {

                logoOffset = scanner2.findAddress("74 53 48 8B 05 ?? ?? ?? ?? 48 85 C0 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C8 4C 8D 05 ?? ?? ?? ?? BA ?? ?? 00 00 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ??", 0, 10900000);

                scaduOffset = scanner2.findAddress("80 b9 ?? ?? 00 00 00 74 08 0f b6 81 ?? ?? 00 00 c3 0f b6 81 ?? ?? 00 00 c3", 0, 2000000, 0, 20);

                fontDrawOffset = scanner2.findAddress("48 89 5C 24 10 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ??", 0, 39000000);
                enemyTargetViewOffset = scanner2.findAddress("40 38 35 ?? ?? ?? ?? 0F 84 ?? ?? 00 00 48 8D 54 24 ?? 48 8B CF E8 ?? ?? 00 00 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 66 44 85 BF ?? ?? 00 00 74 ?? 48 8B 05 ?? ?? ?? ?? 48 85 C0 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C8 4C 8D 05 ?? ?? ?? ?? BA ?? ?? 00 00 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 8B 80 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B 88 ?? ?? ?? ?? 48 8B 49 ?? E8 ?? ?? ?? ?? EB ?? 8B 8F ?? ?? 00 00 E8 ?? ?? ?? ?? F3 0F 11 45 ?? 48 8D 4C 24 ?? 66 85 9F ?? ?? 00 00 74 ?? B2 ?? EB ??", 0, 3200000, 7, 3);
                soundDrawPatchOffset = scanner2.findAddress("74 ?? 48 8B 0D ?? ?? ?? ?? BE 01 00 00 00 89 74 24 ?? 48 85 C9 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ??", 0, 3200000);

                quitoutBase = scanner2.findAddress("48 8B 05 ?? ?? ?? ?? 0F B6 40 10 C3", 0, 6500000, 7, 3);

                gameDataManager = scanner2.findAddress("48 8B 05 ?? ?? ?? ?? 48 85 C0 74 05 48 8B 40 58 C3 C3", 0, 2000000, 7, 3);

                // Player

                worldChrManOffset = scanner2.findAddress("E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 4C 8B A8 ?? ?? ?? ?? 4D 85 ED 0F 84 ?? ?? ?? ??", 0, 1800000, 12, 8);
                worldChrManPlayerOffset = (uint)scanner2.findAddress("E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 4C 8B A8 ?? ?? ?? ?? 4D 85 ED 0F 84 ?? ?? ?? ??", 0, 1800000, 0, 15);

                playerGameDataOffset = (uint)scanner2.findAddress("48 8B 81 ?? ?? 00 00 48 C7 02 FF FF FF FF 48 85 C0 74 0A 48 8B 80 ?? ?? 00 00 48 89 02 48 8B C2", 0, 6400000, 0, 3);

                chrSetOffset = (uint)scanner2.findAddress("48 8B 8C FE ?? ?? ?? 00 48 85 C9 74 ?? 4C 8B 01 8B D0 41 FF 50 ?? 48 8B 7C 24 ?? 48 8B 5C 24 ?? 48 83 C4 ??", 0, 5000000, 0, 4);
                torrentIDOffset = (uint)scanner2.findAddress("48 8B 81 ?? ?? 00 00 48 C7 02 FF FF FF FF 48 85 C0 74 0A 48 8B 80 ?? ?? 00 00 48 89 02 48 8B C2", 0, 6400000, 0, 22);

                noDeathAllOffset = scanner2.findAddress("80 3D ?? ?? ?? ?? 00 75 ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 84 C0 74 ?? 48 83 3D ?? ?? ?? ?? 00", 0, 4200000, 7, 2);
                noDeathOffset = (uint)scanner2.findAddress("48 83 EC 20 F6 81 ?? ?? 00 00 01 48 8B D9 74 08", 0, 4200000, 0, 6);

                infiniteConsumablesOffset = scanner2.findAddress("80 3D ?? ?? ?? ?? 00 75 05 45 33 C0 EB 03 41 B0 01 48 8D 84 24 ?? 00 00 00 48 89 84 24 ?? 00 00 00 41 8B 06 89 84 24 ?? 00 00 00 48 8D 8E ?? ?? 00 00 48 8D 94 24 ?? 00 00 00 E8 ?? ?? ?? ?? 0F B6 D0", 0, 6300000, 7, 2);

                // Enemy
                aiUpdateOffset = scanner2.findAddress("0F B6 3D ?? ?? ?? ?? 48 85 C0 75 2E", 0, 3800000, 7, 3);
                aiRepeatOffset = scanner2.findAddress("48 8B 41 08 0F BE 80 ?? E9 00 00", 0, 0, 7);

                if (aiRepeatOffset < 0)
                { aiRepeatOffset = scanner2.findAddress("48 8B 41 08 0F BE 80 ?? E9 00 00", 1, 0, 7); }

                csFlipperOffset = scanner2.findAddress("48 8B 05 ?? ?? ?? ?? F3 0F 10 88 ?? ?? ?? ?? F3 0F", 0, 13800000, 7, 3);
                gameSpeedOffset = scanner2.findAddress("48 8B 05 ?? ?? ?? ?? F3 0F 10 88 ?? ?? ?? ?? F3 0F", 0, 13800000, 0, 11);
                frameTimeOffset = scanner2.findAddress("89 73 ?? C7 43 ?? ?? 88 88 3C EB ?? 89 73 ??", 0, 13000000, 6);

                weaponHitboxBase = scanner2.findAddress("48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? F3 0F 10 57 ?? 48 8B CB 0F B6 93 ?? ?? 00 00 E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??", 0, 10900000, 7, 3);
                weaponHitboxOffset = scanner2.findAddress("80 B9 ?? ?? 00 00 00 48 8B F9 BE FF FF FF FF 74 ?? 48 8B 19 48 85 DB 74 ??", 0, 5200000, 0, 2);
                meshesOffset = scanner2.findAddress("0F B6 25 ?? ?? ?? ?? 44 0F B6 3D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B6 F8", 0, 7100000, 7, 3);

                // itemspawns

                mapItemManOffset = scanner2.findAddress("48 8B 0D ?? ?? ?? ?? C7 44 24 50 FF FF FF FF", 0, 5700000, 7, 3);
                itemSpawnCall = scanner2.findAddress("8B 02 83 F8 0A", 0, 5400000, -0x52);
                if (itemSpawnCall < 0)
                {
                    itemSpawnCall = scanner2.findAddress("40 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? 00 00 48 C7 45 ?? FE FF FF FF 48 89 9C 24 ?? ?? 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 44 89 4C 24 ?? 4D 8B ?? 4C 89 44 24 ?? 4C 8B ?? 33 FF 89 7C 24 ?? 8B 02 83 F8 ?? 0F 87 ?? ?? 00 00", 0, 5400000);
                }

                // Target stuff
                var cavePattern = "";
                for (int i = 0; i < 0xA0; i++) { cavePattern += "90 "; }
                codeCavePtrLoc = scanner2.findAddress(cavePattern.TrimEnd(), 0, 100000);

                var thl = scanner2.findAddress("48 8B 48 ?? 49 89 8D ?? ?? 00 00 49 8B CE E8 ?? ?? ?? ?? 84 C0 75 ?? 49 8B 5E ?? 48 8D 4D ?? E8 ?? ?? ?? ??", 0, 7100000);
                if (thl > 0) { targetHookLocation = thl; }
                var tho = scanner2.findAddress("48 8B 48 ?? 49 89 8D ?? ?? 00 00 49 8B CE E8 ?? ?? ?? ?? 84 C0 75 ?? 49 8B 5E ?? 48 8D 4D ?? E8 ?? ?? ?? ??", 0, 7100000, 0, 7);
                if (tho > 0) { targetHookOffset = tho; }


                menuOffsets = getMenuOffsets(scanner2);

                caveOffset = scanner2.sectionAddress[0] + (int)scanner2.sectionSize[0];

                // test
                entityListBase = scanner2.findAddress("0F 10 00 0F 11 44 24 70 0F 10 48 10 0F 11 4D 80 48 83 3D", 0);
                //MessageBox.Show( (erBaseAddress + entityListBase).ToString("X16") + " | " + (erBaseAddress + worldChrManOffset).ToString("X16"));
            }

        }

        List<int> getMenuOffsets(AOBMemoryScanner scanner)
        {
            var results = new List<int>();
            
            var result = 0;
            var startAddress = 7500000;

            while (result != -1)
            {
                result = scanner.findAddress("4c 8b dc 53 56 57 48 81 ec 90 00 00 00 49 c7 43 88 fe ff ff ff 48 8b d9", 0, startAddress);
                if (result > -1)
                {
                    results.Add(result);
                    startAddress = result - scanner.sectionAddress[0] + 1;
                }
            }

            result = 0;
            
            while (result != -1)
            {
                result = scanner.findAddress("4c 8b dc 53 48 81 ec 90 00 00 00 49 c7 43 88 fe ff ff ff 48 8b 05 ?? ?? ?? ?? 48 33 c4 48 89 84 24 80 00 00 00 48 8b d9 49 c7 43 e0 00 00 00 00 49 8d 43 a8 49 89 43 90 49 8d 43 a8 49 89 43 98 48 8d 05 ?? ?? ?? ?? 49 89 43 a8 48 8d 05 ?? ?? ?? ?? 49 89 43 a8 48 8d 05 ?? ?? ?? ?? 49 89 43 b0", 0, startAddress);
                if (result > -1)
                {
                    results.Add(result);
                    startAddress = result - scanner.sectionAddress[0] + 1;
                }
            }
            return results;
        }

        // from ERTool
        static readonly byte[] itemSpawnTemplate = new byte[]
        {
            0x48, 0x83, 0xEC, 0x48, //sub rsp,48
            0x4D, 0x31, 0xC9, //xor r9,r9
            0x4C, 0x8D, 0x05, 0x22, 0x00, 0x00, 0x00, //lea r8,[eldenring.exe+28E3F30] - relative address, won't change
            0x49, 0x8D, 0x50, 0x20, //lea rdx,[r8+20]
            0x49, 0xBA, 0, 0, 0, 0, 0, 0, 0, 0, //mov r10,MapItemMan (addr offset 0x14)
            0x49, 0x8B, 0x0A, //mov rcx,[r10]
            0xE8, 0, 0, 0, 0, //call itemSpawnCall. itemSpawnCall - (itemSpawnStart + 0x24) (addr offset 0x20)
            0x48, 0x83, 0xC4, 0x48, //add rsp,48
            0xC3, //ret
        };

        //
        // PATCHES
        //

        readonly byte[] logoScreenOriginal = new byte[] { 0x74, 0x53 };
        readonly byte[] logoScreenPatch = new byte[] { 0x90, 0x90 };

        const byte aiRepeatPatch = 0xB2;
        const byte aiRepeatOriginal = 0xB1;

        const byte aiRepeatPatch108 = 0xC2;
        const byte aiRepeatOriginal108 = 0xC1;

        //
        // READ/WRITE
        //

        public int ReadInt32(IntPtr address)
        {
            var data = ReadMemory(address, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public long ReadInt64(IntPtr address)
        {
            var bytes = ReadMemory(address, 8);
            return BitConverter.ToInt64(bytes, 0);
        }

        public byte ReadUInt8(IntPtr address)
        {
            var data = ReadMemory(address, 1);
            return data[0];
        }

        public uint ReadUInt32(IntPtr address)
        {
            var data = ReadMemory(address, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public ulong ReadUInt64(IntPtr address)
        {
            var data = ReadMemory(address, 8);
            return BitConverter.ToUInt64(data, 0);
        }

        public float ReadFloat(IntPtr address)
        {
            var bytes = ReadMemory(address, 4);
            return BitConverter.ToSingle(bytes, 0);
        }

        public byte[] ReadMemory(IntPtr address, int size)
        {
            var data = new byte[size];
            var i = 1;
            ReadProcessMemory(_erProcessHandle, address, data, size, ref i);
            return data;
        }

        public void WriteUInt8(IntPtr address, byte data)
        {
            var bytes = new byte[] { data };
            WriteMemory(address, bytes);
        }

        public void WriteInt32(IntPtr address, int data)
        {
            WriteMemory(address, BitConverter.GetBytes(data));
        }

        public void WriteUInt32(IntPtr address, uint data)
        {
            WriteMemory(address, BitConverter.GetBytes(data));
        }

        public void WriteFloat(IntPtr address, float data)
        {
            WriteMemory(address, BitConverter.GetBytes(data));
        }

        public void WriteMemory(IntPtr address, byte[] data)
        {
            uint i = 0;
            NtWriteVirtualMemory(_erProcessHandle, address, data, (uint)data.Length, ref i);
        }



        ulong getPlayerInsPtr()
        {
            return ReadUInt64((IntPtr)(ReadUInt64(erBaseAddress + worldChrManOffset) + worldChrManPlayerOffset));
        }

        ulong getCharacterPtrModules()
        {
            return ReadUInt64((IntPtr)(getPlayerInsPtr() + 0x190));
        }

        ulong getCharacterPtrGameData()
        {
            return ReadUInt64((IntPtr)(getPlayerInsPtr() + playerGameDataOffset));
        }

        ulong getTorrentPtr()
        {
            //var ptr1 = ReadUInt64(erBaseAddress + worldChrManOffset);
            //var ptr2 = ReadUInt64((IntPtr)(ptr1 + worldChrManTorrentOff)); //gets a ptr to a ChrSet

            uint torrentID = ReadUInt32((IntPtr)(getCharacterPtrGameData() + torrentIDOffset));
            uint chrSetTorrentOff = (torrentID & 0x0FF00000) >> 20;
            //var ptr2 = ReadUInt64((IntPtr)(ptr1 + chrSetOffset + chrSetTorrentOff * 8)); //gets a ptr to a ChrSet
            //var ptr3 = ReadUInt64((IntPtr)(ptr2 + 0x18)); //no name
            //var ptr4 = ReadUInt64((IntPtr)(ptr3)); //CS::EnemyIns

            return resolvePointerChain(erBaseAddress + worldChrManOffset, (IntPtr)chrSetOffset + (IntPtr)chrSetTorrentOff * 8, 0x18, 0);

        }

        ulong resolvePointerChain(params nint[] pointers)
        {
            //ulong pointer = 0;
            //if (addBaseAddress)
            //{
            var pointer = ReadUInt64(pointers[0]);
            //}
            //else
            //{
            //var pointer = ReadUInt64(pointers[0]);
            //}

            for (int i = 1; i < pointers.Length; i++)
                { pointer = ReadUInt64((IntPtr)pointer + pointers[i]); }
            return pointer;
        }

        //
        // HANDLE STUFF
        //

        public enum GameOptions
        {
            NONE,
            NO_DEATH_PLAYER,
            NO_DEATH_ALL,
            ONE_SHOT,
            RUNE_ARC,
            INFINITE_STAMINA,
            INFINITE_FOCUS,
            INFINITE_CONSUMABLES,
            NO_GRAVITY,
            DISABLE_AI_UPDATES,
            REPEAT_LAST_ACTION,
            FAST_QUIT,
            LOAD_SAVE,

            WEAPON_HITBOXES,

            NUDGE_PLUS_Y,
            NUDGE_MINUS_Y,
        }

        // A lock is a stat that is constantly being set in memory - eg player hp freeze
        public enum StatLocks
        {
            PLAYER_HP,
            TARGET_HP,
            TARGET_POISE,
            TARGET_POISE_TIMER,
        }

        public enum TargetStats
        {
            HP, HP_MAX,
            POISE, POISE_MAX, POISE_TIMER,
            POISON, POISON_MAX,
            ROT, ROT_MAX,
            BLEED, BLEED_MAX,
            BLIGHT, BLIGHT_MAX,
            FROST, FROST_MAX,
            SLEEP, SLEEP_MAX,
            MADNESS, MADNESS_MAX,
        }

        HashSet<GameOptions> ActiveOptions = new HashSet<GameOptions>();
        HashSet<StatLocks> ActiveLocks = new HashSet<StatLocks>();

        //
        // Main loop for memory monitoring/editing
        //

        object loopLock = new object();

        void ERMonitor()
        {
            while (linkActive)
            {
                lock (loopLock)
                {
                    foreach (var option in ActiveOptions)
                    {
                        ActionOption(option, 1);
                    }

                    foreach (var statLock in ActiveLocks)
                    {
                        ActionLocks(statLock);
                    }
                    Thread.Sleep(100);
                }
            }
        }

        public void AddOptionMonitor()
        {
            lock (loopLock)
            {
                
            }
        }

        public void RemoveOptionMonitor()
        {
            lock (loopLock)
            {
                
            }
        }

        public void AddLock(StatLocks option)
        {
            lock (loopLock)
            {
                ActiveLocks.Add(option);
            }
        }

        public void RemoveLock(StatLocks option)
        {
            lock (loopLock)
            {
                ActiveLocks.Remove(option);
            }
        }

        public void ActionOption(GameOptions option, byte state)
        {
            if (!linkActive) { return; }

            switch (option)
            {
                case GameOptions.FAST_QUIT:
                    WriteUInt8((IntPtr)(ReadUInt64(erBaseAddress + quitoutBase)) + 0x10, 1);
                    break;
                case GameOptions.NO_DEATH_PLAYER:
                    var x = ReadUInt64((IntPtr)(getCharacterPtrModules() + 0));
                    WriteUInt8((IntPtr)(x + noDeathOffset), state);
                    break;
                case GameOptions.NO_DEATH_ALL:
                    WriteUInt8(erBaseAddress + noDeathAllOffset, state);
                    break;
                case GameOptions.ONE_SHOT:
                    WriteUInt8(erBaseAddress + infiniteConsumablesOffset - 1, state);
                    break;
                case GameOptions.RUNE_ARC:
                    WriteUInt8((IntPtr)getCharacterPtrGameData() + 0xFF, state);
                    break;
                case GameOptions.NO_GRAVITY:
                    NoGravity(state); // potentially should monitor the current no grav state
                    break;
                case GameOptions.DISABLE_AI_UPDATES:
                    WriteUInt8(erBaseAddress + aiUpdateOffset, state);
                    break;
                case GameOptions.REPEAT_LAST_ACTION:
                    setAiRepeat(state);
                    break;
                case GameOptions.WEAPON_HITBOXES:
                    var ptr = ReadUInt64(erBaseAddress + weaponHitboxBase) + (uint)weaponHitboxOffset + 1;
                    //ptr += (uint)weaponHitboxOffset + 1;
                    WriteUInt8((IntPtr)ptr, 1);
                    break;
            }
        }

        public void noDeathPlayer(byte state)
        {
            if (state == 1)
                { ActiveOptions.Add(GameOptions.NO_DEATH_PLAYER); }
            else
                { ActiveOptions.Remove(GameOptions.NO_DEATH_PLAYER); }

            ActionOption(GameOptions.NO_DEATH_PLAYER, state);
        }

        public void noDeathAll(byte state)
        {
            if (state == 1)
            { ActiveOptions.Add(GameOptions.NO_DEATH_ALL); }
            else
            { ActiveOptions.Remove(GameOptions.NO_DEATH_ALL); }

            ActionOption(GameOptions.NO_DEATH_ALL, state);
        }

        public void InfiniteStamina(byte state)
        {
            WriteUInt8(erBaseAddress + infiniteConsumablesOffset + 1, state);
        }

        public void InfiniteFocus(byte state)
        {
            WriteUInt8(erBaseAddress + infiniteConsumablesOffset + 2, state);
        }

        public void InfiniteConsumables(byte state)
        {
            WriteUInt8(erBaseAddress + infiniteConsumablesOffset, state); // general
            WriteUInt8(erBaseAddress + infiniteConsumablesOffset + 3, state); // arrows
        }

        private void NoGravity(byte state)
        {
            // Player
            var x = ReadUInt64((IntPtr)(getCharacterPtrModules() + 0x68));
            WriteUInt8((IntPtr)(x + 0x1D3), state);

            // Torrent
            //var x2 = ReadUInt64((IntPtr)(getTorrentPtr() + 0x190));
            //var x3 = ReadUInt64((IntPtr)(x2 + 0x68)); //CS::CSChrPhysicsModule
            //WriteUInt8((IntPtr)(x3 + 0x1D3), state);

            var y = resolvePointerChain((IntPtr)getTorrentPtr() + 0x190, 0x68);
            WriteUInt8((IntPtr)y + 0x1D3, state);
        }

        public void ShowStableGround(byte state) // NOT VERSION SAFE
        {
            WriteUInt8((IntPtr)getPlayerInsPtr() + 0x735, state);
        }

        public void ShowAllGraces(byte state)
        {
            WriteUInt8(erBaseAddress + graceOffset, state);
            WriteUInt8(erBaseAddress + graceOffset + 1, state);
        }

        readonly byte[] soundViewMemoryOriginal = { 0x74, 0x53 };
        readonly byte[] soundViewMemoryPatched = { 0x90, 0x90 };

        public void ToggleWeaponHitboxes(byte state)
        {
            if (state == 1)
            { ActiveOptions.Add(GameOptions.WEAPON_HITBOXES); }
            else
            { ActiveOptions.Remove(GameOptions.WEAPON_HITBOXES); }

            ActionOption(GameOptions.WEAPON_HITBOXES, state);
        }

        public void ToggleModelHitboxes(byte state)
        {
            WriteUInt8(erBaseAddress + meshesOffset + 3, state);
        }

        public void ToggleTargetView(byte state)
        {
            WriteUInt8(erBaseAddress + enemyTargetViewOffset, state);
        }

        public void ToggleSoundView(bool state)
        {
            if (state) { setFontDraw(); }

            var existingBytes = ReadMemory(erBaseAddress + soundDrawPatchOffset, 2);
            if (existingBytes.SequenceEqual(soundViewMemoryOriginal) && state)
            {
                WriteMemory(erBaseAddress + soundDrawPatchOffset, soundViewMemoryPatched);
            }
            else if (existingBytes.SequenceEqual(soundViewMemoryPatched) && !state)
            {
                WriteMemory(erBaseAddress + soundDrawPatchOffset, soundViewMemoryOriginal);
            }
        }

        public void ActionLocks(StatLocks option)
        {
            switch (option)
            {
                case StatLocks.TARGET_POISE:
                    getTargetStats(ERLink.TargetStats.POISE, 1);
                    break;
                case StatLocks.TARGET_POISE_TIMER:
                    getTargetStats(ERLink.TargetStats.POISE_TIMER, 10);
                    break;
            }
        }

        void setAiRepeat(byte state)
        {
            var value = ReadUInt8(erBaseAddress + aiRepeatOffset);
            var patchValue = aiRepeatOriginal;

            if (value == aiRepeatOriginal || value == aiRepeatPatch) // older patches
            {
                if (state == 1)
                {
                    patchValue = aiRepeatPatch;
                }
                else
                {
                    patchValue = aiRepeatOriginal;
                }
            }
            else if (value == aiRepeatOriginal108 || value == aiRepeatPatch108) // 1.08+
            {
                if (state == 1)
                {
                    patchValue = aiRepeatPatch108;
                }
                else
                {
                    patchValue = aiRepeatOriginal108;
                }
            }
            else
            {
                //what the fuck is happening lets pretend no one gave us a task
                MessageBox.Show("please don't see me");
                return;
            }

            WriteUInt8(erBaseAddress + aiRepeatOffset, patchValue);
        }

        public void NoLogoPatch()
        {
            if (logoOffset < 0) { return; }

            var data = ReadMemory(erBaseAddress + logoOffset, logoScreenOriginal.Length);

            if (data.SequenceEqual(logoScreenOriginal))
            {
                WriteMemory(erBaseAddress + logoOffset, logoScreenPatch);
            }
        }

        void setFontDraw()
        {//needed for poise bars and some other things. no need to turn off.
            int oldVal = ReadUInt8(erBaseAddress + fontDrawOffset);
            if (oldVal == 0xC3) { return; }
            WriteUInt8(erBaseAddress + fontDrawOffset, 0xC3);
        }

        public enum GameMenus
        {
            CREDITS,
            GREAT_RUNE,
            PHYSICK,
            ASH_OF_WAR,
            MULTIPLAYER,
            SPELLS,
            LEVEL_UP,
            CHEST,
            REBIRTH,
        }

        public void openMenu(int menuId)
        {
            RunThread(erBaseAddress + menuOffsets[menuId], param: erBaseAddress + caveOffset);
        }














        const long ADDRESS_MINIMUM = 0x700000000000;
        const long ADDRESS_MAXIMUM = 0x800000000000;

        // FIGURE OUT WTF THIS IS DOING
        public bool installTargetHook()
        {
            //generate code first
            var targetHookReplacementCode = getTargetHookReplacementCode();
            var targetHookCaveCode = getTargetHookCaveCodeTemplate(); //still needs to have ptr addr added in

            var code = ReadMemory(erBaseAddress + targetHookLocation, targetHookOrigCode.Length).Take(7); //compare first 7 bytes only; ignores target offset

            //MessageBox.Show(targetHookReplacementCode);

            if (code.SequenceEqual(targetHookReplacementCode.Take(7)))
            {
                MessageBox.Show("Already hooked");
                return true;
            }
            if (!code.SequenceEqual(targetHookOrigCode.Take(7)))
            {
                MessageBox.Show("Unexpected code at hook location");
                return false;
            }

            var caveCheck1 = ReadUInt64(erBaseAddress + codeCavePtrLoc);
            var caveCheck2 = ReadUInt64(erBaseAddress + codeCaveCodeLoc);
            if (caveCheck1 != 0x9090909090909090 || caveCheck2 != 0x9090909090909090) //byte reversal doesn't matter
            {
                MessageBox.Show("Code cave not empty");
                return false;
            }

            //set up cave first
            var targetHookFullAddr = erBaseAddress + codeCavePtrLoc;
            var caveCode = new byte[targetHookCaveCode.Length];
            Array.Copy(targetHookCaveCode, caveCode, targetHookCaveCode.Length);
            var fullAddrBytes = BitConverter.GetBytes((Int64)targetHookFullAddr);
            Array.Copy(fullAddrBytes, 0, caveCode, 2, 8);
            //patch cave
            WriteMemory(erBaseAddress + codeCaveCodeLoc, caveCode);
            //patch hook loc
            WriteMemory(erBaseAddress + targetHookLocation, targetHookReplacementCode);

            return true;
        }

        static readonly byte[] targetHookOrigCode = new byte[] { 0x48, 0x8B, 0x48, 0x08, 0x49, 0x89, 0x8D, 0, 0, 0, 0, }; //last four bytes are the 'target hook offset' which varies with patches. followed by 0x49, 0x8B, 0xCE, 0xE8, which stays unchanged.
        static readonly byte[] targetHookReplacementCodeTemplate = new byte[] { 0xE9,
            0, 0, 0, 0, //address offset
            0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        //replacement code contains the offset from the following instruction (basically hook loc + 5) to the code cave.
        //then it just nops to fill out the rest of the old instructions
        byte[] getTargetHookReplacementCode()
        {
            var ret = new byte[targetHookReplacementCodeTemplate.Length];
            //int addrOffset = codeCaveCodeLoc - (targetHookLoc + 5); //target minus next instruction location (ie. the NOP 5 bytes in)
            int addrOffset = codeCaveCodeLoc - (targetHookLocation + 5); //target minus next instruction location (ie. the NOP 5 bytes in)
            //MessageBox.Show(codeCaveCodeLoc.ToString() + " | " + targetHookLoc.ToString());
            Array.Copy(targetHookReplacementCodeTemplate, ret, ret.Length);
            Array.Copy(BitConverter.GetBytes(addrOffset), 0, ret, 1, 4);
            return ret;
        }

        static readonly byte[] targetHookCaveCodeTemplate = new byte[] { 0x48, 0xA3,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //full 64 bit ptr address goes here
        0x48, 0x8B, 0x48, 0x08, //should be identical to orig code from here to just before E9
        0x49, 0x89, 0x8D, 0, 0, 0, 0, //fill in offset from scan
        0xE9,
        0, 0, 0, 0, //address offset
         };

        byte[] getTargetHookCaveCodeTemplate()
        {
            var ret = new byte[targetHookCaveCodeTemplate.Length];
            int addrOffset = targetHookLocation + targetHookReplacementCodeTemplate.Length - (codeCaveCodeLoc + ret.Length); //again, target (after the hook) minus next instruction location (the NOPs after the end of our injection)
            Array.Copy(targetHookCaveCodeTemplate, ret, ret.Length);
            Array.Copy(BitConverter.GetBytes(addrOffset), 0, ret, ret.Length - 4, 4);
            Array.Copy(BitConverter.GetBytes(targetHookOffset), 0, ret, 2 + 8 + 4 + 3, 4);
            return ret;
        }

        //
        // END OF WTF SECTION
        //

        public bool isRiding()
        {//there's multiple ways to get a 'riding state'. see pav's table.
            var ptr = getCharacterPtrModules() + 0xE8; //CS::CSChrRideModule
            var ptr2 = ReadInt64((IntPtr)ptr) + 0x160;
            var rideStatus = ReadUInt32((IntPtr)ptr2);
            return (rideStatus >> 24) == 0x01;
        }

        public (float x, float y, float z) getSetLocalCoords((float, float, float)? pos = null)
        {
            if (isRiding()) { return getSetTorrentLocalCoords(pos); }
            return getSetPlayerLocalCoords(pos);
        }

        public (float, float, float) getSetPlayerLocalCoords((float, float, float)? pos = null)
        {
            var ptr4 = getCharacterPtrModules();
            var ptr5 = ReadUInt64((IntPtr)(ptr4 + 0x68)); //CS::CSChrPhysicsModule
            var ptrX = (IntPtr)(ptr5 + 0x70);
            var ptrY = (IntPtr)(ptr5 + 0x74);
            var ptrZ = (IntPtr)(ptr5 + 0x78);

            float x = ReadFloat(ptrX);
            float y = ReadFloat(ptrY);
            float z = ReadFloat(ptrZ);

            if (pos != null)
            {
                WriteFloat(ptrX, pos.Value.Item1);
                WriteFloat(ptrY, pos.Value.Item2);
                WriteFloat(ptrZ, pos.Value.Item3);
            }

            return (x, y, z);
        }

        public (float, float, float) getSetTorrentLocalCoords((float, float, float)? pos = null)
        {
            var ptr1 = getTorrentPtr();
            var ptr2 = ReadUInt64((IntPtr)(ptr1 + 0x190));
            var ptr3 = ReadUInt64((IntPtr)(ptr2 + 0x68)); //CS::CSChrPhysicsModule
            var ptrX = (IntPtr)(ptr3 + 0x70);
            var ptrY = (IntPtr)(ptr3 + 0x74);
            var ptrZ = (IntPtr)(ptr3 + 0x78);

            float x = ReadFloat(ptrX);
            float y = ReadFloat(ptrY);
            float z = ReadFloat(ptrZ);

            if (pos != null)
            {
                WriteFloat(ptrX, pos.Value.Item1);
                WriteFloat(ptrY, pos.Value.Item2);
                WriteFloat(ptrZ, pos.Value.Item3);
            }

            return (x, y, z);
        }

        public enum Coordinates
        {
            x,y,z,
        }

        public void nudgePlayer(Coordinates direction, bool positive)
        {
            var playerPosition = getSetLocalCoords();
            float distance = 3;
            float x = playerPosition.x;
            float y = playerPosition.y;
            float z = playerPosition.z;

            if (!positive) { distance = -3; }

            if (direction == Coordinates.x)
                { x += distance; }
            else if (direction == Coordinates.y)
                { y += distance; }
            else if (direction == Coordinates.z)
                { z += distance; }

            getSetLocalCoords((x, y, z));

        }

        public enum PlayerStats
        {
            LEVEL,
            VIGOR,
            MIND,
            ENDURANCE,
            STRENGTH,
            DEXTERITY,
            INTELLIGENCE,
            FAITH,
            ARCANE,
            SCADU,
            ASH,
        }
        int statsOffset = 0x3c;
        int levelOffset = 0x68;

        public int[] getPlayerStats(int[] updateStats = null)
        {
            var ptr = (IntPtr)getCharacterPtrGameData();
            int[] returnStats = new int[11];

            // level is separate
            if (updateStats != null)
            {
                WriteInt32(ptr + 0x68, updateStats[0]);
            }
            returnStats[0] = ReadInt32(ptr + 0x68);

            // base stats
            for (int i = 0; i < 9; i++)
            {
                int offset = 0x3c + i * 4;
                int currentStat = ReadInt32(ptr + offset);
                returnStats[i + 1] = currentStat;

                if (updateStats != null)
                {
                    WriteInt32(ptr + offset, updateStats[i + 1]);
                }
            }

            // dlc stats
            if (dlcAvailable())
            {
                for (int i = 0; i < 2; i++ )
                {
                    int offset = scaduOffset + i;
                    int currentStat = ReadUInt8(ptr + offset);
                    returnStats[i + 9] = currentStat;

                    if (updateStats != null)
                    {
                        WriteUInt8(ptr + offset, (byte)updateStats[i + 9]);
                    }

                }
            } else
            {
                returnStats[9] = 0;
                returnStats[10] = 0;
            }

            return returnStats;
            
        }

        public uint getRunes()
        {
            return ReadUInt32((IntPtr)getCharacterPtrGameData() + 0x6C);
        }

        public (ulong, ulong, ulong, ulong) getIGTFormatted()
        {
            var igt = resolvePointerChain(erBaseAddress + gameDataManager, 0XA0);

            var IGTms = (igt % 1000) / 10;
            var x = igt / 1000;
            var IGTseconds = x % 60;
            var IGTminutes = x / 60 % 60;
            var IGThours = x / 3600;

            return (IGThours, IGTminutes, IGTseconds, IGTms);
        }

        public ulong getIGT()
        {
            return resolvePointerChain(erBaseAddress + gameDataManager, 0XA0);
        }

        public double getTargetStats(TargetStats stat, double? setStat = null)
        {
            var targetPtr = ReadUInt64(erBaseAddress + codeCavePtrLoc);
            targetPtr = ReadUInt64((IntPtr)(targetPtr + 0x190));

            if (targetPtr < ADDRESS_MINIMUM || targetPtr > ADDRESS_MAXIMUM) { return double.NaN; }

            IntPtr returnVal = 0;

            switch (stat)
            {
                case TargetStats.HP:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)targetPtr) + 0x138); break;
                case TargetStats.HP_MAX:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)targetPtr) + 0x13c); break;
                case TargetStats.POISE:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x10); break;
                case TargetStats.POISE_MAX:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x14); break;
                case TargetStats.POISE_TIMER:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x1c); break;
                default:
                    int poisonOff = stat - TargetStats.POISON;
                    int statIndex = poisonOff / 2;
                    bool isMax = (poisonOff % 2) == 1;
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x20)) + (uint)((isMax ? 0x2c : 0x10) + 4 * statIndex));
                    break;
            }

            var targetStat = ReadMemory(returnVal, 4);

            // insert setvalues handling
            if (setStat.HasValue)
            {
                if (stat == TargetStats.POISE || stat == TargetStats.POISE_MAX || stat == TargetStats.POISE_TIMER)
                {
                    //double hi = (double)BitConverter.ToSingle(targetStat, 0);
                    //MessageBox.Show("I'm trying plz " + hi.ToString());
                    //WriteUInt8(returnVal, 15);
                    WriteFloat(returnVal, (float)setStat.Value);
                }
                else
                {
                    WriteInt32(returnVal, (int)setStat.Value);
                }

            }

            if (stat == TargetStats.POISE || stat == TargetStats.POISE_MAX || stat == TargetStats.POISE_TIMER)
            {
                return (double)BitConverter.ToSingle(targetStat, 0);
            }

            return (double)BitConverter.ToInt32(targetStat, 0);
        }

        public double getEntityStats(IntPtr pointer, TargetStats stat, double? setStat = null)
        {
            var targetPtr = ReadUInt64(erBaseAddress + codeCavePtrLoc);
            if (pointer == 0)
            {
                targetPtr = ReadUInt64((IntPtr)(targetPtr + 0x190));
            } else
            {
                targetPtr = (ulong)pointer;
            }

            if (targetPtr < ADDRESS_MINIMUM || targetPtr > ADDRESS_MAXIMUM) { return double.NaN; }

            IntPtr returnVal = 0;

            switch (stat)
            {
                case TargetStats.HP:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)targetPtr) + 0x138); break;
                case TargetStats.HP_MAX:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)targetPtr) + 0x13c); break;
                case TargetStats.POISE:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x10); break;
                case TargetStats.POISE_MAX:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x14); break;
                case TargetStats.POISE_TIMER:
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x40)) + 0x1c); break;
                default:
                    int poisonOff = stat - TargetStats.POISON;
                    int statIndex = poisonOff / 2;
                    bool isMax = (poisonOff % 2) == 1;
                    returnVal = (IntPtr)(ReadUInt64((IntPtr)(targetPtr + 0x20)) + (uint)((isMax ? 0x2c : 0x10) + 4 * statIndex));
                    break;
            }

            var targetStat = ReadMemory(returnVal, 4);

            // insert setvalues handling
            if (setStat.HasValue)
            {
                if (stat == TargetStats.POISE || stat == TargetStats.POISE_MAX || stat == TargetStats.POISE_TIMER)
                {
                    //double hi = (double)BitConverter.ToSingle(targetStat, 0);
                    //MessageBox.Show("I'm trying plz " + hi.ToString());
                    //WriteUInt8(returnVal, 15);
                    WriteFloat(returnVal, (float)setStat.Value);
                }
                else
                {
                    WriteInt32(returnVal, (int)setStat.Value);
                }

            }

            if (stat == TargetStats.POISE || stat == TargetStats.POISE_MAX || stat == TargetStats.POISE_TIMER)
            {
                return (double)BitConverter.ToSingle(targetStat, 0);
            }

            return (double)BitConverter.ToInt32(targetStat, 0);
        }

        public uint RunThread(IntPtr address, uint timeout = 0xFFFFFFFF, IntPtr? param = null)
        {
            var thread = CreateRemoteThread(_erProcessHandle, IntPtr.Zero, 0, address, param ?? IntPtr.Zero, 0, IntPtr.Zero);
            var ret = WaitForSingleObject(thread, timeout);
            CloseHandle(thread); //return value unimportant
            return ret;
        }

        public float setFrameTime(float? val = null)
        {
            //var ptr = erBaseAddress + frameTimeOffset;
            var returnValue = ReadFloat(erBaseAddress + frameTimeOffset);
            if (val.HasValue)
                { WriteFloat(erBaseAddress + frameTimeOffset, val.Value); }
            return returnValue;
        }

        public float setGameSpeed(float? val = null)
        {
            /*var ptr = erBaseAddress + csFlipperOffset;
            var ptr2 = (IntPtr)ReadUInt64(ptr) + gameSpeedOffset;
            var ret = ReadFloat(ptr2);
            if (val.HasValue)
            {
                WriteFloat(ptr2, val.Value);
            }
            return ret;*/

            //var ptr2 = (IntPtr)ReadUInt64(erBaseAddress + csFlipperOffset) + gameSpeedOffset;

            var ptr = (IntPtr)ReadUInt64(erBaseAddress + csFlipperOffset) + gameSpeedOffset;
            var returnValue = ReadFloat(ptr);
            //var returnValue = ReadFloat((IntPtr)ReadUInt64(erBaseAddress + csFlipperOffset) + gameSpeedOffset);
            if (val.HasValue)
                { WriteFloat(ptr, val.Value); }
            return returnValue;
        }

        public void LogOutput(String line)
        {
            //MainWindow.
        }

        public void spawnItem(uint itemID, uint quantity, uint ash = 0xFFFFFFFF)
        {
            var buffer = getItemSpawnTemplate();
            WriteMemory(erBaseAddress + itemSpawnStart, buffer);
            WriteUInt32(erBaseAddress + itemSpawnData + 0x20, 1);
            WriteUInt32(erBaseAddress + itemSpawnData + 0x24, itemID);
            WriteUInt32(erBaseAddress + itemSpawnData + 0x28, quantity);
            WriteUInt32(erBaseAddress + itemSpawnData + 0x2C, 0);
            WriteUInt32(erBaseAddress + itemSpawnData + 0x30, ash);
            RunThread(erBaseAddress + itemSpawnStart);

            //if ((itemID & 0xF0000000) == 0x40000000)
            //{//Goods item
            //    var goodsID = itemID & 0xFFFFFFF;
            //   var evt = GoodsEvents.getEvent(goodsID);
            //    if (evt >= 0)
            //    {
            //        Console.WriteLine($"Enabling flag {evt} for goods item {goodsID}");
            //        getSetEventFlag(evt, true);
            //    }
            //}
        }
        byte[] getItemSpawnTemplate()
        {
            var buffer = itemSpawnTemplate.ToArray();
            var mapItemManAddress = (erBaseAddress + mapItemManOffset).ToInt64();
            Array.Copy(BitConverter.GetBytes(mapItemManAddress), 0, buffer, 0x14, 8);
            int callAddress = itemSpawnCall - (itemSpawnStart + 0x24);
            Array.Copy(BitConverter.GetBytes(callAddress), 0, buffer, 0x20, 4);
            return buffer;
        }






        List<IntPtr> entityList = new List<IntPtr>();

        public void getEntityList()
        {
            IntPtr p = erBaseAddress + entityListBase;

            var q = ReadUInt64(p + 24 + (IntPtr)ReadUInt32(p + 19));


            //MessageBox.Show((p + 24).ToString("X16"));

            var start = ReadUInt64((IntPtr)q + 0x1F1B8);
            var end = ReadUInt64((IntPtr)q + 0x1F1C0);
            //MessageBox.Show(start.ToString("X16") + " | " + end.ToString("X16"));
            //MessageBox.Show(ReadUInt64((IntPtr)start).ToString("X16"));

            int count = (int)(end - start) / 8;

            for (int i = 0; i < count; i++)
            {
                entityList.Add((IntPtr)ReadUInt64((IntPtr)start + i * 8));
                //MessageBox.Show(i.ToString() + " " + entityList[i].ToString("X16"));
            }

            //int entityListStart = ReadUInt64()
            //entityListBas2
        }

        public bool populateEntityList(ListBox listbox)
        {
            getEntityList();

            listbox.ItemsSource = entityList;

            return false;
        }
    }
}
