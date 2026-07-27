using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Gwalho
{
    public enum OP : int
    {
        NOPE,
        DEFN,
        MOVE,
        PLUS,
        MNUS,
        MULT,
        DIVD,
        MODL,
        BAND,
        BORR,
        BXOR,
        BNOT,
        BNOR,
        BNND,
        BXNR,

        BITR,
        BITW,

        BFLR,
        BFLW,

        BSHL,
        BROL,
        BSHR,
        BROR,
        BUSR,
        GRET,
        LESS,
        EQUL,
        NEQL,
        GEQL,
        LEQL,
        JUMP,
        READ,
        WRTE,
        ARGW,
        ARGR,
        RETW, RETR,
        CALL,
        EXIT,
        COPY,
        ALOC,
        DELT,
        COMP,
        FREE,
        LOAD,
        SAVE,
        FILL,
        SUMM,
        AVRG,
        FIND,
        SORT,
        MINM,
        MAXM,
        CONT,
        RESZ,
        LNTH,
        EXST,
        BASE,
        SWAP,
        DONE,
        RNDM,

        // ===== 맵형 비교 (구간의 각 원소를 값과 비교해 그 자리에 1/0 덮어씀, 크기 불변) =====
        MLES,
        MLOE,
        MGOE,
        MGRT,
        MEQL,
        MNQL,

        // ===== 맵 (구간 전체에 산술연산 적용) =====
        MPLS,
        MMNS,
        MMLT,
        MDIV,
        MMDL,

        // ===== 비트 맵 (구간 전체에 비트연산 적용, B계열의 배열버전. BFLR/BFLW/BITR/BITW 제외) =====
        MAND,
        MORR,
        MXOR,
        MNOT,
        MNOR,
        MNND,
        MXNR,
        MSHL,
        MROL,
        MSHR,
        MROR,
        MUSR,

        // ===== 마스크 기반 필터 (다른 배열을 마스크로 써서 0인 자리만 지우고 패킹) =====
        MASK,

        // ===== 배열 단위 유틸 =====
        CLON,
        CHNG,
        RVRS,
        SHFL,




        EXTRA = NOPE
    }
    public enum VMState
    {
        Running,
        Compacting,
        DONE,

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct METADATA
    {
        public const uint MAGIC = 0x5B39355D;
        public uint Magic;
        public int ID;
        public int Length;
        public int Base;
        public byte Exists;

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FRAME
    {
        public int PrevArrayBlock;
        public int PrevPC;
        public int Self;
        public fixed int Registers[1024];
        public fixed int ARGS[1024];
        public fixed int RTNS[1024];
    }
    public static unsafe class GWVM
    {



        public static string RootPath { get; private set; }

        public static string ProjectPath { get; private set; }

        public static bool EndRun = false;

        public const int MEMORY_SIZE = 1 << 23;
        public const int MAX_ArrayBlock = 1 << 24;
        public const int MAX_FRAME = 1024;

        public const int Regi_COUNT = 1024;
        public const int Args_COUNT = 1024;
        public const int rtns_COUNT = 1024;

        public static int[] MEMORY = new int[MEMORY_SIZE];
        public static int[] Compaction_MEMORY = new int[MEMORY_SIZE];
        public static METADATA[] Metadatas = new METADATA[MAX_ArrayBlock];
        public static int[] used_ID = new int[MAX_ArrayBlock];
        public static int used_count = 0;
        public static FRAME[] Frames = new FRAME[MAX_FRAME];
        public static int FrameTop = 0;
        public static VMState State = VMState.DONE;
        public static int CurrentArrayBlock = 0;
        public static int PC = 0;
        public static int HeapTop = 1;

        static readonly System.Random _rng = new System.Random(); // SORT/SHFL 등에서 쓰는 무작위용

        public static delegate*<int*, FRAME*, void>[] Ops;
        static GWVM()
        {
            Ops = new delegate*<int*, FRAME*, void>[256];
            Ops[(int)OP.NOPE] = &NOPE;
            Ops[(int)OP.DEFN] = &DEFN;
            Ops[(int)OP.MOVE] = &MOVE;
            Ops[(int)OP.PLUS] = &PLUS;
            Ops[(int)OP.MNUS] = &MNUS;
            Ops[(int)OP.MULT] = &MULT;
            Ops[(int)OP.DIVD] = &DIVD;
            Ops[(int)OP.MODL] = &MODL;

            Ops[(int)OP.BAND] = &BAND;
            Ops[(int)OP.BORR] = &BORR;
            Ops[(int)OP.BXOR] = &BXOR;
            Ops[(int)OP.BNOT] = &BNOT;
            Ops[(int)OP.BNND] = &BNND;
            Ops[(int)OP.BXNR] = &BXNR;
            Ops[(int)OP.BNOR] = &BNOR;

            Ops[(int)OP.BITR] = &BITR;
            Ops[(int)OP.BITW] = &BITW;

            Ops[(int)OP.BFLR] = &BFLR;
            Ops[(int)OP.BFLW] = &BFLW;


            Ops[(int)OP.BSHL] = &BSHL;
            Ops[(int)OP.BROL] = &BROL;
            Ops[(int)OP.BROR] = &BROR;
            Ops[(int)OP.BSHR] = &BSHR;
            Ops[(int)OP.BUSR] = &BUSR;

            Ops[(int)OP.GRET] = &GRET;
            Ops[(int)OP.LESS] = &LESS;
            Ops[(int)OP.EQUL] = &EQUL;
            Ops[(int)OP.NEQL] = &NEQL;
            Ops[(int)OP.GEQL] = &GEQL;
            Ops[(int)OP.LEQL] = &LEQL;

            Ops[(int)OP.JUMP] = &JUMP;
            Ops[(int)OP.READ] = &READ;
            Ops[(int)OP.WRTE] = &WRTE;
            Ops[(int)OP.ARGW] = &ARGW;
            Ops[(int)OP.ARGR] = &ARGR;
            Ops[(int)OP.RETW] = &RETW;
            Ops[(int)OP.RETR] = &RETR;
            Ops[(int)OP.CALL] = &CALL;
            Ops[(int)OP.EXIT] = &EXIT;

            Ops[(int)OP.COPY] = &COPY;
            Ops[(int)OP.ALOC] = &ALOC;
            Ops[(int)OP.COMP] = &COMP;
            Ops[(int)OP.DELT] = &DELT;
            Ops[(int)OP.FREE] = &FREE;
            Ops[(int)OP.LOAD] = &LOAD;
            Ops[(int)OP.SAVE] = &SAVE;
            Ops[(int)OP.FILL] = &FILL;
            Ops[(int)OP.FIND] = &FIND;

            Ops[(int)OP.RESZ] = &RESZ;
            Ops[(int)OP.SWAP] = &SWAP;


            Ops[(int)OP.SUMM] = &SUMM;
            Ops[(int)OP.AVRG] = &AVRG;
            Ops[(int)OP.MINM] = &MINM;
            Ops[(int)OP.MAXM] = &MAXM;
            Ops[(int)OP.SORT] = &SORT;
            Ops[(int)OP.CONT] = &CONT;

            Ops[(int)OP.LNTH] = &LNTH;

            Ops[(int)OP.EXST] = &EXST;

            Ops[(int)OP.BASE] = &BASE;

            Ops[(int)OP.RNDM] = &RNDM;
            Ops[(int)OP.DONE] = &DONE;

            Ops[(int)OP.MLES] = &MLES;
            Ops[(int)OP.MLOE] = &MLOE;
            Ops[(int)OP.MGOE] = &MGOE;
            Ops[(int)OP.MGRT] = &MGRT;
            Ops[(int)OP.MEQL] = &MEQL;
            Ops[(int)OP.MNQL] = &MNQL;

            Ops[(int)OP.MPLS] = &MPLS;
            Ops[(int)OP.MMNS] = &MMNS;
            Ops[(int)OP.MMLT] = &MMLT;
            Ops[(int)OP.MDIV] = &MDIV;
            Ops[(int)OP.MMDL] = &MMDL;

            Ops[(int)OP.MAND] = &MAND;
            Ops[(int)OP.MORR] = &MORR;
            Ops[(int)OP.MXOR] = &MXOR;
            Ops[(int)OP.MNOT] = &MNOT;
            Ops[(int)OP.MNOR] = &MNOR;
            Ops[(int)OP.MNND] = &MNND;
            Ops[(int)OP.MXNR] = &MXNR;
            Ops[(int)OP.MSHL] = &MSHL;
            Ops[(int)OP.MROL] = &MROL;
            Ops[(int)OP.MSHR] = &MSHR;
            Ops[(int)OP.MROR] = &MROR;
            Ops[(int)OP.MUSR] = &MUSR;

            Ops[(int)OP.MASK] = &MASK;

            Ops[(int)OP.CLON] = &CLON;
            Ops[(int)OP.CHNG] = &CHNG;
            Ops[(int)OP.RVRS] = &RVRS;
            Ops[(int)OP.SHFL] = &SHFL;


        }


        [MethodImpl(MethodImplOptions.NoInlining)]


        // ================== ID 유효성 검사 (공통) ==================
        private static bool IsValidID(int id)
        {
            return (uint)id < MAX_ArrayBlock;
        }

        // ================== 등록 (내부 전용, Alloc/Load가 공유) ==================
        // 예전 CreateArrayBlock의 로직입니다. 외부에서 직접 호출할 이유가 없어서
        // private 헬퍼로 내렸습니다. (다른 곳에서 CreateArrayBlock을 직접 부르고
        // 있었다면 그 호출부는 RegisterBlock으로 바꾸거나 Allocate/Load로 대체해야 해요)
        //   1) 메타데이터배열확인 -> 2) 아이디시도 -> (파일 접근은 여기서 안 함)
        private static bool RegisterBlock(METADATA meta)
        {
            int id = meta.ID;

            if (!IsValidID(id))
                return false;

            if (meta.Length <= 0)
                return false;

            if (meta.Length > MEMORY_SIZE - HeapTop)
            {
                Compact();

                if (meta.Length > MEMORY_SIZE - HeapTop)
                    return false;
            }

            meta.Base = HeapTop;
            meta.Exists = 1;

            Metadatas[id] = meta;
            used_ID[used_count++] = id;

            HeapTop += meta.Length;

            return true;
        }
        public static bool DeleteArrayBlock(int id)
        {
            if (!IsValidID(id)) return false;
            if (Metadatas[id].Exists == 0) return false;
            if (id == CurrentArrayBlock) return false;

            for (int i = 0; i <= FrameTop; i++)
                if (Frames[i].Self == id) return false;

            Metadatas[id] = default;   // 슬롯 완전 초기화 (Length도 0으로) — ALOC이 재사용 가능하게
            RemoveUsedID(id);

            return true;
        }
        static void RemoveUsedID(int id)
        {
            int* used = (int*)Unsafe.AsPointer(ref used_ID[0]);

            for (int i = 0; i < used_count; i++)
            {
                if (used[i] == id)
                {
                    used[i] = used[--used_count];
                    return;
                }
            }
        } // 사용중인 아이디에서 제거합니다.

        // ================== Alloc ==================
        public static int AllocateArrayBlock(int length)
        {
            for (int i = 1; i < MAX_ArrayBlock; i++)   // 0은 Boot 전용 제외
            {
                if (Metadatas[i].Exists == 0 && Metadatas[i].Length == 0)
                {
                    METADATA meta = new METADATA
                    {
                        Magic = METADATA.MAGIC,
                        ID = i,
                        Length = length,
                        Base = 0,
                        Exists = 0,
                    };
                    return RegisterBlock(meta) ? i : 0;
                }
            }
            return 0;
        }// 빈 슬롯을 찾아 새 배열을 등록합니다. 실패하면 0을 반환합니다.

        // ================== Free ==================
        public static bool FreeArrayBlock(int id)
        {
            if (!IsValidID(id)) return false;
            if (Metadatas[id].Exists == 0) return false;
            if (id == CurrentArrayBlock) return false;

            for (int i = 0; i <= FrameTop; i++)
                if (Frames[i].Self == id) return false;

            Metadatas[id].Exists = 0;
            RemoveUsedID(id);
            // Length/ID는 그대로 남김 — LOAD로 다시 불러올 수 있는 여지 유지

            return true;
        } // 배열을 메모리에서 지웁니다.

        // ================== Save ==================
        public static bool SaveArrayBlock(int id)
        {
            // 1) 메타데이터배열확인
            if (!IsValidID(id))
                return false;

            var h = Metadatas[id];

            if (h.Exists == 0)
                return false;

            // 2) 아이디시도 (경로 준비)
            string path = GetPath(id);
            string dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // 3) 실제파일헤더 접근
            using var bw = new BinaryWriter(File.Open(path, FileMode.Create));

            bw.Write(new ReadOnlySpan<byte>(&h, sizeof(METADATA)));

            int* mem = (int*)Unsafe.AsPointer(ref MEMORY[0]);
            int ptr = h.Base;

            for (int i = 0; i < h.Length; i++)
                bw.Write(mem[ptr + i]);

            return true;
        } // 배열을 파일로 씁니다.

        // ================== Load ==================
        public static bool LoadArrayBlock(int id)
        {
            // 1) 메타데이터배열확인
            if (!IsValidID(id))
                return false;

            // 2) 아이디시도 (경로만 확보, 아직 파일 내용은 안 읽음)
            string path = GetPath(id);

            if (!File.Exists(path))
                return false;

            try
            {
                // 3) 실제파일헤더 접근
                using var br = new BinaryReader(File.OpenRead(path));

                byte[] headerBytes = br.ReadBytes(sizeof(METADATA));
                METADATA diskHeader;

                fixed (byte* p = headerBytes)
                {
                    diskHeader = *(METADATA*)p;
                }

                if (diskHeader.ID != id)
                    return false;

                if (diskHeader.Length <= 0)
                    return false;

                if (diskHeader.Length > MEMORY_SIZE)
                    return false;

                if (diskHeader.Magic != METADATA.MAGIC)
                    return false;

                if (!RegisterBlock(diskHeader))
                    return false;

                int baseAddr = Metadatas[id].Base;
                int* mem = (int*)Unsafe.AsPointer(ref MEMORY[0]);

                for (int i = 0; i < diskHeader.Length; i++)
                    mem[baseAddr + i] = br.ReadInt32();

                var runtimeHeader = diskHeader;
                runtimeHeader.Base = baseAddr;
                runtimeHeader.Exists = 1;

                Metadatas[id] = runtimeHeader;

                return true;
            }
            catch
            {
                return false;
            }
        } // 헤더를 읽어 메타데이터를 등록하고 배열을 메모리에 올립니다.

        // ================== 압축 ==================
        public static void Compact()
        {
            State = VMState.Compacting;
            int newTop = 1;

            Array.Clear(Compaction_MEMORY, 0, Compaction_MEMORY.Length);

            for (int i = 0; i < used_count; i++)
            {
                int id = used_ID[i];

                if (Metadatas[id].Exists == 0)
                    continue;

                var h = Metadatas[id];

                Array.Copy(MEMORY, h.Base, Compaction_MEMORY, newTop, h.Length);

                h.Base = newTop;
                Metadatas[id] = h;

                newTop += h.Length;
            }

            (MEMORY, Compaction_MEMORY) = (Compaction_MEMORY, MEMORY);

            HeapTop = newTop;
            State = VMState.Running;
        }

        // ================== 경로 ==================
        public static string GetPath(int id)
        {
            return GetArrayBlockFile(id);
        }

        public static string GetProjectFile(string name)
        {
            return Path.Combine(ProjectPath, name);
        }

        public static string GetArrayBlockFile(int id)
        {
            return Path.Combine(ProjectPath, $"{id}.gwl");
        }

        // ================== 부팅 ==================
        public static bool Boot(string projectPath)
        {
            Frames[0] = default;
            Frames[0].Self = 0;
            ProjectPath = Path.GetFullPath(projectPath);

            if (!Directory.Exists(ProjectPath))
                Directory.CreateDirectory(ProjectPath);

            HeapTop = 1;
            used_count = 0;

            Array.Clear(Metadatas, 0, Metadatas.Length);
            Array.Clear(MEMORY, 0, MEMORY.Length);
            Array.Clear(Frames, 0, Frames.Length);

            CurrentArrayBlock = 0;
            PC = 0;
            FrameTop = 0;

            if (!LoadArrayBlock(0))
            {

                return false;
            }

            State = VMState.Running;
            return true;
        }

        public static void Step(int count)
        {
            State = VMState.Running;


            for (int i = 0; i < count; i++)
            {

                dip();
                if (EndRun)
                    break;
            }

        }
        public static void dip()
        {



            fixed (int* mem = MEMORY)
            fixed (FRAME* frames = Frames)
            {

                if (State != VMState.Running) return;

                if (EndRun)
                {
                    FrameTop = 0;
                    CurrentArrayBlock = 1;
                    PC = 0;
                    EndRun = false;
                    Frames[0] = default;      // 추가: 프레임0 레지스터도 초기화
                    Frames[0].Self = 1;
                }

                FRAME* frame = frames + FrameTop;

                var h = Metadatas[CurrentArrayBlock];

                int instructionCount = h.Length >> 2;

                if ((uint)PC >= (uint)instructionCount)
                {

                    return;
                }

                int* ip = mem + h.Base + (PC << 2);

                int op = ip[0];

                if ((uint)op >= (uint)Ops.Length || Ops[op] == null)
                    return;



                PC++;

                Ops[op](ip, frame);

            }
        }

        static void NOPE(int* ip, FRAME* frame)
        { }
        static void DEFN(int* ip, FRAME* frame)
        {
            frame->Registers[ip[1]] =
                ip[2];
        }
        static void MOVE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]];
        }
        static void PLUS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] +
                reg[ip[3]];

        }
        static void MNUS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] -
                reg[ip[3]];



        }
        static void MULT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] *
                reg[ip[3]];



        }
        static void DIVD(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            // [수정 5] 0으로 나누면 DivideByZeroException이 그대로 터져
            // try/catch까지 올라가 "Run Exception" 크래시를 유발하므로 방어.
            if (reg[ip[3]] == 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            reg[ip[1]] =
                reg[ip[2]] /
                reg[ip[3]];



        }
        static void MODL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            // [수정 6] DIV와 동일한 이유로 0 나누기 방어
            if (reg[ip[3]] == 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            reg[ip[1]] =
                reg[ip[2]] %
                reg[ip[3]];



        }
        static void BAND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] &
                reg[ip[3]];



        }
        static void BORR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] |
                reg[ip[3]];



        }
        static void BXOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] ^
                reg[ip[3]];


        }
        static void BNOT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            reg[ip[1]] =
            reg[ip[2]];



        }
        static void BNND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] &
                  reg[ip[3]]);

            //($"NAND : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) & R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BNOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] |
                  reg[ip[3]]);
            //($"NOR : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) | R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BXNR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] ^
                  reg[ip[3]]);
            //($"XNOR : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) ^ R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BITR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                (reg[ip[2]] >> reg[ip[3]]) & 1;

            //($"BITR : R[{ip[1]}]({reg[ip[1]]}) = ( R[{ip[2]}]({reg[ip[1]]}) >> R[{ip[3]}]({reg[ip[1]]}) ) & 1 ");
        }
        static void BITW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int idx = reg[ip[3]] & 31;
            int bit = reg[ip[5]]; // EXTRA 슬롯의 A

            if (bit != 0)
            {
                reg[ip[1]] = value | (1 << idx);
                //($"BITW : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) | (1 << (R[{ip[3]}]({reg[ip[1]]})  & 31)) ");

            }
            else
            {
                reg[ip[1]] = value & ~(1 << idx);
                //($"BITW : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) & ~ (1 << (R[{ip[3]}]({reg[ip[1]]})  & 31)) ");

            }
        }
        static void BFLR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int offset = reg[ip[3]];
            int width = reg[ip[5]];

            uint mask = width >= 32 ? 0xFFFFFFFF : (1u << width) - 1;
            reg[ip[1]] = (int)(((uint)value >> offset) & mask);


        }
        static void BFLW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int insertVal = reg[ip[6]];
            int offset = reg[ip[3]];
            int width = reg[ip[5]];

            uint mask = width >= 32 ? 0xFFFFFFFF : (1u << width) - 1;
            uint cleared = (uint)value & ~(mask << offset);
            uint inserted = ((uint)insertVal & mask) << offset;

            reg[ip[1]] = (int)(cleared | inserted);

            //($"BFLW : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]})[(R[{ip[3]}]({reg[ip[1]]})+...+R[{ip[5]}])({reg[ip[1]]})] = R[{ip[6]}]({reg[ip[1]]}))");
        }
        static void BSHL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]]
                << (reg[ip[3]] & 31);

            //($" Shift(L) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) << R[{ip[3]}]({reg[ip[1]]})");
        }
        static void BROL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int value = reg[ip[2]];
            int shift = reg[ip[3]] & 31;
            reg[ip[1]] = (value << shift) | (int)((uint)value >> (32 - shift));

            //($" Rotate(L) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) <@ R[{ip[3]}]({reg[ip[1]]})");
        }
        static void BROR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int value = reg[ip[2]];
            int shift = reg[ip[3]] & 31;
            reg[ip[1]] = (value >> shift) | (int)((uint)value << (32 - shift));


            //($" Rotate(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) @> R[{ip[3]}]({reg[ip[1]]})");
        }
        static void BSHR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]]
                >> (reg[ip[3]] & 31);

            //($" Shift(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) >> R[{ip[3]}]({reg[ip[1]]})");

        }
        static void BUSR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                (int)(
                    (uint)reg[ip[2]]
                    >> (reg[ip[3]] & 31));

            //($" C-Shift(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) >>> R[{ip[3]}]({reg[ip[1]]})");

        }
        static void GRET(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] >
                reg[ip[3]]
                ? 1 : 0;

            //($" Great : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) > R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void LESS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] <
                reg[ip[3]]
                ? 1 : 0;
            //($" Less : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) < R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void EQUL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] ==
                reg[ip[3]]
                ? 1 : 0;
            //($" Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) == R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void NEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] !=
                reg[ip[3]]
                ? 1 : 0;

            //($" Not_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) != R[{ip[3]}]({reg[ip[1]]}))");

        }
        static void GEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] >=
                reg[ip[3]]
                ? 1 : 0;

            //($" Great_or_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) >= R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void LEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] <=
                reg[ip[3]]
                ? 1 : 0;

            //($" Less_or_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) <= R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void JUMP(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] = 0;

            if (reg[ip[2]] != 0)
            {
                PC = ip[3];
                reg[ip[1]] = 1;
            }

        }
        static void READ(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int ArrayBlockId = reg[ip[2]];


            reg[ip[1]] = 0;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock || Metadatas[ArrayBlockId].Exists == 0)
                return;

            var h = Metadatas[ArrayBlockId];
            int addr = reg[ip[3]];

            if ((uint)addr >= (uint)h.Length)
                return;

            reg[ip[1]] = MEMORY[h.Base + addr];

        }
        static void WRTE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int ArrayBlockid = reg[ip[2]];
            int addr = reg[ip[3]];
            int value = reg[ip[5]]; // EXTRA 슬롯의 A

            reg[ip[1]] = 0;

            if ((uint)ArrayBlockid >= MAX_ArrayBlock || Metadatas[ArrayBlockid].Exists == 0)
                return;

            var h = Metadatas[ArrayBlockid];


            if ((uint)addr >= (uint)h.Length)
                return;



            int oldValue = MEMORY[h.Base + addr];
            MEMORY[h.Base + addr] = value;

            if (oldValue != value)

                reg[ip[1]] = 1;
        }
        static void ARGW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= Args_COUNT)
            {
                reg[ip[1]] = 0;
                return;
            }

            frame->ARGS[idx] =
                frame->Registers[ip[3]];



        }
        static void ARGR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= Args_COUNT)
            {

                return;
            }

            frame->Registers[ip[1]] =
                frame->ARGS[idx];


        }
        static void RETW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];
            reg[ip[1]] = 1;
            if ((uint)idx >= rtns_COUNT)
            {

                return;
            }

            frame->RTNS[idx] =
                frame->Registers[ip[3]];
            reg[ip[1]] = 0;
            //($"[RETW] : RETURNS[{ip[1]}]({frame->RTNS[idx]}) = R[{ip[2]}]({reg[ip[1]]})");
        }
        static void RETR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= rtns_COUNT)
            {

                return;
            }

            frame->Registers[ip[1]] =
                frame->RTNS[idx];

        }
        static void CALL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int target = reg[ip[2]];

            reg[ip[1]] = 0;

            if ((uint)target >= MAX_ArrayBlock || Metadatas[target].Exists == 0)
                return;

            if (FrameTop + 1 >= MAX_FRAME)
                return;




            FrameTop++;

            fixed (FRAME* pFrames = Frames)
            {
                FRAME* next = pFrames + FrameTop;
                *next = default;
                next->PrevArrayBlock = CurrentArrayBlock;
                next->PrevPC = PC;
                next->Self = target;

                for (int i = 0; i < Args_COUNT; i++)
                    next->ARGS[i] = frame->ARGS[i];
            }

            reg[ip[1]] = 1; // 호출부(이전 프레임) 레지스터에 성공 기록 — 컨텍스트 전환 전에 이미 계산됨

            CurrentArrayBlock = target;
            PC = 0;
        }
        static void EXIT(int* ip, FRAME* frame)
        {
            if (FrameTop <= 0)
                return; // 크래시 대신 조용히 무시

            fixed (FRAME* pFrames = Frames)
            {
                FRAME* current = pFrames + FrameTop;
                FRAME* prev = pFrames + (FrameTop - 1);

                for (int i = 0; i < rtns_COUNT; i++)
                    prev->RTNS[i] = current->RTNS[i];

                CurrentArrayBlock = current->PrevArrayBlock;
                PC = current->PrevPC;


            }

            FrameTop--;
        }
        static void DONE(int* ip, FRAME* frame)
        {
            EndRun = true;
        }
        static void RESZ(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id =
                reg[ip[2]];

            int newLength =
                reg[ip[3]];

            reg[ip[1]] = 0;

            if ((uint)id >= MAX_ArrayBlock)
            {
                return;
            }



            if (newLength <= 0)
            {
                return;
            }

            if (Metadatas[id].Exists == 0)
            {
                return;
            }

            var oldHeader =
                Metadatas[id];

            int oldBase =
                oldHeader.Base;

            int oldLength =
                oldHeader.Length;

            // =====================================
            // 새 메모리 확보
            // =====================================

            if (newLength > MEMORY_SIZE - HeapTop)
            {
                Compact();

                if (newLength > MEMORY_SIZE - HeapTop)
                {
                    return;
                }
            }

            int newBase =
                HeapTop;

            HeapTop += newLength;

            // =====================================
            // 데이터 복사
            // =====================================

            int copyLength =
                oldLength < newLength
                ? oldLength
                : newLength;

            Array.Copy(
                MEMORY,
                oldBase,
                MEMORY,
                newBase,
                copyLength);

            // =====================================
            // 추가 영역 초기화
            // =====================================

            for (int i = copyLength;
                 i < newLength;
                 i++)
            {
                MEMORY[newBase + i] = 0;
            }

            // =====================================
            // 헤더 갱신
            // =====================================

            oldHeader.Base =
                newBase;

            oldHeader.Length =
                newLength;

            Metadatas[id] =
                oldHeader;

            frame->Registers[ip[1]] = 1;

        }
        static void ALOC(int* ip, FRAME* frame)
        {

            int* reg = frame->Registers;
            int len =
                reg[ip[2]];


            frame->Registers[ip[1]] =
        AllocateArrayBlock(len);




        }
        static void FREE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = FreeArrayBlock(
                reg[ip[2]]); int id =
    reg[ip[2]];


            frame->Registers[ip[1]] =
                ok ? 1 : 0;
        }
        static void LOAD(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id = reg[ip[2]];

            bool ok = LoadArrayBlock(id);

            frame->Registers[ip[1]] =
                ok ? 1 : 0;
        }
        static void SAVE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id = reg[ip[2]];
            bool ok = SaveArrayBlock(id
                );

            frame->Registers[ip[1]] =
                ok ? 1 : 0;

        }
        static void COPY(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;


            int dstId =
                reg[ip[2]];

            int dstStart =
                reg[ip[3]];


            int srcId =
                reg[ip[5]];

            int srcStart =
                reg[ip[6]];


            int length =
                reg[ip[7]];



            reg[ip[1]] = 0;



            if (srcId < 0 || dstId < 0)
                return;


            if ((uint)srcId >= MAX_ArrayBlock ||
                (uint)dstId >= MAX_ArrayBlock)
                return;



            var src = Metadatas[srcId];
            var dst = Metadatas[dstId];



            if (src.Exists == 0 ||
                dst.Exists == 0)
                return;


            if (length <= 0)
                return;



            if (srcStart < 0 ||
                srcStart + length > src.Length)
                return;


            if (dstStart < 0 ||
                dstStart + length > dst.Length)
                return;



            int srcBase =
                src.Base + srcStart;

            int dstBase =
                dst.Base + dstStart;



            // MEMORY 내부 겹침 검사

            bool overlap =
                srcBase < dstBase + length &&
                dstBase < srcBase + length;



            if (overlap)
            {
                // 뒤에서부터 복사

                for (int i = length - 1; i >= 0; i--)
                {
                    MEMORY[dstBase + i] =
                        MEMORY[srcBase + i];
                }
            }
            else
            {
                // 일반 순차 복사

                for (int i = 0; i < length; i++)
                {
                    MEMORY[dstBase + i] =
                        MEMORY[srcBase + i];
                }
            }



            reg[ip[1]] = 1;


        }
        static void SWAP(int* ip, FRAME* frame)
        {


            int* reg =
                frame->Registers;

            int srcId =
                reg[ip[5]];

            int srcStart =
                reg[ip[6]];

            int dstId =
                reg[ip[2]];

            int dstStart =
                reg[ip[3]];

            reg[ip[1]] = 0;



            if ((uint)srcId >= MAX_ArrayBlock)
                return;

            if ((uint)dstId >= MAX_ArrayBlock)
                return;
            if (Metadatas[srcId].Exists == 0)
                return;

            if (Metadatas[dstId].Exists == 0)
                return;


            if (srcId < 0 ||
                dstId < 0)
                return;

            var src =
                Metadatas[srcId];

            var dst =
                Metadatas[dstId];

            int temp = MEMORY[src.Base + srcStart];
            MEMORY[src.Base + srcStart] = MEMORY[dst.Base + dstStart];
            MEMORY[dst.Base + dstStart] = temp;

            frame->Registers[ip[1]] = 1;
        }
        static void LNTH(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;



            if (Metadatas[id].Exists == 0)
                return;


            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Length;

        }

        // OP enum에 COMP 추가 (DONE 앞이나 뒤에)
        static void COMP(int* ip, FRAME* frame)
        {
            Compact();
            frame->Registers[ip[1]] = 1;   // 결과: 항상 성공
        }

        static void DELT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = DeleteArrayBlock(reg[ip[2]]);
            reg[ip[1]] = ok ? 1 : 0;
        }
        static void EXST(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;

            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Exists;

        }

        static void BASE(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;



            if (Metadatas[id].Exists == 0)
                return;


            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Base;

        }
        static void FILL(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];


            reg[ip[1]] = 0;

            if (ArrayBlockId < 0)
                return;


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;



            if (Metadatas[ArrayBlockId].Exists == 0)
                return;



            var meta =
                Metadatas[ArrayBlockId];

            if (start < 0 ||
                length + start > meta.Length ||
                0 >= length)
                return;


            int baseAddr =
                meta.Base;

            for (int i = start; i < start + length; i++)
            {
                MEMORY[baseAddr + i] =
                    value;
            }

            reg[ip[1]] = 1;
        }
        static void SUMM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            reg[ip[1]] = 0;

            long sum = 0;
            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            for (int i = start; i < start + length; i++)
            {
                sum +=
                    MEMORY[meta.Base + i];
            }

            reg[ip[1]] =
                (int)sum;
        }
        static void AVRG(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];


            reg[ip[1]] = 0;

            if (length <= 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            long sum = 0;
            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            for (int i = start; i < start + length; i++)
            {
                sum +=
                    MEMORY[meta.Base + i];
            }

            reg[ip[1]] =
                (int)(sum / length);
        }
        static void MINM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            reg[ip[1]] = 0;

            if (start >= start + length)
                return;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            int min =
                MEMORY[meta.Base + start];

            for (int i = start + 1; i < start + length; i++)
            {
                int v =
                    MEMORY[meta.Base + i];

                if (v < min)
                    min = v;
            }

            reg[ip[1]] =
                min;
        }
        static void MAXM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];


            reg[ip[1]] = 0;

            if (start >= start + length)
                return;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            int max =
                MEMORY[meta.Base + start];

            for (int i = start + 1; i < start + length; i++)
            {
                int v =
                    MEMORY[meta.Base + i];

                if (v > max)
                    max = v;
            }

            reg[ip[1]] =
                max;
        }
        static void FIND(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];


            reg[ip[1]] =
                0;


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;

            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;

            for (int i = start; i < start + length; i++)
            {
                if (MEMORY[meta.Base + i] == value)
                {
                    reg[ip[1]] = i;
                    return;
                }
            }
        }
        static void CONT(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];

            reg[ip[1]] = 0;

            int count = 0;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;


            for (int i = start; i < start + length; i++)
            {
                if (MEMORY[meta.Base + i] == value)
                    count++;
            }

            reg[ip[1]] =
                count;
        }

        // ================== 맵형 비교 공통 로직 ==================
        // mode: 0=< 1=<= 2=>= 3=> 4=== 5=!=
        private static bool CompareValue(int mode, int value, int val)
        {
            switch (mode)
            {
                case 0: return value < val;
                case 1: return value <= val;
                case 2: return value >= val;
                case 3: return value > val;
                case 4: return value == val;
                default: return value != val;
            }
        }

        // [MLES 등](결과, 배열ID, 시작, 길이, 값) 공통 구현.
        // 구간의 각 원소를 값과 비교해서 참이면 1, 거짓이면 0을 그 자리에 덮어씀.
        // MPLS 등과 동일한 "맵" 철학 — 배열 크기는 그대로.
        private static bool CompareWriteRange(int id, int start, int length, int val, int mode)
        {
            if (!IsValidID(id))
                return false;

            if (Metadatas[id].Exists == 0)
                return false;

            var meta = Metadatas[id];

            if (start < 0 || length <= 0 || start + length > meta.Length)
                return false;

            int baseAddr = meta.Base;

            for (int i = start; i < start + length; i++)
            {
                MEMORY[baseAddr + i] = CompareValue(mode, MEMORY[baseAddr + i], val) ? 1 : 0;
            }

            return true;
        }

        // [MPLS 등](결과, 배열ID, 시작, 길이, 값) 공통 구현. 구간 전체에 산술연산 적용.
        // mode: 0=+ 1=- 2=* 3=/ 4=%
        private static bool MapRange(int id, int start, int length, int val, int mode)
        {
            if (!IsValidID(id))
                return false;

            if (Metadatas[id].Exists == 0)
                return false;

            var meta = Metadatas[id];

            if (start < 0 || length <= 0 || start + length > meta.Length)
                return false;

            // MDIV/MMDL 0 나누기 방어 (DIVD/MODL과 동일한 이유)
            if ((mode == 3 || mode == 4) && val == 0)
                return false;

            int baseAddr = meta.Base;

            for (int i = start; i < start + length; i++)
            {
                int v = MEMORY[baseAddr + i];

                switch (mode)
                {
                    case 0: v = v + val; break;
                    case 1: v = v - val; break;
                    case 2: v = v * val; break;
                    case 3: v = v / val; break;
                    default: v = v % val; break;
                }

                MEMORY[baseAddr + i] = v;
            }

            return true;
        }

        static void MLES(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 0);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간의 각 원소가 값 미만이면 그 자리에 1, 아니면 0
        static void MLOE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 1);
            reg[ip[1]] = ok ? 1 : 0;
        } // 값 이하
        static void MGOE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 2);
            reg[ip[1]] = ok ? 1 : 0;
        } // 값 이상
        static void MGRT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 3);
            reg[ip[1]] = ok ? 1 : 0;
        } // 값 초과
        static void MEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 4);
            reg[ip[1]] = ok ? 1 : 0;
        } // 값과 같음
        static void MNQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = CompareWriteRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 5);
            reg[ip[1]] = ok ? 1 : 0;
        } // 값과 다름

        static void MPLS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = MapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 0);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간에 값을 더함
        static void MMNS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = MapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 1);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간에서 값을 뺌
        static void MMLT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = MapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 2);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간에 값을 곱함
        static void MDIV(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = MapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 3);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간을 값으로 나눔
        static void MMDL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = MapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 4);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간을 값으로 나눈 나머지

        // ================== 비트 맵 공통 로직 ==================
        // B계열(BAND 등, 단 BFLR/BFLW/BITR/BITW 제외)의 배열버전.
        // mode: 0=AND 1=OR 2=XOR 3=NOT(val 무시) 4=NOR 5=NAND 6=XNOR 7=SHL 8=ROL 9=SHR 10=ROR 11=USR
        private static int BitOp(int mode, int value, int val)
        {
            int shift = val & 31;

            switch (mode)
            {
                case 0: return value & val;                                            // MAND
                case 1: return value | val;                                            // MORR
                case 2: return value ^ val;                                            // MXOR
                case 3: return ~value;                                                 // MNOT
                case 4: return ~(value | val);                                         // MNOR
                case 5: return ~(value & val);                                         // MNND
                case 6: return ~(value ^ val);                                         // MXNR
                case 7: return value << shift;                                         // MSHL
                case 8: return (value << shift) | (int)((uint)value >> (32 - shift));  // MROL
                case 9: return value >> shift;                                         // MSHR (부호 유지)
                case 10: return (value >> shift) | (int)((uint)value << (32 - shift)); // MROR
                default: return (int)((uint)value >> shift);                           // MUSR
            }
        }

        // [MAND 등](결과, 배열ID, 시작, 길이, 값) 공통 구현. 구간 전체에 비트연산 적용.
        private static bool BitMapRange(int id, int start, int length, int val, int mode)
        {
            if (!IsValidID(id))
                return false;

            if (Metadatas[id].Exists == 0)
                return false;

            var meta = Metadatas[id];

            if (start < 0 || length <= 0 || start + length > meta.Length)
                return false;

            int baseAddr = meta.Base;

            for (int i = start; i < start + length; i++)
            {
                MEMORY[baseAddr + i] = BitOp(mode, MEMORY[baseAddr + i], val);
            }

            return true;
        }

        static void MAND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 0);
            reg[ip[1]] = ok ? 1 : 0;
        } // 구간 전체에 값을 비트 AND
        static void MORR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 1);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 OR
        static void MXOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 2);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 XOR
        static void MNOT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
      
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], 0, 3);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 not
        static void MNOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 4);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 NOR
        static void MNND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 5);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 NAND
        static void MXNR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 6);
            reg[ip[1]] = ok ? 1 : 0;
        } // 비트 XNOR
        static void MSHL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 7);
            reg[ip[1]] = ok ? 1 : 0;
        } // 왼쪽 시프트
        static void MROL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 8);
            reg[ip[1]] = ok ? 1 : 0;
        } // 왼쪽 회전
        static void MSHR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 9);
            reg[ip[1]] = ok ? 1 : 0;
        } // 오른쪽 시프트 (부호 유지)
        static void MROR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 10);
            reg[ip[1]] = ok ? 1 : 0;
        } // 오른쪽 회전
        static void MUSR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = BitMapRange(reg[ip[2]], reg[ip[3]], reg[ip[5]], reg[ip[6]], 11);
            reg[ip[1]] = ok ? 1 : 0;
        } // 오른쪽 시프트 (부호 없음)

        // [MASK](결과, 대상배열ID, 대상시작, 마스크배열ID, 마스크시작, 길이)
        // 마스크 배열의 [마스크시작, 마스크시작+길이) 구간을 훑어서 0이 아닌 위치만 남기고,
        // 대상 배열의 [대상시작, 대상시작+길이) 구간을 그만큼 패킹+축소한다.
        static void MASK(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int maskId = reg[ip[5]];
            int maskStart = reg[ip[6]];
            int targetId = reg[ip[2]];
            int targetStart = reg[ip[3]];
            int length = reg[ip[7]];

            reg[ip[1]] = 0;

            if (!IsValidID(maskId) || !IsValidID(targetId))
                return;

            if (Metadatas[maskId].Exists == 0 || Metadatas[targetId].Exists == 0)
                return;

            var maskMeta = Metadatas[maskId];
            var meta = Metadatas[targetId];

            if (length <= 0)
                return;

            if (maskStart < 0 || maskStart + length > maskMeta.Length)
                return;

            if (targetStart < 0 || targetStart + length > meta.Length)
                return;

            int maskBase = maskMeta.Base;
            int baseAddr = meta.Base;

            // 1) 남길 개수 미리 계산 (읽기 전용 — 실패시 대상 배열 훼손 없음)
            int keepCount = 0;

            for (int i = 0; i < length; i++)
            {
                if (MEMORY[maskBase + maskStart + i] != 0)
                    keepCount++;
            }

            int removed = length - keepCount;
            int newLength = meta.Length - removed;

            if (newLength <= 0)
                return;

            // 2) 대상 구간 안에서 마스크가 0이 아닌 값만 앞으로 패킹
            //    (maskId == targetId 이고 구간이 겹치면 패킹 도중 마스크 값이 먼저
            //     덮여쓰일 수 있으니, 그런 조합으로는 쓰지 않는 걸 권장)
            int writePos = targetStart;

            for (int i = 0; i < length; i++)
            {
                int v = MEMORY[baseAddr + targetStart + i];
                int m = MEMORY[maskBase + maskStart + i];

                if (m != 0)
                {
                    MEMORY[baseAddr + writePos] = v;
                    writePos++;
                }
            }

            // 3) 대상 구간 뒤에 남은 부분을 당겨서 빈 칸 메움
            int tailStart = targetStart + length;
            int tailLength = meta.Length - tailStart;

            if (tailLength > 0)
            {
                Array.Copy(MEMORY, baseAddr + tailStart, MEMORY, baseAddr + writePos, tailLength);
            }

            meta.Length = newLength;
            Metadatas[targetId] = meta;

            reg[ip[1]] = 1;
        }

        static void CLON(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int srcId = reg[ip[2]];

            reg[ip[1]] = 0;

            if (!IsValidID(srcId))
                return;

            if (Metadatas[srcId].Exists == 0)
                return;

            var src = Metadatas[srcId];
            int newId = AllocateArrayBlock(src.Length);

            if (newId == 0)
                return;

            // AllocateArrayBlock 내부에서 Compact()가 걸렸을 수 있어 src.Base가
            // 이동했을 수 있으므로 복사 직전에 메타데이터를 다시 읽는다.
            src = Metadatas[srcId];
            var dst = Metadatas[newId];

            Array.Copy(MEMORY, src.Base, MEMORY, dst.Base, src.Length);

            reg[ip[1]] = newId;
        } // 배열을 통째로 복제하고 새 배열ID를 반환

        static void CHNG(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int id1 = reg[ip[2]];
            int id2 = reg[ip[3]];

            reg[ip[1]] = 0;

            if (!IsValidID(id1) || !IsValidID(id2))
                return;

            if (Metadatas[id1].Exists == 0 || Metadatas[id2].Exists == 0)
                return;

            // FREE와 동일한 이유로 현재 실행 중이거나 콜스택에 걸린 배열은 교환 금지
            if (id1 == CurrentArrayBlock || id2 == CurrentArrayBlock)
                return;

            for (int i = 0; i <= FrameTop; i++)
                if (Frames[i].Self == id1 || Frames[i].Self == id2)
                    return;

            var m1 = Metadatas[id1];
            var m2 = Metadatas[id2];

            m1.ID = id2;
            m2.ID = id1;

            Metadatas[id1] = m2;
            Metadatas[id2] = m1;

            reg[ip[1]] = 1;
        } // 두 배열ID가 가리키는 내용(Base/Length 등)을 맞바꿈 (데이터 복사 없이 O(1))

        static void RVRS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int id = reg[ip[2]];

            reg[ip[1]] = 0;

            if (!IsValidID(id))
                return;

            if (Metadatas[id].Exists == 0)
                return;

            var meta = Metadatas[id];
            int baseAddr = meta.Base;
            int len = meta.Length;

            for (int i = 0, j = len - 1; i < j; i++, j--)
            {
                int tmp = MEMORY[baseAddr + i];
                MEMORY[baseAddr + i] = MEMORY[baseAddr + j];
                MEMORY[baseAddr + j] = tmp;
            }

            reg[ip[1]] = 1;
        } // 배열 전체를 반전

        static void SHFL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int id = reg[ip[2]];

            reg[ip[1]] = 0;

            if (!IsValidID(id))
                return;

            if (Metadatas[id].Exists == 0)
                return;

            var meta = Metadatas[id];
            int baseAddr = meta.Base;
            int len = meta.Length;

            for (int i = len - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);

                int tmp = MEMORY[baseAddr + i];
                MEMORY[baseAddr + i] = MEMORY[baseAddr + j];
                MEMORY[baseAddr + j] = tmp;
            }

            reg[ip[1]] = 1;
        } // 배열 전체를 무작위로 섞음 (Fisher-Yates)

        static void RNDM(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int a = reg[ip[2]];
            int b = reg[ip[3]];
            if (a == b)
            {
                reg[ip[1]] = a;
            }
            else if (a > b)
            {
                reg[ip[1]] = RandomNumberGenerator.GetInt32(b, a);
            }
            else
            {
                reg[ip[1]] = RandomNumberGenerator.GetInt32(a, b);
            }
        }
        static void SORT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            // ip[1] = 결과(성공/실패) 레지스터
            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            // ip[5] = EXTRA 슬롯 A필드
            int length =
                reg[ip[5]];


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;


            var meta =
                Metadatas[ArrayBlockId];

            if (start < 0 ||
                length <= 0 ||
                start + length > meta.Length)
                return;


            int baseAddr =
                meta.Base;


            for (int i = start; i < start + length - 1; i++)
            {
                int pick = i;

                for (int j = i + 1; j < start + length; j++)
                {
                    if (MEMORY[baseAddr + j] < MEMORY[baseAddr + pick])
                    {
                        pick = j;
                    }
                }

                if (pick == i)
                    continue;


                int temp =
                    MEMORY[baseAddr + i];

                MEMORY[baseAddr + i] =
                    MEMORY[baseAddr + pick];

                MEMORY[baseAddr + pick] =
                    temp;
            }


            frame->Registers[ip[1]] = 1;
        }

    }
}
